using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Store.Api.Middlewares;
using Store.Api.Extensions;
using Store.Api.Configuration;
using Store.Infrastructure.Authentication;
using Store.Infrastructure.Authentication.Abstractions;
using Store.Infrastructure.Persistence;
using Store.Infrastructure;
using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add controllers with FluentValidation
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// Add endpoints explorer
builder.Services.AddEndpointsApiExplorer();

// Swagger Configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Store Management API",
        Version = "v1",
        Description = "API for Store Management System - Sistem Manajemen Toko Kelontong"
    });

    // Fix schema collision: use full type name instead of just class name
    c.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database Configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Configuration
var jwtSecret = StartupSecurity.ValidateJwtSecret(
    builder.Configuration["JwtSettings:Secret"],
    builder.Environment.IsDevelopment());
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "StoreManagement";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "StoreManagementClient";

builder.Services.Configure<JwtSettings>(options =>
{
    options.Secret = jwtSecret;
    options.Issuer = jwtIssuer;
    options.Audience = jwtAudience;
    options.AccessTokenExpirationMinutes = int.Parse(builder.Configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "60");
    options.RefreshTokenExpirationDays = int.Parse(builder.Configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7");
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SysAdminOnly", policy => policy.RequireRole("sysadmin"));
    options.AddPolicy("OwnerOnly", policy => policy.RequireRole("owner"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
    options.AddPolicy("OwnerOrAdmin", policy => policy.RequireRole("owner", "admin"));
    options.AddPolicy("SysAdminOrOwner", policy => policy.RequireRole("sysadmin", "owner"));
    options.AddPolicy("SysAdminOrOwnerOrAdmin", policy => policy.RequireRole("sysadmin", "owner", "admin"));
});

// Memory Cache Registration
builder.Services.AddMemoryCache();

// Rate Limiting Configuration
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("LoginPolicy", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("AiMessagePolicy", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                         ?? httpContext.Connection.RemoteIpAddress?.ToString() 
                         ?? httpContext.Request.Headers.Host.ToString(),
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:4200";
        policy.WithOrigins(frontendUrl)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Infrastructure services (includes repositories and application services)
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandling();

// CORS
app.UseCors("AllowFrontend");

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Store Management API v1");
        c.RoutePrefix = string.Empty;
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<TenantMiddleware>();

app.MapControllers();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();
    
    if (app.Environment.IsDevelopment())
    {
        await DbInitializer.InitializeAsync(db);
    }
}

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
