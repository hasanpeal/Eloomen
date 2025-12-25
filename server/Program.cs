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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        // Prevents Object Reference Looping in JSON Serialization Object Cycles
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });


var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
