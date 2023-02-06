using Serilog;
using Tomlyn.Model;

namespace QuickImGuiNET;

public partial class Config
{
    public class Toml : IConfigSink, IConfigSource
    {
        private readonly Context? _context;
        private readonly string _path;

        public Toml(string path, ref Context context)
        {
            _path = Path.GetFullPath(path);
            _context = context;
        }

        private ILogger Logger => _context is null ? Log.Logger : _context.Logger;

        public bool Write(TomlTable data)
        {
            Logger.Information("Saving TOML config file");
            try
            {
                Logger.Information(File.Exists(_path)
                    ? $"Found Existing TOML config file, overwriting: {_path}"
                    : "No TOML config file found, creating new config w/ current settings");
                File.WriteAllText(_path, Tomlyn.Toml.FromModel(data));
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
                        Logger.Information("Could not validate TOML config file.");
                    else
                    {
                        Logger.Information("Loading TOML config file");
                        Recurse(Tomlyn.Toml.ToModel(fileText), ref data);

                        void Recurse(TomlTable tbl, ref TomlTable data)
                        {
                            foreach (var kvp in tbl)
                                if (kvp.Value is TomlTable child)
                                    Recurse(child, ref data);
                                else if (!data.ContainsKey(kvp.Key))
                                    data.Add(kvp.Key, kvp.Value);
                        }
                        
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