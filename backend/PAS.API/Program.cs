using Microsoft.EntityFrameworkCore;
using PAS.API.Data;
using PAS.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ── EF Core DbContext ──────────────────────────────────────────────────────
builder.Services.AddDbContext<PASDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            );
        }
    ));

// ── Application services ───────────────────────────────────────────────────
builder.Services.AddScoped<IUserAdminService, UserAdminService>();

// ── MVC Controllers ────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Swagger / OpenAPI ──────────────────────────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

// Health-check / root endpoint
app.MapGet("/", () => "PAS API is running successfully!");

app.Run();