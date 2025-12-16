using System.Text;
using Auth_Server.Data;
using Auth_Server.Extensions;
using Auth_Server.Model;
using Auth_Server.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

builder
    .Services.AddIdentityCore<UserAggregate>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Bearer";
        options.DefaultChallengeScheme = "Bearer";
    })
    .AddJwtBearer(
        "Bearer",
        options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                RequireExpirationTime = false,
                ValidIssuer = builder.Configuration["Jwt:Application:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Application:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Application:Key"]!)
                ),
            };

            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    Console.WriteLine(
                        $"Token validated successfully for: {context.Principal?.Identity?.Name}"
                    );
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    Console.WriteLine($"Challenge: {context.Error} - {context.ErrorDescription}");
                    return Task.CompletedTask;
                },
            };
        }
    );

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "ApplicationToken",
        policy =>
        {
            policy.RequireAuthenticatedUser();
        }
    );
});
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IApplicationJwtService, ApplicationJwtService>();
builder.Services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

builder.Services.AddFastEndpoints();

var app = builder.Build();

// Configure the HTTP request pipeline.
await app.ApplyMigrations();
await app.Services.SeedDatabaseAsync();

//app.CreateApplicationTokens();

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}
app.Run();
