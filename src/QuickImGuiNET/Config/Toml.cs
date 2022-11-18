using Serilog;
using Tomlyn.Model;

namespace QuickImGuiNET;
public partial class Config
{
    public class Toml : IConfigSink, IConfigSource
    {
        private string path;
        public Toml(string path)
        {
            this.path = Path.GetFullPath(path);
        }
        public bool Write(TomlTable data)
        {
            try
            {
                if (File.Exists(path))
                {
                    Log.Information($"Found Existing Config file, Overwriting...: {path}");
                    File.WriteAllText(path, Tomlyn.Toml.FromModel(data));
                }
                else
                {
                    Log.Information("No Config file found, creating new config w/ current options");
                    File.Create(path);
                    File.WriteAllText(path, Tomlyn.Toml.FromModel(data));
                }

                return true;
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                return false;
            }
        }

        public bool Read(ref TomlTable data)
        {
            try
            {
                if (File.Exists(path))
                {
                    var fileText = File.ReadAllText(path);
                    if (Tomlyn.Toml.Validate(Tomlyn.Toml.Parse(fileText)).HasErrors)
                        Log.Information("Could not validate config file.");
                    else
                    {
                        Log.Information("Loading Config file into temp object");
                        data = Tomlyn.Toml.ToModel(fileText);
                    }
                }
                else
                {
                    Log.Information("No Config file found, creating new config w/ current options");
                    File.Create(path);
                    File.WriteAllText(path, Tomlyn.Toml.FromModel(data));
                }
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                return false;
            }
        }
    }
}