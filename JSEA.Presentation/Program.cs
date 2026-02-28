using JSEA_Application.Interfaces.Auth;
using JSEA_Application.Interfaces;
using JSEA_Application.Services.Auth;
using JSEA_Infrastructure;
using JSEA_Infrastructure.Repositories;
using JSEA_Application.Enums;
using JSEA_Presentation.Services;
using PayOS;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using JSEA_Presentation.Swagger;

using Npgsql;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

#region Database + Enum mapping

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

// Geography/Geometry (Point, etc.) cho MicroExperience.Location, Journey, ...
dataSourceBuilder.UseNetTopologySuite();

// PostgreSQL enum ↔ C# enum
dataSourceBuilder.MapEnum<UserRole>("user_role");
dataSourceBuilder.MapEnum<UserStatus>("user_status");

var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource, o =>
    {
        o.UseNetTopologySuite();
    })
);

#endregion

#region Dependency Injection

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IEmailOtpService, EmailOtpService>();
builder.Services.AddScoped<IEmailOtpRepository, EmailOtpRepository>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// Micro-Experience
builder.Services.AddScoped<IMicroExperienceService, JSEA_Application.Services.MicroExperience.MicroExperienceService>();
builder.Services.AddScoped<IMicroExperienceRepository, MicroExperienceRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// Journey Setup (Goong Maps)
builder.Services.AddHttpClient();
builder.Services.Configure<JSEA_Infrastructure.Services.Goong.GoongOptions>(
    builder.Configuration.GetSection(JSEA_Infrastructure.Services.Goong.GoongOptions.SectionName));
builder.Services.AddScoped<IGoongMapsService, JSEA_Infrastructure.Services.Goong.GoongMapsService>();
builder.Services.AddScoped<IJourneyService, JSEA_Application.Services.Journey.JourneyService>();
builder.Services.AddScoped<IJourneyRepository, JourneyRepository>();

// Rate & Feedback
builder.Services.AddScoped<IVisitRepository, VisitRepository>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IRewardService, JSEA_Application.Services.Experience.RewardService>();
builder.Services.AddScoped<IRateFeedbackService, JSEA_Application.Services.Experience.RateFeedbackService>();

// PayOS
builder.Services.Configure<PayOSOptions>(builder.Configuration.GetSection("PayOS"));
builder.Services.AddScoped<IPayOSPaymentService, PayOSPaymentService>();
#endregion

#region Controllers + JSON Enum as string

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );
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
            ClockSkew = TimeSpan.Zero
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

    c.OperationFilter<SwaggerExamplesOperationFilter>();

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
});

#endregion

var app = builder.Build();

#region Middleware pipeline (THỨ TỰ CỰC QUAN TRỌNG)

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

#endregion

app.Run();
