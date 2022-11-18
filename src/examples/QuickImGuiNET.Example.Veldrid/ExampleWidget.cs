using ImGuiNET;
using System.Reflection;
using System.Numerics;

namespace QuickImGuiNET.Example.Veldrid;

public class ExampleWidget : Widget
{
    public Texture IconTexture;
    public Vector2 IconRenderSize;
    public ExampleWidget(Backend backend, string? Name, bool AutoRegister = true) : base(backend, Name, AutoRegister)
    {
        backend.Events["onMainMenuBar"]["Debug"].Hook += RenderOnMainMenuBar_Debug;
        IconTexture = Texture.Bind(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? String.Empty, "Icon.png"), Program.backend);
    }
    public dynamic? RenderOnMainMenuBar_Debug(params dynamic[]? args)
    {
        ImGui.MenuItem($"Open {Name.Replace("#", @"\#")}", string.Empty, ref Visible);
        return null;
    }
    public override void RenderContent()
    {
        ImGui.Text("Hello QIMGUIN!");
        ImGui.Image(IconTexture.ID, IconRenderSize);
        if (!ImGui.BeginListBox(String.Empty)) return;
        foreach (var sink in backend.Config.Sinks)
            if(ImGui.Button($"Write config via sink {sink.GetType().Name}"))
                backend.Config.To(sink);
        ImGui.EndListBox();
    }
}
