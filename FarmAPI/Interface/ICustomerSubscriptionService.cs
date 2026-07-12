using FarmAPI.DTOs;


namespace FarmAPI.Services.Interfaces;

public interface ICustomerSubscriptionService
{
    Task<PagedResponse<CustomerSubscriptionListDto>> GetAllAsync(
        CustomerSubscriptionFilterDto filter);

    Task<CustomerSubscriptionDto?> GetByIdAsync(
        long id);

    Task<long> CreateAsync(
        CreateCustomerSubscriptionDto dto);

    Task UpdateAsync(
        long id,
        UpdateCustomerSubscriptionDto dto);

    Task DeleteAsync(
        long id);
}