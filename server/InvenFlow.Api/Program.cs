using System.IdentityModel.Tokens.Jwt;
using System.Text;
using InvenFlow.Api;
using InvenFlow.Api.Application.ProductDetails;
using InvenFlow.Api.Application.Search;
using InvenFlow.Api.Services;
using InvenFlow.Core.Entities;
using InvenFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<GeminiInvoiceService>();
builder.Services.AddScoped<ICurrentTenantService, CurrentTenantService>();
builder.Services.AddScoped<IProductAdapter, FetchchipsLocalAdapter>();
builder.Services.AddHttpClient<IProductAdapter, ArrowElectronicsAdapter>();
builder.Services.AddHttpClient<IProductAdapter, DigiKeyElectronicsAdapter>();
builder.Services.AddScoped<ISearchProviderAdapter>(sp => (ISearchProviderAdapter)sp.GetServices<IProductAdapter>().First(a => a is FetchchipsLocalAdapter));
builder.Services.AddScoped<ISearchProviderAdapter>(sp => (ISearchProviderAdapter)sp.GetServices<IProductAdapter>().First(a => a is ArrowElectronicsAdapter));
builder.Services.AddScoped<ISearchProviderAdapter>(sp => (ISearchProviderAdapter)sp.GetServices<IProductAdapter>().First(a => a is DigiKeyElectronicsAdapter));
builder.Services.AddScoped<AggregatorSearchService>();
builder.Services.Configure<VendorKeyMappingOptions>(builder.Configuration.GetSection("VendorDetails"));
builder.Services.Configure<ProviderSettings>(builder.Configuration.GetSection("ProviderSettings"));
builder.Services.AddScoped<IVendorDetailsProvider, DefaultProductDetailsProvider>();
builder.Services.AddScoped<IVendorDetailsProvider, MockProductDetailsProvider>();
builder.Services.AddScoped<IVendorKeyMapper, VendorKeyMapper>();
builder.Services.AddScoped<IVendorDetailsService, VendorOrchestratorService>();
builder.Services.AddScoped<VendorOrchestratorService>();
builder.Services.AddHostedService<ExternalLinkMonitorService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "invenflow",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "invenflow-clients",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "dev-secret-key-for-invenflow-ai-application"))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireManagerOrAdmin", policy => policy.RequireRole("Manager", "Admin"));
    options.AddPolicy("RequireEmployeeOrAbove", policy => policy.RequireRole("Employee", "Manager", "Admin"));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    await DbInitializer.InitializeAsync(db, startupLogger, app.Lifetime.ApplicationStopping);

    foreach (var roleName in new[] { "Admin", "Manager", "Employee" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        }
    }

    var adminEmail = builder.Configuration["Admin:Email"];
    var adminPassword = builder.Configuration["Admin:Password"];
    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
    {
        // No bootstrap admin configured; continue serving the API normally.
    }
    else
    {
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == "invenflow-hq");
        if (tenant == null)
        {
            tenant = new Tenant { Name = "InvenFlow HQ", Slug = "invenflow-hq" };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, DisplayName = "System Admin", TenantId = tenant.Id, EmailConfirmed = true };
            try
            {
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            catch (Exception ex) when (ex.InnerException?.Message?.Contains("duplicate") ?? false)
            {
                // User already exists, skip creation
            }
        }
    }
}

app.UseCors("AllowReactApp");
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "InvenFlow API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();

// Retrieve port from environment or fallback to 8080
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");