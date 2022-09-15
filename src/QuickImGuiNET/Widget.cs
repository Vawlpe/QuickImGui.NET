using ImGuiNET;
using System.Numerics;

namespace QuickImGuiNET;

public abstract class Widget
{
    public string Name = String.Empty;
    public Vector2 Size = Vector2.Zero;
    public Vector2 Position = Vector2.Zero;
    public ImGuiCond PositionCond = ImGuiCond.Always;
    public ImGuiCond SizeCond = ImGuiCond.Always;
    public bool Visible = false;
    private bool _visible = false;
    public WidgetRenderMode RenderMode = WidgetRenderMode.Raw;
    public abstract void RenderContent();
    public virtual void Update(float delta) {}
    public void Render(bool? border = null, ImGuiWindowFlags? flags = null)
    {
        // Detect direct visibility changes and trigger appropriate event
        if (_visible != Visible) {
            if (Visible) Open();
            else Close();
            _visible = Visible;
        }
        
        // Render
        switch (RenderMode)
        {
            case WidgetRenderMode.Raw:
                RenderContent();
                break;

            case WidgetRenderMode.Window:
                RenderInWindow(flags ?? ImGuiWindowFlags.None);
                break;
            
            case WidgetRenderMode.Child:
                RenderInChild(border ?? false, flags ?? ImGuiWindowFlags.None);
                break;

            case WidgetRenderMode.Popup:
                RenderInPopup(flags ?? ImGuiWindowFlags.None);
                break;
            
            case WidgetRenderMode.Modal:
                RenderInModal(flags ?? ImGuiWindowFlags.None);
                break;
        };
    }
    public void RenderInChild(bool border, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        ImGui.SetCursorPos(Position);
        if (!Visible || !ImGui.BeginChild(Name, Size, border, flags))
            return;

        RenderContent();
        ImGui.EndChildFrame();
    }
    public void RenderInWindow(ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        ImGui.SetNextWindowSize(Size, SizeCond);
        ImGui.SetNextWindowPos(Position, PositionCond);
        if (!Visible || !ImGui.Begin(Name, ref Visible, flags))
            return;

        RenderContent();
        ImGui.End();
    }
    public void RenderInPopup(ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        if (Visible && !ImGui.IsPopupOpen(Name))
            ImGui.OpenPopup(Name);

        ImGui.SetNextWindowSize(Size, SizeCond);
        ImGui.SetNextWindowPos(Position, PositionCond);
        if (!Visible || !ImGui.BeginPopup(Name, flags))
            return;

        if (!Visible && ImGui.IsPopupOpen(Name))
            ImGui.CloseCurrentPopup();

        RenderContent();
        ImGui.EndPopup();
    }

    public void RenderInModal(ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        if (Visible && !ImGui.IsPopupOpen(Name))
            ImGui.OpenPopup(Name);

        ImGui.SetNextWindowSize(Size, SizeCond);
        ImGui.SetNextWindowPos(Position, PositionCond);
        if (!Visible || !ImGui.BeginPopupModal(Name, ref Visible, flags))
            return;

        if (!Visible && ImGui.IsPopupOpen(Name))
            ImGui.CloseCurrentPopup();
        
        RenderContent();
        ImGui.End();
    }

    public void Open()
    {
        Visible = true;
        OnOpen(this);
    }
    public void Close()
    {
        Visible = false;
        OnClose(this);
    }
    public void Toggle()
    {
        Visible = !Visible;
        if (Visible)
            OnOpen(this);
        else
            OnClose(this);
        OnToggle(this);
    }
    public event Action<Widget> OnOpen = delegate { };
    public event Action<Widget> OnClose = delegate { };
    public event Action<Widget> OnToggle = delegate { };
}

public enum WidgetRenderMode
{
    Raw,
    Window,
    Child,
    Popup,
    Modal
}
