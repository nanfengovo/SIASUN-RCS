using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using OpenIddict.Server.AspNetCore;
using SIASUN.RCS.EntityFrameworkCore;
using SIASUN.RCS.MultiTenancy;
using SIASUN.RCS.HealthChecks;
using Microsoft.OpenApi;
using Volo.Abp;
using Volo.Abp.Studio;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.Libs;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite.Bundling;
using Microsoft.AspNetCore.Hosting;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.OpenIddict;
using Volo.Abp.Swashbuckle;
using Volo.Abp.Studio.Client.AspNetCore;
using Volo.Abp.Security.Claims;
using SIASUN.RCS.Infrastructure.Logging;
using SIASUN.RCS.Infrastructure.AuditLog.Sqlite;
using System;

namespace SIASUN.RCS;

[DependsOn(
    typeof(RCSHttpApiModule),
    typeof(AbpStudioClientAspNetCoreModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreMultiTenancyModule),
    typeof(RCSApplicationModule),
    typeof(RCSEntityFrameworkCoreModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpSwashbuckleModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(RCSInfrastructureLoggingModule),
    typeof(RCSInfrastructureAuditLogSqliteModule)
    )]
[ExcludeFromCodeCoverage]
public class RCSHttpApiHostModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();

        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("RCS");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });

        if (!hostingEnvironment.IsDevelopment())
        {
            var pfxPath = Path.Combine(hostingEnvironment.ContentRootPath, "openiddict.pfx");
            if (File.Exists(pfxPath))
            {
                PreConfigure<AbpOpenIddictAspNetCoreOptions>(options =>
                {
                    options.AddDevelopmentEncryptionAndSigningCertificate = false;
                });

                PreConfigure<OpenIddictServerBuilder>(serverBuilder =>
                {
                    serverBuilder.AddProductionEncryptionAndSigningCertificate("openiddict.pfx", configuration["AuthServer:CertificatePassPhrase"]!);
                    serverBuilder.SetIssuer(new Uri(configuration["AuthServer:Authority"]!));
                });
            }
            else
            {
                PreConfigure<AbpOpenIddictAspNetCoreOptions>(options =>
                {
                    options.AddDevelopmentEncryptionAndSigningCertificate = true;
                });
            }
        }
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (!configuration.GetValue<bool>("App:DisablePII"))
        {
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.LogCompleteSecurityArtifact = true;
        }

        if (!configuration.GetValue<bool>("AuthServer:RequireHttpsMetadata"))
        {
            Configure<OpenIddictServerAspNetCoreOptions>(options =>
            {
                options.DisableTransportSecurityRequirement = true;
            });

            Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }

        if (hostingEnvironment.IsDevelopment())
        {
            context.Services.AddRazorPages()
                .AddRazorRuntimeCompilation();
        }

        ConfigureStudio(hostingEnvironment);
        ConfigureAuthentication(context);
        ConfigureUrls(configuration);
        ConfigureBundles(hostingEnvironment);
        ConfigureConventionalControllers();
        ConfigureHealthChecks(context);
        ConfigureSwagger(context, configuration);
        ConfigureVirtualFileSystem(context);
        ConfigureCors(context, configuration);

        Configure<AbpMvcLibsOptions>(options =>
        {
            options.CheckLibs = false;
        });
    }

    private void ConfigureStudio(IHostEnvironment hostingEnvironment)
    {
        if (hostingEnvironment.IsProduction())
        {
            Configure<AbpStudioClientOptions>(options =>
            {
                options.IsLinkEnabled = false;
            });
        }
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context)
    {
        context.Services.ForwardIdentityAuthenticationForBearer(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });
    }

    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
            options.RedirectAllowedUrls.AddRange(configuration["App:RedirectAllowedUrls"]?.Split(',') ?? Array.Empty<string>());
        });
    }

    private void ConfigureBundles(IHostEnvironment hostingEnvironment)
    {
        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                LeptonXLiteThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                }
            );

            options.ScriptBundles.Configure(
                LeptonXLiteThemeBundles.Scripts.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-scripts.js");
                    if (hostingEnvironment.IsDevelopment())
                    {
                        bundle.AddFiles("/dev-login-helper.js");
                    }
                }
            );
        });
    }


    private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                TryReplaceEmbeddedByPhysical<RCSDomainSharedModule>(options, hostingEnvironment, "02.Domain", "SIASUN.RCS.Domain.Shared");
                TryReplaceEmbeddedByPhysical<RCSDomainModule>(options, hostingEnvironment, "02.Domain", "SIASUN.RCS.Domain");
                TryReplaceEmbeddedByPhysical<RCSApplicationContractsModule>(options, hostingEnvironment, "03.Application", "SIASUN.RCS.Application.Contracts");
                TryReplaceEmbeddedByPhysical<RCSApplicationModule>(options, hostingEnvironment, "03.Application", "SIASUN.RCS.Application");
            });
        }
    }

    private static void TryReplaceEmbeddedByPhysical<TModule>(
        AbpVirtualFileSystemOptions options,
        IHostEnvironment hostingEnvironment,
        string layerFolder,
        string projectName)
    {
        var targetPath = Path.GetFullPath(Path.Combine(
            hostingEnvironment.ContentRootPath,
            "..",
            "..",
            layerFolder,
            projectName));

        if (Directory.Exists(targetPath))
        {
            options.FileSets.ReplaceEmbeddedByPhysical<TModule>(targetPath);
        }
    }

    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(RCSApplicationModule).Assembly);
        });
    }

    private static void ConfigureSwagger(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAbpSwaggerGenWithOidc(
            configuration["AuthServer:Authority"]!,
            ["RCS"],
            [AbpSwaggerOidcFlows.AuthorizationCode],
            null,
            options =>
            {
                //定义多个分组；新增分组复制一行就可以
                options.SwaggerDoc("system", new OpenApiInfo { Title = "ABP 系统底层基础接口", Version = "v1" });
                options.SwaggerDoc("business", new OpenApiInfo { Title = "RCS 核心业务接口", Version = "v1" });
                options.SwaggerDoc("adapters", new OpenApiInfo { Title = "RCS 适配器接口", Version = "v1" });
                options.SwaggerDoc("monitor", new OpenApiInfo { Title = "RCS 监控和仪表盘", Version = "v1" });
                // 根据路由精准分流（自定义业务精准匹配，system 作为其余 ABP 底层接口的闭环兜底）
                options.DocInclusionPredicate((docName, description) =>
                {
                    var path = description.RelativePath ?? string.Empty;

                    var isBusiness = path.StartsWith("api/rcs/", StringComparison.OrdinalIgnoreCase);
                    var isAdapters = path.StartsWith("api/adapters/", StringComparison.OrdinalIgnoreCase);
                    var isMonitor = path.StartsWith("api/monitor/", StringComparison.OrdinalIgnoreCase);

                    return docName switch
                    {
                        "business" => isBusiness,
                        "adapters" => isAdapters,
                        "monitor" => isMonitor,
                        "system" => !isBusiness && !isAdapters && !isMonitor,
                        _ => true
                    };
                });
                options.CustomSchemaIds(type => type.FullName);
                //xml 注释
                var httpApiXml = Path.Combine(AppContext.BaseDirectory, "SIASUN.RCS.HttpApi.xml");
                if (File.Exists(httpApiXml))
                {
                    options.IncludeXmlComments(httpApiXml, true);
                }
                var applicationXml = Path.Combine(AppContext.BaseDirectory, "SIASUN.RCS.Application.xml");
                if (File.Exists(applicationXml))
                {
                    options.IncludeXmlComments(applicationXml, true);
                }
            });
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(
                        configuration["App:CorsOrigins"]?
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.Trim().RemovePostFix("/"))
                            .ToArray() ?? Array.Empty<string>()
                    )
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    private void ConfigureHealthChecks(ServiceConfigurationContext context)
    {
        context.Services.AddRCSHealthChecks();
    }


    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        app.UseForwardedHeaders();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
        }

        app.UseRouting();
        app.MapAbpStaticAssets();
        app.UseAbpStudioLink();
        app.UseAbpSecurityHeaders();
        app.UseCors();
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();

        if (MultiTenancyConsts.IsEnabled)
        {
            app.UseMultiTenancy();
        }

        app.UseUnitOfWork();
        app.UseDynamicClaims();
        app.UseAuthorization();

        app.UseSwagger();
        app.UseAbpSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/business/swagger.json", "RCS 核心业务接口");
            options.SwaggerEndpoint("/swagger/adapters/swagger.json", "RCS 硬件和三方系统适配器接口");
            options.SwaggerEndpoint("/swagger/system/swagger.json", "ABP 系统底层基础接口");
            options.SwaggerEndpoint("/swagger/monitor/swagger.json", "RCS 监控和仪表盘");

            var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
            options.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);

            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            options.EnableFilter();
        });

        // 初始化 API 审计日志过滤规则引擎（预加载规则至内存快照）
        var filterEvaluator = context.ServiceProvider.GetRequiredService<SIASUN.RCS.Infrastructure.Logging.Filtering.IAuditLogFilterEvaluator>();
        await filterEvaluator.InitializeAsync();

        var entityEvaluator = context.ServiceProvider.GetRequiredService<SIASUN.RCS.Auditing.IEntityAuditRuleEvaluator>();
        await entityEvaluator.RefreshRulesAsync();

        // 报文日志拦截中间件
        app.UseMiddleware<SIASUN.RCS.Infrastructure.Logging.InboundAuditMiddleware>();
        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints();
    }
}
