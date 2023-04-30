using Microsoft.Extensions.DependencyInjection;

namespace TEST.API.Plugin.Services
{
    public interface IServiceRegistrar
    {
        void Register(IServiceCollection services);
    }
}
