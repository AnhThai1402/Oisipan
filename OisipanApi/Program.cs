using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Oishipan.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddDbContext<OishipanContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
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
    var context = scope.ServiceProvider.GetRequiredService<OishipanContext>();
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

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
