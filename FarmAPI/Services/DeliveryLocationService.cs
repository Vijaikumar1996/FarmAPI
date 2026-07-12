using FarmAPI.Data;
using FarmAPI.Entities;
using FarmAPI.Interface;
using Microsoft.EntityFrameworkCore;
using static FarmAPI.DTOs.DeliveryLocationDto;

namespace FarmAPI.Services
{
    public class DeliveryLocationService : IDeliveryLocationService
    {
        private readonly FarmDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DeliveryLocationService(
            FarmDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<List<DeliveryLocationResponse>> GetAllAsync()
        {
            return await _context.DeliveryLocations
                .Include(x => x.Area)
                .OrderBy(x => x.Area!.AreaName)
                .ThenBy(x => x.DeliveryOrder)
                .Select(x => new DeliveryLocationResponse
                {
                    Id = x.Id,
                    AreaId = x.AreaId,
                    AreaCode = x.Area!.AreaCode,
                    AreaName = x.Area.AreaName,
                    LocationName = x.LocationName,
                    DeliveryOrder = x.DeliveryOrder,
                    Address = x.Address,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<DeliveryLocation?> GetByIdAsync(long id)
        {
            var location = await _context.DeliveryLocations
                .Include(x => x.Area)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (location == null)
                throw new Exception("Delivery location not found.");

            return location;
        }

        public async Task<DeliveryLocation> CreateAsync(
            CreateDeliveryLocationRequest request)
        {
            var areaExists = await _context.Areas
                .AnyAsync(x => x.Id == request.AreaId);

            if (!areaExists)
                throw new Exception("Area not found.");

            var exists = await _context.DeliveryLocations
                .AnyAsync(x =>
                    x.AreaId == request.AreaId &&
                    x.LocationName == request.LocationName);

            if (exists)
                throw new Exception("Delivery location already exists.");
            var location = new DeliveryLocation
            {
                AreaId = request.AreaId,
                LocationName = request.LocationName.Trim(),
                Address = request.Address?.Trim(),
                IsActive = true,
                DeliveryOrder = request.DeliveryOrder,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId
            };

            _context.DeliveryLocations.Add(location);

            await _context.SaveChangesAsync();

            return location;
        }

        public async Task UpdateAsync(
            long id,
            UpdateDeliveryLocationRequest request)
        {
            var location = await _context.DeliveryLocations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (location == null)
                throw new Exception("Delivery location not found.");

            var duplicate = await _context.DeliveryLocations.AnyAsync(x =>
                x.Id != id &&
                x.AreaId == request.AreaId &&
                x.LocationName == request.LocationName);

            if (duplicate)
                throw new Exception("Delivery location already exists.");

            location.AreaId = request.AreaId;
            location.LocationName = request.LocationName.Trim();
            location.Address = request.Address?.Trim();
            location.IsActive = request.IsActive;
            location.DeliveryOrder = request.DeliveryOrder;
            location.UpdatedAt = DateTime.UtcNow;
            location.UpdatedBy = _currentUser.UserId;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var location = await _context.DeliveryLocations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (location == null)
                throw new Exception("Delivery location not found.");

            location.IsActive = false;
            location.UpdatedAt = DateTime.UtcNow;
            location.UpdatedBy = _currentUser.UserId;

            await _context.SaveChangesAsync();
        }
    }
}