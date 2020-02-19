using System;
using System.Threading.Tasks;

namespace DevTraining
{
    public interface IBank
    {
        Task<IBankResult> GetRateAsync(string currency, DateTime date);
    }
}