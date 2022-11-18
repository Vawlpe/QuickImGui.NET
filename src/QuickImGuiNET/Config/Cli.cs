using Serilog;
using Tomlyn.Model;

namespace QuickImGuiNET;

public partial class Config
{
    public class Cli : IConfigSink, IConfigSource
    {
        private string[] _args;
        private readonly Backend? _backend;
        private ILogger Logger => _backend is null ? Log.Logger : _backend.Logger;
        public Cli(string[] args, ref Backend backend)
        {
            _args = args;
            _backend = backend;
        }

        public bool Write(TomlTable data)
        {           
            Logger.Information("Exporting command w/ CLI args for current config");
            if (!Tomlyn.Toml.Validate(Tomlyn.Toml.Parse(Tomlyn.Toml.FromModel(data))).HasErrors)
            {
                //TODO Export data as runnable command w/ args for config
                Logger.Information("dotnet QuickImguiNET.Example.Veldrid.dll [todo]");
                return true;
            }
            Logger.Error("Failed to validate internal config while exporting command");
            return false;
        }

        public bool Read(ref TomlTable data)
        {
            //TODO Parse _args into data 
            Logger.Information("Loading CLI args as config");
            //throw new NotImplementedException();
            return false;
        }
    }
}