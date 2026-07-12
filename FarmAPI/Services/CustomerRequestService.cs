using FarmAPI.Data;
using FarmAPI.DTOs;
using FarmAPI.Entities;
using FarmAPI.Exceptions;
using FarmAPI.Interface;
using FarmAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using static FarmAPI.DTOs.CustomerRequestDto;
using static FarmAPI.Utils.Constant;



namespace FarmAPI.Services;

public class CustomerRequestService : ICustomerRequestService
{
    private readonly FarmDbContext _context;

    public CustomerRequestService(FarmDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<CustomerRequestListDto>> GetAllAsync(
        CustomerRequestFilterDto filter)
    {
        var query = _context.CustomerRequests
    .AsNoTracking()
    .Include(x => x.Customer)
    .Include(x => x.Product)
    .Where(x => x.IsActive)
    .AsQueryable();

        if (filter.CustomerId.HasValue)
        {
            query = query.Where(x =>
                x.CustomerId == filter.CustomerId.Value);
        }

        if (filter.ProductId.HasValue)
        {
            query = query.Where(x =>
                x.ProductId == filter.ProductId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.RequestAction))
        {
            query = query.Where(x =>
                x.RequestAction == filter.RequestAction);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(x =>
                x.Status == filter.Status);
        }

        if (filter.RequestDate.HasValue)
        {
            var requestDate = filter.RequestDate.Value;

            query = query.Where(x =>
                x.EffectiveFrom <= requestDate &&
                (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= requestDate));
        }


        var totalRecords = await query.CountAsync();

        var requests = await query
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenBy(x => x.Customer.CustomerName)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize) 
            .ToListAsync();

        var canEdit =
        filter.RequestDate.HasValue &&
        filter.RequestDate.Value > DateOnly.FromDateTime(DateTime.Today);

        return new PagedResponse<CustomerRequestListDto>
        {
            Items = requests
                 .Select(x =>
                 {
                     var dto = MapToListDto(x);
                     dto.CanEdit = (x.Status == CustomerRequestStatus.Pending 
                     || x.Status ==  CustomerRequestStatus.InProgress) && canEdit;
                     return dto;
                 })
        .ToList(),

            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<CustomerRequestResponseDto?> GetByIdAsync(long id)
    {
        var entity = await _context.CustomerRequests
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
        {
            throw new ValidationException(
                "Customer request not found.");
        }

        return MapToDto(entity);
    }

    private static CustomerRequestListDto MapToListDto(
        CustomerRequest entity)
    {
        return new CustomerRequestListDto
        {
            Id = entity.Id,

            CustomerId = entity.CustomerId,

            CustomerName = entity.Customer.CustomerName,

            RequestAction = entity.RequestAction,

            ProductId = entity.ProductId,

            ProductName = entity.Product?.ProductName,

            Quantity = entity.Quantity,

            EffectiveFrom = entity.EffectiveFrom,

            EffectiveTo = entity.EffectiveTo,

            IsActive = entity.IsActive,

            Status = entity.Status,

            RequestDescription = GetRequestDescription(entity)
        };
    }

    private static CustomerRequestResponseDto MapToDto(
        CustomerRequest entity)
    {
        return new CustomerRequestResponseDto
        {
            Id = entity.Id,

            CustomerId = entity.CustomerId,

            CustomerName = entity.Customer.CustomerName,

            RequestAction = entity.RequestAction,

            ProductId = entity.ProductId,

            ProductName = entity.Product?.ProductName,

            Quantity = entity.Quantity,

            EffectiveFrom = entity.EffectiveFrom,

            EffectiveTo = entity.EffectiveTo,

            Remarks = entity.Remarks,

            Status= entity.Status,

            IsActive = entity.IsActive
        };
    }

    private static string GetRequestDescription(
        CustomerRequest entity)
    {
        var productName = entity.Product?.ProductName ?? "All Products";

        return entity.RequestAction.ToUpper() switch
        {
            "ADD" =>
                $"Add - {productName} x {entity.Quantity}",

            "PAUSE" =>
                $"Pause - {productName}",

            "REPLACE" =>
                $"Replace - {productName} x {entity.Quantity}",

            _ =>
                entity.RequestAction
        };
    }

    public async Task<CustomerRequestLookupDto> GetCustomerRequestLookupAsync(
    long customerId, DateOnly deliveryDate)
    {
        
        var customer = await _context.Customers
            .AsNoTracking()
            .Where(x => x.Id == customerId && x.IsActive)
            .Select(x => new CustomerLookupDto
            {
                Id = x.Id,
                CustomerName = x.CustomerName
            })
            .FirstOrDefaultAsync();

        if (customer == null)
        {
            throw new ValidationException(
                "Customer not found.");
        }

        var subscriptions = await _context.CustomerSubscriptions
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.Frequency)
            .Include(x => x.Schedules)
            .Where(x =>
                x.CustomerId == customerId &&
                x.IsActive &&
                x.StartDate <= deliveryDate &&
                (x.EndDate == null || x.EndDate >= deliveryDate))
            .OrderBy(x => x.Product.ProductName)
            .ToListAsync();

        var requests = await _context.CustomerRequests
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x =>
                x.CustomerId == customerId &&
                x.IsActive &&
                (x.Status == CustomerRequestStatus.Pending
                     || x.Status == CustomerRequestStatus.InProgress
                     || x.Status == CustomerRequestStatus.Processed) &&
                x.EffectiveFrom <= deliveryDate &&
                (x.EffectiveTo == null || x.EffectiveTo >= deliveryDate))
            .OrderByDescending(x => x.EffectiveFrom)
            .ToListAsync();

        return new CustomerRequestLookupDto
        {
            Customer = customer,

            Subscriptions = subscriptions
    .Select(subscription =>
    {
        var pendingRequest = requests.FirstOrDefault(r =>
            r.SubscriptionId == subscription.Id &&
            (r.RequestAction == CustomerRequestAction.Pause ||
             r.RequestAction == CustomerRequestAction.Replace));

        return new CustomerSubscriptionLookupDto
        {
            SubscriptionId = subscription.Id,
            ProductId = subscription.ProductId,
            ProductName = subscription.Product.ProductName,

            FrequencyId = subscription.FrequencyId,
            FrequencyName = subscription.Frequency.FrequencyName,
            IntervalDays = subscription.IntervalDays,

            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,

            Quantity = subscription.Schedules.Sum(x => x.Quantity),

            IsActive = subscription.IsActive,

            ScheduleDescription = GetScheduleDescription(subscription),

            HasPendingRequest = pendingRequest != null,
            PendingRequestAction = pendingRequest?.RequestAction
        };
    })
    .ToList(),

            Requests = requests
                .Select(x => new ActiveCustomerRequestDto
                {
                    Id = x.Id,
                    RequestAction = x.RequestAction,
                    ProductId = x.ProductId,
                    ProductName = x.Product?.ProductName,
                    Quantity = x.Quantity,
                    EffectiveFrom = x.EffectiveFrom,
                    EffectiveTo = x.EffectiveTo,
                    RequestDescription = GetRequestDescription(x),
                    Status = x.Status,
                    SubscriptionId = x.SubscriptionId
                })
                .ToList()
        };
    }

