using Tomlyn.Model;

namespace QuickImGuiNET;

public partial class Config
{
    public TomlTable _config;
    public TomlTable _default;
    public IConfigSink[] Sinks;
    public IConfigSource[] Sources;

    public dynamic this[string key]
    {
        get => _config[key];
        set => _config[key] = value;
    }

    public void LoadDefault()
    {
        _config = _default;
    }

    public void From(IConfigSource source)
    {
        source.Read(ref _config);
    }

    public void To(IConfigSink sink)
    {
        sink.Write(_config);
    }
}

public interface IConfigSink
{
    public bool Write(TomlTable data);
}

public interface IConfigSource
{
    public bool Read(ref TomlTable data);
}