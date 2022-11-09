using ImGuiNET;
using System.Numerics;

namespace QuickImGuiNET;

public abstract class Widget
{
    public readonly string Name;
    public Vector2 Size = Vector2.Zero;
    public Vector2 Position = Vector2.Zero;
    public ImGuiCond PositionCond = ImGuiCond.Always;
    public ImGuiCond SizeCond = ImGuiCond.Always;
    public ImGuiWindowFlags WindowFlags = ImGuiWindowFlags.None;
    public WidgetRenderMode RenderMode = WidgetRenderMode.Raw;
    public bool Visible;
    public bool ChildBorder;
    private bool _visible;

    private Backend backend;
    public Widget(Backend backend, string? Name = null, bool AutoRegister = true)
    {
        this.Name = Name ?? $"{DateTime.UtcNow.ToBinary()}";
        if (AutoRegister) {
            backend.Events["widgetReg"].Children.Add(this.Name, new(new() {
                { "open", new() },
                { "close", new() },
                { "toggle", new() }
            }));
            backend.WidgetReg.Add(Name ?? $"{DateTime.UtcNow.Millisecond}", this);
        }
        this.backend = backend;
    }

    public abstract void RenderContent();
    public virtual void Update(float delta) {}
    public void Render()
    {
        // Detect direct visibility changes and trigger appropriate event
        if (_visible != Visible) {
            if (Visible) Open();
            else Close();
            _visible = Visible;
        }

        // Render
        Action renderFunc = RenderMode switch {
            WidgetRenderMode.Window => () => RenderInWindow(),
            WidgetRenderMode.Child  => () => RenderInChild(),
            WidgetRenderMode.Popup  => () => RenderInPopup(),
            WidgetRenderMode.Modal  => () => RenderInModal(),
            _                       => () => RenderContent(),
        };
        renderFunc.Invoke();
    }
    public void RenderInChild()
    {
        ImGui.SetCursorPos(Position);
        if (!Visible || !ImGui.BeginChild(Name, Size, ChildBorder, WindowFlags))
            return;

        RenderContent();
        ImGui.EndChildFrame();
    }
    public void RenderInWindow()
    {
        ImGui.SetNextWindowSize(Size, SizeCond);
        ImGui.SetNextWindowPos(Position, PositionCond);
        if (!Visible || !ImGui.Begin(Name, ref Visible, WindowFlags))
            return;

        RenderContent();
        ImGui.End();
    }
    public void RenderInPopup()
    {
        if (Visible != ImGui.IsPopupOpen(Name))
            if (Visible)
                 ImGui.OpenPopup(Name);
            else ImGui.CloseCurrentPopup();

        ImGui.SetNextWindowSize(Size, SizeCond);
        ImGui.SetNextWindowPos(Position, PositionCond);
        if (!Visible || !ImGui.BeginPopup(Name, WindowFlags))
            return;

        RenderContent();
        ImGui.EndPopup();
    }
    public void RenderInModal()
    {
        if (Visible != ImGui.IsPopupOpen(Name))
            if (Visible)
                 ImGui.OpenPopup(Name);
            else ImGui.CloseCurrentPopup();

        ImGui.SetNextWindowSize(Size, SizeCond);
        ImGui.SetNextWindowPos(Position, PositionCond);
        if (!Visible || !ImGui.BeginPopupModal(Name, ref Visible, WindowFlags))
            return;

        RenderContent();
        ImGui.End();
    }

    public void Open()
    {
        Visible = true;
        backend.Events["widgetReg"][Name]["open"].Invoke(this);
    }
    public void Close()
    {
        Visible = false;
        backend.Events["widgetReg"][Name]["close"].Invoke(this);
    }
    public void Toggle()
    {
        Visible = !Visible;
        if (Visible)
            backend.Events["widgetReg"][Name]["close"].Invoke(this);
        else
            backend.Events["widgetReg"][Name]["open"].Invoke(this);
        backend.Events["widgetReg"][Name]["toggle"].Invoke(this);
    }
}

public enum WidgetRenderMode
{
    Raw,
    Window,
    Child,
    Popup,
    Modal
}
