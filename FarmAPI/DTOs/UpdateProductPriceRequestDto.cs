namespace FarmAPI.DTOs
{
    public class UpdateProductPriceRequestDto
    {
        public decimal SellingPrice { get; set; }

        public DateOnly EffectiveFrom { get; set; }
    }
}
