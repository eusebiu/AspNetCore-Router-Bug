using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TEST.API.ASOP.Plugin.Interfaces;

namespace TEST.API.ASOP.Plugin.Services
{
    public class AsopService : IAsopService
    {

        public ILogger<AsopService> Logger { get; }
        public IConfiguration Configuration { get; }

        public AsopService(ILogger<AsopService> logger, IConfiguration configuration)
        {
            Logger = logger;
            Configuration = configuration;
        }

    }
}
