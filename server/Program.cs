using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using server.Interfaces;
using server.Models;
using server.Services;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// Controllers + JSON
// --------------------
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling =
            Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        // Serialize enums as strings instead of numbers
        options.SerializerSettings.Converters.Add(
            new Newtonsoft.Json.Converters.StringEnumConverter()
        );
    });

// --------------------
// Database (PostgreSQL / Supabase)
// --------------------
var postgresConnection = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseNpgsql(postgresConnection)
);

// --------------------
// Identity
// --------------------
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDBContext>()
.AddDefaultTokenProviders();

// --------------------
// JWT Authentication
// --------------------
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true, // IMPORTANT: Enforce token expiration

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!)
            )
        };
        
        // Prevent automatic redirects to login page
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                // Don't challenge for non-authorized endpoints
                var endpoint = context.HttpContext.GetEndpoint();
                if (endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAuthorizeData>() == null)
                {
                    // Not an authorized endpoint, allow request to continue without authentication
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
                
                // For authorized endpoints, return 401 instead of redirecting
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"message\":\"Unauthorized\"}");
            },
            OnTokenValidated = async context =>
            {
                // Only validate token for authorized endpoints
                var endpoint = context.HttpContext.GetEndpoint();
                if (endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAuthorizeData>() == null)
                {
                    // Not an authorized endpoint, skip token validation
                    return;
                }

                var userManager = context.HttpContext.RequestServices
                    .GetRequiredService<UserManager<User>>();

                // Get user ID - ASP.NET Core maps JWT claims to XML schema claim types
                var userId = context.Principal?
                    .FindFirstValue(ClaimTypes.NameIdentifier) ??
                    context.Principal?
                    .FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                    context.Principal?
                    .FindFirstValue("sub");
                
                var tokenStamp = context.Principal?
                    .FindFirst("security_stamp")?.Value;

                if (userId == null)
                {
                    context.Fail("Invalid token: missing user ID");
                    return;
                }

                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    context.Fail("Token revoked: user not found");
                    return;
                }

                // If SecurityStamp is null in DB, update it and allow the request
                if (string.IsNullOrEmpty(user.SecurityStamp))
                {
                    await userManager.UpdateSecurityStampAsync(user);
                    return; // Allow the request to proceed
                }

                // Compare security stamps
                if (string.IsNullOrEmpty(tokenStamp))
                {
                    // Token was created before SecurityStamp was set, allow it
                    return;
                }

                if (tokenStamp != user.SecurityStamp)
                {
                    context.Fail("Token revoked: security stamp mismatch");
                    return;
                }
            }
        };
    });

// --------------------
// Swagger
// --------------------
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
        Description = "Enter JWT token",
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

// --------------------
// CORS
// --------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(builder.Configuration["App:BaseUrl"])
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Important for cookies (refresh token)
    });
});

// --------------------
// App Services
// --------------------
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IVaultService, VaultService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<ICloudflareR2Service, CloudflareR2Service>();
builder.Services.AddScoped<IVaultItemService, VaultItemService>();

var app = builder.Build();

// --------------------
// Automatic Database Migrations
// --------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDBContext>();
        dbContext.Database.Migrate(); // Automatically applies pending migrations
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// --------------------
// Dev-only behavior
// --------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Eloomen API v1");
        options.RoutePrefix = "swagger";
    });
}

// --------------------
// Middleware pipeline
// --------------------
app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
