using ClosedXML.Excel;
using FarmAPI.Data;
using FarmAPI.Entities;
using FarmAPI.Interface;
using FarmAPI.Utils;
using FarmManagement.Entities;
using FarmManagement.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using static FarmAPI.DTOs.DeliveryPlanningDto;
using static FarmAPI.Utils.Constant;

public class DeliveryPlanningService : IDeliveryPlanningService
{
    private readonly FarmDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeliveryPlanningService(FarmDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<DeliveryGenerationStatusDto> GetGenerationStatusAsync(
    DateOnly deliveryDate)
    {
        var query = _context.DeliveryDetails
            .AsNoTracking()
            .Where(x => x.DeliveryDate == deliveryDate);

        var total = await query.CountAsync();

        if (total == 0)
        {
            return new DeliveryGenerationStatusDto
            {
                DeliveryDate = deliveryDate,
                IsGenerated = false
            };
        }

        var latest = await query
            .OrderByDescending(x => x.GeneratedAt)
            .Select(x => new
            {
                x.GeneratedAt,
                UserName = ""
            })
            .FirstAsync();

        return new DeliveryGenerationStatusDto
        {
            DeliveryDate = deliveryDate,
            IsGenerated = true,
            TotalDeliveries = total,
            GeneratedAt = latest.GeneratedAt,
            GeneratedBy = latest.UserName
        };
    }

    public async Task<GenerateDeliveryResponse> GenerateDeliveryAsync(
     GenerateDeliveryRequest request)
    {
        long userId = _currentUser.UserId;
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // ===========================
            // Delete existing deliveries
            // ===========================
            await _context.DeliveryDetails
                .Where(x => x.DeliveryDate == request.DeliveryDate)
                .ExecuteDeleteAsync();

            // ===========================
            // Load active subscriptions (WITH AREA FILTER)
            // ===========================
            var subscriptions = await _context.CustomerSubscriptions
     .Include(x => x.Schedules)
     .Include(x => x.Product) // ✅ ADD
     .Include(x => x.Customer)
         .ThenInclude(c => c.Area)
     .Where(x =>
         x.IsActive &&
         x.Product.IsActive &&        // ✅ PRODUCT FILTER
         x.Customer.Area.IsActive &&
         x.StartDate <= request.DeliveryDate &&
         (x.EndDate == null || x.EndDate >= request.DeliveryDate))
     .ToListAsync();

            // ===========================
            // Load prices
            // ===========================
            var productPrices = await _context.ProductPrices
                .AsNoTracking()
                .Where(x => x.EffectiveFrom <= request.DeliveryDate)
                .ToListAsync();

            var priceDictionary = productPrices
                .GroupBy(x => x.ProductId)
                .ToDictionary(
                    x => x.Key,
                    x => x.OrderByDescending(y => y.EffectiveFrom)
                          .First()
                          .SellingPrice);

            // ===========================
            // Load customer requests (WITH AREA FILTER)
            // ===========================
            var customerRequests = await _context.CustomerRequests
     .Include(x => x.Product) // ✅ ADD
     .Include(x => x.Customer)
         .ThenInclude(c => c.Area)
     .AsNoTracking()
     .Where(x =>
         x.Status != CustomerRequestStatus.Cancelled &&
         x.IsActive &&
         x.Customer.Area.IsActive &&
         x.Product.IsActive &&     // ✅ PRODUCT FILTER
         x.EffectiveFrom <= request.DeliveryDate &&
         (x.EffectiveTo == null ||
          x.EffectiveTo >= request.DeliveryDate))
     .ToListAsync();

            List<DeliveryDetail> deliveryDetails = new();
            HashSet<long> processedRequestIds = new();

            // ===========================
            // Process subscriptions
            // ===========================
            foreach (var subscription in subscriptions)
            {
                // Extra safety check
                if (!subscription.Customer.Area.IsActive)
                    continue;

                if (!subscription.Product.IsActive)
                    continue;

                var replaceRequest = customerRequests.FirstOrDefault(x =>
                    x.SubscriptionId == subscription.Id &&
                    x.RequestAction == CustomerRequestAction.Replace);

                if (replaceRequest == null &&
                    !IsDeliveryApplicable(subscription, request.DeliveryDate))
                    continue;

                decimal quantity = GetQuantity(
                    subscription,
                    request.DeliveryDate);

                var pauseRequest = customerRequests.FirstOrDefault(x =>
                    x.SubscriptionId == subscription.Id &&
                    x.RequestAction == CustomerRequestAction.Pause);

                var deliveries = BuildDeliveries(
                    subscription,
                    pauseRequest,
                    replaceRequest,
                    quantity,
                    priceDictionary,
                    request.DeliveryDate,
                    userId);

                deliveryDetails.AddRange(deliveries);

                if (pauseRequest != null)
                    processedRequestIds.Add(pauseRequest.Id);

                if (replaceRequest != null)
                    processedRequestIds.Add(replaceRequest.Id);
            }

            // ===========================
            // Handle ADD requests (non-subscription)
            // ===========================
            var addRequests = customerRequests
                .Where(x =>
                    x.RequestAction == CustomerRequestAction.Add &&
                    x.SubscriptionId == null)
                .ToList();

            foreach (var addRequest in addRequests)
            {
                // Extra safety check
                if (!addRequest.Customer.Area.IsActive)
                    continue;

                if (!addRequest.Product.IsActive)
                    continue;

                deliveryDetails.Add(CreateDelivery(
                    customerId: addRequest.CustomerId,
                    subscriptionId: null,
                    requestId: addRequest.Id,
                    productId: addRequest.ProductId!.Value,
                    quantity: addRequest.Quantity ?? 1,
                    unitPrice: GetProductPrice(
                        addRequest.ProductId.Value,
                        priceDictionary),
                    deliveryDate: request.DeliveryDate,
                    userId: userId));

                processedRequestIds.Add(addRequest.Id);
            }

     //       var duplicateDeliveries = deliveryDetails
     //.GroupBy(x => new
     //{
     //    x.DeliveryDate,
     //    x.CustomerId,
     //    x.ProductId
     //})
     //.Where(g => g.Count() > 1)
     //.ToList();

            // ===========================
            // Save deliveries
            // ===========================
            if (deliveryDetails.Any())
            {
                await _context.DeliveryDetails.AddRangeAsync(deliveryDetails);
            }

            // ===========================
            // Update request statuses
            // ===========================
            if (processedRequestIds.Any())
            {
                var requests = await _context.CustomerRequests
                    .Where(x => processedRequestIds.Contains(x.Id))
                    .ToListAsync();

                foreach (var customerRequest in requests)
                {
                    if (customerRequest.Status == CustomerRequestStatus.Cancelled ||
                        customerRequest.Status == CustomerRequestStatus.Processed)
                    {
                        continue;
                    }

                    if (customerRequest.EffectiveTo.HasValue &&
                        customerRequest.EffectiveTo.Value <= request.DeliveryDate)
                    {
                        customerRequest.Status = CustomerRequestStatus.Processed;
                    }
                    else if (customerRequest.EffectiveFrom <= request.DeliveryDate)
                    {
                        customerRequest.Status = CustomerRequestStatus.InProgress;
                    }
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new GenerateDeliveryResponse
            {
                Success = true,
                Message = "Delivery generated successfully.",
                TotalRecords = deliveryDetails.Count
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private bool IsDeliveryApplicable(
    CustomerSubscription subscription,
    DateOnly deliveryDate)
    {
        switch (subscription.FrequencyId)
        {
            case Constant.SubscriptionFrequency.Daily: // Daily
                return true;

            case Constant.SubscriptionFrequency.Weekly:  // Weekly
                return subscription.Schedules.Any(x =>
                    x.DayOfWeek == (short)deliveryDate.DayOfWeek);

            case Constant.SubscriptionFrequency.Monthly: // Monthly
                return subscription.Schedules.Any(x =>
                    x.DayOfMonth == deliveryDate.Day);

            case Constant.SubscriptionFrequency.Interval: // Interval

                if (!subscription.IntervalDays.HasValue ||
                    subscription.IntervalDays.Value <= 0)
                    return false;

                var totalDays =
                    deliveryDate.DayNumber -
                    subscription.StartDate.DayNumber;

                return totalDays % subscription.IntervalDays.Value == 0;

            default:
                return false;
        }
    }

    private decimal GetQuantity(
     CustomerSubscription subscription,
     DateOnly deliveryDate)
    {
        switch (subscription.FrequencyId)
        {
            case Constant.SubscriptionFrequency.Daily:
            case Constant.SubscriptionFrequency.Interval:

                var schedules = subscription.Schedules
                    .OrderBy(x => x.PatternOrder)
                    .ToList();

                // No pattern configured
                if (schedules.Count == 1)
                    return schedules[0].Quantity;

                var days = deliveryDate.DayNumber - subscription.StartDate.DayNumber;

                // For Interval use delivery count, not calendar days
                if (subscription.FrequencyId == Constant.SubscriptionFrequency.Interval)
                {
                    days = days / subscription.IntervalDays!.Value;
                }

                var index = days % schedules.Count;

                return schedules[index].Quantity;

            case  Constant.SubscriptionFrequency.Weekly:

                return subscription.Schedules
                    .First(x => x.DayOfWeek == (short)deliveryDate.DayOfWeek)
                    .Quantity;

            case Constant.SubscriptionFrequency.Monthly:

                return subscription.Schedules
                    .First(x => x.DayOfMonth == deliveryDate.Day)
                    .Quantity;

            default:
                throw new Exception("Unsupported frequency.");
        }
    }

    private decimal GetProductPrice(
      long productId,
      Dictionary<long, decimal> priceDictionary)
    {
        if (!priceDictionary.TryGetValue(productId, out var price))
        {
            throw new InvalidOperationException(
                $"Price not configured for Product Id : {productId}");
        }

        return price;
    }



    private List<DeliveryDetail> BuildDeliveries(
    CustomerSubscription subscription,
    CustomerRequest? pauseRequest,
    CustomerRequest? replaceRequest,
    decimal quantity,
    Dictionary<long, decimal> priceDictionary,
    DateOnly deliveryDate,
    long userId)
    {
        List<DeliveryDetail> deliveries = new();

        if (pauseRequest != null)
        {
            return deliveries;
        }

        long productId = subscription.ProductId;
        decimal finalQuantity = quantity;

        if (replaceRequest != null)
        {
            productId = replaceRequest.ProductId ?? subscription.ProductId;
            finalQuantity = replaceRequest.Quantity ?? quantity;
        }

        decimal unitPrice = GetProductPrice(
            productId,
            priceDictionary);

        deliveries.Add(CreateDelivery(
            customerId: subscription.CustomerId,
            subscriptionId: subscription.Id,
            requestId: replaceRequest?.Id,
            productId: productId,
            quantity: finalQuantity,
            unitPrice: unitPrice,
            deliveryDate: deliveryDate,
            userId: userId));

        return deliveries;
    }

    private DeliveryDetail CreateDelivery(
    long customerId,
    long? subscriptionId,
    long? requestId,
    long productId,
    decimal quantity,
    decimal unitPrice,
    DateOnly deliveryDate,
    long userId)
    {
        return new DeliveryDetail
        {
            DeliveryDate = deliveryDate,

            BillingMonth = new DateOnly(
            deliveryDate.Year,
            deliveryDate.Month,
            1),

            CustomerId = customerId,

            SubscriptionId = subscriptionId,

            RequestId = requestId,

            ProductId = productId,

            PlannedQty = quantity,

            DeliveredQty = 0,

            UnitPrice = unitPrice,

            Status = CustomerDeliveryStatus.Pending,

            GeneratedAt = DateTime.UtcNow,

            GeneratedBy = userId
        };
    }


    public async Task<List<FarmSummaryDto>> GetFarmSummaryAsync(
     DateOnly deliveryDate,
     short? categoryId)
    {
        var query = _context.DeliveryDetails
            .AsNoTracking()
            .Where(x => x.DeliveryDate == deliveryDate);

        if (categoryId.HasValue)
        {
            query = query.Where(x =>
                x.Product.CategoryId == categoryId.Value);
        }

        var result = await query
            .GroupBy(x => new
            {
                x.ProductId,
                x.Product.ProductCode,
                x.Product.ProductName,
                x.Product.LitresPerUnit,
                x.Product.DisplayOrder
            })
            .Select(g => new FarmSummaryDto
            {
                ProductId = g.Key.ProductId,

                ProductCode = g.Key.ProductCode,

                ProductName = g.Key.ProductName,

                DisplayOrder = g.Key.DisplayOrder,

                Quantity = g.Sum(x => x.PlannedQty),

                Litres = g.Key.LitresPerUnit == null
                    ? null
                    : g.Sum(x => x.PlannedQty) *
                      g.Key.LitresPerUnit.Value
            })
            .ToListAsync();

        return result
            .OrderBy(x => x.DisplayOrder ?? int.MaxValue)
            .ThenBy(x => x.ProductCode)
            .ToList();
    }

    public async Task<List<DriverLoadingDto>> GetDriverLoadingAsync(
     DateOnly deliveryDate)
    {
        var data = await _context.DeliveryDetails
            .AsNoTracking()
            .Where(x => x.DeliveryDate == deliveryDate)
            .Select(x => new
            {
                AreaId = x.Customer.AreaId,
                AreaCode = x.Customer.Area.AreaCode,
                AreaName = x.Customer.Area.AreaName,               

                ProductId = x.ProductId,
                ProductCode = x.Product.ProductCode,
                ProductName = x.Product.ProductName,
                ProductDisplayOrder = x.Product.DisplayOrder,
                LitresPerUnit = x.Product.LitresPerUnit,

                Quantity = x.PlannedQty
            })
            .ToListAsync();

        var result = data

            .GroupBy(x => new
            {
                x.AreaId,
                x.AreaCode,
                x.AreaName,                
            })

            .OrderBy(x => x.Key.AreaName)
            

            .Select(area => new DriverLoadingDto
            {
                AreaId = area.Key.AreaId,

                AreaCode = area.Key.AreaCode,

                AreaName = area.Key.AreaName,

                Products = area

                    .GroupBy(x => new
                    {
                        x.ProductId,
                        x.ProductCode,
                        x.ProductName,
                        x.ProductDisplayOrder,
                        x.LitresPerUnit
                    })

                    .OrderBy(x => x.Key.ProductDisplayOrder ?? int.MaxValue)
                    .ThenBy(x => x.Key.ProductCode)

                    .Select(product => new DriverLoadingItemDto
                    {
                        ProductId = product.Key.ProductId,

                        ProductCode = product.Key.ProductCode,

                        ProductName = product.Key.ProductName,

                        Quantity = product.Sum(x => x.Quantity),
                       
                    })

                    .Where(x => x.Quantity > 0)

                    .ToList()

            })

            .Where(x => x.Products.Any())

            .ToList();

        return result;
    }


    public async Task<List<DeliveryOrderDto>> GetDeliveryBoySheetAsync(
      DateOnly deliveryDate,
      long? areaId = null)
    {
        // ============================================
        // Load delivery details
        // ============================================

        var query = _context.DeliveryDetails
            .AsNoTracking()
            .Where(x => x.DeliveryDate == deliveryDate);

        if (areaId.HasValue)
        {
            query = query.Where(
                x => x.Customer.AreaId == areaId.Value);
        }

        var data = await query
            .Select(x => new
            {
                CustomerId = x.CustomerId,

                CustomerName = x.Customer.CustomerName,

                AreaCode = x.Customer.Area.AreaCode,

                GroupDeliverySheetByLocation =
                    x.Customer.Area.GroupDeliverySheetByLocation,

                DeliveryLocation =
                    x.Customer.DeliveryLocation.LocationName,

                DeliveryLocationAddress =
                    x.Customer.DeliveryLocation.Address,

                DeliveryNotes = x.Customer.DeliveryNotes,

                DeliveryOrder =
                    x.Customer.DeliveryLocation.DeliveryOrder,

                HouseDoorNo =
                    x.Customer.HouseDoorNo,

                DoorNoAtEnd =
                    x.Customer.DeliveryLocation.DoorNoAtEnd,

                ProductId =
                    x.ProductId,

                ProductCode =
                    x.Product.ProductCode,

                ProductDisplayOrder =
                    x.Product.DisplayOrder,

                CategoryId =
                    x.Product.CategoryId,

                Quantity =
                    x.PlannedQty
            })
            .ToListAsync();


        // ============================================
        // Group:
        //
        // Delivery Order
        //      ↓
        // House
        //      ↓
        // Customer
        //      ↓
        // Products
        // ============================================

        var result = data

            // ========================================
            // DELIVERY ORDER / LOCATION
            // ========================================

            .GroupBy(x => new
            {
                x.AreaCode,

                x.DeliveryOrder,

                x.DeliveryLocation,

                x.DeliveryLocationAddress,

                x.GroupDeliverySheetByLocation
            })

            .OrderBy(x => x.Key.AreaCode)

            .ThenBy(x =>
                x.Key.DeliveryOrder)

            .Select(deliveryGroup => new DeliveryOrderDto
            {
                AreaCode =
                    deliveryGroup.Key.AreaCode,

                DeliveryOrder =
                    deliveryGroup.Key.DeliveryOrder,

                DeliveryLocation =
                    deliveryGroup.Key.DeliveryLocation,

                DeliveryLocationAddress =
                    deliveryGroup.Key.DeliveryLocationAddress,

                GroupDeliverySheetByLocation =
                    deliveryGroup.Key.GroupDeliverySheetByLocation,


                // ====================================
                // HOUSE
                // ====================================

                Houses = deliveryGroup

                    .GroupBy(x => x.HouseDoorNo)

                    .OrderBy(x => x.Key)

                    .Select(houseGroup => new DeliveryHouseDto
                    {
                        HouseDoorNo =
                            houseGroup.Key,


                        // ============================
                        // CUSTOMERS INSIDE HOUSE
                        // ============================

                        Customers = houseGroup

                            .GroupBy(x => new
                            {
                                x.CustomerId,

                                x.CustomerName,

                                x.AreaCode,

                                x.DeliveryLocation,

                                x.DeliveryLocationAddress,

                                x.GroupDeliverySheetByLocation,

                                x.DoorNoAtEnd,

                                x.HouseDoorNo,

                                x.DeliveryNotes
                            })

                            .OrderBy(x =>
                                x.Key.HouseDoorNo)

                            .Select(customer =>
                                new DeliveryBoySheetDto
                                {
                                    CustomerId =
                                        customer.Key.CustomerId,

                                    AreaCode =
                                        customer.Key.AreaCode,

                                    CustomerName =
                                        customer.Key.CustomerName,

                                    DeliveryLocation =
                                        customer.Key.DeliveryLocation,

                                    GroupDeliverySheetByLocation =
                                        customer.Key
                                            .GroupDeliverySheetByLocation,

                                    DeliveryNotes = customer.Key.DeliveryNotes,

                                    // ====================
                                    // ADDRESS
                                    // ====================

                                    Address =
                                        customer.Key.DoorNoAtEnd

                                        ? string.Join(
                                            ", ",
                                            new[]
                                            {
                                            $"{customer.Key.DeliveryLocation} - {customer.Key.HouseDoorNo}",

                                            customer.Key
                                                .DeliveryLocationAddress
                                            }
                                            .Where(x =>
                                                !string.IsNullOrWhiteSpace(x)))

                                        : string.Join(
                                            ", ",
                                            new[]
                                            {
                                            customer.Key.HouseDoorNo,

                                            customer.Key
                                                .DeliveryLocation,

                                            customer.Key
                                                .DeliveryLocationAddress
                                            }
                                            .Where(x =>
                                                !string.IsNullOrWhiteSpace(x))),


                                    // ====================
                                    // MILK PRODUCTS
                                    // ====================

                                    MilkProducts = customer

                                        .Where(x =>
                                            x.CategoryId ==
                                            Constant
                                                .ProductCategory
                                                .Milk)

                                        .GroupBy(x => new
                                        {
                                            x.ProductId,

                                            x.ProductCode,

                                            x.ProductDisplayOrder
                                        })

                                        .OrderBy(x =>
                                            x.Key.ProductDisplayOrder
                                            ?? int.MaxValue)

                                        .ThenBy(x =>
                                            x.Key.ProductCode)

                                        .Select(product =>
                                            new DeliveryBoyProductDto
                                            {
                                                ProductId =
                                                    product.Key.ProductId,

                                                ProductCode =
                                                    product.Key.ProductCode,

                                                Quantity =
                                                    product.Sum(
                                                        x => x.Quantity),

                                                DisplayOrder =
                                                    product.Key
                                                        .ProductDisplayOrder
                                            })

                                        .ToList(),


                                    // ====================
                                    // OTHER PRODUCTS
                                    // ====================

                                    OtherProducts = customer

                                        .Where(x =>
                                            x.CategoryId !=
                                            Constant
                                                .ProductCategory
                                                .Milk)

                                        .GroupBy(x => new
                                        {
                                            x.ProductId,

                                            x.ProductCode,

                                            x.ProductDisplayOrder
                                        })

                                        .OrderBy(x =>
                                            x.Key.ProductDisplayOrder
                                            ?? int.MaxValue)

                                        .ThenBy(x =>
                                            x.Key.ProductCode)

                                        .Select(product =>
                                            new DeliveryBoyProductDto
                                            {
                                                ProductId =
                                                    product.Key.ProductId,

                                                ProductCode =
                                                    product.Key.ProductCode,

                                                Quantity =
                                                    product.Sum(
                                                        x => x.Quantity),

                                                DisplayOrder =
                                                    product.Key
                                                        .ProductDisplayOrder
                                            })

                                        .ToList()
                                })

                            .ToList()
                    })

                    .ToList()
            })

            .ToList();


        return result;
    }

    public async Task<byte[]> ExportDeliveryBoySheetAsync(
     DateOnly deliveryDate,
     long? areaId)
    {
        var data = await GetDeliveryBoySheetAsync(
            deliveryDate,
            areaId);

        using var workbook = new XLWorkbook();

        var areaGroups = data
            .GroupBy(x => x.AreaCode)
            .OrderBy(x => x.Key);

        foreach (var area in areaGroups)
        {
            var worksheet = workbook.Worksheets.Add(area.Key);

            BuildDeliveryBoyWorksheet(
                worksheet,
                area.ToList(),
                deliveryDate);
        }

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private static void BuildDeliveryBoyWorksheet(
     IXLWorksheet worksheet,
     List<DeliveryOrderDto> deliveryOrders,
     DateOnly deliveryDate)
    {
        // ===========================
        // Basic validation
        // ===========================

        if (deliveryOrders == null || !deliveryOrders.Any())
        {
            worksheet.Cell(1, 1).Value = "No delivery data available.";
            return;
        }

        // ===========================
        // Title
        // ===========================

        worksheet.Cell(1, 1).Value = "Delivery Boy Sheet";

        worksheet.Range(1, 1, 1, 5).Merge();

        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;

        worksheet.Cell(1, 1).Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        // ===========================
        // Information
        // ===========================

        worksheet.Cell(2, 1).Value = "Delivery Date";

        worksheet.Cell(2, 2).Value =
            deliveryDate.ToString("dd-MM-yyyy");

        worksheet.Cell(2, 4).Value = "Area";

        worksheet.Cell(2, 5).Value =
            deliveryOrders.First().AreaCode;

        worksheet.Range(2, 1, 2, 5)
            .Style.Font.Bold = true;

        // ===========================
        // Header
        // ===========================

        worksheet.Cell(4, 1).Value = "Area";
        worksheet.Cell(4, 2).Value = "Customer";
        worksheet.Cell(4, 3).Value = "Address";
        worksheet.Cell(4, 4).Value = "Milk";
        worksheet.Cell(4, 5).Value = "Other Products";

        var header = worksheet.Range(4, 1, 4, 5);

        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;

        header.Style.Fill.BackgroundColor =
            XLColor.FromHtml("#2563EB");

        header.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        header.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;

        // ===========================
        // Customer Details
        // ===========================

        var row = 5;

        foreach (var deliveryOrder in deliveryOrders)
        {
           

            // =====================================
            // Houses inside Delivery Order
            // =====================================

            foreach (var house in deliveryOrder.Houses)
            {


                // ---------------------------------
                // Customers in this house
                // ---------------------------------

                foreach (var customer in house.Customers)
                {
                    worksheet.Cell(row, 1).Value =
                        customer.AreaCode;

                    worksheet.Cell(row, 2).Value =
                        customer.CustomerName;

                    // =============================
                    // Address
                    // =============================

                    var addressCell = worksheet.Cell(row, 3);

                    addressCell.Value = customer.Address;

                    addressCell.Style.Alignment.WrapText = true;

                    addressCell.Style.Alignment.Vertical =
                        XLAlignmentVerticalValues.Top;

                    // =============================
                    // Milk Products
                    // =============================

                    worksheet.Cell(row, 4).Value =
                        string.Join(
                            Environment.NewLine,
                            customer.MilkProducts
                                .Select(x =>
                                    $"{FormatQuantity(x.Quantity)} {x.ProductCode}")
                        );

                    // =============================
                    // Other Products
                    // =============================

                    var otherProductLines =
      customer.OtherProducts
          .Select(x =>
              $"{FormatQuantity(x.Quantity)} {x.ProductCode}")
          .ToList();

                    if (!string.IsNullOrWhiteSpace(
                            customer.DeliveryNotes))
                    {
                        otherProductLines.Add(
                            customer.DeliveryNotes);
                    }

                    worksheet.Cell(row, 5).Value =
                        string.Join(
                            Environment.NewLine,
                            otherProductLines);

                    // =============================
                    // Row formatting
                    // =============================

                    var currentRow = worksheet.Row(row);

                    currentRow.Style.Alignment.WrapText = true;

                    currentRow.Style.Alignment.Vertical =
                        XLAlignmentVerticalValues.Top;

                    // =============================
                    // Set row height
                    // =============================

                    var addressLength =
                        customer.Address?.Length ?? 0;

                    var estimatedLines =
                        Math.Max(
                            1,
                            (int)Math.Ceiling(addressLength / 55.0)
                        );

                    var productLines =
                        Math.Max(
                            customer.MilkProducts.Count,
                            customer.OtherProducts.Count
                        );

                    var requiredLines =
                        Math.Max(
                            estimatedLines,
                            productLines
                        );

                    currentRow.Height =
      Math.Min(
          Math.Max(
              requiredLines * 20,
              40),
          150);

                    // =============================
                    // Highlight other products
                    // =============================

                    if (customer.OtherProducts.Any())
                    {
                        worksheet.Range(row, 1, row, 5)
                            .Style.Fill.BackgroundColor =
                                XLColor.Yellow;
                    }

                    row++;
                }
            }

            // ---------------------------------
            // Blank row between houses
            // ---------------------------------

            row++;

            // =====================================
            // Delivery Order Summary
            // =====================================

            if (deliveryOrder.GroupDeliverySheetByLocation)
            {
                var locationCustomers =
                    deliveryOrder.Houses
                        .SelectMany(x => x.Customers)
                        .ToList();

                var locationSummary =
                    locationCustomers
                        .SelectMany(x =>
                            x.MilkProducts
                                .Concat(x.OtherProducts))
                        .GroupBy(x => new
                        {
                            x.ProductCode,
                            x.DisplayOrder
                        })
                        .OrderBy(x =>
                            x.Key.DisplayOrder ?? int.MaxValue)
                        .ThenBy(x =>
                            x.Key.ProductCode)
                        .Select(x =>
                            $"{FormatQuantity(
                                x.Sum(p => p.Quantity))} {x.Key.ProductCode}");

                worksheet.Cell(row, 3).Value =
                    "Delivery Total";

                worksheet.Cell(row, 4).Value =
                    string.Join(", ", locationSummary);

                var summaryRange =
                    worksheet.Range(row, 1, row, 5);

                summaryRange.Style.Fill.BackgroundColor =
                    XLColor.Yellow;

                summaryRange.Style.Font.Bold = true;

                summaryRange.Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

                summaryRange.Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Center;

                row++;

                // Blank row between delivery orders
                row++;
            }
        }

        // ===========================
        // Loading Summary
        // ===========================

        row += 1;

        worksheet.Cell(row, 1).Value =
            "Loading Summary";

        worksheet.Range(row, 1, row, 2).Merge();

        worksheet.Cell(row, 1).Style.Font.Bold = true;

        worksheet.Cell(row, 1).Style.Font.FontSize = 13;

        row++;

        worksheet.Cell(row, 1).Value =
            "Product";

        worksheet.Cell(row, 2).Value =
            "Quantity";

        var summaryHeader =
            worksheet.Range(row, 1, row, 2);

        summaryHeader.Style.Font.Bold = true;

        summaryHeader.Style.Fill.BackgroundColor =
            XLColor.LightGray;

        row++;

        // =====================================
        // Flatten:
        //
        // DeliveryOrder
        //     -> Houses
        //         -> Customers
        //             -> Products
        // =====================================

        var allCustomers =
            deliveryOrders
                .SelectMany(x => x.Houses)
                .SelectMany(x => x.Customers)
                .ToList();

        var summary =
            allCustomers

                .SelectMany(x =>
                    x.MilkProducts
                        .Concat(x.OtherProducts))

                .GroupBy(x => new
                {
                    x.ProductCode,
                    x.DisplayOrder
                })

                .OrderBy(x =>
                    x.Key.DisplayOrder ?? int.MaxValue)

                .ThenBy(x =>
                    x.Key.ProductCode);

        foreach (var product in summary)
        {
            worksheet.Cell(row, 1).Value =
                product.Key.ProductCode;

            worksheet.Cell(row, 2).Value =
                FormatQuantity(
                    product.Sum(x => x.Quantity));

            row++;
        }

        // ===========================
        // Borders
        // ===========================

        var customerTable =
            worksheet.Range(
                4,
                1,
                row - 1,
                5);

        customerTable.Style.Border.OutsideBorder =
            XLBorderStyleValues.Thin;

        customerTable.Style.Border.InsideBorder =
            XLBorderStyleValues.Thin;

        // ===========================
        // Summary Borders
        // ===========================

        // Find Loading Summary row
        var loadingSummaryCell =
            worksheet.CellsUsed()
                .FirstOrDefault(x =>
                    x.Value.ToString() == "Loading Summary");

        if (loadingSummaryCell != null)
        {
            var summaryStartRow =
                loadingSummaryCell.Address.RowNumber;

            var summaryTable =
                worksheet.Range(
                    summaryStartRow,
                    1,
                    row - 1,
                    2);

            summaryTable.Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;

            summaryTable.Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;
        }

        // ===========================
        // Auto Filter
        // ===========================

        // Filtering is useful only for
        // actual customer header/data rows.
        worksheet.Range(
            4,
            1,
            row - 1,
            5)
            .SetAutoFilter();

        // ===========================
        // Freeze Header
        // ===========================

        worksheet.SheetView.FreezeRows(4);

        worksheet.SheetView.FreezeColumns(2);

        // ===========================
        // Column Widths
        // ===========================

        worksheet.Column(1).Width = 10;

        worksheet.Column(2).Width = 25;

        worksheet.Column(3).Width = 60;

        worksheet.Column(4).Width = 18;

        worksheet.Column(5).Width = 22;

        // ===========================
        // Auto Fit Rows
        // ===========================

        worksheet.Rows().AdjustToContents();

        // ===========================
        // Worksheet Style
        // ===========================

        worksheet.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;

        worksheet.Style.Font.FontName =
            "Calibri";

        worksheet.Style.Font.FontSize =
            12;
    }

    private static string FormatQuantity(decimal quantity)
    {
        return quantity.ToString("0.##");
    }

    public async Task<List<ExpectedDeliveryDto>> GetExpectedDeliveriesAsync(
     DateOnly deliveryDate,
     string source,
     long productId)
    {
        // ============================================================
        // Load active subscriptions
        // ============================================================

        var subscriptions = await _context.CustomerSubscriptions
            .Include(x => x.Schedules)
            .Include(x => x.Product)
            .Include(x => x.Customer)
                .ThenInclude(c => c.Area)
            .Where(x =>
                x.IsActive &&
                x.Product.IsActive &&
                x.Customer.Area.IsActive &&
                x.StartDate <= deliveryDate &&
                (x.EndDate == null ||
                 x.EndDate >= deliveryDate))
            .ToListAsync();


        // ============================================================
        // Load active customer requests applicable for this date
        // ============================================================

        var customerRequests = await _context.CustomerRequests
            .Include(x => x.Product)
            .Include(x => x.Customer)
                .ThenInclude(c => c.Area)
            .AsNoTracking()
            .Where(x =>
                x.Status != CustomerRequestStatus.Cancelled &&
                x.IsActive &&
                x.Customer.Area.IsActive &&
                x.Product.IsActive &&
                x.EffectiveFrom <= deliveryDate &&
                (x.EffectiveTo == null ||
                 x.EffectiveTo >= deliveryDate))
            .ToListAsync();


        var result = new List<ExpectedDeliveryDto>();


        // ============================================================
        // SUBSCRIPTION
        // ============================================================

        if (source.Equals(
     "subscription",
     StringComparison.OrdinalIgnoreCase) ||
     source.Equals(
         "all",
         StringComparison.OrdinalIgnoreCase))
        {
            foreach (var subscription in subscriptions)
            {
                // ----------------------------------------------------
                // Extra safety checks
                // ----------------------------------------------------

                if (!subscription.Customer.Area.IsActive)
                    continue;

                if (!subscription.Product.IsActive)
                    continue;


                // ----------------------------------------------------
                // Find Pause request
                // ----------------------------------------------------

                var pauseRequest = customerRequests.FirstOrDefault(x =>
                    x.SubscriptionId == subscription.Id &&
                    x.RequestAction == CustomerRequestAction.Pause);


                // ----------------------------------------------------
                // If paused, don't show this customer
                // ----------------------------------------------------

                if (pauseRequest != null)
                    continue;


                // ----------------------------------------------------
                // Find Replace request
                //
                // Replace is your override.
                // If Replace exists, it becomes the final delivery.
                // ----------------------------------------------------

                var replaceRequest = customerRequests.FirstOrDefault(x =>
                    x.SubscriptionId == subscription.Id &&
                    x.RequestAction == CustomerRequestAction.Replace);


                // ----------------------------------------------------
                // If Replace exists
                // ----------------------------------------------------

                if (replaceRequest != null)
                {
                    // Make sure replacement product exists
                    if (!replaceRequest.ProductId.HasValue)
                        continue;

                    if (replaceRequest.Product == null)
                        continue;

                    if (!replaceRequest.Product.IsActive)
                        continue;


                    // ------------------------------------------------
                    // Product filter
                    //
                    // User selected a product in the UI.
                    // Only show replacement if it matches.
                    // ------------------------------------------------

                    if (replaceRequest.ProductId.Value != productId)
                        continue;


                    // ------------------------------------------------
                    // Quantity
                    //
                    // Replace quantity is the final quantity.
                    // If quantity is null, fallback to subscription
                    // quantity.
                    // ------------------------------------------------

                    decimal subscriptionQuantity = GetQuantity(
                        subscription,
                        deliveryDate);

                    decimal finalQuantity =
                        replaceRequest.Quantity ??
                        subscriptionQuantity;


                    // ------------------------------------------------
                    // Add ONLY replacement
                    //
                    // IMPORTANT:
                    // We use continue so the normal subscription
                    // record is NOT added.
                    // ------------------------------------------------

                    result.Add(new ExpectedDeliveryDto
                    {
                        CustomerId =
                            subscription.CustomerId,

                        CustomerName =
                            subscription.Customer.CustomerName,

                        SubscriptionId =
                            subscription.Id,

                        ProductId =
                            replaceRequest.ProductId.Value,

                        ProductCode =
                            replaceRequest.Product.ProductCode,

                        ProductName =
                            replaceRequest.Product.ProductName,

                        Quantity =
                            finalQuantity,

                        Source = "Replace",

                        RequestId =
                            replaceRequest.Id
                    });

                    continue;
                }


                // ----------------------------------------------------
                // No Replace request
                //
                // Therefore normal subscription delivery applies.
                // ----------------------------------------------------

                // Check whether selected product matches
                // subscription product.

                if (subscription.ProductId != productId)
                    continue;


                // ----------------------------------------------------
                // Check subscription schedule
                // ----------------------------------------------------

                if (!IsDeliveryApplicable(
                    subscription,
                    deliveryDate))
                {
                    continue;
                }


                // ----------------------------------------------------
                // Get normal subscription quantity
                // ----------------------------------------------------

                decimal quantity = GetQuantity(
                    subscription,
                    deliveryDate);


                // ----------------------------------------------------
                // Add normal subscription delivery
                // ----------------------------------------------------

                result.Add(new ExpectedDeliveryDto
                {
                    CustomerId =
                        subscription.CustomerId,

                    CustomerName =
                        subscription.Customer.CustomerName,

                    SubscriptionId =
                        subscription.Id,

                    ProductId =
                        subscription.ProductId,

                    ProductCode =
                        subscription.Product.ProductCode,

                    ProductName =
                        subscription.Product.ProductName,

                    Quantity =
                        quantity,

                    Source = "Subscription",

                    RequestId = null
                });
            }
        }


        // ============================================================
        // REQUEST / ADD
        // ============================================================

        if (source.Equals(
     "request",
     StringComparison.OrdinalIgnoreCase) ||
     source.Equals(
         "all",
         StringComparison.OrdinalIgnoreCase))
        {
            var addRequests = customerRequests
                .Where(x =>
                    x.RequestAction ==
                        CustomerRequestAction.Add &&
                    x.SubscriptionId == null &&
                    x.ProductId.HasValue &&
                    x.ProductId.Value == productId)
                .ToList();


            foreach (var addRequest in addRequests)
            {
                // ----------------------------------------------------
                // Safety checks
                // ----------------------------------------------------

                if (!addRequest.Customer.Area.IsActive)
                    continue;

                if (addRequest.Product == null)
                    continue;

                if (!addRequest.Product.IsActive)
                    continue;


                // ----------------------------------------------------
                // Add request
                // ----------------------------------------------------

                result.Add(new ExpectedDeliveryDto
                {
                    CustomerId =
                        addRequest.CustomerId,

                    CustomerName =
                        addRequest.Customer.CustomerName,

                    SubscriptionId = null,

                    ProductId =
                        addRequest.ProductId!.Value,

                    ProductCode =
                        addRequest.Product.ProductCode,

                    ProductName =
                        addRequest.Product.ProductName,

                    Quantity =
                        addRequest.Quantity ?? 1,

                    Source = "Request",

                    RequestId =
                        addRequest.Id
                });
            }
        }


        // ============================================================
        // Return sorted result
        // ============================================================

        return result
            .OrderBy(x => x.CustomerName)
            .ToList();
    }
}