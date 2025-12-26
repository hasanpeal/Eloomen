using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using server.Interfaces;
using server.Models;
using server.Services;

// Load .env file first (this sets ASPNETCORE_ENVIRONMENT and other variables)
// Try base .env file first (contains ASPNETCORE_ENVIRONMENT setting)
if (File.Exists(".env"))
{
    DotNetEnv.Env.Load(); // This will set ASPNETCORE_ENVIRONMENT from .env
}

// Now get the environment (from .env, system env var, or default)
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var isDevelopment = environment == "Development";
var isProduction = environment == "Production";

// Load environment-specific .env file if it exists
var envSpecificFile = isDevelopment ? ".env.dev" : isProduction ? ".env.prod" : ".env";
if (File.Exists(envSpecificFile))
{
    DotNetEnv.Env.Load(envSpecificFile); // Override/merge with env-specific values
}

// Access environment variables directly (after .env files are loaded)
// Example:
// var dbConnection = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULT");
// var apiKey = Environment.GetEnvironmentVariable("APIKEYS__EXTERNALAPI");

// Access environment variables via Configuration (recommended - works with appsettings + env vars)
// Example:
// var dbConnection = builder.Configuration["ConnectionStrings:Default"];
// var apiKey = builder.Configuration["ApiKeys:ExternalApi"];
// Note: Use double underscore __ in .env for nested keys (maps to colon : in config)

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        // Prevents Object Reference Looping in JSON Serialization Object Cycles
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

// Postgres connection string "Postgres" thats in appsettings.json
var postgresConnection = Environment.GetEnvironmentVariable("POSTGRES");
builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseNpgsql(postgresConnection)
);

// Identity configuration
builder.Services.AddIdentity<User, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
    }
).AddEntityFrameworkStores<ApplicationDBContext>();

// JWT Setup
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = 
    options.DefaultChallengeScheme = 
    options.DefaultForbidScheme =
    options.DefaultScheme =
    options.DefaultSignInScheme =
    options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
        ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
        IssuerSigningKey = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SIGNING_KEY"))),
    };
});

// Swagger/OpenAPI configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Eloomen API", 
        Version = "v1",
        Description = "API for Eloomen application"
    });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
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

builder.Services.AddScoped<ITokenService, TokenService>();

var app = builder.Build();

// Swagger configuration - available in Development
if (isDevelopment)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Eloomen API v1");
        options.RoutePrefix = "swagger"; // Access Swagger UI at /swagger
    });
}

app.UseHttpsRedirection();

// These two needed for JWT
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
