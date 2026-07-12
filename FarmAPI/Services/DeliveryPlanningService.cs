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
            // Delete existing deliveries
            await _context.DeliveryDetails
    .Where(x => x.DeliveryDate == request.DeliveryDate)
    .ExecuteDeleteAsync();

            // Load active subscriptions
            var subscriptions = await _context.CustomerSubscriptions
                .Include(x => x.Schedules)
                .Where(x =>
                    x.IsActive &&
                    x.StartDate <= request.DeliveryDate &&
                    (x.EndDate == null || x.EndDate >= request.DeliveryDate))
                .ToListAsync();

            // Load prices once
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

            var customerRequests = await _context.CustomerRequests
    .AsNoTracking()
    .Where(x =>
        x.Status == CustomerRequestStatus.Pending &&
        x.IsActive &&
        x.EffectiveFrom <= request.DeliveryDate &&
        (x.EffectiveTo == null ||
         x.EffectiveTo >= request.DeliveryDate))
    .ToListAsync();

            List<DeliveryDetail> deliveryDetails = new();
            HashSet<long> processedRequestIds = new();

            foreach (var subscription in subscriptions)
            {
                if (!IsDeliveryApplicable(subscription, request.DeliveryDate))
                    continue;
              
                decimal quantity = GetQuantity(
                    subscription,
                    request.DeliveryDate);
              

                var pauseRequest = customerRequests.FirstOrDefault(x =>
                    x.SubscriptionId == subscription.Id &&
                    x.RequestAction == CustomerRequestAction.Pause);

                var replaceRequest = customerRequests.FirstOrDefault(x =>
                    x.SubscriptionId == subscription.Id &&
                    x.RequestAction == CustomerRequestAction.Replace);

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

            var addRequests = customerRequests
                .Where(x =>
                x.RequestAction == CustomerRequestAction.Add &&
                x.SubscriptionId == null)
                .ToList();

            foreach (var addRequest in addRequests)
            {
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



            if (deliveryDetails.Any())
            {
                await _context.DeliveryDetails.AddRangeAsync(deliveryDetails);
            }

           
            if (processedRequestIds.Any())
            {
                var requests = await _context.CustomerRequests
                    .Where(x => processedRequestIds.Contains(x.Id))
                    .ToListAsync();

                foreach (var customerRequest in requests)
                {
                    if (customerRequest.EffectiveFrom <= request.DeliveryDate)
                    {
                        customerRequest.Status = CustomerRequestStatus.InProgress;
                    }

                    if (customerRequest.EffectiveTo.HasValue &&
                        customerRequest.EffectiveTo.Value <= request.DeliveryDate)
                    {
                        customerRequest.Status = CustomerRequestStatus.Processed;
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

            CustomerId = customerId,

            SubscriptionId = subscriptionId,

            RequestId = requestId,

            ProductId = productId,

            PlannedQty = quantity,

            DeliveredQty = quantity,

            UnitPrice = unitPrice,

            Status = CustomerDeliveryStatus.Delivered,

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


    public async Task<List<DeliveryBoySheetDto>> GetDeliveryBoySheetAsync(
    DateOnly deliveryDate,
    long? areaId)
    {
        var query = _context.DeliveryDetails
            .AsNoTracking()
            .Where(x => x.DeliveryDate == deliveryDate);

        if (areaId.HasValue)
        {
            query = query.Where(x => x.Customer.AreaId == areaId.Value);
        }

        var data = await query
            .Select(x => new
            {
                CustomerId = x.CustomerId,

                CustomerName = x.Customer.CustomerName,

                AreaCode = x.Customer.Area.AreaCode,

                DeliveryLocation = x.Customer.DeliveryLocation.LocationName,

                DeliveryLocationAddress = x.Customer.DeliveryLocation.Address,

                DeliveryOrder = x.Customer.DeliveryLocation.DeliveryOrder,

                HouseDoorNo = x.Customer.HouseDoorNo,

                ProductId = x.ProductId,

                ProductCode = x.Product.ProductCode,

                ProductDisplayOrder = x.Product.DisplayOrder,

                CategoryId = x.Product.CategoryId,

                Quantity = x.PlannedQty
            })
            .ToListAsync();

        var result = data

            .GroupBy(x => new
            {
                x.CustomerId,
                x.CustomerName,
                x.AreaCode,
                x.DeliveryLocation,
                x.DeliveryLocationAddress,
                x.DeliveryOrder,
                x.HouseDoorNo
            })

            .OrderBy(x => x.Key.AreaCode)
            .ThenBy(x => x.Key.DeliveryOrder)
            .ThenBy(x => x.Key.CustomerName)

            .Select(customer => new DeliveryBoySheetDto
            {
                CustomerId = customer.Key.CustomerId,

                AreaCode = customer.Key.AreaCode,

                CustomerName = customer.Key.CustomerName,

                Address = string.Join(", ",
                    new[]
                    {
                    customer.Key.DeliveryLocation,
                    customer.Key.DeliveryLocationAddress,
                    customer.Key.HouseDoorNo
                    }
                    .Where(x => !string.IsNullOrWhiteSpace(x))),

                MilkProducts = customer

                    .Where(x => x.CategoryId == Constant.ProductCategory.Milk)

                    .GroupBy(x => new
                    {
                        x.ProductId,
                        x.ProductCode,
                        x.ProductDisplayOrder
                    })

                    .OrderBy(x => x.Key.ProductDisplayOrder ?? int.MaxValue)
                    .ThenBy(x => x.Key.ProductCode)

                    .Select(product => new DeliveryBoyProductDto
                    {
                        ProductId = product.Key.ProductId,

                        ProductCode = product.Key.ProductCode,

                        Quantity = product.Sum(x => x.Quantity),

                        DisplayOrder = product.Key.ProductDisplayOrder
                    })

                    .ToList(),

                OtherProducts = customer

                    .Where(x => x.CategoryId != Constant.ProductCategory.Milk)

                    .GroupBy(x => new
                    {
                        x.ProductId,
                        x.ProductCode,
                        x.ProductDisplayOrder
                    })

                    .OrderBy(x => x.Key.ProductDisplayOrder ?? int.MaxValue)
                    .ThenBy(x => x.Key.ProductCode)

                   .Select(product => new DeliveryBoyProductDto
                   {
                       ProductId = product.Key.ProductId,

                       ProductCode = product.Key.ProductCode,

                       Quantity = product.Sum(x => x.Quantity),

                       DisplayOrder = product.Key.ProductDisplayOrder
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
     List<DeliveryBoySheetDto> customers,
     DateOnly deliveryDate)
    {
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
            customers.First().AreaCode;

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

        foreach (var customer in customers)
        {
            worksheet.Cell(row, 1).Value =
                customer.AreaCode;

            worksheet.Cell(row, 2).Value =
                customer.CustomerName;

            worksheet.Cell(row, 3).Value =
                customer.Address;

            worksheet.Cell(row, 4).Value =
                string.Join(
                    Environment.NewLine,
                    customer.MilkProducts.Select(x =>
                        $"{x.Quantity} {x.ProductCode}"));

            worksheet.Cell(row, 5).Value =
                string.Join(
                    Environment.NewLine,
                    customer.OtherProducts.Select(x =>
                        $"{x.Quantity} {x.ProductCode}"));

            worksheet.Row(row)
                .Style.Alignment.WrapText = true;

            worksheet.Row(row)
                .Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Top;

            if (customer.OtherProducts.Any())
            {
                worksheet.Range(row, 1, row, 5)
                    .Style.Fill.BackgroundColor =
                    XLColor.LightYellow;
            }

            row++;
        }
        // ===========================
        // Loading Summary
        // ===========================

        row += 2;

        worksheet.Cell(row, 1).Value = "Loading Summary";

        worksheet.Range(row, 1, row, 2).Merge();

        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Font.FontSize = 13;

        row++;

        worksheet.Cell(row, 1).Value = "Product";
        worksheet.Cell(row, 2).Value = "Quantity";

        var summaryHeader = worksheet.Range(row, 1, row, 2);

        summaryHeader.Style.Font.Bold = true;
        summaryHeader.Style.Fill.BackgroundColor = XLColor.LightGray;

        row++;

        var summary = customers

            .SelectMany(x => x.MilkProducts.Concat(x.OtherProducts))

            .GroupBy(x => new
            {
                x.ProductCode,
                x.DisplayOrder
            })

            .OrderBy(x => x.Key.DisplayOrder ?? int.MaxValue)

            .ThenBy(x => x.Key.ProductCode);

        foreach (var product in summary)
        {
            worksheet.Cell(row, 1).Value =
                product.Key.ProductCode;

            worksheet.Cell(row, 2).Value =
                product.Sum(x => x.Quantity);

            row++;
        }

        // ===========================
        // Borders
        // ===========================

        var customerTable = worksheet.Range(
            4,
            1,
            customers.Count + 4,
            5);

        customerTable.Style.Border.OutsideBorder =
            XLBorderStyleValues.Thin;

        customerTable.Style.Border.InsideBorder =
            XLBorderStyleValues.Thin;

        var summaryTable = worksheet.Range(
            customers.Count + 7,
            1,
            row - 1,
            2);

        summaryTable.Style.Border.OutsideBorder =
            XLBorderStyleValues.Thin;

        summaryTable.Style.Border.InsideBorder =
            XLBorderStyleValues.Thin;

        // ===========================
        // Auto Filter
        // ===========================

        customerTable.SetAutoFilter();

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

        worksheet.Column(3).Width = 45;

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

        worksheet.Style.Font.FontName = "Calibri";

        worksheet.Style.Font.FontSize = 11;
    }
}