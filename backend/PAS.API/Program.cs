using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ISupervisorService, SupervisorService>();
builder.Services.AddScoped<IModuleLeaderService, ModuleLeaderService>();
builder.Services.AddScoped<IResearchAreaService, ResearchAreaService>();
builder.Services.AddScoped<ICourseworkService, CourseworkService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<ISupervisorDashboardService, SupervisorDashboardService>();

// ── JWT Bearer Authentication ──────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey   = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtSettings["Issuer"],
        ValidAudience            = jwtSettings["Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(
                                       Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew                = TimeSpan.Zero   // no grace period on expiry
    };
});

builder.Services.AddAuthorization(options =>
{
    // Accessible by MODULE LEADER role
    options.AddPolicy("ModuleLeaderOnly", policy =>
        policy.RequireRole("MODULE LEADER"));

    // Accessible by MODULE LEADER or ADMIN
    options.AddPolicy("ModuleLeaderOrAdmin", policy =>
        policy.RequireRole("MODULE LEADER", "ADMIN"));

    options.AddPolicy("StudentOnly", policy =>
        policy.RequireRole("STUDENT"));
        
    options.AddPolicy("SupervisorOnly", policy =>
        policy.RequireRole("SUPERVISOR"));

    options.AddPolicy("SystemAdminOnly", policy =>
        policy.RequireRole("ADMIN"));
});

// ── CORS (allow Vite dev server) ─────────────────────────────────────────
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod()));

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
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health-check / root endpoint
app.MapGet("/", () => "PAS API is running successfully!");

app.Run();