    public async Task<long> CreateAsync(
        CreateCustomerRequestDto dto)
    {
        await ValidateCustomerAsync(dto.CustomerId);

        if (dto.ProductId.HasValue)
        {
            await ValidateProductAsync(dto.ProductId.Value);
        }

        if (dto.SubscriptionId.HasValue)
        {
            await ValidateSubscriptionAsync(
                dto.SubscriptionId.Value,
                dto.CustomerId);
        }

        ValidateDates(
            dto.EffectiveFrom,
            dto.EffectiveTo);

      

        var result = await ValidateSubscriptionRequestAsync(
     dto.RequestAction,
     dto.SubscriptionId,
     dto.EffectiveFrom,
     dto.EffectiveTo);

        dto.RequestAction = result.RequestAction;
        dto.SubscriptionId = result.SubscriptionId;


        await ValidateRequestAsync(
  null,
  dto.CustomerId,
  dto.RequestAction,
  dto.ProductId,
  dto.Quantity,
  dto.EffectiveFrom,
  dto.EffectiveTo);

        var entity = MapToEntity(dto);

        _context.CustomerRequests.Add(entity);

        await _context.SaveChangesAsync();

        return entity.Id;
    }

    private static CustomerRequest MapToEntity(
        CreateCustomerRequestDto dto)
    {
        return new CustomerRequest
        {
            CustomerId = dto.CustomerId,

            RequestAction = dto.RequestAction.Trim().ToUpper(),

            ProductId = dto.ProductId,

            Quantity = dto.Quantity,

            EffectiveFrom = dto.EffectiveFrom,

            EffectiveTo = dto.EffectiveTo,

            Remarks = dto.Remarks,

            IsActive = true,

            Status = CustomerRequestStatus.Pending,

            SubscriptionId = dto.SubscriptionId,

            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task UpdateAsync(
    long id,
    UpdateCustomerRequestDto dto)
    {
        if (id != dto.Id)
        {
            throw new ValidationException(
                "Invalid customer request.");
        }

        var entity = await _context.CustomerRequests
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
        {
            throw new ValidationException(
                "Customer request not found.");
        }

        await ValidateCustomerAsync(dto.CustomerId);

        if (dto.ProductId.HasValue)
        {
            await ValidateProductAsync(dto.ProductId.Value);
        }

        if (dto.SubscriptionId.HasValue)
        {
            await ValidateSubscriptionAsync(
                dto.SubscriptionId.Value,
                dto.CustomerId);
        }

        ValidateDates(
            dto.EffectiveFrom,
            dto.EffectiveTo);

    

        var result = await ValidateSubscriptionRequestAsync(
    dto.RequestAction,
    dto.SubscriptionId,
    dto.EffectiveFrom,
    dto.EffectiveTo);

        dto.RequestAction = result.RequestAction;
        dto.SubscriptionId = result.SubscriptionId;


        await ValidateRequestAsync(
 dto.Id,
 dto.CustomerId,
 dto.RequestAction,
 dto.ProductId,
 dto.Quantity,
 dto.EffectiveFrom,
 dto.EffectiveTo);

        if (dto.Status != CustomerRequestStatus.Pending &&
    dto.Status != CustomerRequestStatus.Cancelled)
        {
            throw new ValidationException(
                "Invalid Status.");
        }

        entity.CustomerId = dto.CustomerId;
        entity.RequestAction = dto.RequestAction.Trim().ToUpper();
        entity.ProductId = dto.ProductId;
        entity.Quantity = dto.Quantity;
        entity.EffectiveFrom = dto.EffectiveFrom;
        entity.EffectiveTo = dto.EffectiveTo;
        entity.Remarks = dto.Remarks;
        entity.IsActive = dto.IsActive;
        entity.Status = dto.Status;
        entity.SubscriptionId = dto.SubscriptionId;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await _context.CustomerRequests
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
        {
            throw new ValidationException(
                "Customer request not found.");
        }

        if (!entity.IsActive)
        {
            throw new ValidationException(
                "Customer request is already inactive.");
        }

        entity.IsActive = false;

        await _context.SaveChangesAsync();
    }

    private async Task ValidateCustomerAsync(
        long customerId)
    {
        bool exists = await _context.Customers
            .AnyAsync(x =>
                x.Id == customerId &&
                x.IsActive);

        if (!exists)
        {
            throw new ValidationException(
                "Customer does not exist.");
        }
    }

    private async Task ValidateProductAsync(
        long productId)
    {
        bool exists = await _context.Products
            .AnyAsync(x =>
                x.Id == productId &&
                x.IsActive);

        if (!exists)
        {
            throw new ValidationException(
                "Product does not exist.");
        }
    }

    private async Task ValidateSubscriptionAsync(
    long subscriptionId,
    long customerId)
    {
        var exists = await _context.CustomerSubscriptions
            .AnyAsync(x =>
                x.Id == subscriptionId &&
                x.CustomerId == customerId &&
                x.IsActive);

        if (!exists)
        {
            throw new ValidationException(
                "Invalid subscription.");
        }
    }

    private static void ValidateDates(
        DateOnly effectiveFrom,
        DateOnly? effectiveTo)
    {
        if (effectiveTo.HasValue &&
            effectiveTo.Value < effectiveFrom)
        {
            throw new ValidationException(
                "Effective To Date cannot be less than Effective From Date.");
        }
    }

    private async Task ValidateRequestAsync(
    long? requestId,
    long customerId,
    string requestAction,
    long? productId,
    decimal? quantity,
    DateOnly effectiveFrom,
    DateOnly? effectiveTo)
    {
        // validation
    
        if (string.IsNullOrWhiteSpace(requestAction))
        {
            throw new ValidationException(
                "Request Action is required.");
        }

        switch (requestAction.Trim().ToUpper())
        {
            case "ADD":

                if (!productId.HasValue)
                {
                    throw new ValidationException(
                        "Product is required.");
                }

                if (!quantity.HasValue || quantity <= 0)
                {
                    throw new ValidationException(
                        "Quantity should be greater than zero.");
                }

                await ValidateAddRequestAsync(
                    customerId,
                    productId.Value,
                    effectiveFrom,
                    effectiveTo);

                break;

            case "PAUSE":

                break;

            case "REPLACE":

                if (!productId.HasValue)
                {
                    throw new ValidationException(
                        "Modified product is required.");
                }

                if (!quantity.HasValue ||
                    quantity <= 0)
                {
                    throw new ValidationException(
                        "Quantity should be greater than zero.");
                }

                break;

            default:

                throw new ValidationException(
                    "Invalid Request Action.");
        }

        bool exists = await _context.CustomerRequests
            .AnyAsync(x =>
                x.Id != requestId &&
                x.CustomerId == customerId &&
                x.IsActive &&
                x.RequestAction == requestAction &&
                x.ProductId == productId &&
                (
                    effectiveTo == null ||
                    x.EffectiveFrom <= effectiveTo
                ) &&
                (
                    x.EffectiveTo == null ||
                    x.EffectiveTo >= effectiveFrom
                ));

        if (exists)
        {
            throw new ValidationException(
                "Another active request already exists for the selected period.");
        }
    }


    private async Task ValidateAddRequestAsync(
    long customerId,
    long productId,
    DateOnly effectiveFrom,
    DateOnly? effectiveTo)
    {
        var subscription = await _context.CustomerSubscriptions
            .AsNoTracking()
            .Include(x => x.Schedules)
            .FirstOrDefaultAsync(x =>
                x.CustomerId == customerId &&
                x.ProductId == productId &&
                x.IsActive);

        if (subscription == null)
        {
            return;
        }

        var toDate = effectiveTo ?? effectiveFrom;

        for (var date = effectiveFrom;
             date <= toDate;
             date = date.AddDays(1))
        {
            if (IsSubscriptionScheduled(subscription, date))
            {
                throw new ValidationException(
                    "Customer already has this product scheduled for the selected period. Use Replace instead.");
            }
        }
    }

    private static string GetScheduleDescription(CustomerSubscription subscription)
    {
        switch (subscription.FrequencyId)
        {
            case 1: // Daily
                {
                    var quantities = subscription.Schedules
                        .OrderBy(x => x.PatternOrder)
                        .Select(x => x.Quantity.ToString("0.##"));

                    return $"Daily ({string.Join(" → ", quantities)})";
                }

            case 2: // Weekly
                {
                    var days = subscription.Schedules
                        .OrderBy(x => x.DayOfWeek)
                        .Select(x => $"{GetDayShortName(x.DayOfWeek)} ({x.Quantity:0.##})");

                    return string.Join(", ", days);
                }

            case 3: // Monthly
                {
                    var dates = subscription.Schedules
                        .OrderBy(x => x.DayOfMonth)
                        .Select(x => $"{GetOrdinal(x.DayOfMonth!.Value)} ({x.Quantity:0.##})");

                    return string.Join(", ", dates);
                }

            case 4: // Interval
                {
                    var quantities = subscription.Schedules
                        .OrderBy(x => x.PatternOrder)
                        .Select(x => x.Quantity.ToString("0.##"));

                    return $"Every {subscription.IntervalDays} Day{(subscription.IntervalDays > 1 ? "s" : "")} ({string.Join(" → ", quantities)})";
                }

            default:
                return string.Empty;
        }
    }

    private static string GetDayShortName(short? dayOfWeek)
    {
        return dayOfWeek switch
        {
            1 => "Mon",
            2 => "Tue",
            3 => "Wed",
            4 => "Thu",
            5 => "Fri",
            6 => "Sat",
            7 => "Sun",
            _ => string.Empty
        };
    }

    private static string GetOrdinal(int day)
    {
        if (day % 100 is 11 or 12 or 13)
            return $"{day}th";

        return (day % 10) switch
        {
            1 => $"{day}st",
            2 => $"{day}nd",
            3 => $"{day}rd",
            _ => $"{day}th"
        };
    }

    private async Task<(string RequestAction, long? SubscriptionId)> ValidateSubscriptionRequestAsync(
    string requestAction,
    long? subscriptionId,
    DateOnly effectiveFrom,
    DateOnly? effectiveTo)
    {
        requestAction = requestAction.Trim().ToUpper();

        // Only applicable for subscription-based requests.
        if (requestAction != "PAUSE" &&
            requestAction != "REPLACE")
        {
            return (requestAction, subscriptionId);
        }

        if (!subscriptionId.HasValue)
        {
            throw new ValidationException(
                "Subscription is required.");
        }

        var subscription = await _context.CustomerSubscriptions
            .AsNoTracking()
            .Include(x => x.Schedules)
            .FirstOrDefaultAsync(x =>
                x.Id == subscriptionId.Value &&
                x.IsActive);

        if (subscription == null)
        {
            throw new ValidationException(
                "Subscription not found.");
        }

        bool hasSubscriptionDay = false;
        bool hasNonSubscriptionDay = false;

        var toDate = effectiveTo ?? effectiveFrom;

        for (var date = effectiveFrom;
             date <= toDate;
             date = date.AddDays(1))
        {
            if (IsSubscriptionScheduled(subscription, date))
            {
                hasSubscriptionDay = true;
            }
            else
            {
                hasNonSubscriptionDay = true;
            }
        }

        // -------------------------------
        // PAUSE
        // -------------------------------
        if (requestAction == "PAUSE")
        {
            if (!hasSubscriptionDay)
            {
                throw new ValidationException(
                    "There are no subscription deliveries in the selected period to pause.");
            }

            return (requestAction, subscriptionId);
        }

        // -------------------------------
        // REPLACE
        // -------------------------------

        // Mixed range (subscription + non-subscription)
        if (hasSubscriptionDay && hasNonSubscriptionDay)
        {
            throw new ValidationException(
                "The selected period contains both subscription and non-subscription delivery days. Please create separate requests.");
        }

        // No subscription deliveries.
        // Treat as ADD.
        if (!hasSubscriptionDay)
        {
            return ("ADD", null);
        }

        return (requestAction, subscriptionId);
    }

    private static bool IsSubscriptionScheduled(
    CustomerSubscription subscription,
    DateOnly deliveryDate)
    {
        switch (subscription.FrequencyId)
        {
            case 1: // Daily
                return true;

            case 2: // Weekly
                {
                    int dayOfWeek = deliveryDate.DayOfWeek == DayOfWeek.Sunday
                        ? 7
                        : (int)deliveryDate.DayOfWeek;

                    return subscription.Schedules.Any(x =>
                        x.DayOfWeek == dayOfWeek);
                }

            case 3: // Monthly
                return subscription.Schedules.Any(x =>
                    x.DayOfMonth == deliveryDate.Day);

            case 4: // Interval
                    // TODO: Use your existing interval calculation here
                return IsIntervalScheduled(subscription, deliveryDate);

            default:
                return false;
        }       
}

    private static bool IsIntervalScheduled(
   CustomerSubscription subscription,
   DateOnly deliveryDate)
    {
        if (!subscription.IntervalDays.HasValue)
        {
            return false;
        }

        // Before subscription starts
        if (deliveryDate < subscription.StartDate)
        {
            return false;
        }

        // After subscription ends
        if (subscription.EndDate.HasValue &&
            deliveryDate > subscription.EndDate.Value)
        {
            return false;
        }

        var days = deliveryDate.DayNumber - subscription.StartDate.DayNumber;

        return days % subscription.IntervalDays.Value == 0;
    }
}