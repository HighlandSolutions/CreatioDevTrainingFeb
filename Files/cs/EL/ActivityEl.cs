using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terrasoft.Core;

using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;
using Terrasoft.Core.Entities.Events;

namespace DevTraining.Files.cs.EL
{
    [EntityEventListener(SchemaName = "Activity")]
    class ActivityEl : BaseEntityEventListener
    {

        private static readonly ILog _log = LogManager.GetLogger("DevTrainingLogger");
        private static readonly Guid activityType = Guid.Parse("FBE0ACDC-CFC0-DF11-B00F-001D60E938C6");//Task
        private static readonly Guid activityCategory = Guid.Parse("01EADF60-9EC9-4BDF-A685-F3CD4248948B"); //Out of the office
        private static readonly Guid activityStatusNotStarted = Guid.Parse("384D4B84-58E6-DF11-971B-001D60E938C6"); // Not Started Status
        private static readonly Guid activityStatusInProgress = Guid.Parse("394D4B84-58E6-DF11-971B-001D60E938C6"); // INProgress status
        

        public override void OnSaving(object sender, EntityBeforeEventArgs e) {
            base.OnSaving(sender, e);

            Entity entity = (Entity)sender;
            if (CountOverlappingActivities(entity) !=0)
                e.IsCanceled = true;
        }

        public static int CountOverlappingActivities(Entity activity) {
            int result = 0;
            UserConnection userConnection = activity.UserConnection;

            if (activity.GetTypedColumnValue<Guid>("TypeId") != activityType)
                return result;
            
            if (activity.GetTypedColumnValue<Guid>("ActivityCategoryId") != activityCategory)
                return result;

            if (activity.GetTypedColumnValue<Guid>("StatusId") != activityStatusInProgress &&
                activity.GetTypedColumnValue<Guid>("StatusId") != activityStatusNotStarted)
                return result;


            Guid owner = activity.GetTypedColumnValue<Guid>("OwnerId");
            Guid activityId = activity.GetTypedColumnValue<Guid>("Id");

            Select select = new Select(userConnection)
                .Column(Func.Count("Id"))
                .From("Activity")
                .Where("TypeId").IsEqual(Column.Parameter(activityType)) 
                    .And("ActivityCategoryId").IsEqual(Column.Parameter(activityCategory))
                    .And("Id").IsNotEqual(Column.Parameter(activityId))
                as Select;
            
            
            TimeZoneInfo userTimeZoneInfo = userConnection.CurrentUser.TimeZone;
            DateTime start = TimeZoneInfo.ConvertTimeToUtc(activity.GetTypedColumnValue<DateTime>("StartDate"), userTimeZoneInfo);
            DateTime due =  TimeZoneInfo.ConvertTimeToUtc(activity.GetTypedColumnValue<DateTime>("DueDate"), userTimeZoneInfo);


            select.And()
                //Case 1: Starts during new activity
                .OpenBlock("StartDate").IsBetween(Column.Parameter(start)).And(Column.Parameter(due))

                //Case2: Ends during new Activity
                .Or("DueDate").IsBetween(Column.Parameter(start)).And(Column.Parameter(due))


                .Or()
                    .OpenBlock("StartDate").IsLessOrEqual(Column.Parameter(start))
                    .And("DueDate").IsGreaterOrEqual(Column.Parameter(due))
                .CloseBlock();

            select.BuildParametersAsValue = true;
            _log.Info(select.GetSqlText());

            using (DBExecutor executor = userConnection.EnsureDBConnection()) 
            {
                result = select.ExecuteScalar<int>(executor);

                if (result != 0) {
                    MsgChannelUtilities.PostMessageToAll("ActivityEl.OnSaving", "Overlapping Activities Detected");
                }
            }
            return result;
        }


