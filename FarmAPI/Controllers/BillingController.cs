using FarmAPI.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static FarmAPI.DTOs.BillingDto;

namespace FarmAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BillingController : ControllerBase
    {
        private readonly IBillingService _billingService;

        public BillingController(
            IBillingService billingService)
        {
            _billingService = billingService;
        }

        [HttpGet("monthly")]
        public async Task<IActionResult> GetMonthlyBilling(
            [FromQuery] BillingFilterRequest request)
        {
            return Ok(await _billingService
                .GetMonthlyBillingAsync(request));
        }

        //[HttpGet("summary")]
        //public async Task<IActionResult> GetSummary(
        //    [FromQuery] DateOnly billingMonth)
        //{
        //    return Ok(await _billingService
        //        .GetSummaryAsync(billingMonth));
        //}

        [HttpPost("payment")]
        public async Task<IActionResult> ReceivePayment(
            ReceivePaymentRequest request)
        {
            await _billingService
                .ReceivePaymentAsync(request);

            return Ok();
        }

        [HttpPost("adjustment")]
        public async Task<IActionResult> AddAdjustment(
            BillingAdjustmentRequest request)
        {
            await _billingService
                .AddAdjustmentAsync(request);

            return Ok();
        }

        [HttpGet("details")]
        public async Task<IActionResult> GetBillingDetails(
    long customerId,
    DateOnly billingMonth)
        {
            return Ok(await _billingService.GetBillingDetailsAsync(
                customerId,
                billingMonth));
        }
    }
}