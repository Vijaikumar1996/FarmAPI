using FarmAPI.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace FarmAPI.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _scopeFactory;

        public ExceptionMiddleware(
            RequestDelegate next,
            IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _scopeFactory = scopeFactory;
        }

        public async Task InvokeAsync(
            HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                context.Response.StatusCode = 400;

                await context.Response.WriteAsJsonAsync(
                    new
                    {
                        success = false,
                        message = ex.Message
                    });
            }
            catch (Exception ex)
            {
                try
                {
                    //using var scope =
                    //    _scopeFactory.CreateScope();

                    //var db = scope.ServiceProvider
                    //    .GetRequiredService<FarmDbContext>();

                    //long? userId = null;

                    //var claim = context.User?
                    //    .FindFirst(ClaimTypes.NameIdentifier);

                    //if (claim != null &&
                    //    long.TryParse(claim.Value, out var parsedUserId))
                    //{
                    //    userId = parsedUserId;
                    //}

                    //db.ExceptionLogs.Add(
                    //    new ExceptionLog
                    //    {
                    //        Message = ex.Message,
                    //        StackTrace = ex.ToString(),
                    //        InnerException = ex.InnerException?.Message,
                    //        Path = context.Request.Path,
                    //        Method = context.Request.Method,
                    //        UserId = userId,
                    //        CreatedAt = DateTime.UtcNow
                    //    });

                    //await db.SaveChangesAsync();
                }
                catch
                {
                    // Never throw from logging
                }

                context.Response.StatusCode = 500;

                await context.Response.WriteAsJsonAsync(
                    new
                    {
                        success = false,
                        message = ex.Message

                        // Production:
                        // message = "An unexpected error occurred."
                    });
            }
        }
    }
}
