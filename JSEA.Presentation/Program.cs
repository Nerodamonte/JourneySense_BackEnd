using JSEA_Application.Interfaces.Auth;
using JSEA_Application.Interfaces;
using JSEA_Application.Services.Auth;
using JSEA_Infrastructure;
using JSEA_Infrastructure.Repositories;
using JSEA_Application.Enums;
using JSEA_Presentation.Services;
using PayOS;

using JSEA_Presentation.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Npgsql;
using Pgvector;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JSEA_Application.Services.Journey;
using JSEA_Application.Options;
using JSEA_Application.Services.Portal;
using JSEA_Infrastructure.Services;
using JSEA_Application.Services.Profile;
using JSEA_Presentation.JsonConverters;
using JSEA_Application.Services.Category;
using JSEA_Application.Services.Package;
using JSEA_Application.Services.UserPackage;
using Microsoft.Extensions.Caching.Distributed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

#region Database + Enum mapping

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

// Geography/Geometry (Point, etc.) cho MicroExperience.Location, Journey, ...
dataSourceBuilder.UseNetTopologySuite();

// pgvector extension cho kiểu vector(768)
dataSourceBuilder.UseVector();

// PostgreSQL enum ↔ C# enum
dataSourceBuilder.MapEnum<UserRole>("user_role");
dataSourceBuilder.MapEnum<UserStatus>("user_status");

var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource, o =>
    {
        o.UseNetTopologySuite();
        o.UseVector();
    })
);

#endregion

#region Dependency Injection
//Auth 
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IEmailOtpService, EmailOtpService>();
builder.Services.AddScoped<IEmailOtpRepository, EmailOtpRepository>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

//Profile
builder.Services.AddScoped<ITravelStyleTextGenerator, TravelStyleTextGenerator>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();

// Micro-Experience
builder.Services.AddScoped<IExperiencePhotoStorage, LocalExperiencePhotoStorage>();
builder.Services.AddScoped<IMicroExperienceService, JSEA_Application.Services.MicroExperience.MicroExperienceService>();
builder.Services.AddScoped<IMicroExperienceRepository, MicroExperienceRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IPackageRepository, PackageRepository>();
builder.Services.AddScoped<IPackageService, PackageService>();
builder.Services.AddScoped<IUserPackageRepository, UserPackageRepository>();
builder.Services.AddScoped<IUserPackageService, UserPackageService>();


// Journey Setup (Goong Maps)
builder.Services.AddHttpClient();
builder.Services.AddHttpClient(JSEA_Infrastructure.Services.Goong.GoongOptions.HttpClientName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "JourneySense-API/1.0");
});
builder.Services.Configure<JSEA_Infrastructure.Services.Goong.GoongOptions>(
    builder.Configuration.GetSection(JSEA_Infrastructure.Services.Goong.GoongOptions.SectionName));
builder.Services.AddScoped<IGoongMapsService, JSEA_Infrastructure.Services.Goong.GoongMapsService>();

// Thời tiết: Open-Meteo + cache. Redis khi có Redis:ConnectionString (hoặc ConnectionStrings:Redis) VÀ WeatherCache:UseRedis != false.
builder.Services.Configure<JSEA_Infrastructure.Services.OpenMeteo.WeatherCacheOptions>(
    builder.Configuration.GetSection(JSEA_Infrastructure.Services.OpenMeteo.WeatherCacheOptions.SectionName));
var weatherCacheOpts = builder.Configuration
    .GetSection(JSEA_Infrastructure.Services.OpenMeteo.WeatherCacheOptions.SectionName)
    .Get<JSEA_Infrastructure.Services.OpenMeteo.WeatherCacheOptions>()
    ?? new JSEA_Infrastructure.Services.OpenMeteo.WeatherCacheOptions();
var redisSection = builder.Configuration.GetSection("Redis");
var redisConn = redisSection["ConnectionString"] ?? builder.Configuration.GetConnectionString("Redis");
var redisDatabase = redisSection.GetValue("Database", 0);
var useRedis = !string.IsNullOrWhiteSpace(redisConn) && weatherCacheOpts.UseRedis != false;
if (useRedis)
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.ConfigurationOptions = StackExchange.Redis.ConfigurationOptions.Parse(redisConn!);
        options.ConfigurationOptions.DefaultDatabase = redisDatabase;
        options.InstanceName = "JSEA:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddScoped<JSEA_Infrastructure.Services.OpenMeteo.OpenMeteoWeatherService>();
builder.Services.AddScoped<IWeatherService, JSEA_Infrastructure.Services.OpenMeteo.CachingWeatherService>();
builder.Services.AddScoped<IJourneyService, JourneyService>();
builder.Services.Configure<JourneyShareOptions>(
    builder.Configuration.GetSection(JourneyShareOptions.SectionName));
