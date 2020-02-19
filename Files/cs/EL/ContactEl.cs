using global::Common.Logging;
using Terrasoft.Core;
using Terrasoft.Core.Entities;
using Terrasoft.Core.Entities.Events;

namespace GuidedLearningClio.Files.cs.el
{
    /// <summary>
    /// Listener for 'EntityName' entity events.
    /// </summary>
    /// <seealso cref="Terrasoft.Core.Entities.Events.BaseEntityEventListener" />
    [EntityEventListener(SchemaName = "Contact")]
    class ContactEventListener : BaseEntityEventListener
    {
        private static readonly ILog _log = LogManager.GetLogger("DevTrainingLogger");
        public override void OnSaved(object sender, EntityAfterEventArgs e)
        {
            //base.OnSaved(sender, e);
            //Entity entity = (Entity)sender;
            ////UserConnection userConnection = entity.UserConnection;

            //string message = $"TEST: {entity.GetTypedColumnValue<string>("Name")}";
            ////_log.Info(message);
            //entity.SetColumnValue("Name", message);
            //entity.Save();

        }

        public override void OnSaving(object sender, EntityBeforeEventArgs e)
        {
            base.OnSaving(sender, e);
            Entity entity = (Entity)sender;
            //UserConnection userConnection = entity.UserConnection;

            string oldName = entity.GetTypedOldColumnValue<string>("Name");
            string newName = entity.GetTypedColumnValue<string>("Name");
            string vName = $"{oldName} --> {newName}";

            //_log.Info(message);
            entity.SetColumnValue("Name", vName);
            entity.Save();

        }

    }
}