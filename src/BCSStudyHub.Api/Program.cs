using System.Security.Claims;
using System.Text;
using BCSStudyHub.Api.Middleware;
using BCSStudyHub.Application.Authorization;
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
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PermissionCodes.AdminAccess, policy =>
        policy.RequireClaim("permission", PermissionCodes.AdminAccess));
    options.AddPolicy(PermissionCodes.UsersManage, policy =>
        policy.RequireClaim("permission", PermissionCodes.UsersManage));
    options.AddPolicy(PermissionCodes.ContentManage, policy =>
        policy.RequireClaim("permission", PermissionCodes.ContentManage));
    options.AddPolicy(PermissionCodes.SyllabusManage, policy =>
        policy.RequireClaim("permission", PermissionCodes.SyllabusManage));
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
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
    await scope.ServiceProvider.GetRequiredService<SyllabusDataSeeder>().SeedAsync();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
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
