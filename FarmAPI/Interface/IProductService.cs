using FarmAPI.DTOs;
using static FarmAPI.DTOs.ProductDto;

namespace FarmAPI.Interface
{
    public interface IProductService
    {
        Task CreateAsync(CreateProductRequestDto request);

        Task UpdateAsync(long id, UpdateProductRequestDto request);

        Task<ProductResponseDto?> GetByIdAsync(long id);

        Task<PagedResponse<ProductResponseDto>> SearchAsync(ProductSearchRequestDto request);

        Task<List<DropdownDto>> GetDropdownAsync();

        Task<List<ProductCategoryDropdownDto>> GetProductCategoriesAsync();

        Task UpdatePriceAsync(long productId,UpdateProductPriceRequestDto request);

        Task<List<ProductPriceResponseDto>> GetPriceHistoryAsync(long productId);
    }
}
