using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using VR = Veldrid;

namespace QuickImGuiNET.Veldrid;

public class Context : QuickImGuiNET.Context, IDisposable
{

    public new  Renderer Renderer
    {
        get => (Renderer)base.Renderer;
        set => base.Renderer = value;
    }

    public new InputManager InputManager 
    {
        get => (InputManager)base.InputManager;
        set => base.InputManager = value;
    }
    public new WindowManager WindowManager 
    {
        get => (WindowManager)base.WindowManager;
        set => base.WindowManager = value;
    }
    public new TextureManager TextureManager
    {
        get => (TextureManager)base.TextureManager;
        set => base.TextureManager = value;
    }

    public VR.GraphicsBackend GraphicsBackend;
    private static readonly Vector3 _clearColor = new(0.45f, 0.55f, 0.6f);

    public override unsafe void Init()
    {
        var width = (int)Config["window"]["width"];
        var height = (int)Config["window"]["height"];
        var gfxbk = Config["veldrid"]["renderer"];
        if (gfxbk == -1)
            GraphicsBackend = VR.GraphicsDevice.IsBackendSupported(VR.GraphicsBackend.Vulkan)
                ? VR.GraphicsBackend.Vulkan // Vulkan & MoltenVK are prioritized on all platforms
                : VR.GraphicsDevice.IsBackendSupported(VR.GraphicsBackend.Metal)
                    ? VR.GraphicsBackend.Metal // Metal is only available on macOS but is prioritized over OpenGL
                    : VR.GraphicsDevice.IsBackendSupported(VR.GraphicsBackend.OpenGL)
                        ? VR.GraphicsBackend.OpenGL // OpenGL is available on all platforms as a default renderer if Vulkan is not available
                        : VR.GraphicsDevice.IsBackendSupported(VR.GraphicsBackend.Direct3D11)
                            ? VR.GraphicsBackend.Direct3D11 // Direct3D11 is only available on Windows and is only a fallback if OpenGL is not available either
                            : throw new InvalidOperationException("No supported renderer found...");
        else
            GraphicsBackend = (VR.GraphicsBackend)gfxbk;

        // Initialize ImGui Context
        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        Io = ImGui.GetIO();
        PlatformIo = ImGui.GetPlatformIO();

        // Initialize QIMGUIN Systems
        TextureManager = new(this);
        Renderer = new(this);
        WindowManager = new(width, height, this);
        InputManager = new();

        // Set up ImGui IO
        Io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        if ((VR.GraphicsBackend)gfxbk == VR.GraphicsBackend.Vulkan)
            Io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

        Io.NativePtr->BackendPlatformName =
            (byte*)new FixedAsciiString("QuickImGuiNET.Veldrid (SDL2)").DataPtr;
        Io.NativePtr->BackendRendererName =
            (byte*)new FixedAsciiString(Enum.GetName(typeof(VR.GraphicsBackend), GraphicsBackend)).DataPtr;
        Io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
        Io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
        Io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
        Io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;
        Io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        Io.Fonts.AddFontDefault();

        Renderer.CreateDeviceResources();
        InputManager.SetKeyMappings();

        Renderer.SetPerFrameImGuiData(1f / 60f);
        Renderer.UpdateMonitors();
    }

    public override void Run(
        Action DrawUICallback,
        float deltaSeconds = 1f / 60f,
        Action<float>? UpdateCallback = null,
        Action? RenderCallback = null,
        Action? EarlyUpdateCallback = null,
        Action? EarlyRenderCallback = null
    )
    {
        // Main application loop
        while (WindowManager.MainWindow.Exists)
        {
            var input = WindowManager.MainWindow.PumpEvents();
            if (!WindowManager.MainWindow.Exists) break;

            EarlyUpdateCallback?.Invoke();
            Update(deltaSeconds, input);
            UpdateCallback?.Invoke(deltaSeconds);

            DrawUICallback.Invoke();

            Renderer.CmdList.Begin();
            Renderer.CmdList.SetFramebuffer(Renderer.GDevice.MainSwapchain.Framebuffer);
            Renderer.CmdList.ClearColorTarget(0, new VR.RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, 1f));

            EarlyRenderCallback?.Invoke();
            Render();
            RenderCallback?.Invoke();

            Renderer.CmdList.End();
            Renderer.GDevice.SubmitCommands(Renderer.CmdList);
            Renderer.GDevice.SwapBuffers(Renderer.GDevice.MainSwapchain);
            WindowManager.SwapExtraWindows();
        }

        // Clean up resources
        Renderer.GDevice.WaitForIdle();
        Dispose();
    }

    private void Update(float deltaSeconds, VR.InputSnapshot input)
    {
        if (Renderer.FrameBegun)
        {
            ImGui.Render();
            ImGui.UpdatePlatformWindows();
        }

        Renderer.SetPerFrameImGuiData(deltaSeconds);
        InputManager.UpdateInput(input);
        Renderer.UpdateMonitors();

        Renderer.FrameBegun = true;
        ImGui.NewFrame();
    }

    public override void Render()
    {
        if (!Renderer.FrameBegun) return;
        Renderer.FrameBegun = false;
        ImGui.Render();
        Renderer.RenderImDrawData(ImGui.GetDrawData());

        // Update and Render additional Platform Windows
        if ((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) == 0) return;
        ImGui.UpdatePlatformWindows();
        for (var i = 1; i < PlatformIo.Viewports.Size; i++)
        {
            var vp = PlatformIo.Viewports[i];
            var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
            Renderer.CmdList.SetFramebuffer(window.Swapchain.Framebuffer);
            Renderer.RenderImDrawData(vp.DrawData);
        }
    }
    
    public override void Dispose()
    {
        Renderer.Dispose();
        TextureManager.Dispose();
        WindowManager.Dispose();
    }
}