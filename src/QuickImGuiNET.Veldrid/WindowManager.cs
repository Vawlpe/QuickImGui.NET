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
    
    private Platform_CreateWindow _createWindow;
    private Platform_DestroyWindow _destroyWindow;
    private Platform_ShowWindow _showWindow;
    private Platform_SetWindowPos _setWindowPos;
    private Platform_SetWindowSize _setWindowSize;
    private Platform_SetWindowFocus _setWindowFocus;
    private Platform_GetWindowFocus _getWindowFocus;
    private Platform_GetWindowMinimized _getWindowMinimized;
    private Platform_SetWindowTitle _setWindowTitle;
    private Platform_GetWindowPos _getWindowPos;
    private Platform_GetWindowPos _getWindowSize;
    
    public unsafe WindowManager(int mainWidth, int mainHeight, Context ctx)
    {
        _createWindow = CreateWindow;
        _destroyWindow = DestroyWindow;
        _showWindow = ShowWindow;
        _setWindowPos = SetWindowPos;
        _setWindowSize = SetWindowSize;
        _setWindowFocus = SetWindowFocus;
        _getWindowFocus = GetWindowFocus;
        _getWindowMinimized = GetWindowMinimized;
        _setWindowTitle = SetWindowTitle;
        _getWindowPos = GetWindowPos;
        _getWindowSize = GetWindowSize;

        _ctx = ctx;
        _ctx.PlatformIo.Platform_CreateWindow = Marshal.GetFunctionPointerForDelegate(_createWindow);
        _ctx.PlatformIo.Platform_DestroyWindow = Marshal.GetFunctionPointerForDelegate(_destroyWindow);
        _ctx.PlatformIo.Platform_ShowWindow = Marshal.GetFunctionPointerForDelegate(_showWindow);
        _ctx.PlatformIo.Platform_SetWindowPos = Marshal.GetFunctionPointerForDelegate(_setWindowPos);
        _ctx.PlatformIo.Platform_SetWindowSize = Marshal.GetFunctionPointerForDelegate(_setWindowSize);
        _ctx.PlatformIo.Platform_SetWindowFocus = Marshal.GetFunctionPointerForDelegate(_setWindowFocus);
        _ctx.PlatformIo.Platform_GetWindowFocus = Marshal.GetFunctionPointerForDelegate(_getWindowFocus);
        _ctx.PlatformIo.Platform_GetWindowMinimized = Marshal.GetFunctionPointerForDelegate(_getWindowMinimized);
        _ctx.PlatformIo.Platform_SetWindowTitle = Marshal.GetFunctionPointerForDelegate(_setWindowTitle);
        ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowPos(_ctx.PlatformIo.NativePtr,
            Marshal.GetFunctionPointerForDelegate(_getWindowPos));
        ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowSize(_ctx.PlatformIo.NativePtr,
            Marshal.GetFunctionPointerForDelegate(_getWindowSize));
        
        MainWindow = VeldridStartup.CreateWindow(new WindowCreateInfo(50, 50, mainWidth, mainHeight, VR.WindowState.Normal, "QIMGUIN"));
        MainWindow.Resized += () =>
        {
            _ctx.Renderer.GDevice.MainSwapchain.Resize((uint)MainWindow.Width, (uint)MainWindow.Height);
            WindowResized(MainWindow.Width, MainWindow.Height);
        };
        MainWindow.Width = mainWidth;
        MainWindow.Height = mainHeight;
        
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