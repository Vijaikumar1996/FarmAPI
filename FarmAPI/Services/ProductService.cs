using FarmAPI.Data;
using FarmAPI.DTOs;
using FarmAPI.Entities;
using FarmAPI.Interface;
using Microsoft.EntityFrameworkCore;
using static FarmAPI.DTOs.ProductDto;

namespace FarmAPI.Services
{
    public partial class ProductService : IProductService
    {
        private readonly FarmDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ProductService(
            FarmDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }


        public async Task<PagedResponse<ProductResponseDto>> SearchAsync(
    ProductSearchRequestDto request)
        {
            var query = _context.Products
                .Include(x => x.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                var search = request.SearchText.Trim().ToLower();

                query = query.Where(x =>
                    x.ProductCode.ToLower().Contains(search) ||
                    x.ProductName.ToLower().Contains(search));
            }

            if (request.CategoryId.HasValue)
            {
                query = query.Where(x =>
                    x.CategoryId == request.CategoryId.Value);
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(x =>
                    x.IsActive == request.IsActive.Value);
            }

            var totalRecords = await query.CountAsync();

            var items = await query

                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.ProductName)

                .Skip((request.PageNumber - 1) * request.PageSize)

                .Take(request.PageSize)

                .Select(x => new ProductResponseDto
                {
                    Id = x.Id,

                    ProductCode = x.ProductCode,

                    ProductName = x.ProductName,

                    CategoryId = x.CategoryId,

                    CategoryName = x.Category.CategoryName,

                    LitresPerUnit = x.LitresPerUnit,

                    TrackInventory = x.TrackInventory,

                    DisplayOrder = x.DisplayOrder,

                    CurrentPrice = x.ProductPrices
    .OrderByDescending(p => p.EffectiveFrom)
    .Select(p => p.SellingPrice)
    .FirstOrDefault(),

                    IsActive = x.IsActive,

                    CreatedAt = x.CreatedAt
                })

                .ToListAsync();

            return new PagedResponse<ProductResponseDto>
            {
                Items = items,

                PageNumber = request.PageNumber,

                PageSize = request.PageSize,

                TotalRecords = totalRecords
            };
        }

        //public async Task<List<ProductResponse>> GetAllAsync()
        //{
        //    return await _context.Products
        //        .Include(x => x.Category)
        //        .OrderBy(x => x.DisplayOrder)
        //        .ThenBy(x => x.ProductName)
        //        .Select(x => new ProductResponse
        //        {
        //            Id = x.Id,
        //            ProductCode = x.ProductCode,
        //            ProductName = x.ProductName,

        //            CategoryId = x.CategoryId,
        //            CategoryName = x.Category.CategoryName,

        //            LitresPerUnit = x.LitresPerUnit,
        //            TrackInventory = x.TrackInventory,

        //            DisplayOrder = x.DisplayOrder,
        //            IsActive = x.IsActive,

        //            CreatedAt = x.CreatedAt
        //        })
        //        .ToListAsync();
        //}

        public async Task<ProductResponseDto?> GetByIdAsync(long id)
        {
            var product = await _context.Products

                .Include(x => x.Category)

                .Where(x => x.Id == id)

                .Select(x => new ProductResponseDto
                {
                    Id = x.Id,
                    ProductCode = x.ProductCode,
                    ProductName = x.ProductName,

                    CategoryId = x.CategoryId,
                    CategoryName = x.Category.CategoryName,

                    LitresPerUnit = x.LitresPerUnit,
                    TrackInventory = x.TrackInventory,

                    DisplayOrder = x.DisplayOrder,
                    IsActive = x.IsActive,

                    CreatedAt = x.CreatedAt
                })

                .FirstOrDefaultAsync();

            if (product == null)
                throw new Exception("Product not found.");

            return product;
        }

        public async Task CreateAsync(CreateProductRequestDto request)
        {
            await ValidateCategoryAsync(request.CategoryId);

            await ValidateDuplicateProductAsync(
                null,
                request.ProductCode,
                request.ProductName);

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var category = await _context.ProductCategories
                    .FirstAsync(x => x.Id == request.CategoryId);

                var product = new Product
                {
                    ProductCode = request.ProductCode.Trim(),
                    ProductName = request.ProductName.Trim(),

                    CategoryId = request.CategoryId,

                    LitresPerUnit = request.LitresPerUnit,

                    TrackInventory = category.TrackInventoryDefault,

                    DisplayOrder = request.DisplayOrder,

                    IsActive = true,

                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUser.UserId
                };

                var productPrice = new ProductPrice
                {
                    Product = product,

                    SellingPrice = request.SellingPrice,

                    EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow),

                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUser.UserId
                };

