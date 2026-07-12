using FarmAPI.Data;
using FarmAPI.Entities;
using FarmAPI.Interface;
using Microsoft.EntityFrameworkCore;
using static FarmAPI.DTOs.AreaDto;

namespace FarmAPI.Services
{
    public class AreaService : IAreaService
    {
        private readonly FarmDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public AreaService(
            FarmDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<List<Area>> GetAllAsync()
        {
            return await _context.Areas
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<Area?> GetByIdAsync(long id)
        {
            var area = await _context.Areas
                .FirstOrDefaultAsync(x => x.Id == id);

            if (area == null)
                throw new Exception("Area not found.");

            return area;
        }

        public async Task<Area> CreateAsync(
            CreateAreaRequest request)
        {
            var exists = await _context.Areas.AnyAsync(x =>
                x.AreaCode == request.AreaCode ||
                x.AreaName == request.AreaName);

            if (exists)
                throw new Exception("Area already exists.");

            var area = new Area
            {
                AreaCode = request.AreaCode.Trim(),
                AreaName = request.AreaName.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId
            };

            _context.Areas.Add(area);

            await _context.SaveChangesAsync();

            return area;
        }

        public async Task UpdateAsync(
            long id,
            UpdateAreaRequest request)
        {
            var area = await _context.Areas
                .FirstOrDefaultAsync(x => x.Id == id);

            if (area == null)
                throw new Exception("Area not found.");

            var duplicate = await _context.Areas.AnyAsync(x =>
                x.Id != id &&
                (x.AreaCode == request.AreaCode ||
                 x.AreaName == request.AreaName));

            if (duplicate)
                throw new Exception("Area code or area name already exists.");

            area.AreaCode = request.AreaCode.Trim();
            area.AreaName = request.AreaName.Trim();
            area.IsActive = request.IsActive;
            area.UpdatedAt = DateTime.UtcNow;
            area.UpdatedBy = _currentUser.UserId;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var area = await _context.Areas
                .FirstOrDefaultAsync(x => x.Id == id);

            if (area == null)
                throw new Exception("Area not found.");

            area.IsActive = false;
            area.UpdatedAt = DateTime.UtcNow;
            area.UpdatedBy = _currentUser.UserId;

            await _context.SaveChangesAsync();
        }
    }
}
