using ImGuiNET;

namespace QuickImGuiNET;

public abstract class Backend
{
    public abstract void Run(
        Action  DrawUICallback,
        float   deltaSeconds          = 1f / 60f,
        Action<float>? UpdateCallback = null,
        Action? RenderCallback        = null,
        Action? EarlyUpdateCallback   = null,
        Action? EarlyRenderCallback   = null
    );
    public abstract void RenderImDrawData(ImDrawDataPtr drawData);
    public abstract IntPtr BindTexture(Texture texture);
    public abstract IntPtr UpdateTexture(Texture texture);
    public abstract void FreeTexture(IntPtr ID);
    public abstract void UpdateInput(dynamic input);
    public Dictionary<IntPtr, dynamic> Textures = new();
    public (IntPtr ID, dynamic Texture) FontTexture;
    public Dictionary<string, Event> Events = new();
}