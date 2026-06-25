using Identity.Application.Abstractions;
using Identity.Infrastructure;
using Identity.Infrastructure.Data;
using Scheduling.Infrastructure;
using Scheduling.Infrastructure.Data;
using Clinical.Infrastructure;
using Clinical.Infrastructure.Data;
using Notification.Infrastructure;
using Notification.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Authorization;
using SharedKernel.Caching;
using SharedKernel.Logging;
using SharedKernel.Middleware;
using SharedKernel.Validation;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authorization;
using Host.Hubs;
using Host.HealthChecks;
using Host.Services;
using Serilog;
using Serilog.Events;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Threading.RateLimiting;

// Serilog bootstrap — early console logging before host is built
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.With<PhiScrubbingEnricher>()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId:l} {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
var builder = WebApplication.CreateBuilder(args);

// Serilog host integration (AC-1: structured JSON + configurable sinks)
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.With<PhiScrubbingEnricher>()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId:l} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/log-.json",
        rollingInterval: RollingInterval.Day,
        formatter: new Serilog.Formatting.Json.JsonFormatter()));

// Kestrel TLS 1.2+ enforcement (NFR-006: reject TLS 1.0/1.1)
builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    serverOptions.ConfigureHttpsDefaults(httpsOptions =>
    {
        // Enforce TLS 1.2 minimum, allow TLS 1.3
        httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
    });
});

// HSTS configuration (1-year max-age, include subdomains)
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

// Module registration with configuration
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddSchedulingModule(builder.Configuration);
builder.Services.AddClinicalModule(builder.Configuration);
builder.Services.AddNotificationModule(builder.Configuration);

// Cross-module service registrations
builder.Services.AddScoped<Scheduling.Application.Abstractions.ISlotNotificationService, Host.Services.SlotNotificationService>();
builder.Services.AddScoped<Scheduling.Application.Abstractions.IPatientLookupService, Host.Services.PatientLookupService>();
builder.Services.AddScoped<Clinical.Application.Abstractions.IIntakeRecordPersistence, Host.Services.IntakeRecordPersistenceService>();
builder.Services.AddScoped<Clinical.Application.Abstractions.IManualIntakePersistence, Host.Services.ManualIntakePersistenceService>();
builder.Services.AddScoped<Clinical.Application.Abstractions.IPatientDemographicsService, Host.Services.PatientDemographicsService>();
builder.Services.AddScoped<Notification.Application.Abstractions.IReminderAppointmentQuery, Host.Services.ReminderAppointmentQueryService>();
builder.Services.AddScoped<Notification.Application.Abstractions.ICalendarAppointmentQuery, Host.Services.CalendarAppointmentQueryService>();
builder.Services.AddScoped<Notification.Application.Abstractions.IConfirmationAppointmentQuery, Host.Services.ConfirmationAppointmentQueryService>();

// Audit logging service (FR-031, DR-011)
builder.Services.AddScoped<SharedKernel.Audit.IAuditService>(sp =>
{
    var dbContext = sp.GetRequiredService<Clinical.Infrastructure.Data.ClinicalDbContext>();
    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SharedKernel.Audit.AuditService<Clinical.Infrastructure.Data.ClinicalDbContext>>>();
    var redis = sp.GetService<StackExchange.Redis.IConnectionMultiplexer>();
    return new SharedKernel.Audit.AuditService<Clinical.Infrastructure.Data.ClinicalDbContext>(dbContext, logger, redis);
});

// Replace NoOp calendar sync with real implementation (AC-2, AC-3, AC-4)
builder.Services.AddScoped<Scheduling.Application.Abstractions.ICalendarSyncService, Host.Services.CalendarSyncService>();

// Send confirmation email with PDF immediately after booking
builder.Services.AddScoped<Scheduling.Application.Abstractions.IBookingConfirmationNotifier, Host.Services.BookingConfirmationNotifier>();

// JWT Bearer authentication (AC-1, AC-2: role-based claims)
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? "DefaultDevKeyForDevelopmentOnlyDoNotUseInProduction!!";
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
        ValidIssuer = jwtSection["Issuer"] ?? "UnifiedPatientAccess",
        ValidAudience = jwtSection["Audience"] ?? "UnifiedPatientAccess",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromSeconds(30),
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // SignalR sends token as query param for WebSocket connections
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var blacklist = context.HttpContext.RequestServices.GetService<ITokenBlacklistService>();
            if (blacklist is null) return;

            var jti = context.Principal?.FindFirst(
                System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
            if (jti is not null && await blacklist.IsBlacklistedAsync(jti))
            {
                context.Fail("Token has been revoked.");
            }
        }
    };
});

