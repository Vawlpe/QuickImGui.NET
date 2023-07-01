using System.Numerics;
using System.Reflection;
using ImGuiNET;

namespace QuickImGuiNET.Example.Veldrid;

public class ExampleWidget : Widget
{
    public Vector2 IconRenderSize;
    public Texture IconTexture;

    public ExampleWidget(Context ctx, string? Name, bool AutoRegister = true) : base(ctx, Name, AutoRegister)
    {
        ctx.Events["onMainMenuBar"]["Debug"].Hook += RenderOnMainMenuBar_Debug;
        IconTexture =
            Texture.Bind(
                Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? string.Empty, "Icon.png"),
                Program.ctx);
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
        if (!ImGui.BeginListBox(string.Empty)) return;
        foreach (var sink in ctx.Config.Sinks)
            if (ImGui.Button($"Write config via sink {sink.GetType().Name}"))
                ctx.Config.To(sink);
        ImGui.EndListBox();
    }
}