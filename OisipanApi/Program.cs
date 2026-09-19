using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Oishipan.Models;
using Oishipan.Services;
using QuestPDF.Infrastructure;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// Configure QuestPDF license for development
try
{
    QuestPDF.Settings.License = LicenseType.Community;
    QuestPDF.Settings.EnableDebugging = false;
}
catch (Exception ex)
{
    Console.WriteLine($"QuestPDF license configuration warning: {ex.Message}");
}

var builder = WebApplication.CreateBuilder(args);
    if (FirebaseApp.DefaultInstance == null)
    {
        _ = FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromFile("firebasenumphone-adminsdk.json")
        });
    }
// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddDbContext<OishipanContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null))
    .ConfigureWarnings(w =>
        w.Log(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddScoped<JwtService>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"];
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret ?? "")),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

builder.Services.Configure<CloudinaryOptions>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins(
                  //"http://localhost:5010",
                  "http://localhost:5110",
                  "https://localhost:7111")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "uploads")),
    RequestPath = "/uploads"
});

app.UseHttpsRedirection();
app.UseCors("AllowLocalhost");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OishipanContext>();
    try
    {
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration failed: {ex}");
        throw;
    }

    try
    {
        var passwordHasher = new PasswordHasher<Account>();
        const string adminEmail = "admin123@gmail.com";
        const string adminPassword = "Admin@123";

        var admin = context.Accounts.FirstOrDefault(a => a.Email == adminEmail);
        if (admin is null)
        {
            var adminPhone = Enumerable.Range(0, 10)
                .Select(index => $"090000000{index}")
                .First(phone => !context.Accounts.Any(a => a.PhoneNumber == phone));

            admin = new Account
            {
                FullName = "Administrator",
                Email = adminEmail,
                PhoneNumber = adminPhone,
                Role = "Admin",
                Status = true,
                Address = "Oisipan",
                AuthProvider = "Local"
            };

            admin.Password = passwordHasher.HashPassword(admin, adminPassword);
            context.Accounts.Add(admin);
        }
        else
        {
            admin.FullName = string.IsNullOrWhiteSpace(admin.FullName) ? "Administrator" : admin.FullName;
            admin.Role = "Admin";
            admin.Status = true;
            admin.AuthProvider = string.IsNullOrWhiteSpace(admin.AuthProvider) ? "Local" : admin.AuthProvider;
            admin.Password = passwordHasher.HashPassword(admin, adminPassword);
        }

        context.SaveChanges();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Seeding error: {ex.Message}");
    }
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
