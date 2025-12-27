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
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!)
            )
        };
        
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userManager = context.HttpContext.RequestServices
                    .GetRequiredService<UserManager<User>>();

                var userId = context.Principal?
                    .FindFirstValue(JwtRegisteredClaimNames.Sub);

                var tokenStamp = context.Principal?
                    .FindFirst("security_stamp")?.Value;

                if (userId == null || tokenStamp == null)
                {
                    context.Fail("Invalid token");
                    return;
                }

                var user = await userManager.FindByIdAsync(userId);

                if (user == null || user.SecurityStamp != tokenStamp)
                {
                    context.Fail("Token revoked");
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
// App Services
// --------------------
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

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
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
