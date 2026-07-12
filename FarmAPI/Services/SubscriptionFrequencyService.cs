using FarmAPI.Data;
using FarmAPI.DTOs;
using FarmAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FarmAPI.Services;

public class SubscriptionFrequencyService : ISubscriptionFrequencyService
{
    private readonly FarmDbContext _context;

    public SubscriptionFrequencyService(
        FarmDbContext context)
    {
        _context = context;
    }

    public async Task<List<SubscriptionFrequencyDto>> GetAllAsync()
    {
        return await _context.SubscriptionFrequencies
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new SubscriptionFrequencyDto
            {
                Id = x.Id,
                FrequencyName = x.FrequencyName
            })
            .ToListAsync();
    }

    public async Task<List<DropdownDto>> GetDropdownAsync()
    {
        return await _context.SubscriptionFrequencies
            //.Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .Select(x => new DropdownDto
            {
                Id = x.Id,
                Name = x.FrequencyName
            })
            .ToListAsync();
    }
}