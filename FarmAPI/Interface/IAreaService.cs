using FarmAPI.DTOs;
using FarmAPI.Entities;
using static FarmAPI.DTOs.AreaDto;

namespace FarmAPI.Interface
{
    public interface IAreaService
    {
        Task<List<Area>> GetAllAsync();

        Task<List<DropdownDto>> GetDropdownAsync();

        Task<Area?> GetByIdAsync(long id);

        Task<Area> CreateAsync(CreateAreaRequest request);

        Task UpdateAsync(long id, UpdateAreaRequest request);

        Task DeleteAsync(long id);
    }
}
