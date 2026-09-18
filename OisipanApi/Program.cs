using System.Text.Json;
using System.Text.Json.Serialization;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Identity;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Oishipan.Models;
using Oishipan.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnectionString) ||
    defaultConnectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
    defaultConnectionString.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
{
    defaultConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=Oisipan;Trusted_Connection=True;TrustServerCertificate=True;";
}

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
        defaultConnectionString,
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null))
    .ConfigureWarnings(w => 
        w.Log(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Add Cloudinary service
builder.Services.Configure<CloudinaryOptions>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// Đăng ký JwtService
builder.Services.AddScoped<JwtService>();

// Cấu hình JWT Authentication
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

builder.Services.AddHttpClient();
builder.Services.AddScoped<Oishipan.Services.IShippingService, Oishipan.Services.ShippingService>();

// Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:5010")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (FirebaseApp.DefaultInstance == null)
{
    var credentialPath = Path.Combine(app.Environment.ContentRootPath, "firebase-adminsdk.json");
    if (File.Exists(credentialPath))
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromFile(credentialPath)
        });
    }
}

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

app.UseCors("AllowAll");
app.UseCors("AllowLocalhost");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
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

        if (!context.Database.CanConnect())
        {
            Console.WriteLine("Database is unavailable. Skipping migrations and admin seeding until SQL Server is running.");
        }
        else
        {
            try
            {
                context.Database.Migrate();

                if (TableExists(context, "Accounts"))
                {
                    // Some local databases were created from partial migrations and still need the newer Account columns.
                    EnsureAccountSchema(context);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration attempt failed: {ex.Message}");

                if (!TableExists(context, "Accounts"))
                {
                    try
                    {
                        context.Database.EnsureCreated();
                        Console.WriteLine("Database was reset to the current EF model because the existing schema was incomplete.");
                    }
                    catch (Exception ensureEx)
                    {
                        Console.WriteLine($"EnsureCreated fallback failed: {ensureEx.Message}");
                    }
                }

                if (TableExists(context, "Accounts"))
                {
                    EnsureAccountSchema(context);
                }
            }

            if (!TableExists(context, "Accounts"))
            {
                Console.WriteLine("Accounts table is still unavailable. Skipping admin seeding until the database schema is repaired.");
            }
            else
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
                    admin.AuthProvider = "Local";
                    admin.Password = passwordHasher.HashPassword(admin, adminPassword);
                }

                context.SaveChanges();
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Seeding error: {ex.Message}");
    }
}

app.Run();

static void EnsureAccountSchema(OishipanContext context)
{
    var sql = @"
        IF OBJECT_ID('dbo.Accounts', 'U') IS NULL
            RETURN;

        IF COL_LENGTH('Accounts', 'AuthProvider') IS NULL
            ALTER TABLE [Accounts] ADD [AuthProvider] nvarchar(20) NOT NULL DEFAULT 'Local';

        IF COL_LENGTH('Accounts', 'GoogleId') IS NULL
            ALTER TABLE [Accounts] ADD [GoogleId] nvarchar(100) NULL;

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Accounts_GoogleId' AND object_id = OBJECT_ID('Accounts'))
            CREATE UNIQUE INDEX [IX_Accounts_GoogleId] ON [Accounts] ([GoogleId]) WHERE [GoogleId] IS NOT NULL;
    ";

    context.Database.ExecuteSqlRaw(sql);
}

static bool TableExists(OishipanContext context, string tableName)
{
    var connection = context.Database.GetDbConnection();
    var wasOpen = connection.State == ConnectionState.Open;

    try
    {
        if (!wasOpen)
        {
            connection.Open();
        }

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT CASE WHEN OBJECT_ID(@tableName, 'U') IS NOT NULL THEN 1 ELSE 0 END";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = command.ExecuteScalar();
        return result is not null && Convert.ToInt32(result) == 1;
    }
    catch
    {
        return false;
    }
    finally
    {
        if (!wasOpen && connection.State == ConnectionState.Open)
        {
            connection.Close();
        }
    }
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
