using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TEST.API.Plugin.Commands;
using TEST.API.Plugin.Services;

namespace TEST.API.Plugin
{
    public static class PluginHelpers
    {
        public static Assembly LoadAssembly(this string path)
        {
            string pluginLocation = Path.GetFullPath(path);
            return new PluginLoadContext(pluginLocation).LoadFromAssemblyName(AssemblyName.GetAssemblyName(pluginLocation));
        }

        public static void LoadBaseServices(this Assembly assembly, IServiceCollection services)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(ICommand).IsAssignableFrom(type))
                {
                    services.AddSingleton(typeof(ICommand), type);
                }
                if (typeof(IServiceRegistrar).IsAssignableFrom(type))
                {
                    services.AddSingleton(typeof(IServiceRegistrar), type);
                }
            }
        }
    }
}
