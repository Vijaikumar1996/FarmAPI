using FarmAPI.DTOs;
using static FarmAPI.DTOs.CustomerRequestDto;


namespace FarmAPI.Services.Interfaces;

public interface ICustomerRequestService
{
    Task<PagedResponse<CustomerRequestListDto>> GetAllAsync(
        CustomerRequestFilterDto filter);

    Task<CustomerRequestResponseDto?> GetByIdAsync(long id);

    Task<CustomerRequestLookupDto> GetCustomerRequestLookupAsync(
        long customerId, DateOnly deliveryDate);

    Task<long> CreateAsync(
        CreateCustomerRequestDto dto);

    Task UpdateAsync(
        long id,
        UpdateCustomerRequestDto dto);

    Task DeleteAsync(long id);
}