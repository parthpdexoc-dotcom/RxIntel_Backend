using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Rxintel.AI.HostedServices;
using Rxintel.AI.Mappings;
using Rxintel.Generic.Entities.Entities;
using Rxintel.Generic.Models.Filter;
using Rxintel.Generic.Repositories.Interfaces;
using Rxintel.Generic.Repositories.Repositories;
using Rxintel.Generic.Services.Interfaces;
using Rxintel.Generic.Services.Services;
using Rxintel.Generic.Services.TypeSafeAI;
using Rxintel.Generic.Services.MetaAI;
using Rxintel.AI.Converters;

namespace Rxintel.AI.Extensions;

/// <summary>
/// Host composition root helpers (aligned with OrigencePart DependencyInjection pattern).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers framework defaults: auth, controllers, validation, CORS, Swagger, AutoMapper, DbContext.
    /// </summary>
    public static IServiceCollection InitializeDefaultService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<Rxintel.Generic.Common.Implementations.Helpers.Configurations>();
        services.AddScoped<ITokenParser, TokenParser>();
        services.AddJwtAuth(configuration);
        services.AddAuthorization();

        services.AddControllers(options =>
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.Filters.Add(new AuthorizeFilter(policy));
            options.Filters.Add(typeof(CustomExceptionFilter));
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new FlexibleNullableDateTimeConverter());
            options.JsonSerializerOptions.Converters.Add(new FlexibleDateTimeConverter());
            options.JsonSerializerOptions.Converters.Add(new FlexibleNullableTimeSpanConverter());
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssembly(typeof(Program).Assembly);

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                policy =>
                {
                    policy.WithOrigins("http://localhost:3000")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .WithExposedHeaders("Content-Disposition");
                });
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header: Bearer {token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

        services.AddAutoMapper(
            typeof(AdminMappingProfile),
            typeof(LoginMappingProfile),
            typeof(PharmacyMappingProfile),
            typeof(PharmacyUserMappingProfile),
            typeof(InsuranceGroupMappingProfile),
            typeof(InsuranceMappingProfile),
            typeof(NDCGroupMappingProfile),
            typeof(MedicineMappingProfile),
            typeof(PharmacyWholesalerMappingProfile),
            typeof(WholesalerMappingProfile),
            typeof(BillingFieldMappingMappingProfile),
            typeof(WholesalerFieldMappingMappingProfile),
            typeof(BillingMappingProfile),
            typeof(PurchaseMappingProfile),
            typeof(StateMappingProfile),
            typeof(PharmacySoftwareMappingProfile),
            typeof(ErxAuditMappingProfile),
            typeof(TermsMappingProfile));

        services.AddDbContext<RxintelDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }

    /// <summary>
    /// Registers application repositories and services.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Repositories / Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<ILoginRepository, LoginRepository>();
        services.AddScoped<IPharmacyRepository, PharmacyRepository>();
        services.AddScoped<IPharmacyUserRepository, PharmacyUserRepository>();
        services.AddScoped<IInsuranceGroupRepository, InsuranceGroupRepository>();
        services.AddScoped<IInsuranceRepository, InsuranceRepository>();
        services.AddScoped<INDCGroupRepository, NDCGroupRepository>();
        services.AddScoped<IMedicineRepository, MedicineRepository>();
        services.AddScoped<IPharmacyWholesalerRepository, PharmacyWholesalerRepository>();
        services.AddScoped<IWholesalerRepository, WholesalerRepository>();
        services.AddScoped<IBillingFieldMappingRepository, BillingFieldMappingRepository>();
        services.AddScoped<IWholesalerFieldMappingRepository, WholesalerFieldMappingRepository>();
        services.AddScoped<IImportErrorFileRepository, ImportErrorFileRepository>();
        services.AddScoped<IImportErrorDetailRepository, ImportErrorDetailRepository>();
        services.AddScoped<IBillingRepository, BillingRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<IStateRepository, StateRepository>();
        services.AddScoped<IPharmacySoftwareRepository, PharmacySoftwareRepository>();
        services.AddScoped<IErxAuditBatchRepository, ErxAuditBatchRepository>();
        services.AddScoped<IErxAuditRecordRepository, ErxAuditRecordRepository>();
        services.AddScoped<IErxColumnMatchRuleRepository, ErxColumnMatchRuleRepository>();
        services.AddScoped<IErxAuditDiscrepancyRepository, ErxAuditDiscrepancyRepository>();
        services.AddScoped<IErxAuditAlertLogRepository, ErxAuditAlertLogRepository>();
        services.AddScoped<IInsuranceCompanyRepository, InsuranceCompanyRepository>();
        services.AddScoped<IErxSigMappingRepository, ErxSigMappingRepository>();
        services.AddScoped<ITermsAndConditionsRepository, TermsAndConditionsRepository>();

        // Services
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IPharmacyService, PharmacyService>();
        services.AddScoped<IPharmacyRegistrationService, PharmacyRegistrationService>();
        services.AddScoped<ITermsService, TermsService>();
        services.AddScoped<IPharmacyUserService, PharmacyUserService>();
        services.AddScoped<IInsuranceGroupService, InsuranceGroupService>();
        services.AddScoped<IInsuranceService, InsuranceService>();
        services.AddScoped<INDCGroupService, NDCGroupService>();
        services.AddScoped<IMedicineService, MedicineService>();
        services.AddScoped<IPharmacyWholesalerService, PharmacyWholesalerService>();
        services.AddScoped<IWholesalerService, WholesalerService>();
        services.AddScoped<IBillingFieldMappingService, BillingFieldMappingService>();
        services.AddScoped<IWholesalerFieldMappingService, WholesalerFieldMappingService>();
        services.AddScoped<IImportErrorService, ImportErrorService>();
        services.AddHttpClient<IOpenFdaNdcResolver, OpenFdaNdcResolver>(client =>
        {
            client.BaseAddress = new Uri("https://api.fda.gov/");
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RxIntel/1.0");
        });
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IStateService, StateService>();
        services.AddScoped<IPharmacySoftwareService, PharmacySoftwareService>();
        services.AddScoped<IDataAnalyticsService, DataAnalyticsService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddSingleton<IBackgroundTaskRunner, BackgroundTaskRunner>();
        services.AddScoped<IBillingFieldMappingNotificationService, BillingFieldMappingNotificationService>();
        services.AddScoped<IWholesalerFieldMappingNotificationService, WholesalerFieldMappingNotificationService>();
        services.AddScoped<IWholesalerNotificationService, WholesalerNotificationService>();
        services.AddHostedService<WholesalerExpiryReminderHostedService>();
        services.AddSingleton<IErxReferenceDataService, ErxReferenceDataService>();
        services.AddSingleton<IDeaScheduleService, DeaScheduleService>();
        services.AddScoped<IErxSigMappingService, ErxSigMappingService>();
        services.AddScoped<IErxComparisonEngine, ErxComparisonEngine>();
        services.AddScoped<IErxAuditAlertService, ErxAuditAlertService>();
        services.AddScoped<IErxAuditImportService, ErxAuditImportService>();
        services.AddScoped<IErxAuditService, ErxAuditService>();
        services.AddScoped<IAutonomousAuditLearningService, AutonomousAuditLearningService>();
        services.AddHostedService<MuseAutonomousLearningHostedService>();

        return services;
    }

    /// <summary>
    /// Primes reference data caches (Scheduled Drugs, State Validity, Supervising Rules, NPI Directory) on startup.
    /// </summary>
    public static async Task<WebApplication> InitializeReferenceDataAsync(this WebApplication app)
    {
        try
        {
            var refService = app.Services.GetService<IErxReferenceDataService>();
            if (refService != null)
            {
                await refService.EnsureLoadedAsync();
            }
        }
        catch
        {
            // Fail-safe
        }

        return app;
    }

    /// <summary>
    /// Primes SIG mappings dynamic cache and synchronizes into PharmacySigNormalizer on startup.
    /// </summary>
    public static async Task<WebApplication> InitializeSigMappingsAsync(this WebApplication app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var sigService = scope.ServiceProvider.GetService<IErxSigMappingService>();
            if (sigService != null)
            {
                await sigService.EnsureCacheLoadedAsync();
            }
        }
        catch
        {
            // Fail-safe if database connection is deferred at initial bootstrap
        }

        return app;
    }

    /// <summary>
    /// Registers TypeSafe AI HTTP client and configuration options.
    /// </summary>
    public static IServiceCollection AddTypeSafeClient(
        this IServiceCollection services,
        Action<TypeSafeClientOptions> configureOptions)
    {
        var options = new TypeSafeClientOptions();
        configureOptions(options);
        services.AddSingleton(options);

        services.AddHttpClient<ITypeSafeClient, TypeSafeClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
            }
        });

        return services;
    }

    /// <summary>
    /// Registers Meta Model AI (dev.meta.ai) HTTP client and configuration options.
    /// Supports Llama models for SIG, Days Supply, and Qty Dispensed evaluations.
    /// </summary>
    public static IServiceCollection AddMetaAiClient(
        this IServiceCollection services,
        Action<MetaAiClientOptions> configureOptions)
    {
        var options = new MetaAiClientOptions();
        configureOptions(options);
        services.AddSingleton(options);

        services.AddHttpClient<IMetaAiClient, MetaAiClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 15);
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
            }
        });

        return services;
    }
}
