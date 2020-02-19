using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using Terrasoft.Web.Common;

namespace DevTraining.Files.cs.WS
{
    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class ExchangeRateWS : BaseService
    {
        [OperationContract]
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public BankResult ExecuteGet(int bankId, string date, string currency)
        {
            DateTime.TryParse(date, out DateTime dt);
            IBank bank = BankFactory.GetBank((BankFactory.SupportedBanks)bankId);

            IBankResult bankResult = Task.Run(() => bank.GetRateAsync(currency.ToUpper(), dt)).Result;
            BankResult result = new BankResult
            {
                ExchangeRate = bankResult.ExchangeRate,
                RateDate = bankResult.RateDate,
                HomeCurrency = bankResult.HomeCurrency,
                BankName = bankResult.BankName
            };
            MsgChannelUtilities.PostMessageToAll(ToString(), "this is my custom websocket message");
            return result;
        }
    }
}


/***
 *[Bank of Canada]              http://k_krylov_nb:8090/0/rest/ExchangeRateWS/ExecuteGet?bankId=0&date=2020-02-19&currency=USD
 *[National bank of Russia]     http://k_krylov_nb:8090/0/rest/ExchangeRateWS/ExecuteGet?bankId=1&date=2020-02-19&currency=USD
 *[National bank of Ukraine]    http://k_krylov_nb:8090/0/rest/ExchangeRateWS/ExecuteGet?bankId=2&date=2020-02-19&currency=USD
 *[European Central Bank]       http://k_krylov_nb:8090/0/rest/ExchangeRateWS/ExecuteGet?bankId=3&date=2020-02-19&currency=USD
 */
