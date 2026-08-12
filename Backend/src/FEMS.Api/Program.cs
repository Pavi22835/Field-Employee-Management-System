using System.Text;
using AspNetCoreRateLimit;
using FEMS.Api.Common;
using FEMS.Api.Middleware;
using FEMS.Application.Common.Interfaces;
using FEMS.Infrastructure;
using FEMS.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---- Serilog (section 5.1: structured logging, file + console sinks) ----
builder.Host.UseSerilog((ctx, services, configuration) => configuration
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/fems-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30));

// ---- MVC / Validation ----
builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();

// ---- Swagger / OpenAPI (section 5.1) ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Field Employee Management System API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// ---- Infrastructure (EF Core / PostgreSQL, services) ----
builder.Services.AddInfrastructure(builder.Configuration);

// ---- HttpContext / current user ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ---- JWT Authentication (section 19) ----
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Secret"]!)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

// ---- Policy-based RBAC (section 4 &amp; 5.1) ----
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.SuperAdminOnly, p => p.RequireRole(RoleNames.SuperAdmin));
    options.AddPolicy(PolicyNames.AdminOnly, p => p.RequireRole(RoleNames.SuperAdmin, RoleNames.Admin));
    options.AddPolicy(PolicyNames.ManagementOnly, p => p.RequireRole(RoleNames.SuperAdmin, RoleNames.Admin, RoleNames.Supervisor));
    options.AddPolicy(PolicyNames.EmployeeOnly, p => p.RequireRole(RoleNames.Employee));
});

// ---- CORS (for the Admin Web app) ----
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminWeb", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

// ---- Rate limiting on auth/sensitive endpoints (section 19) ----
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
// Section 19 requires HTTPS-only in production (enforced by the Nginx/IIS reverse proxy
// there per section 5.4). In Development we skip the redirect so the plain-HTTP endpoint
// stays reachable for local testing from a physical device/emulator that hasn't been
// configured to trust the dev machine's self-signed certificate.
// Non-Development environments also allow an explicit opt-out (Security:UseHttpsRedirection
// = false) for interim HTTP-only Kestrel hosting where no HTTPS binding/certificate exists
// yet — without it, every request 307s to a non-existent HTTPS listener and the API becomes
// unreachable. Defaults to true (redirect enforced) so this must be turned off deliberately.
if (!app.Environment.IsDevelopment() && app.Configuration.GetValue("Security:UseHttpsRedirection", true))
{
    app.UseHttpsRedirection();
}
app.UseIpRateLimiting();
app.UseCors("AdminWeb");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ---- Apply migrations + seed roles / bootstrap Super Admin on startup ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await FEMS.Infrastructure.Persistence.Seed.DbSeeder.SeedAsync(db, hasher, app.Configuration);
}

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
