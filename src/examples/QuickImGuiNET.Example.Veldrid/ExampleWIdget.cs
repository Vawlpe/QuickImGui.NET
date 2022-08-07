using ImGuiNET;
using System.Reflection;
using System.Numerics;

namespace QuickImGuiNET.Example.Veldrid;

public class ExampleWidget : Widget
{
    public Texture IconTexture;
    public Vector2 IconRenderSize;
    public ExampleWidget() : base()
    {
        IconTexture = Texture.Bind(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Icon.png"), Program.backend);
    }
    public override void RenderContent()
    {
        ImGui.Text("Hello World!");
        ImGui.Image(IconTexture.ID, IconRenderSize);
    }
}
