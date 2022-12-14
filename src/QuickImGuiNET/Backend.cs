using ImGuiNET;
using Serilog;

namespace QuickImGuiNET;

public abstract class Backend
{
    public Config Config = new();
    public Dictionary<string, Event> Events = new();
    public (IntPtr ID, dynamic Texture) FontTexture;
    public ILogger Logger = Log.Logger;
    public Dictionary<IntPtr, dynamic> Textures = new();
    public Dictionary<string, Widget> WidgetReg = new();

    public abstract void Run(
        Action DrawUICallback,
        float deltaSeconds = 1f / 60f,
        Action<float>? UpdateCallback = null,
        Action? RenderCallback = null,
        Action? EarlyUpdateCallback = null,
        Action? EarlyRenderCallback = null
    );

    public abstract void RenderImDrawData(ImDrawDataPtr drawData);
    public abstract IntPtr BindTexture(Texture texture);
    public abstract IntPtr UpdateTexture(Texture texture);
    public abstract void Init();
    public abstract void FreeTexture(IntPtr ID);
    public abstract void UpdateInput(dynamic input);
}