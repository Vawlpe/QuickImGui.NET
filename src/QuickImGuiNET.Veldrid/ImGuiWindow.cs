#pragma warning disable CS8618

using System.Runtime.InteropServices;
using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace QuickImGuiNET.Veldrid;

public class ImGuiWindow : IDisposable
{
    private readonly GCHandle _gcHandle;
    private readonly GraphicsDevice _gd;
    private readonly ImGuiViewportPtr _vp;

    public ImGuiWindow(GraphicsDevice gd, ImGuiViewportPtr vp)
    {
        _gcHandle = GCHandle.Alloc(this);
        _gd = gd;
        _vp = vp;

        var flags = SDL_WindowFlags.Hidden;
        if ((vp.Flags & ImGuiViewportFlags.NoTaskBarIcon) != 0) flags |= SDL_WindowFlags.SkipTaskbar;
        if ((vp.Flags & ImGuiViewportFlags.NoDecoration) != 0)
            flags |= SDL_WindowFlags.Borderless;
        else
            flags |= SDL_WindowFlags.Resizable;

        if ((vp.Flags & ImGuiViewportFlags.TopMost) != 0) flags |= SDL_WindowFlags.AlwaysOnTop;

        Window = new Sdl2Window(
            "No Title Yet",
            (int)vp.Pos.X, (int)vp.Pos.Y,
            (int)vp.Size.X, (int)vp.Size.Y,
            flags,
            false);
        Window.Resized += () => _vp.PlatformRequestResize = true;
        Window.Moved += p => _vp.PlatformRequestMove = true;
        Window.Closed += () => _vp.PlatformRequestClose = true;

        var scSource = VeldridStartup.GetSwapchainSource(Window);
        var scDesc = new SwapchainDescription(scSource, (uint)Window.Width, (uint)Window.Height, null, true, false);
        Swapchain = _gd.ResourceFactory.CreateSwapchain(scDesc);
        Window.Resized += () => Swapchain.Resize((uint)Window.Width, (uint)Window.Height);

        vp.PlatformUserData = (IntPtr)_gcHandle;
    }

    public ImGuiWindow(GraphicsDevice gd, ImGuiViewportPtr vp, Sdl2Window window)
    {
        _gcHandle = GCHandle.Alloc(this);
        _gd = gd;
        _vp = vp;
        Window = window;
        vp.PlatformUserData = (IntPtr)_gcHandle;
    }

    public Sdl2Window Window { get; }

    public Swapchain Swapchain { get; }

    public void Dispose()
    {
        _gd.WaitForIdle(); // TODO: Shouldn't be necessary, but Vulkan backend trips a validation error (swapchain in use when disposed).
        Swapchain.Dispose();
        Window.Close();
        _gcHandle.Free();
    }

    public void Update()
    {
        Window.PumpEvents();
    }
}

#pragma warning restore CS8618