// Deny-by-default RBAC authorization policies (NFR-009, AC-5)
// FallbackPolicy ensures all endpoints require authentication unless explicitly [AllowAnonymous]
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build())
    .AddPolicy("PatientOnly", policy => policy.RequireRole("Patient"))
    .AddPolicy("PatientPolicy", policy => policy.RequireRole("Patient", "Admin"))
    .AddPolicy("StaffOrAbove", policy => policy.RequireRole("Provider", "FrontDesk", "Staff", "Admin"))
    .AddPolicy("StaffPolicy", policy => policy.RequireRole("Provider", "FrontDesk", "Admin"))
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
    .AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"))
    .AddPolicy("ComplianceOfficer", policy => policy.RequireRole("ComplianceOfficer", "Admin"));

// Register resource-based authorization handler for patient data ownership (NFR-009)
builder.Services.AddScoped<IAuthorizationHandler, PatientOwnershipHandler>();

// Register staff data scope filter for role-based data access (NFR-009, FR-032)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IStaffDataScopeService, StaffDataScopeFilter>();

// Rate limiting on auth endpoints (NFR-010: 5 requests per 15-minute window per IP)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "900"; // 15 minutes
        if (context.Lease.TryGetMetadata(System.Threading.RateLimiting.MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }
        await context.HttpContext.Response.WriteAsync(
            "Too many authentication attempts. Please try again later.", token);
    };
    options.AddFixedWindowLimiter("AuthRateLimit", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(15);
        limiterOptions.QueueLimit = 0;
    });
});

// AI Network Guard: Validate Ollama runs on localhost only (AIR-S01)
builder.Services.AddAiNetworkGuard();

// Redis caching with graceful degradation to in-memory (AC-4)
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    try
    {
        var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
        redisOptions.AbortOnConnectFail = false;
        redisOptions.Ssl = redisConnectionString.Contains("upstash.io", StringComparison.OrdinalIgnoreCase);

        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisOptions));
        builder.Services.AddSingleton<ICacheService, RedisCacheService>();

        builder.Services.AddHealthChecks()
            .AddCheck("redis_cache", new RedisHealthCheck(redisOptions), tags: ["infrastructure"]);
    }
    catch (Exception)
    {
        // Redis config invalid — fall back to in-memory
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
    }
}
else
{
    // No Redis configured — use in-memory cache fallback
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
}

// Health checks for DB, Ollama, and Backup (AC-4, DR-014, DR-015)
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("sql_server", tags: ["ready", "infrastructure"])
    .AddCheck<OllamaHealthCheck>("ollama_ai", tags: ["ready", "infrastructure"])
    .AddCheck<EncryptionHealthCheck>("encryption_status", tags: ["ready", "security"])
    .AddCheck<BackupHealthCheck>("backup_status", tags: ["ready", "infrastructure"]);

// Platform reliability monitoring (NFR-012, NFR-018)
builder.Services.AddSingleton<MtbfTracker>();
builder.Services.AddHostedService<UptimeMonitorService>();
builder.Services.AddHostedService<RestoreVerificationService>();

// SignalR real-time communication (AD-008, NFR-002)
builder.Services.AddSignalR();

// CORS for frontend origin
var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>() ?? ["http://localhost:3000"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddApplicationPart(typeof(Identity.API.Controllers.IdentityController).Assembly)
    .AddApplicationPart(typeof(Scheduling.API.Controllers.SchedulingController).Assembly)
    .AddApplicationPart(typeof(Clinical.API.Controllers.ClinicalController).Assembly)
    .AddApplicationPart(typeof(Notification.API.Controllers.NotificationController).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat([
        "application/json"
    ]);
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Unified Patient Access API",
        Version = "v1",
        Description = "Modular monolith API for the Unified Patient Access & Clinical Intelligence Platform",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Platform Team"
        }
    });

    // Include XML documentation from all API assemblies
    var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.API.xml", SearchOption.TopDirectoryOnly);
    foreach (var xmlFile in xmlFiles)
    {
        options.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
    }

    var hostXml = Path.Combine(AppContext.BaseDirectory, "Host.xml");
    if (File.Exists(hostXml))
    {
        options.IncludeXmlComments(hostXml);
    }
});

var app = builder.Build();

// HSTS must be called before other middleware (NFR-006)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Security headers middleware (NFR-010: CSP, X-Frame-Options, X-Content-Type-Options)
app.UseSecurityHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Unified Patient Access API v1");
    });
}