        /// <summary>
        /// Example using Uses Entity Schema Query
        /// </summary>
        /// <param name="activity">Activity Entity</param>
        /// <returns>Return number of records found</returns>
        /// <remarks> <see cref="https://academy.creatio.com/documents/technic-sdk/7-15/introduction-13" /></remarks>
        private static int CountOverlappingActivityEsq(Entity activity)
        {
            UserConnection userConnection = activity.UserConnection;
            TimeZoneInfo userTimeZonInfo = userConnection.CurrentUser.TimeZone;

            DateTime start = TimeZoneInfo.ConvertTimeToUtc(activity.GetTypedColumnValue<DateTime>("StartDate").AddSeconds(1), userTimeZonInfo);
            DateTime due = TimeZoneInfo.ConvertTimeToUtc(activity.GetTypedColumnValue<DateTime>("DueDate").AddSeconds(-1), userTimeZonInfo);
            Guid ownerId = activity.GetTypedColumnValue<Guid>("OwnerId");

            EntitySchemaQuery esqResult = new EntitySchemaQuery(userConnection.EntitySchemaManager, "Activity");
            esqResult.AddColumn("Title");
            esqResult.AddColumn("StartDate");
            esqResult.AddColumn("DueDate");
            esqResult.AddColumn("Type");
            esqResult.AddColumn("ActivityCategory");
            esqResult.AddColumn("Owner");
            esqResult.AddColumn("Status");

            //Add Filters
            esqResult.Filters.LogicalOperation = Terrasoft.Common.LogicalOperationStrict.And;
            IEntitySchemaQueryFilterItem activityIdFilter =
                esqResult.CreateFilterWithParameters(FilterComparisonType.NotEqual, "Id", activity.GetTypedColumnValue<Guid>("Id"));
            esqResult.Filters.Add(activityIdFilter);

            esqResult.Filters.LogicalOperation = Terrasoft.Common.LogicalOperationStrict.And;
            IEntitySchemaQueryFilterItem activityTypeFilter =
                esqResult.CreateFilterWithParameters(FilterComparisonType.Equal, "Type", activityType);
            esqResult.Filters.Add(activityTypeFilter);

            IEntitySchemaQueryFilterItem activityCategoryFilter =
                esqResult.CreateFilterWithParameters(FilterComparisonType.Equal, "ActivityCategory", activityCategory);
            esqResult.Filters.Add(activityCategoryFilter);

            IEntitySchemaQueryFilterItem activityOwnerFilter =
                esqResult.CreateFilterWithParameters(FilterComparisonType.Equal, "Owner", ownerId);
            esqResult.Filters.Add(activityOwnerFilter);


            //Set Statuses
            esqResult.Filters.Add(
                new EntitySchemaQueryFilterCollection(
                    esqResult,
                    Terrasoft.Common.LogicalOperationStrict.Or,
                    esqResult.CreateFilterWithParameters(FilterComparisonType.Equal, "Status", activityStatusInProgress),
                    esqResult.CreateFilterWithParameters(FilterComparisonType.Equal, "Status", activityStatusNotStarted)
                    )
                );


            //Add Dates
            var dateFilter1 = new EntitySchemaQueryFilterCollection(
                esqResult,
                Terrasoft.Common.LogicalOperationStrict.Or,
                esqResult.CreateFilterWithParameters(FilterComparisonType.Between, "StartDate", start, due),
                esqResult.CreateFilterWithParameters(FilterComparisonType.Between, "DueDate", start, due)
            );

            var dateFilter2 = new EntitySchemaQueryFilterCollection(
                esqResult,
                Terrasoft.Common.LogicalOperationStrict.And,
                esqResult.CreateFilterWithParameters(FilterComparisonType.LessOrEqual, "StartDate", start),
                esqResult.CreateFilterWithParameters(FilterComparisonType.GreaterOrEqual, "DueDate", due)
            );


            esqResult.Filters.Add(
               new EntitySchemaQueryFilterCollection(
                       esqResult,
                       Terrasoft.Common.LogicalOperationStrict.Or,
                       dateFilter1,
                       dateFilter2
                   )
              );

            EntityCollection activities = esqResult.GetEntityCollection(userConnection);
            int result = activities.Count;
            return result;
        }
    }
}
