
using static FarmAPI.DTOs.ReportDto;

public interface IReportsService
{
    Task<List<DeliveredItemsDto>> GetDeliveredItemsAsync(
        DateOnly deliveryDate,
        short? categoryId);
}