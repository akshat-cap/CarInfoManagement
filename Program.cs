using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CarInfoManagementSystem.Data;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Net.Http.Headers;
using CarInfoManagementSystem.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add logging to verify connection string
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});
var startupLogger = loggerFactory.CreateLogger<Program>();

var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
startupLogger.LogInformation($"DefaultConnection: {defaultConnectionString}");

var azureBlobStorageConnectionString = builder.Configuration.GetConnectionString("AzureBlobStorage");
startupLogger.LogInformation($"AzureBlobStorage: {azureBlobStorageConnectionString}");

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Azure Blob Storage
builder.Services.AddSingleton(x => new BlobServiceClient(
    builder.Configuration.GetConnectionString("AzureBlobStorage")));

// Add memory cache for image optimization
builder.Services.AddMemoryCache();

// Configure Identity with proper session settings
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure authentication cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Configure session with shorter timeout
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);  // Changed from 20 to 5 minutes
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".CarInfo.Session";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await DbInitializer.Initialize(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Configure static files with caching
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 1 day
        ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=86400";
        ctx.Context.Response.Headers["Expires"] = DateTime.UtcNow.AddDays(1).ToString("R");
    }
});

// Add cache control headers for static files
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache all static files for 30 days
        ctx.Context.Response.Headers[HeaderNames.CacheControl] = 
            "public,max-age=2592000";
    }
});

// Add image optimization middleware
app.UseImageOptimization();

// Add CORS policy specifically for Azure Blob Storage
app.UseCors(builder => builder
    .WithOrigins("https://*.blob.core.windows.net")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .SetIsOriginAllowedToAllowWildcardSubdomains());

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
