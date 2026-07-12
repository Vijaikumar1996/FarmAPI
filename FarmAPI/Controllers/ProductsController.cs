using FarmAPI.DTOs;
using FarmAPI.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static FarmAPI.DTOs.ProductDto;

namespace FarmAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(
        IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search(
        ProductSearchRequestDto request)
    {
        var result = await _productService.SearchAsync(request);

        return Ok(result);
    }

    [HttpGet("dropdown")]
    public async Task<IActionResult> GetDropdown()
    {
        var result = await _productService.GetDropdownAsync();

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id)
    {
        var result = await _productService.GetByIdAsync(id);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateProductRequestDto request)
    {
        await _productService.CreateAsync(request);

        return Ok(new
        {
            Success = true,
            Message = "Product created successfully."
        });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        UpdateProductRequestDto request)
    {
        await _productService.UpdateAsync(id, request);

        return Ok(new
        {
            Success = true,
            Message = "Product updated successfully."
        });
    }

    //[HttpDelete("{id:long}")]
    //public async Task<IActionResult> Delete(
    //    long id)
    //{
    //    await _productService.DeleteAsync(id);

    //    return Ok(new
    //    {
    //        Success = true,
    //        Message = "Product deleted successfully."
    //    });
    //}

    //[HttpGet("dropdown")]
    //public async Task<IActionResult> GetDropdown()
    //{
    //    var result = await _productService.GetDropdownAsync();

    //    return Ok(result);
    //}

    [HttpPost("{id:long}/price")]
    public async Task<IActionResult> UpdatePrice(
        long id,
        UpdateProductPriceRequestDto request)
    {
        await _productService.UpdatePriceAsync(id, request);

        return Ok(new
        {
            Success = true,
            Message = "Product price updated successfully."
        });
    }

    [HttpGet("{id:long}/price-history")]
    public async Task<IActionResult> GetPriceHistory(
        long id)
    {
        var result = await _productService.GetPriceHistoryAsync(id);

        return Ok(result);
    }  
}