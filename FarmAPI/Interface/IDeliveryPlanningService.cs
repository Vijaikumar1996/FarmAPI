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

    Task<List<DeliveryBoySheetDto>> GetDeliveryBoySheetAsync(
    DateOnly deliveryDate,
    long? areaId);
    Task<byte[]> ExportDeliveryBoySheetAsync(
    DateOnly deliveryDate,
    long? areaId);
}