app.UseCors("FrontendPolicy");
app.UseResponseCompression();

// Rate limiting middleware (AC-4)
app.UseRateLimiter();

// Correlation ID middleware (AC-5: request tracing)
app.UseMiddleware<CorrelationIdMiddleware>();

// Serilog request logging with structured properties
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        if (httpContext.Response.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            diagnosticContext.Set("CorrelationId", correlationId.ToString());
        }
    };
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// SignalR hub endpoints
app.MapHub<AppointmentHub>("/hubs/appointments");
app.MapHub<DocumentHub>("/hubs/documents");

app.MapGet("/health/live", () => Results.Ok(new { Status = "Alive" }))
    .AllowAnonymous();

// SignalR connection count monitoring (AC-5)
app.MapGet("/health/signalr", () => Results.Ok(new
{
    ActiveConnections = AppointmentHub.ConnectionCount + DocumentHub.ConnectionCount
}))
    .AllowAnonymous();

app.MapGet("/health/ready", async (HealthCheckService healthCheckService) =>
{
    var report = await healthCheckService.CheckHealthAsync(
        registration => registration.Tags.Contains("module") || registration.Tags.Contains("ready"));

    var result = new
    {
        Status = report.Status.ToString(),
        Checks = report.Entries.Select(e => new
        {
            Name = e.Key,
            Status = e.Value.Status.ToString(),
            Description = e.Value.Description
        })
    };

    return report.Status == HealthStatus.Healthy
        ? Results.Ok(result)
        : Results.Json(result, statusCode: 503,
            contentType: "application/json");
})
    .AllowAnonymous();

// Forward-only migration runner
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    async Task MigrateAsync<TContext>(string moduleName) where TContext : DbContext
    {
        try
        {
            logger.LogInformation("Starting migration check for {Module}...", moduleName);
            var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

            // MigrateAsync will create the database if it doesn't exist,
            // so we call it directly instead of gating on CanConnectAsync
            // which fails when the DB has not been created yet.
            var pending = await dbContext.Database.GetPendingMigrationsAsync();
            if (pending.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations for {Module}...", pending.Count(), moduleName);
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully for {Module}.", moduleName);
            }
            else
            {
                logger.LogInformation("No pending migrations for {Module}.", moduleName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Migration failed for {Module}. Error: {Message}. Application will continue without this module's schema updates.", moduleName, ex.Message);
        }
    }

    await MigrateAsync<IdentityDbContext>("Identity");
    await MigrateAsync<SchedulingDbContext>("Scheduling");
    await MigrateAsync<ClinicalDbContext>("Clinical");
    await MigrateAsync<NotificationDbContext>("Notification");

    // Seed default admin user
    await SeedAdminUserAsync(scope.ServiceProvider, logger);

    // Seed sample scheduling data (dev only)
    if (app.Environment.IsDevelopment())
    {
        try
        {
            var schedulingDb = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
            await SchedulingDataSeeder.SeedAsync(schedulingDb, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed scheduling data: {Message}", ex.Message);
        }
    }

    // Seed ICD-10 and CPT reference tables for medical coding (AIR-004, AIR-005)
    try
    {
        var clinicalDb = scope.ServiceProvider.GetRequiredService<ClinicalDbContext>();
        await Icd10SeedData.SeedAsync(clinicalDb);
        await CptSeedData.SeedAsync(clinicalDb);
        logger.LogInformation("Medical coding reference tables seeded (ICD-10, CPT).");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to seed medical coding reference tables: {Message}", ex.Message);
    }
}

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

static async Task SeedAdminUserAsync(IServiceProvider services, Microsoft.Extensions.Logging.ILogger logger)
{
    try
    {
        var dbContext = services.GetRequiredService<IdentityDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();

        var adminExists = await dbContext.Users
            .AnyAsync(u => u.Role == Identity.Domain.Enums.UserRole.Admin);

        if (adminExists)
        {
            return;
        }

        var admin = new Identity.Domain.Entities.User
        {
            Email = "admin@upap.com",
            FullName = "System Admin",
            PasswordHash = passwordHasher.Hash("Admin@123"),
            Role = Identity.Domain.Enums.UserRole.Admin,
            Status = Identity.Domain.Enums.UserStatus.Active,
            ContactNumber = "0000000000",
        };

        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Default admin user seeded (admin@upap.com).");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to seed default admin user.");
    }
}

// Make Program accessible for WebApplicationFactory in integration tests
public partial class Program { }