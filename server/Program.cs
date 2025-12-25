using Microsoft.EntityFrameworkCore;
using server.Models;

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
var postgresConnection = Environment.GetEnvironmentVariable("Postgres");
builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseNpgsql(postgresConnection)
);

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
