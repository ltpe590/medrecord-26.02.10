using Core.Configuration;
using Core.Data.Context;
using Core.Data.Seeding;
using Core.Entities;
using Core.Http;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Middleware;
using Core.Profiles;
using Core.Repositories;
using Core.Services;
using Core.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===== Configuration =====
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// ===== Logging =====
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

// ===== Database =====
var cs = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"[DB CONNECTION STRING] {cs}");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("Core")));

// ===== Identity & Authentication =====
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var secretKey = builder.Configuration["Jwt:Key"] ??
    throw new InvalidOperationException("JWT Key not found in configuration");
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false, // Set to true in production with valid issuer
        ValidateAudience = false, // Set to true in production with valid audience
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ===== HTTP Services =====
builder.Services.AddHttpClient<IApiService, ApiService>();
builder.Services.AddScoped<IApiService, ApiService>();

// ===== Consolidated Diagnostic Service (Replaces DebugService, ApplicationLoggingService, etc.) =====

// ===== Application Settings =====
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSingleton<IAppSettingsService>(sp =>
    sp.GetRequiredService<IOptions<AppSettings>>().Value);

// ===== FluentValidation =====
builder.Services.AddValidatorsFromAssemblyContaining<PatientCreateDtoValidator>();

// ===== Repositories =====
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IVisitRepository, VisitRepository>();
builder.Services.AddScoped<IDrugCatalogRepository, DrugCatalogRepository>();
builder.Services.AddScoped<ILabResultsRepository, LabResultsRepository>();
builder.Services.AddScoped<ITestCatalogRepository, TestCatalogRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();

// ===== Services =====
builder.Services.AddScoped<IAuthSession, AuthSession>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IVisitService, VisitService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddSingleton<ISpecialtyProfile, ObGyneProfile>();
builder.Services.AddSingleton<ISpecialtyProfile, OphthalmologyProfile>();
builder.Services.AddSingleton<ISpecialtyProfile, OrthopedicProfile>();

// Register ClinicalCatalog to aggregate systems/sections/profiles
builder.Services.AddSingleton<ClinicalCatalog>(sp => new ClinicalCatalog(sp.GetServices<ISpecialtyProfile>()));
builder.Services.AddScoped<IPatientMappingService, PatientMappingService>();
builder.Services.AddScoped<IUserMappingService, UserMappingService>();
builder.Services.AddScoped<ILabResultsMappingService, LabResultsMappingService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

// ===== Controllers with JSON Configuration =====
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
    });

// ===== API Explorer & Swagger =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MedRecordWebApi API",
        Version = "v1",
        Description = "API for medical records management",
        Contact = new OpenApiContact
        {
            Name = "Medical Records Team",
            Email = "support@medrecords.example.com"
        }
    });

    // Add JWT Authentication support to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme.
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// ===== CORS (Configure as needed) =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// ===== Seed Default Users =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userStore = scope.ServiceProvider.GetRequiredService<IUserStore<AppUser>>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();

    try
    {
        await UserSeeder.SeedAdminUserAsync(db, userStore, passwordHasher);
        await TestCatalogSeeder.SeedAsync(db);
        await DrugCatalogSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SEEDING ERROR] {ex.Message}");
    }
}

// ===== Exception Handling Middleware =====
app.UseExceptionHandling();

// ===== Development Configuration =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MedRecordWebApi API V1");
        c.ConfigObject.AdditionalItems.Add("persistAuthorization", "true");
        c.DisplayRequestDuration();
    });

    // Development-only middleware
    app.UseDeveloperExceptionPage();
}
else
{
    // Production error handling
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// ===== Security Middleware =====
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// ===== Health Check Endpoint =====
app.MapHealthChecks("/health");

// ===== Controllers =====
app.MapControllers();

app.Run();