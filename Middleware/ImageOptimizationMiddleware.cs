using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using System.Collections.Concurrent;

namespace CarInfoManagementSystem.Middleware
{
    public class ImageOptimizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public ImageOptimizationMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Only handle requests for car images
            if (!path.StartsWith("/cars/", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // Strong caching headers
            context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromDays(1),
                MustRevalidate = true
            };

            // Add Vary header to handle different client capabilities
            context.Response.Headers.Vary = "Accept-Encoding";

            // Add ETag support
            var fileInfo = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", path.TrimStart('/')));
            if (fileInfo.Exists)
            {
                var etag = $"\"{fileInfo.LastWriteTimeUtc.Ticks}\"";
                context.Response.Headers[HeaderNames.ETag] = etag;

                if (context.Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var ifNoneMatch) && ifNoneMatch.ToString() == etag)
                {
                    context.Response.StatusCode = StatusCodes.Status304NotModified;
                    return;
                }
            }

            await _next(context);
        }
    }
}