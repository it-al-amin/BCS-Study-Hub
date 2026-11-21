using System.Security.Claims;
using System.Text;
using BCSStudyHub.Api.Middleware;
using System.Threading.RateLimiting;
using BCSStudyHub.Api.Controllers;
using BCSStudyHub.Api.Authorization;
using BCSStudyHub.Application.Authorization;
using BCSStudyHub.Application.Library;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using BCSStudyHub.Infrastructure;
using BCSStudyHub.Infrastructure.Configuration;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT configuration is required.");
jwtSettings.Validate();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "BCS Study Hub API", Version = "v1" }));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.MapInboundClaims = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        };
    });
builder.Services.AddAuthorization(PermissionPolicies.Register);

// Upload endpoints accept multipart bodies up to the hard ceiling; the configured per-file limit is enforced by the service.
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = LibraryFileRules.AbsoluteMaxUploadBytes);
builder.Services.AddRateLimiter(SecurityLimits.ConfigureRateLimiter);

// Behind a reverse proxy (nginx, a load balancer) the client IP and scheme arrive in X-Forwarded-* headers.
// Only enable this when such a proxy is the sole entry point, otherwise clients could spoof their IP.
var behindProxy = builder.Configuration.GetValue<bool>("ReverseProxy:Enabled");
if (behindProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        var proxies = builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [];
        if (proxies.Length > 0)
        {
            foreach (var proxy in proxies)
            {
                options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
            }
        }
        else
        {
            // Container networks assign proxy IPs dynamically; trust the single upstream hop.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        }
    });
}

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    }
}));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("mysql", tags: ["ready"]);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    // Off by default: production normally applies the reviewed migration script before deploying. The container
    // setup can opt in so a fresh database is created and upgraded on start. Migrations are additive.
    if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
    {
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    }

    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
    await scope.ServiceProvider.GetRequiredService<SyllabusDataSeeder>().SeedAsync();
    await scope.ServiceProvider.GetRequiredService<OfficialSourceSeeder>().SeedAsync();
}

if (behindProxy)
{
    app.UseForwardedHeaders();
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

await app.RunAsync();

public partial class Program
{
}
