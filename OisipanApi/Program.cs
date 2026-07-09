using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Oishipan.Models;
using Oishipan.Services;
using QuestPDF.Infrastructure;

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

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddDbContext<OishipanContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    .ConfigureWarnings(w => 
        w.Log(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Add Cloudinary service
builder.Services.Configure<CloudinaryOptions>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
// Enable CORS so the frontend running on http://localhost:5010 can call this API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:5010")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
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
// Apply CORS policy
app.UseCors("AllowLocalhost");
app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
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
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<OishipanContext>();
        
        // Ensure database is created and migrations are applied
        context.Database.Migrate();
        
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
                Address = "Oisipan"
            };

            admin.Password = passwordHasher.HashPassword(admin, adminPassword);
            context.Accounts.Add(admin);
        }
        else
        {
            admin.FullName = string.IsNullOrWhiteSpace(admin.FullName) ? "Administrator" : admin.FullName;
            admin.Role = "Admin";
            admin.Status = true;
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