                _context.Products.Add(product);
                _context.ProductPrices.Add(productPrice);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateAsync(
    long id,
    UpdateProductRequestDto request)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
                throw new Exception("Product not found.");

            await ValidateCategoryAsync(request.CategoryId);

            await ValidateDuplicateProductAsync(
                id,
                request.ProductCode,
                request.ProductName);

            product.ProductCode = request.ProductCode.Trim();

            product.ProductName = request.ProductName.Trim();

            product.CategoryId = request.CategoryId;

            var category = await _context.ProductCategories
            .FirstAsync(x => x.Id == request.CategoryId);

            product.LitresPerUnit = request.LitresPerUnit;

            product.TrackInventory = category.TrackInventoryDefault;

            product.DisplayOrder = request.DisplayOrder;

            product.IsActive = request.IsActive;

            product.UpdatedAt = DateTime.UtcNow;

            product.UpdatedBy = _currentUser.UserId;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
                throw new Exception("Product not found.");

            product.IsActive = false;

            product.UpdatedAt = DateTime.UtcNow;

            product.UpdatedBy = _currentUser.UserId;

            await _context.SaveChangesAsync();
        }

        private async Task ValidateCategoryAsync(short categoryId)
        {
            var exists = await _context.ProductCategories
                .AnyAsync(x => x.Id == categoryId);

            if (!exists)
                throw new Exception("Product category not found.");
        }

        private async Task ValidateDuplicateProductAsync(
            long? productId,
            string productCode,
            string productName)
        {
            productCode = productCode.Trim();
            productName = productName.Trim();

            var duplicateCode = await _context.Products
                .AnyAsync(x =>
                    x.ProductCode == productCode &&
                    (!productId.HasValue || x.Id != productId.Value));

            if (duplicateCode)
                throw new Exception("Product code already exists.");

            var duplicateName = await _context.Products
                .AnyAsync(x =>
                    x.ProductName == productName &&
                    (!productId.HasValue || x.Id != productId.Value));

            if (duplicateName)
                throw new Exception("Product name already exists.");
        }

        public async Task UpdatePriceAsync(
    long productId,
    UpdateProductPriceRequestDto request)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.Id == productId);

            if (product == null)
                throw new Exception("Product not found.");

            if (!product.IsActive)
                throw new Exception("Cannot update price for inactive product.");

            if (request.SellingPrice <= 0)
                throw new Exception("Selling price must be greater than zero.");

            var exists = await _context.ProductPrices
                .AnyAsync(x =>
                    x.ProductId == productId &&
                    x.EffectiveFrom == request.EffectiveFrom);

            if (exists)
                throw new Exception("Price already exists for the selected effective date.");

            var productPrice = new ProductPrice
            {
                ProductId = productId,

                SellingPrice = request.SellingPrice,

                EffectiveFrom = request.EffectiveFrom,

                CreatedAt = DateTime.UtcNow,

                CreatedBy = _currentUser.UserId
            };

            _context.ProductPrices.Add(productPrice);

            await _context.SaveChangesAsync();
        }

        public async Task<List<ProductPriceResponseDto>> GetPriceHistoryAsync(
    long productId)
        {
            var exists = await _context.Products
                .AnyAsync(x => x.Id == productId);

            if (!exists)
                throw new Exception("Product not found.");

            return await _context.ProductPrices

                .Where(x => x.ProductId == productId)

                .OrderByDescending(x => x.EffectiveFrom)

                .Select(x => new ProductPriceResponseDto
                {
                    Id = x.Id,

                    SellingPrice = x.SellingPrice,

                    EffectiveFrom = x.EffectiveFrom,

                    CreatedAt = x.CreatedAt
                })

                .ToListAsync();
        }

        public async Task<List<ProductCategoryDropdownDto>> GetProductCategoriesAsync()
        {
            return await _context.ProductCategories
                .Where(x => x.IsActive)
                .OrderBy(x => x.CategoryName)
                .Select(x => new ProductCategoryDropdownDto
                {
                    Id = x.Id,
                    Name = x.CategoryName,
                    TrackInventoryDefault = x.TrackInventoryDefault
                })
                .ToListAsync();
        }

        public async Task<List<DropdownDto>> GetDropdownAsync()
        {
            return await _context.Products
                .Where(x => x.IsActive)
                .OrderBy(x => x.ProductName)
                .Select(x => new DropdownDto
                {
                    Id = x.Id,
                    Name = x.ProductCode + " - " + x.ProductName
                })
                .ToListAsync();
        }


    }
}

