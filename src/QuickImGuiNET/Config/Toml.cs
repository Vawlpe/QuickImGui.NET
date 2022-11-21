using Serilog;
using Tomlyn.Model;

namespace QuickImGuiNET;

public partial class Config
{
    public class Toml : IConfigSink, IConfigSource
    {
        private readonly Backend? _backend;
        private readonly string _path;

        public Toml(string path, ref Backend backend)
        {
            _path = Path.GetFullPath(path);
            _backend = backend;
        }

        private ILogger Logger => _backend is null ? Log.Logger : _backend.Logger;

        public bool Write(TomlTable data)
        {
            Logger.Information("Saving TOML config file");
            try
            {
                if (File.Exists(_path))
                {
                    Logger.Information($"Found Existing TOML config file, overwriting: {_path}");
                    File.WriteAllText(_path, Tomlyn.Toml.FromModel(data));
                }
                else
                {
                    Logger.Information("No TOML config file found, creating new config w/ current options");
                    File.WriteAllText(_path, Tomlyn.Toml.FromModel(data));
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Error($"Error writing TOML config file: {e}");
                return false;
            }
        }

        public bool Read(ref TomlTable data)
        {
            try
            {
                if (File.Exists(_path))
                {
                    var fileText = File.ReadAllText(_path);
                    if (Tomlyn.Toml.Validate(Tomlyn.Toml.Parse(fileText)).HasErrors)
                    {
                        Logger.Information("Could not validate TOML config file.");
                    }
                    else
                    {
                        Logger.Information("Loading TOML config file");
                        data = Tomlyn.Toml.ToModel(fileText);
                    }
                }
                else
                {
                    Logger.Information("No TOML config file found, creating new config w/ current options");
                    File.WriteAllText(_path, Tomlyn.Toml.FromModel(data));
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Error($"Error reading TOML config file: {e}");
                return false;
            }
        }
    }
}