
using FarmAPI.Data;
using FarmAPI.DTOs;
using FarmAPI.Entities;
using FarmAPI.Exceptions;
using FarmAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace FarmAPI.Services;

public class CustomerSubscriptionService : ICustomerSubscriptionService
{
    private readonly FarmDbContext _context;

    public CustomerSubscriptionService(FarmDbContext context)
    {
        _context = context;
    }



    public async Task<PagedResponse<CustomerSubscriptionListDto>> GetAllAsync(
        CustomerSubscriptionFilterDto filter)
    {
        var query = _context.CustomerSubscriptions
     .AsNoTracking()
     .Include(x => x.Customer)
     .Include(x => x.Product)
     .Include(x => x.Frequency)
     .Include(x => x.Schedules)
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

        if (filter.IsActive.HasValue)
        {
            query = query.Where(x =>
                x.IsActive == filter.IsActive.Value);
        }

        var totalRecords = await query.CountAsync();

        var subscriptions = await query
            .OrderBy(x => x.Customer.CustomerName)
            .ThenBy(x => x.Product.ProductName)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResponse<CustomerSubscriptionListDto>
        {
            Items = subscriptions
                .Select(MapToListDto)
                .ToList(),

            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalRecords = totalRecords
        };
    }





    public async Task<CustomerSubscriptionDto?> GetByIdAsync(long id)
    {
        var entity = await _context.CustomerSubscriptions
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Product)
            .Include(x => x.Frequency)
            .Include(x => x.Schedules)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
        {
            throw new ValidationException(
                "Subscription not found.");
        }

