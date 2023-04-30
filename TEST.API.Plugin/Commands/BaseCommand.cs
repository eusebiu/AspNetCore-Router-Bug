using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace TEST.API.Plugin.Commands
{
    public abstract class BaseCommand : Command, ICommand
    {
        public ILogger<BaseCommand> Logger { get; private set; }

        public IConfigurationRoot Configuration { get; }

        public BaseCommand(string name, string description, ILogger<BaseCommand> logger, IConfigurationRoot configuration)
            : base(name, description)
        {
            Logger = logger;
        }
    }
}
