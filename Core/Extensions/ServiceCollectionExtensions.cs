using Core.Entities;
using Core.Http;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Profiles;
using Core.Repositories;
using Core.Services;
using Core.Validators;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configuration
            services.AddSingleton<IAppSettingsService, AppSettingsService>();

            // Auth
            services.AddSingleton<IAuthSession, AuthSession>();

            // HTTP Services
            services.AddHttpClient<IApiService, ApiService>();
            services.AddScoped<IPatientHttpClient, PatientHttpClient>();

            // Core Services
            services.AddScoped<IUserService, UserService>();
            services.AddSingleton<IConnectionService, ConnectionService>();
            services.AddScoped<ILoginService, LoginService>();
            services.AddScoped<IVisitService, VisitService>();
            services.AddScoped<IPatientMappingService, PatientMappingService>();
            services.AddScoped<IUserMappingService, UserMappingService>();
            services.AddScoped<ILabResultsMappingService, LabResultsMappingService>();
            services.AddScoped<IPatientService, PatientService>();

            // Repositories
            services.AddScoped<IPatientRepository, PatientRepository>();
            services.AddScoped<IVisitRepository, VisitRepository>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddSingleton<ISpecialtyProfile, ObGyneProfile>();
            services.AddSingleton<ISpecialtyProfile, OphthalmologyProfile>();
            services.AddSingleton<ISpecialtyProfile, OrthopedicProfile>();

            // Clinical catalog aggregates systems, sections and registered profiles
            services.AddSingleton<ClinicalCatalog>(sp => new ClinicalCatalog(sp.GetServices<ISpecialtyProfile>()));
            services.AddScoped<ITestCatalogRepository, TestCatalogRepository>();
            services.AddScoped<IDrugCatalogRepository, DrugCatalogRepository>();
            services.AddScoped<ILabResultsRepository, LabResultsRepository>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            // Validators
            services.AddValidatorsFromAssemblyContaining<PatientCreateDtoValidator>();

            // Logging is already configured by default in ASP.NET Core

            return services;
        }
    }
}