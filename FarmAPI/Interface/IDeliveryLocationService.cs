using FarmAPI.Entities;
using static FarmAPI.DTOs.DeliveryLocationDto;

namespace FarmAPI.Interface
{
    public interface IDeliveryLocationService
    {
        Task<List<DeliveryLocationResponse>> GetAllAsync();

        Task<DeliveryLocation?> GetByIdAsync(long id);

        Task<DeliveryLocation> CreateAsync(CreateDeliveryLocationRequest request);

        Task UpdateAsync(long id, UpdateDeliveryLocationRequest request);

        Task DeleteAsync(long id);
    }
}