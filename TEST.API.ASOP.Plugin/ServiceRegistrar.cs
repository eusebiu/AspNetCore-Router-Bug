using Microsoft.Extensions.DependencyInjection;
using TEST.API.ASOP.Plugin.Interfaces;
using TEST.API.ASOP.Plugin.Services;
using TEST.API.Plugin.Services;

namespace TEST.API.ASOP.Plugin
{
    public sealed class ServiceRegistrar : IServiceRegistrar
    {
        public void Register(IServiceCollection services)
        {
            services.AddScoped<IAsopService, AsopService>();
        }
    }
}
