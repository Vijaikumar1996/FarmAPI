using FarmAPI.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FarmAPI.Controllers
{
    [Route("api/[controller]")]
    public class ProductCategoriesController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductCategoriesController(
            IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProductCategories()
        {
            var result = await _productService.GetProductCategoriesAsync();

            return Ok(result);
        }
    }
}
