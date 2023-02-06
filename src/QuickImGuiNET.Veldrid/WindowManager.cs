using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;
using VR = Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace QuickImGuiNET.Veldrid;

public class WindowManager : IWindowManager
{
    private IntPtr _icon;
    private Context _ctx;
    public Sdl2Window MainWindow;
    
    public unsafe WindowManager(int mainWidth, int mainHeight, Context ctx)
    {
        _ctx = ctx;
        
        _ctx.PlatformIo.Platform_CreateWindow = Marshal.GetFunctionPointerForDelegate<Platform_CreateWindow>(CreateWindow);
        _ctx.PlatformIo.Platform_DestroyWindow = Marshal.GetFunctionPointerForDelegate<Platform_DestroyWindow>(DestroyWindow);
        _ctx.PlatformIo.Platform_ShowWindow = Marshal.GetFunctionPointerForDelegate<Platform_ShowWindow>(ShowWindow);
        _ctx.PlatformIo.Platform_SetWindowPos = Marshal.GetFunctionPointerForDelegate<Platform_SetWindowPos>(SetWindowPos);
        _ctx.PlatformIo.Platform_SetWindowSize = Marshal.GetFunctionPointerForDelegate<Platform_SetWindowSize>(SetWindowSize);
        _ctx.PlatformIo.Platform_SetWindowFocus = Marshal.GetFunctionPointerForDelegate<Platform_SetWindowFocus>(SetWindowFocus);
        _ctx.PlatformIo.Platform_GetWindowFocus = Marshal.GetFunctionPointerForDelegate<Platform_GetWindowFocus>(GetWindowFocus);
        _ctx.PlatformIo.Platform_GetWindowMinimized = Marshal.GetFunctionPointerForDelegate<Platform_GetWindowMinimized>(GetWindowMinimized);
        _ctx.PlatformIo.Platform_SetWindowTitle = Marshal.GetFunctionPointerForDelegate<Platform_SetWindowTitle>(SetWindowTitle);
        ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowPos(_ctx.PlatformIo.NativePtr,
            Marshal.GetFunctionPointerForDelegate<Platform_GetWindowPos>(GetWindowPos));
        ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowSize(_ctx.PlatformIo.NativePtr,
            Marshal.GetFunctionPointerForDelegate<Platform_GetWindowSize>(GetWindowSize));
        
        MainWindow = VeldridStartup.CreateWindow(new WindowCreateInfo(50, 50, mainWidth, mainHeight, VR.WindowState.Normal, "QIMGUIN"));
        MainWindow.Resized += () => WindowResized(MainWindow.Width, MainWindow.Height);
        
        var mainViewport = _ctx.PlatformIo.Viewports[0];
        mainViewport.PlatformHandle = MainWindow.Handle;
        _ = new ImGuiWindow(_ctx.Renderer.GDevice, mainViewport, MainWindow);

        // Load and set icon
        if (!File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) ?? "", "Icon.png")))
            return;
        var iconSrc = SDL2Extensions.SDL_RWFromFile.Invoke(
            Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) ?? "", "Icon.png"), "rb");
        _icon = SDL2Extensions.SDL_LoadBMP_RW.Invoke(iconSrc, 1);
        SDL2Extensions.SDL_SetWindowIcon.Invoke(MainWindow.SdlWindowHandle, _icon);
    }

    public void CreateWindow(ImGuiViewportPtr vp)
    {
        _ = new ImGuiWindow(_ctx.Renderer.GDevice, vp);
    }

    public void DestroyWindow(ImGuiViewportPtr vp)
    {
        if (vp.PlatformUserData == IntPtr.Zero) return;
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        window.Dispose();

        vp.PlatformUserData = IntPtr.Zero;
    }

    public void ShowWindow(ImGuiViewportPtr vp)
    {
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        Sdl2Native.SDL_ShowWindow(window.Window.SdlWindowHandle);
    }

    public unsafe void GetWindowPos(ImGuiViewportPtr vp, Vector2* outPos)
    {
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        *outPos = new Vector2(window.Window.Bounds.X, window.Window.Bounds.Y);
    }

    public void SetWindowPos(ImGuiViewportPtr vp, Vector2 pos)
    {
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        window.Window.X = (int)pos.X;
        window.Window.Y = (int)pos.Y;
    }

    public void SetWindowSize(ImGuiViewportPtr vp, Vector2 size)
    {
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        Sdl2Native.SDL_SetWindowSize(window.Window.SdlWindowHandle, (int)size.X, (int)size.Y);
    }

    public unsafe void GetWindowSize(ImGuiViewportPtr vp, Vector2* outSize)
    {
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        var bounds = window.Window.Bounds;
        *outSize = new Vector2(bounds.Width, bounds.Height);
    }

    public void SetWindowFocus(ImGuiViewportPtr vp)
    {
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        SDL2Extensions.SDL_RaiseWindow.Invoke(window.Window.SdlWindowHandle);
    }

    public byte GetWindowFocus(ImGuiViewportPtr vp)
    {
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        var flags = Sdl2Native.SDL_GetWindowFlags(window.Window.SdlWindowHandle);
        return (flags & SDL_WindowFlags.InputFocus) != 0 ? (byte)1 : (byte)0;
    }

    public byte GetWindowMinimized(ImGuiViewportPtr vp)
    {
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        var flags = Sdl2Native.SDL_GetWindowFlags(window.Window.SdlWindowHandle);
        return (flags & SDL_WindowFlags.Minimized) != 0 ? (byte)1 : (byte)0;
    }

    public unsafe void SetWindowTitle(ImGuiViewportPtr vp, IntPtr title)
    {
        var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        var titlePtr = (byte*)title;
        var count = 0;
        while (titlePtr[count] != 0) titlePtr += 1;
        window.Window.Title = Encoding.ASCII.GetString(titlePtr, count);
    }

    public void WindowResized(int width, int height) => 
        _ctx.Renderer.GDevice.MainSwapchain.Resize((uint)MainWindow.Width, (uint)MainWindow.Height);

    public void SwapExtraWindows()
    {
        var platformIo = ImGui.GetPlatformIO();
        for (var i = 1; i < platformIo.Viewports.Size; i++)
        {
            var vp = platformIo.Viewports[i];
            var window = (ImGuiWindow)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
            SDL2Extensions.SDL_SetWindowIcon(window.Window.SdlWindowHandle, _icon);
            _ctx.Renderer.GDevice.SwapBuffers(window.Swapchain);
        }
    }

    public void Dispose() => SDL2Extensions.SDL_FreeSurface.Invoke(_icon);
}