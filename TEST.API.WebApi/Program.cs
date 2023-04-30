using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace TEST.API.WebApi
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        public static bool IsStartedWithMain { get; private set; }

        public static void Main(string[] args)
        {
            IsStartedWithMain = true;
            try
            {
                BuildWebHost(args).Run();
            }
            catch (Exception exception) when (LogException(exception, "Application failed to start"))
            {
                // This will not be executed
            }
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var webHostBuilder = CreateWebHostBuilder(args);
            ConfigureKestrel(webHostBuilder);

            var webHost = webHostBuilder.Build();
            return webHost;
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging(x => x.AddApplicationInsights())
                .ConfigureAppConfiguration((ctx, builder) =>
                {
                })
                .UseStartup<Startup>();

        private static void ConfigureKestrel(IWebHostBuilder webHostBuilder)
        {
            webHostBuilder.ConfigureKestrel((ctx, options) =>
            {
                options.AddServerHeader = false;
                var config = ctx.Configuration;

                // If use HTTPS flag is true
                if (config.GetValue<bool>("KestrelOptions:UseSsl"))
                {
                    var certificate = config.GetValue<string>("KestrelOptions:Certificate");
                    var certificatePassword = config.GetValue<string>("KestrelOptions:CertificatePassword");
                    options.Listen(IPAddress.Any, 443, listenOptions =>
                    {
                        listenOptions.UseHttps(certificate, certificatePassword);
                    });
                }
            });
        }

        private static bool LogException(Exception exception, string message)
        {
            var telemetryMessage = new ExceptionTelemetry(exception)
            {
                Message = message
            };

            var telemetryConfig = TelemetryConfiguration.CreateDefault();
            var telemetryClient = new TelemetryClient(telemetryConfig);
            telemetryClient.TrackException(telemetryMessage);

            return false;
        }
    }
}