builder.Services.AddScoped<IJourneyRepository, JourneyRepository>();
builder.Services.AddScoped<IJourneyProgressService, JourneyProgressService>();
builder.Services.AddScoped<IAchievementRepository, AchievementRepository>();
builder.Services.AddScoped<IRewardTransactionRepository, RewardTransactionRepository>();
builder.Services.AddScoped<ISharedJourneyRepository, SharedJourneyRepository>();
builder.Services.AddScoped<IJourneyMemberRepository, JourneyMemberRepository>();
builder.Services.AddScoped<IJourneyShareService, JSEA_Application.Services.Journey.JourneyShareService>();
builder.Services.AddScoped<IEmergencyNearbyService, JSEA_Application.Services.Journey.EmergencyNearbyService>();

if (!string.IsNullOrWhiteSpace(redisConn))
{
    var signalRConn = redisConn!.Contains("defaultDatabase=", StringComparison.OrdinalIgnoreCase)
        ? redisConn
        : $"{redisConn.TrimEnd(',', ' ')},defaultDatabase={redisDatabase}";
    builder.Services.AddSignalR().AddStackExchangeRedis(signalRConn, options =>
    {
        options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("JSEA:");
    });
}
else
{
    builder.Services.AddSignalR();
}

builder.Services.AddSingleton<JourneyLiveLocationRateLimiter>();
builder.Services.AddSingleton<IJourneyLocationCache>(sp =>
    new JSEA_Infrastructure.Services.RedisJourneyLocationCache(sp.GetRequiredService<IDistributedCache>()));
builder.Services.AddSingleton<IJourneyLiveNotifier, JourneyLiveNotifier>();

//Embedding
builder.Services.AddScoped<IExperienceEmbeddingRepository, ExperienceEmbeddingRepository>();
builder.Services.AddScoped<EmbeddingGeneratorService>();

//Suggest pipeline
builder.Services.AddScoped<ISuggestService, SuggestService>();

// Admin / Staff portal
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IPortalAuditLogger, PortalAuditLogger>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminAnalyticsService, AdminAnalyticsService>();
builder.Services.AddScoped<IAdminAuditLogService, AdminAuditLogService>();
builder.Services.AddScoped<IStaffFeedbackService, StaffFeedbackService>();

// Rate & Feedback
builder.Services.AddScoped<IVisitRepository, VisitRepository>();
builder.Services.AddScoped<IExperienceMetricRepository, ExperienceMetricRepository>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IRewardService, JSEA_Application.Services.Experience.RewardService>();
builder.Services.AddScoped<IRateFeedbackService, JSEA_Application.Services.Experience.RateFeedbackService>();
builder.Services.AddScoped<IVibeQuizService, JSEA_Application.Services.Quiz.VibeQuizService>();

// PayOS
builder.Services.Configure<PayOSOptions>(builder.Configuration.GetSection("PayOS"));
builder.Services.AddScoped<IPayOSPaymentService, PayOSPaymentService>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IPurchaseService, JSEA_Application.Services.Payment.PurchaseService>();
#endregion

#region Controllers + JSON Enum as string

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );

        // Serialize all DateTime as Vietnam time (+07:00) for API responses.
        options.JsonSerializerOptions.Converters.Add(new VietnamDateTimeJsonConverterFactory());
    });

#endregion

#region JWT Authentication

var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["Secret"]!;
var registerSecretKey = jwtSection["RegisterKey"]!;

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // dev
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],

            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey)
            ),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/journey-live"))
                    context.Token = accessToken;

                return Task.CompletedTask;
            }
        };
    })

    // ===== JWT RIÊNG CHO REGISTER =====
    .AddJwtBearer("Register", options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(registerSecretKey)
            ),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

#endregion

#region Swagger + JWT Support

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Journey Sense API",
        Version = "v1"
    });


    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT token: Bearer {token}"
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

    // Include XML comments from all projects for better Swagger docs (DTO + controller summaries).
    var xmlFiles = new[]
    {
        "JSEA_Presentation.xml",
        "JSEA_Application.xml",
        "JSEA_Infrastructure.xml"
    };
    var basePath = AppContext.BaseDirectory;
    foreach (var xml in xmlFiles)
    {
        var fullPath = Path.Combine(basePath, xml);
        if (File.Exists(fullPath))
        {
            c.IncludeXmlComments(fullPath, includeControllerXmlComments: true);
        }
    }
});

#endregion

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins is { Length: > 0 })
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        else
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

#region Middleware pipeline (THỨ TỰ CỰC QUAN TRỌNG)

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<JourneyLiveHub>("/hubs/journey-live");

#endregion

app.Run();
