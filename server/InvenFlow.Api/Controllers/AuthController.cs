using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InvenFlow.Core.Entities;
using InvenFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace InvenFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, AppDbContext context, IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName))
        {
            return BadRequest(new { message = "A company name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var companyName = request.CompanyName.Trim();
        var normalizedSlug = NormalizeSlug(companyName);

        // 1. Find or create the Tenant (Ignore global query filters to find tenant accurately)
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == normalizedSlug);

        if (tenant == null)
        {
            tenant = new Tenant { Id = Guid.NewGuid(), Name = companyName, Slug = normalizedSlug, CreatedAt = DateTime.UtcNow };
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();
        }

        // 2. Check if user already exists for THIS tenant
        var existingUser = await _userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.NormalizedEmail == normalizedEmail.ToUpperInvariant());

        if (existingUser != null)
        {
            return BadRequest(new { message = $"A user with email '{normalizedEmail}' already exists for company '{companyName}'." });
        }

        // 3. Create the User with explicit TenantId
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            DisplayName = request.DisplayName?.Trim() ?? string.Empty,
            TenantId = tenant.Id,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        // 4. Assign Admin role
        await _userManager.AddToRoleAsync(user, "Admin");

        var token = CreateToken(user, tenant.Id, "Admin");
        return Ok(new AuthResponse 
        { 
            Token = token, 
            TenantId = tenant.Id, 
            Role = "Admin", 
            UserName = user.Email, 
            TenantName = tenant.Name 
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Find user ignoring query filters to ensure multi-tenant matching
        var user = await _userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail.ToUpperInvariant());

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var signIn = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!signIn.Succeeded)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == user.TenantId);

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Employee";

        var token = CreateToken(user, user.TenantId, role);

        return Ok(new AuthResponse 
        { 
            Token = token, 
            TenantId = user.TenantId, 
            Role = role, 
            UserName = user.Email, 
            TenantName = tenant?.Name ?? "Unknown" 
        });
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

        if (user == null) return Unauthorized();

        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == user.TenantId);

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new AuthResponse 
        { 
            Token = string.Empty, 
            TenantId = user.TenantId, 
            Role = roles.FirstOrDefault() ?? "Employee", 
            UserName = user.Email ?? string.Empty, 
            TenantName = tenant?.Name ?? "Unknown" 
        });
    }

    private string CreateToken(ApplicationUser user, Guid tenantId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("tenantId", tenantId.ToString()),
            new(ClaimTypes.Name, user.DisplayName ?? string.Empty)
        };

        claims.AddRange(roles.Where(role => !string.IsNullOrWhiteSpace(role)).Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "dev-secret-key-for-invenflow-ai-application"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "invenflow",
            audience: _configuration["Jwt:Audience"] ?? "invenflow-clients",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string NormalizeSlug(string value) => 
        string.Concat(value.ToLowerInvariant().Where(ch => char.IsLetterOrDigit(ch)).Take(40));
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
}