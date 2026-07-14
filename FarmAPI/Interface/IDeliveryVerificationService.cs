
using FarmAPI.DTOs;

namespace Farm.API.Services.Interfaces
{
    public interface IDeliveryVerificationService
    {
        Task<PagedResponse<DeliveryVerificationListDto>> SearchAsync(
            DeliveryVerificationSearchRequestDto request);

        Task MarkAllDeliveredAsync(MarkAllDeliveredRequestDto request);

        Task<DeliveryVerificationDto> GetAsync(
    long customerId,
    DateOnly deliveryDate);

        Task SaveAsync(
    SaveDeliveryVerificationRequestDto request);
    }
}