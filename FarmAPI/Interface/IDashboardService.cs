using FarmAPI.DTOs;

namespace FarmAPI.Interface
{
    public interface IDashboardService
    {
        Task<DashboardResponse> GetDashboardAsync();
    }
}
