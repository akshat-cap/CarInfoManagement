using Microsoft.AspNetCore.Builder;

namespace CarInfoManagementSystem.Middleware
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseImageOptimization(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ImageOptimizationMiddleware>();
        }
    }
}