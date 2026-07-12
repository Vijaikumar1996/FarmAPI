using FarmAPI.DTOs;
using static FarmAPI.DTOs.CustomerDto;

namespace FarmAPI.Interface
{
    public interface ICustomerService
    {
        Task<List<CustomerResponse>> GetAllAsync();

        Task<CustomerResponse?> GetByIdAsync(long id);

        Task<CustomerResponse> CreateAsync(
            CreateCustomerRequest request);

        Task UpdateAsync(
            long id,
            UpdateCustomerRequest request);

        Task DeleteAsync(long id);

        Task<List<DropdownDto>> GetTypeaheadAsync(
    string? searchText);
    }
}