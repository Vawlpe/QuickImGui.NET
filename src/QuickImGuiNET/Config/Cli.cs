using System.Reflection;
using System.Text;
using Serilog;
using Tomlyn.Model;

namespace QuickImGuiNET;

public partial class Config
{
    public class Cli : IConfigSink, IConfigSource
    {
        private readonly string[] _args;
        private readonly Context? _context;

        public Cli(string[] args, ref Context context)
        {
            _args = args;
            _context = context;
        }

        private ILogger Logger => _context is null ? Log.Logger : _context.Logger;

        public bool Write(TomlTable data)
        {
            Logger.Information("Exporting command w/ CLI args for current config");
            if (!Tomlyn.Toml.Validate(Tomlyn.Toml.Parse(Tomlyn.Toml.FromModel(data))).HasErrors)
            {
                var sb = new StringBuilder($"dotnet {Assembly.GetEntryAssembly()?.GetName().Name}.dll");

                string RecurseTable(TomlTable tbl, string p) => tbl.Aggregate(string.Empty,
                    (current, kvp) => current + kvp.Value.GetType().Name switch
                    {
                        "TomlTable" => RecurseTable((TomlTable)kvp.Value,
                            p == string.Empty ? kvp.Key : $"{p}/{kvp.Key}"),
                        _ => $" --{p}/{kvp.Key}={kvp.Value.GetType().Name}:{kvp.Value}"
                    });

                var p = string.Empty;
                sb.Append(RecurseTable(data, p));

                Logger.Information(sb.ToString());
                return true;
            }

            Logger.Error("Failed to validate internal config while exporting command");
            return false;
        }

        public bool Read(ref TomlTable data)
        {
            // Make sure there actually is args
            Logger.Information("Loading CLI args as config");
            if (_args.Length <= 0)
            {
                Logger.Warning("No CLI args were given");
                return true;
            }

            // Loop over args and parse
            for (var i = 0; i < _args.Length; i++)
            {
                string name;
                string valueStr;
                // --name=type:value
                if (_args[i].Contains('='))
                {
                    var argSplit = _args[i].Split('=', 2);
                    name = argSplit[0];
                    valueStr = argSplit[1];
                }
                // --name type:value
                else if (i + 1 <= _args.Length || !_args[i + 1].StartsWith("--"))
                {
                    name = _args[i];
                    valueStr = _args[++i];
                }
                // --name
                else
                {
                    name = _args[1];
                    valueStr = "bool:true";
                }

                // Remove prefix
                if (!name.StartsWith("--"))
                {
                    Logger.Error($"Invalid argument #{i}, missing -- prefix");
                    return false;
                }

                name = name[2..];

                // Figure out value type
                var valueSplit = valueStr.Split(':', 2);
                var type = valueSplit[0];
                dynamic value = type switch
                {
                    "String" => valueSplit[1],
                    "Char" => char.Parse(valueSplit[1]),
                    "Boolean" => bool.Parse(valueSplit[1]),
                    "SByte" => sbyte.Parse(valueSplit[1]),
                    "Byte" => byte.Parse(valueSplit[1]),
                    "Int16" => short.Parse(valueSplit[1]),
                    "UInt16" => ushort.Parse(valueSplit[1]),
                    "Int32" => int.Parse(valueSplit[1]),
                    "UInt32" => uint.Parse(valueSplit[1]),
                    "Int64" => long.Parse(valueSplit[1]),
                    "UInt64" => ulong.Parse(valueSplit[1]),
                    "Single" => float.Parse(valueSplit[1]),
                    "Double" => double.Parse(valueSplit[1]),
                    "Decimal" => decimal.Parse(valueSplit[1]),
                    _ => throw new Exception("oopsie")
                };

                // Add to table
                var path = name.Split('/');
                var parent = data;
                for (var j = 0; j < path.Length - 1; j++)
                    if (parent.TryGetValue(path[j], out var newParent))
                    {
                        parent = (TomlTable)newParent;
                    }
                    else
                    {
                        parent.Add(path[j], new TomlTable());
                        parent = (TomlTable)parent[path[j]];
                    }

                if (parent.ContainsKey(path.Last()))
                    parent[path.Last()] = value;
                else
                    parent.Add(path.Last(), value);
            }

            return true;
        }
    }
}