        return MapToDto(entity);
    }

   

    public async Task<long> CreateAsync(
    CreateCustomerSubscriptionDto dto)
    {
        await ValidateCustomerAsync(dto.CustomerId);

        await ValidateProductAsync(dto.ProductId);

        await ValidateFrequencyAsync(dto.FrequencyId);

        ValidateInterval(dto.FrequencyId, dto.IntervalDays);



        ValidateDates(dto.StartDate, dto.EndDate);

        ValidateSchedules(
            dto.FrequencyId,
            dto.Schedules);

        // Part 3
       

        await ValidateScheduleConflictAsync(
    null,
    dto.CustomerId,
    dto.ProductId);

        //await ValidateOverlappingSubscriptionAsync(dto);

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var entity = MapToEntity(dto);

            _context.CustomerSubscriptions.Add(entity);

            await _context.SaveChangesAsync();

            if (dto.Schedules.Any())
            {
                var schedules = dto.Schedules
    .Select(x => new SubscriptionSchedule
    {
        SubscriptionId = entity.Id,
        DayOfWeek = x.DayOfWeek,
        DayOfMonth = x.DayOfMonth,
        PatternOrder = x.PatternOrder,
        Quantity = x.Quantity,
        CreatedAt = DateTime.UtcNow
    });

                _context.SubscriptionSchedules.AddRange(schedules);

                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            return entity.Id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateAsync(
     long id,
     UpdateCustomerSubscriptionDto dto)
    {
        if (id != dto.Id)
        {
            throw new ValidationException("Invalid subscription.");
        }

        var entity = await _context.CustomerSubscriptions
            .Include(x => x.Schedules)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
        {
            throw new ValidationException("Subscription not found.");
        }

        await ValidateCustomerAsync(dto.CustomerId);

        await ValidateProductAsync(dto.ProductId);

        await ValidateFrequencyAsync(dto.FrequencyId);

        ValidateInterval(dto.FrequencyId, dto.IntervalDays);



        ValidateDates(dto.StartDate, dto.EndDate);

        ValidateSchedules(dto.FrequencyId, dto.Schedules);

        await ValidateScheduleConflictAsync(
     dto.Id,
     dto.CustomerId,
     dto.ProductId);

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            MapToEntity(dto, entity);

            _context.SubscriptionSchedules.RemoveRange(entity.Schedules);

            if (dto.Schedules.Any())
            {
                var schedules = dto.Schedules
                    .Select(x => new SubscriptionSchedule
                    {
                        SubscriptionId = entity.Id,
                        DayOfWeek = x.DayOfWeek,
                        DayOfMonth = x.DayOfMonth,
                        PatternOrder = x.PatternOrder,
                        Quantity = x.Quantity,
                        CreatedAt = DateTime.UtcNow
                    });

                _context.SubscriptionSchedules.AddRange(schedules);
            }

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await _context.CustomerSubscriptions
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
        {
            throw new ValidationException("Subscription not found.");
        }

        if (!entity.IsActive)
        {
            throw new ValidationException("Subscription is already inactive.");
        }

        entity.IsActive = false;

        await _context.SaveChangesAsync();
    }



    private static CustomerSubscriptionListDto MapToListDto(
    CustomerSubscription entity)
    {
        return new CustomerSubscriptionListDto
        {
            Id = entity.Id,
            CustomerName = entity.Customer.CustomerName,
            ProductName = entity.Product.ProductName,
            FrequencyName = entity.Frequency.FrequencyName,
            ScheduleSummary = GetScheduleSummary(entity),
            IsActive = entity.IsActive
        };
    }

    private static string GetScheduleSummary(
      CustomerSubscription entity)
    {
        switch (entity.FrequencyId)
        {
            // Daily
            case 1:

                if (!entity.Schedules.Any())
                {
                    return string.Empty;
                }

                var dailyPattern = string.Join(" → ",
                    entity.Schedules
                        .OrderBy(x => x.PatternOrder)
                        .Select(x => x.Quantity));

                return $"Daily ({dailyPattern})";

            // Weekly
            case 2:

                return string.Join(", ",
                    entity.Schedules
                        .OrderBy(x => x.DayOfWeek)
                        .Select(x =>
                            $"{GetWeekDayName(x.DayOfWeek)} ({x.Quantity})"));

            // Monthly
            case 3:

                return string.Join(", ",
                    entity.Schedules
                        .OrderBy(x => x.DayOfMonth)
                        .Select(x =>
                            $"{GetOrdinal(x.DayOfMonth!.Value)} ({x.Quantity})"));

            // Interval
            case 4:

                var interval = entity.IntervalDays ?? 1;

                var intervalPattern = string.Join(" → ",
                    entity.Schedules
                        .OrderBy(x => x.PatternOrder)
                        .Select(x => x.Quantity));

                return $"Every {interval} Days ({intervalPattern})";

            default:

                return string.Empty;
        }
    }

    private static string GetOrdinal(int number)
    {
        if (number % 100 is 11 or 12 or 13)
        {
            return $"{number}th";
        }

        return (number % 10) switch
        {
            1 => $"{number}st",
            2 => $"{number}nd",
            3 => $"{number}rd",
            _ => $"{number}th"
        };
    }

    private static string GetWeekDayName(short? dayOfWeek)
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

    private static CustomerSubscriptionDto MapToDto(
        CustomerSubscription entity)
    {
        return new CustomerSubscriptionDto
        {
            Id = entity.Id,

            CustomerId = entity.CustomerId,
            CustomerName = entity.Customer.CustomerName,

            ProductId = entity.ProductId,
            ProductName = entity.Product.ProductName,
            

            FrequencyId = entity.FrequencyId,
            FrequencyName = entity.Frequency.FrequencyName,

            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            IntervalDays = entity.IntervalDays,
            IsActive = entity.IsActive,

            Schedules = entity.Schedules
    .Select(x => new SubscriptionScheduleDto
    {
        DayOfWeek = x.DayOfWeek,
        DayOfMonth = x.DayOfMonth,
        PatternOrder = x.PatternOrder,
        Quantity = x.Quantity
    })
    .ToList(),
        };
    }

    private static CustomerSubscription MapToEntity(
        CreateCustomerSubscriptionDto dto)
    {
        return new Entities.CustomerSubscription
        {
            CustomerId = dto.CustomerId,
            ProductId = dto.ProductId,
            FrequencyId = dto.FrequencyId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IntervalDays = dto.IntervalDays,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static void MapToEntity(
        UpdateCustomerSubscriptionDto dto,
        CustomerSubscription entity)
    {
        entity.CustomerId = dto.CustomerId;
        entity.ProductId = dto.ProductId;
        entity.FrequencyId = dto.FrequencyId;
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
        entity.IntervalDays = dto.IntervalDays;
        entity.IsActive = dto.IsActive;
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

    private async Task ValidateFrequencyAsync(
    short frequencyId)
    {
        bool exists = await _context.SubscriptionFrequencies
            .AnyAsync(x =>
                x.Id == frequencyId);

        if (!exists)
        {
            throw new ValidationException(
                "Invalid subscription frequency.");
        }
    }

    

    private static void ValidateDates(
    DateOnly startDate,
    DateOnly? endDate)
    {
        if (endDate.HasValue &&
            endDate.Value < startDate)
        {
            throw new ValidationException(
                "End Date cannot be less than Start Date.");
        }
    }

    private static void ValidateSchedules(
    short frequencyId,
    List<SubscriptionScheduleDto> schedules)
    {
        if (!schedules.Any())
        {
            throw new ValidationException(
                "At least one schedule is required.");
        }

        if (schedules.Any(x => x.Quantity <= 0))
        {
            throw new ValidationException(
                "Quantity should be greater than zero.");
        }

        switch (frequencyId)
        {
            // Daily
            case 1:

                if (schedules.Any(x => !x.PatternOrder.HasValue))
                {
                    throw new ValidationException(
                        "Pattern order is required.");
                }

                if (schedules.GroupBy(x => x.PatternOrder)
                    .Any(g => g.Count() > 1))
                {
                    throw new ValidationException(
                        "Duplicate pattern order is not allowed.");
                }

                break;

            // Weekly
            case 2:

                if (schedules.Any(x => !x.DayOfWeek.HasValue))
                {
                    throw new ValidationException(
                        "Weekday is required.");
                }

                if (schedules.Any(x =>
                    x.DayOfWeek < 1 ||
                    x.DayOfWeek > 7))
                {
                    throw new ValidationException(
                        "Invalid weekday.");
                }

                foreach (var group in schedules.GroupBy(x => x.DayOfWeek))
                {
                    if (group.Count() > 1)
                    {
                        if (group.Any(x => !x.PatternOrder.HasValue))
                        {
                            throw new ValidationException(
                                $"Pattern order is required for {GetWeekDayName(group.Key)}.");
                        }

                        if (group.GroupBy(x => x.PatternOrder)
                            .Any(g => g.Count() > 1))
                        {
                            throw new ValidationException(
                                $"Duplicate pattern order is not allowed for {GetWeekDayName(group.Key)}.");
                        }
                    }
                }

                break;

            // Monthly
            case 3:

                if (schedules.Any(x => !x.DayOfMonth.HasValue))
                {
                    throw new ValidationException(
                        "Day of month is required.");
                }

                if (schedules.Any(x =>
                    x.DayOfMonth < 1 ||
                    x.DayOfMonth > 31))
                {
                    throw new ValidationException(
                        "Invalid day of month.");
                }

                foreach (var group in schedules.GroupBy(x => x.DayOfMonth))
                {
                    if (group.Count() > 1)
                    {
                        if (group.Any(x => !x.PatternOrder.HasValue))
                        {
                            throw new ValidationException(
                                $"Pattern order is required for day {group.Key}.");
                        }

                        if (group.GroupBy(x => x.PatternOrder)
                            .Any(g => g.Count() > 1))
                        {
                            throw new ValidationException(
                                $"Duplicate pattern order is not allowed for day {group.Key}.");
                        }
                    }
                }

                break;

            // Interval
            case 4:

                if (schedules.Any(x => !x.PatternOrder.HasValue))
                {
                    throw new ValidationException(
                        "Pattern order is required.");
                }

                if (schedules.GroupBy(x => x.PatternOrder)
                    .Any(g => g.Count() > 1))
                {
                    throw new ValidationException(
                        "Duplicate pattern order is not allowed.");
                }

                break;

            default:

                throw new ValidationException(
                    "Invalid frequency.");
        }
    }

    private async Task ValidateScheduleConflictAsync(
    long? subscriptionId,
    long customerId,
    long productId)
    {
        bool exists = await _context.CustomerSubscriptions
            .AnyAsync(x =>
                x.Id != subscriptionId &&
                x.CustomerId == customerId &&
                x.ProductId == productId &&
                x.IsActive);

        if (exists)
        {
            throw new ValidationException(
                "An active subscription already exists for this customer and product.");
        }
    }

    private static void ValidateInterval(short frequencyId, short? intervalDays)
    {
        if (frequencyId == 4)
        {
            if (!intervalDays.HasValue || intervalDays <= 0)
                throw new ValidationException("Interval days should be greater than zero.");
        }
        else if (intervalDays.HasValue)
        {
            throw new ValidationException("Interval days is only applicable for Interval frequency.");
        }
    }
}

    
