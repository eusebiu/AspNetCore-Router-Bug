using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using TEST.API.WebApi.Middleware;
using TEST.API.WebApi.Secured;
using TEST.API.Plugin;
using TEST.API.Plugin.Services;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.FileProviders;

namespace TEST.API.WebApi
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        private readonly ILogger<Startup> _logger;
        private readonly bool _isDev;

        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory, IWebHostEnvironment env)
        {
            Configuration = configuration;

            // Create the Startup logger after adding the AI logger
            _logger = loggerFactory.CreateLogger<Startup>();

            _isDev = env.IsDevelopment();
        }

        public IConfiguration Configuration { get; }

        public void ConfigureDevelopmentServices(IServiceCollection services)
        {
            ConfigureBaseServices(services);
        }

        public void ConfigureStagingServices(IServiceCollection services)
        {
            ConfigureBaseServices(services);
        }

        public void ConfigureProductionServices(IServiceCollection services)
        {
            ConfigureBaseServices(services);

            // Optional. Configure HSTS if you want to use this OWASP recommendation. https://www.owasp.org/index.php/HTTP_Strict_Transport_Security_Cheat_Sheet
            services.AddHsts(x =>
            {
                x.Preload = false; // Only necessary for publicly accessible apps/api's. You need to submit the URI for this to work to https://hstspreload.org/
                x.IncludeSubDomains = false; // To either include or exclude all subdomains of the root URI
            });
        }

        private void ConfigureBaseServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddApplicationInsightsTelemetry();

            services.AddAzureClients(builder =>
            {

            });

            services
                .AddCors()
                .AddControllers(options =>
                {
                    options.Filters.Add(new ResponseCacheAttribute
                    {
                        NoStore = true,
                        Location = ResponseCacheLocation.None
                    });
                    //options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                    options.OutputFormatters.RemoveType<StringOutputFormatter>();
                }).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                //.AddJwtBearer(options => Configuration.GetSection("Authentication").Bind(options))
                .AddMicrosoftIdentityWebApi(Configuration.GetSection("Authentication"))
                    .EnableTokenAcquisitionToCallDownstreamApi()
                        .AddMicrosoftGraph(Configuration.GetSection("DownstreamApi"))
                        .AddInMemoryTokenCaches();
            services.AddAuthorization();

            if (_isDev)
            {
                ConfigureSwagger(services);
            }

            RegisterPlugins(services);

            // Register AutoMapper profiles
            services.AddAutoMapper(System.Reflection.Assembly.GetExecutingAssembly());
        }

        private void RegisterPlugins(IServiceCollection services)
        {
            var pluginsPath = Path.Combine(AppContext.BaseDirectory, @".\Plugins");
            string[] pluginPaths = Directory.GetFiles(pluginsPath, "*.Plugin.dll", SearchOption.AllDirectories);

            foreach (var pluginPath in pluginPaths)
            {
                Assembly pluginAssembly = pluginPath.LoadAssembly();
                pluginAssembly.LoadBaseServices(services);
                services.AddAutoMapper(pluginAssembly);

                IMvcBuilder mvcBuilder = services.AddControllers();
                mvcBuilder.ConfigureApplicationPartManager(pm =>
                {
                    pm.ApplicationParts.Add(new AssemblyPart(pluginAssembly));
                });
            }

            // init plugins
            var serviceProvider = services.BuildServiceProvider();
            var registrars = serviceProvider.GetServices<IServiceRegistrar>();

            foreach (var registrar in registrars)
            {
                registrar.Register(services);
            }
        }

        public void ConfigureDevelopment(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();

            DefaultHttpPipeline(app);
        }

        public void ConfigureStaging(IApplicationBuilder app)
        {
            app.UseHttpsRedirection();

            DefaultHttpPipeline(app);
        }

        public void ConfigureProduction(IApplicationBuilder app)
        {
            app.UseHttpsRedirection();

            // HSTS tells your web browser to cache the fact that your web app/api should
            // only be reachable over HTTPS. A browser then performs a local redirect to HTTPS.
            app.UseHsts();

            DefaultHttpPipeline(app);
        }

        private void DefaultHttpPipeline(IApplicationBuilder app)
        {
            var policyCollection = new HeaderPolicyCollection()
                .AddFrameOptionsDeny()
                .AddXssProtectionBlock()
                .AddContentTypeOptionsNoSniff()
                .AddStrictTransportSecurityMaxAgeIncludeSubDomains(maxAgeInSeconds: 60 * 60 * 24 * 365) // maxage = one year in seconds
                .AddReferrerPolicyStrictOriginWhenCrossOrigin()
                .RemoveServerHeader()
                .AddContentSecurityPolicy(builder =>
                {
                    builder.AddObjectSrc().None();
                    builder.AddFormAction().Self();
                    builder.AddFrameAncestors().None();
                });
            app.UseSecurityHeaders(policyCollection);

            app.UseRouting();
            var origins = GetCorsOrigins();
            app.UseCors(builder => builder
                .WithOrigins(origins)
                .SetPreflightMaxAge(TimeSpan.FromHours(24))
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());

            if (_isDev)
            {
                // Before UseAuthentication to allow anonymous users to access the API docs.
                AddSwagger(app);
            }

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHeaderRemover("X-Powered-By", "x-powered-by");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }

        private void ConfigureSwagger(IServiceCollection services)
        {
            var tenantId = Configuration["Authentication:TenantId"];
            var apiVersions = GetApiVersions();

            if (apiVersions.Length == 0)
            {
                _logger.LogError("Missing \"ApiVersions\" configuration entry for Swagger in appsettings.json!");
                return;
            }

            services.AddSwaggerGen(x =>
            {
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);

                if (!System.IO.File.Exists(xmlPath))
                {
                    _logger.LogWarning("XML documentation file not found! Expected location: {0}", xmlPath);
                }
                else
                {
                    x.IncludeXmlComments(xmlPath);
                }

                // What if the same version occurs multiple times?
                foreach (var apiVersion in apiVersions)
                {
                    x.SwaggerDoc(apiVersion.Version, apiVersion);
                    x.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme()
                    {
                        Type = SecuritySchemeType.OAuth2,
                        Flows = new OpenApiOAuthFlows()
                        {
                            AuthorizationCode = new OpenApiOAuthFlow()
                            {
                                AuthorizationUrl = new Uri($"https://login.windows.net/{tenantId}/oauth2/v2.0/authorize"),
                                TokenUrl = new Uri($"https://login.windows.net/{tenantId}/oauth2/v2.0/token"),
                                Scopes = new Dictionary<string, string> { { "user.read", "authenticaatino" } }
                            }
                        }
                    }); ;
                    x.OperationFilter<SecurityRequirementsOperationFilter>();
                    x.IgnoreObsoleteActions();
                    x.IgnoreObsoleteProperties();
                }
            });
        }

        private void AddSwagger(IApplicationBuilder app)
        {
            var swaggerClientId = Configuration["Swagger:ClientId"];
            var swaggerClientSecret = Configuration["Swagger:ClientSecret"];
            var clientId = Configuration["Authentication:ClientId"];
            var audience = Configuration["Authentication:Audience"];
            var apiVersions = GetApiVersions();

            if (apiVersions.Length == 0)
            {
                // We already logged an error, so we simple exit without setting up swagger
                return;
            }

#if DEBUG
            app.UseSwagger(c =>
                {
                    c.RouteTemplate = "api/swagger/{documentname}/swagger.json";
                })
                .UseSwaggerUI(x =>
                {
                    foreach (var apiVersion in apiVersions.OrderBy(v => v.Version))
                    {
                        x.RoutePrefix = "api/swagger";
                        x.DocumentTitle = "MODX.API Swagger UI";
                        x.OAuthClientId(swaggerClientId);
                        x.OAuthClientSecret(swaggerClientSecret);
                        x.OAuthRealm(clientId);
                        x.OAuthAppName("MODX.API Swagger");
                        x.OAuthScopeSeparator(" ");
                        //x.OAuthAdditionalQueryStringParams(new Dictionary<string, string>() { { "resource", audience } });
                        x.OAuthUseBasicAuthenticationWithAccessCodeGrant();
                        x.OAuthUsePkce();
                        x.SwaggerEndpoint($"{apiVersion.Version}/swagger.json", $"{apiVersion.Title} {apiVersion.Version}");
                    }
                });
#endif
        }

        private OpenApiInfo[] GetApiVersions() => GetConfigurationValues<OpenApiInfo>("ApiVersions");
        private string[] GetCorsOrigins() => Configuration
            .GetValue<string>("AllowedOrigins")
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        private T[] GetConfigurationValues<T>(string key) => Configuration.GetSection(key).Get<T[]>() ?? Array.Empty<T>();
    }
}
