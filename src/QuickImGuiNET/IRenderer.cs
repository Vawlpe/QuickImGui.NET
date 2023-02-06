using ImGuiNET;

namespace QuickImGuiNET;

public interface IRenderer
{
    public void SetPerFrameImGuiData(float deltaSeconds);
    public void CreateDeviceResources();
    public void RenderImDrawData(ImDrawDataPtr drawData);
    public unsafe void UpdateMonitors();
    public abstract void Dispose();
}