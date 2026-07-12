
using FarmAPI.DTOs;

namespace FarmAPI.Services.Interfaces;

public interface ISubscriptionFrequencyService
{
    Task<List<SubscriptionFrequencyDto>> GetAllAsync();

    Task<List<DropdownDto>> GetDropdownAsync();
}