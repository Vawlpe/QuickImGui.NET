using System.Numerics;
using ImGuiNET;

namespace QuickImGuiNET;

public interface IWindowManager
{
    public void CreateWindow(ImGuiViewportPtr vp);
    public void DestroyWindow(ImGuiViewportPtr vp);
    public void ShowWindow(ImGuiViewportPtr vp);
    public void SetWindowPos(ImGuiViewportPtr vp, Vector2 pos);
    public void SetWindowSize(ImGuiViewportPtr vp, Vector2 size);
    public unsafe void GetWindowPos(ImGuiViewportPtr vp, Vector2* outPos);
    public unsafe void GetWindowSize(ImGuiViewportPtr vp, Vector2* outSize);
    public void SetWindowFocus(ImGuiViewportPtr vp);
    public byte GetWindowFocus(ImGuiViewportPtr vp);
    public byte GetWindowMinimized(ImGuiViewportPtr vp);
    public unsafe void SetWindowTitle(ImGuiViewportPtr vp, IntPtr title);
    public void WindowResized(int width, int height);
    public void SwapExtraWindows();
    public void Dispose();
}