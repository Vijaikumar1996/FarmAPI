using FarmAPI.Entities;

namespace FarmAPI.Interface
{
    public interface ICustomerHelperService
    {
        Task<HashSet<long>> GetSubscriptionCustomerIdsAsync();
        string GetCustomerRequestDescription(
                CustomerRequest entity);
    }
}
