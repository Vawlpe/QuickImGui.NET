using ImGuiNET;

namespace QuickImGuiNET;
public abstract class Context
{
    public Config Config = new();
    public Serilog.ILogger Logger = Serilog.Log.Logger;
    public Dictionary<string, Event> Events = new();
    public Dictionary<string, Widget> WidgetRegistry = new();

    public IRenderer Renderer;
    public IWindowManager WindowManager;
    public IInputManager InputManager;
    public ITextureManager TextureManager;

    public ImGuiIOPtr Io = ImGui.GetIO();
    public ImGuiPlatformIOPtr PlatformIo = ImGui.GetPlatformIO();

    public abstract void Run(
        Action DrawUICallback,
        float deltaSeconds = 1f / 60f,
        Action<float>? UpdateCallback = null,
        Action? RenderCallback = null,
        Action? EarlyUpdateCallback = null,
        Action? EarlyRenderCallback = null
    );
    public abstract void Init();
    public abstract void Render();
    public abstract void Dispose();
}