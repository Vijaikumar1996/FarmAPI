using FarmAPI.Data;
using FarmAPI.DTOs;
using FarmAPI.Entities;
using FarmAPI.Interface;
using Microsoft.EntityFrameworkCore;
using static FarmAPI.DTOs.CustomerDto;

namespace FarmAPI.Services
{
    public partial class CustomerService : ICustomerService
    {
        private readonly FarmDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CustomerService(
            FarmDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<List<CustomerResponse>> GetAllAsync()
        {
            return await _context.Customers
                .Include(x => x.Area)
                .Include(x => x.DeliveryLocation)
                .OrderBy(x => x.CustomerName)
                .Select(x => new CustomerResponse
                {
                    Id = x.Id,
                    CustomerCode = x.CustomerCode,
                    CustomerName = x.CustomerName,
                    MobileNo = x.MobileNo,
                    AlternateMobileNo = x.AlternateMobileNo,
                    Email = x.Email,

                    AreaId = x.AreaId,
                    AreaCode = x.Area.AreaCode,
                    AreaName = x.Area.AreaName,

                    DeliveryLocationId = x.DeliveryLocationId,
                    DeliveryLocationName = x.DeliveryLocation != null
                        ? x.DeliveryLocation.LocationName
                        : null,

                    HouseDoorNo = x.HouseDoorNo,
                    Landmark = x.Landmark,
                    Remarks = x.Remarks,
                    DeliveryNotes = x.DeliveryNotes,

                    Latitude = x.Latitude,
                    Longitude = x.Longitude,

                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<CustomerResponse?> GetByIdAsync(
            long id)
        {
            var customer = await _context.Customers

                .Include(x => x.Area)

                .Include(x => x.DeliveryLocation)

                .Where(x => x.Id == id)

                .Select(x => new CustomerResponse
                {
                    Id = x.Id,
                    CustomerCode = x.CustomerCode,
                    CustomerName = x.CustomerName,
                    MobileNo = x.MobileNo,
                    AlternateMobileNo = x.AlternateMobileNo,
                    Email = x.Email,

                    AreaId = x.AreaId,
                    AreaCode = x.Area.AreaCode,
                    AreaName = x.Area.AreaName,

                    DeliveryLocationId = x.DeliveryLocationId,
                    DeliveryLocationName =
                        x.DeliveryLocation != null
                        ? x.DeliveryLocation.LocationName
                        : null,

                    HouseDoorNo = x.HouseDoorNo,
                    Landmark = x.Landmark,
                    Remarks = x.Remarks,
                    DeliveryNotes = x.DeliveryNotes,

                    Latitude = x.Latitude,
                    Longitude = x.Longitude,

                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt
                })

                .FirstOrDefaultAsync();

            if (customer == null)
                throw new Exception("Customer not found.");

            return customer;
        }

        public async Task<CustomerResponse> CreateAsync(
    CreateCustomerRequest request)
        {
            await ValidateAreaAsync(request.AreaId);

            await ValidateDeliveryLocationAsync(
                request.AreaId,
                request.DeliveryLocationId);

            await ValidateDuplicateCustomerAsync(
                null,
                request.MobileNo);

            var customer = new Customer
            {
                CustomerCode = await GenerateCustomerCodeAsync(),

                CustomerName = request.CustomerName.Trim(),
                MobileNo = request.MobileNo.Trim(),
                AlternateMobileNo = request.AlternateMobileNo?.Trim(),
                Email = request.Email?.Trim(),

                AreaId = request.AreaId,
                DeliveryLocationId = request.DeliveryLocationId,

                HouseDoorNo = request.HouseDoorNo.Trim(),
                Landmark = request.Landmark?.Trim(),

                Remarks = request.Remarks?.Trim(),
                DeliveryNotes = request.DeliveryNotes?.Trim(),

                Latitude = request.Latitude,
                Longitude = request.Longitude,

                IsActive = true,

                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId
            };

            _context.Customers.Add(customer);

            await _context.SaveChangesAsync();

            return await GetByIdAsync(customer.Id)
                ?? throw new Exception("Customer not found.");
        }

        public async Task UpdateAsync(
            long id,
            UpdateCustomerRequest request)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Id == id);

            if (customer == null)
                throw new Exception("Customer not found.");

            await ValidateAreaAsync(request.AreaId);

            await ValidateDeliveryLocationAsync(
                request.AreaId,
                request.DeliveryLocationId);

            await ValidateDuplicateCustomerAsync(
                id,                
                request.MobileNo);           

            customer.CustomerName = request.CustomerName.Trim();

            customer.MobileNo = request.MobileNo.Trim();

            customer.AlternateMobileNo =
                request.AlternateMobileNo?.Trim();

            customer.Email =
                request.Email?.Trim();

            customer.AreaId =
                request.AreaId;

            customer.DeliveryLocationId =
                request.DeliveryLocationId;

            customer.HouseDoorNo =
                request.HouseDoorNo.Trim();

            customer.Landmark =
                request.Landmark?.Trim();

            customer.Remarks =
                request.Remarks?.Trim();

            customer.DeliveryNotes =
                request.DeliveryNotes?.Trim();

            customer.Latitude =
                request.Latitude;

            customer.Longitude =
                request.Longitude;

            customer.IsActive =
                request.IsActive;

            customer.UpdatedAt =
                DateTime.UtcNow;

            customer.UpdatedBy =
                _currentUser.UserId;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Id == id);

            if (customer == null)
                throw new Exception("Customer not found.");

            customer.IsActive = false;

            customer.UpdatedAt = DateTime.UtcNow;

            customer.UpdatedBy = _currentUser.UserId;

            await _context.SaveChangesAsync();
        }

        private async Task ValidateAreaAsync(long areaId)
        {
            var exists = await _context.Areas
                .AnyAsync(x => x.Id == areaId);

            if (!exists)
                throw new Exception("Area not found.");
        }

        private async Task ValidateDeliveryLocationAsync(
            long areaId,
            long? deliveryLocationId)
        {
            if (!deliveryLocationId.HasValue)
                return;

            var exists = await _context.DeliveryLocations
                .AnyAsync(x =>
                    x.Id == deliveryLocationId &&
                    x.AreaId == areaId);

            if (!exists)
                throw new Exception("Invalid delivery location selected.");
        }

        private async Task ValidateDuplicateCustomerAsync(
            long? customerId,            
            string mobileNo)
        {            
            mobileNo = mobileNo.Trim();            

            var duplicateMobile = await _context.Customers
                .AnyAsync(x =>
                    x.MobileNo == mobileNo &&
                    (!customerId.HasValue || x.Id != customerId.Value));

            if (duplicateMobile)
                throw new Exception("Mobile number already exists.");
        }

        public async Task<List<DropdownDto>> GetTypeaheadAsync(
    string? searchText)
        {
            var query = _context.Customers
                .Include(x => x.DeliveryLocation)
                .Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(x =>
                    EF.Functions.ILike(
                        x.CustomerName,
                        $"%{searchText}%")
                    ||
                    EF.Functions.ILike(
                        x.MobileNo,
                        $"%{searchText}%")
                    ||
                    EF.Functions.ILike(
                        x.CustomerCode,
                        $"%{searchText}%"));
            }

            return await query
                .OrderBy(x => x.CustomerName)
                .Take(20)
                .Select(x => new DropdownDto
                {
                    Id = x.Id,

                    Name =
                        x.CustomerName +
                        " - " +
                        x.MobileNo +
                        " - " +
                        x.DeliveryLocation.LocationName +
                        " - " +
                        x.HouseDoorNo
                })
                .ToListAsync();
        }

        private async Task<string> GenerateCustomerCodeAsync()
        {
            var lastCustomerCode = await _context.Customers
                .OrderByDescending(x => x.Id)
                .Select(x => x.CustomerCode)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(lastCustomerCode))
            {
                return "CUS000001";
            }

            var number = int.Parse(lastCustomerCode.Replace("CUS", ""));

            return $"CUS{number + 1:D6}";
        }
    }
}