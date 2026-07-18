using static FarmAPI.DTOs.BillingDto;

namespace FarmAPI.Interface
{
    public interface IBillingService
    {
        Task<BillingSearchResponse> GetMonthlyBillingAsync(
      BillingFilterRequest request);

        //Task<BillingSummaryResponse> GetSummaryAsync(
        //    DateOnly billingMonth);

        Task ReceivePaymentAsync(
            ReceivePaymentRequest request);

        Task AddAdjustmentAsync(
            BillingAdjustmentRequest request);

        Task<BillingDetailsResponse> GetBillingDetailsAsync(
    long customerId,
    DateOnly billingMonth);
    }
}