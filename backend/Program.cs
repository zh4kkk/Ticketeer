using Microsoft.EntityFrameworkCore;
using Ticketeer.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'Default' is not configured. Set ConnectionStrings__Default.");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

app.MapGet("/health", async (AppDbContext db) =>
    await db.Database.CanConnectAsync()
        ? Results.Ok(new { status = "ok", database = "ok" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

app.Run();
