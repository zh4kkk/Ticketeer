using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Ticketeer.Data;
using Ticketeer.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'Default' is not configured. Set ConnectionStrings__Default.");
}

var jwtOptions = new JwtOptions
{
    Key = RequireConfiguration(builder.Configuration, "Jwt:Key"),
    Issuer = RequireConfiguration(builder.Configuration, "Jwt:Issuer"),
    Audience = RequireConfiguration(builder.Configuration, "Jwt:Audience"),
    ExpiresMinutes = RequirePositiveInt(builder.Configuration, "Jwt:ExpiresMinutes")
};
if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
{
    throw new InvalidOperationException("JWT key must be at least 32 bytes long.");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<BootstrapAdminService>();
builder.Services.AddControllers();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var claim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (!int.TryParse(claim, out var userId))
                {
                    context.Fail("The token does not contain a valid user identifier.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var userIsActive = await db.Users.AsNoTracking()
                    .AnyAsync(x => x.Id == userId && x.IsActive, context.HttpContext.RequestAborted);
                if (!userIsActive)
                {
                    context.Fail("The user is not active.");
                }
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<BootstrapAdminService>().TryCreateAsync(app.Lifetime.ApplicationStopping);
}

app.MapGet("/health", async (AppDbContext db) =>
    await db.Database.CanConnectAsync()
        ? Results.Ok(new { status = "ok", database = "ok" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));
app.MapControllers();

app.Run();

static string RequireConfiguration(IConfiguration configuration, string key)
{
    var value = configuration[key];
    return string.IsNullOrWhiteSpace(value)
        ? throw new InvalidOperationException($"Configuration '{key}' is required.")
        : value;
}

static int RequirePositiveInt(IConfiguration configuration, string key)
{
    var value = configuration[key];
    return int.TryParse(value, out var result) && result > 0
        ? result
        : throw new InvalidOperationException($"Configuration '{key}' must be a positive integer.");
}
