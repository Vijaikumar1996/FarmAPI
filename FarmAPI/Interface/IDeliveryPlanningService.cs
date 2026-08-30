using static FarmAPI.DTOs.DeliveryPlanningDto;

namespace FarmManagement.Interfaces;

public interface IDeliveryPlanningService
{
    Task<GenerateDeliveryResponse> GenerateDeliveryAsync(
        GenerateDeliveryRequest request);

    Task<DeliveryGenerationStatusDto> GetGenerationStatusAsync(
    DateOnly deliveryDate);

    Task<List<FarmSummaryDto>> GetFarmSummaryAsync(
    DateOnly deliveryDate,
    short? categoryId);

    Task<List<DriverLoadingDto>> GetDriverLoadingAsync(
      DateOnly deliveryDate);

    Task<List<DeliveryOrderDto>> GetDeliveryBoySheetAsync(
    DateOnly deliveryDate,
    long? areaId);
    Task<byte[]> ExportDeliveryBoySheetAsync(
    DateOnly deliveryDate,
    long? areaId);

    Task<List<ExpectedDeliveryDto>> GetExpectedDeliveriesAsync(
               DateOnly deliveryDate,
               string source,
               long productId);
}