using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Threading;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Web.Common;


namespace DevTraining.Files.cs.WS
{
    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    class DemoWS: BaseService
    {

        [OperationContract]
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped,
        ResponseFormat = WebMessageFormat.Json)]
        public string TestMethod(Guid contactId) {

            Select select = new Select(UserConnection)
                .Column("Email")
                .From("Contact")
                .Where("Id").IsEqual(Column.Parameter(contactId)) as Select;


            string result = string.Empty;
            using (DBExecutor executor = UserConnection.EnsureDBConnection()) 
            {
                result = select.ExecuteScalar<string>(executor);
            }


            return result ?? "no email";
        }

        [OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped,
        ResponseFormat = WebMessageFormat.Json)]
        public string TestPostMethod(Guid contactId)
        {

            Select select = new Select(UserConnection)
                .Column("Email")
                .From("Contact")
                .Where("Id").IsEqual(Column.Parameter(contactId)) as Select;


            string result = string.Empty;
            using (DBExecutor executor = UserConnection.EnsureDBConnection())
            {
                result = select.ExecuteScalar<string>(executor);
            }


            return result ?? "no email";
        }
    }
}
