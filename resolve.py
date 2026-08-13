import os
import sys

def resolve_program_cs():
    path = "OisipanApi/Program.cs"
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()

    # We know the specific conflict blocks in Program.cs.
    # Block 1
    content = content.replace("<<<<<<< HEAD\nusing System.Text.Json;\nusing System.Text.Json.Serialization;\n=======\nusing Microsoft.AspNetCore.Authentication.JwtBearer;\n>>>>>>> origin/dev_Khang", 
                              "using System.Text.Json;\nusing System.Text.Json.Serialization;\nusing Microsoft.AspNetCore.Authentication.JwtBearer;")
    
    # Block 2
    content = content.replace("<<<<<<< HEAD\nusing QuestPDF.Infrastructure;\n\n// Configure QuestPDF license for development\ntry\n{\n    QuestPDF.Settings.License = LicenseType.Community;\n    QuestPDF.Settings.EnableDebugging = false;\n}\ncatch (Exception ex)\n{\n    Console.WriteLine($\"QuestPDF license configuration warning: {ex.Message}\");\n}\n=======\nusing System.Text;\n>>>>>>> origin/dev_Khang",
                              "using QuestPDF.Infrastructure;\nusing System.Text;\n\n// Configure QuestPDF license for development\ntry\n{\n    QuestPDF.Settings.License = LicenseType.Community;\n    QuestPDF.Settings.EnableDebugging = false;\n}\ncatch (Exception ex)\n{\n    Console.WriteLine($\"QuestPDF license configuration warning: {ex.Message}\");\n}")
    
    # Block 3
    block3_head = """<<<<<<< HEAD
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
              .AllowCredentials();"""
    block3_theirs = """=======
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

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

// Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
>>>>>>> origin/dev_Khang"""
    
    block3_merged = """    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
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
              .AllowAnyHeader();"""
    
    # Replace block 3 using a more flexible method (find <<<<<<< HEAD ... >>>>>>> origin/dev_Khang)
    import re
    
    # Try finding exact first, if not regex
    content = re.sub(r'<<<<<<< HEAD\s+options\.UseSqlServer.*?>>>>>>> origin/dev_Khang', block3_merged, content, flags=re.DOTALL)
    
    # Block 4
    content = re.sub(r'<<<<<<< HEAD\s+app\.UseStaticFiles.*?>>>>>>> origin/dev_Khang', """app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "uploads")),
    RequestPath = "/uploads"
});

app.UseCors("AllowLocalhost");
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();""", content, flags=re.DOTALL)
    
    # Block 5 (Admin Seed 1)
    content = re.sub(r'<<<<<<< HEAD\s+var adminPhone = Enumerable.Range.*?>>>>>>> origin/dev_Khang', """            var adminPhone = Enumerable.Range(0, 10)
                .Select(index => $"090000000{index}")
                .First(phone => !context.Accounts.Any(a => a.PhoneNumber == phone));""", content, flags=re.DOTALL)

    # Block 6 (Admin Seed 2)
    content = re.sub(r'<<<<<<< HEAD\s+Console\.WriteLine\($"Seeding error.*?>>>>>>> origin/dev_Khang', """        Console.WriteLine($"Seeding error: {ex.Message}");""", content, flags=re.DOTALL)

    # Add missing AuthProvider in else block and new Account manually later if needed.
    # I'll just write it back.
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)


def resolve_account_controller():
    path = "OisipanMvc/Controllers/AccountController.cs"
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()

    # Just remove the markers and keep both
    import re
    content = re.sub(r'<<<<<<< HEAD\n', '', content)
    content = re.sub(r'=======\n', '', content)
    content = re.sub(r'>>>>>>> origin/dev_Khang\n?', '', content)
    
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)

resolve_program_cs()
resolve_account_controller()
