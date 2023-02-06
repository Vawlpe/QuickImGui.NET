using System.Numerics;
using ImGuiNET;

namespace QuickImGuiNET;

public abstract class Widget
{
    public readonly string Name;
    public bool Visible = false;
    public WidgetRenderMode RenderMode = WidgetRenderMode.Raw;
    public Vector2 Size = Vector2.Zero;
    public ImGuiCond SizeCond = ImGuiCond.None;
    public Vector2 Position = Vector2.Zero;
    public ImGuiCond PositionCond = ImGuiCond.None;
    public ImGuiWindowFlags WindowFlags = ImGuiWindowFlags.None;
    public bool ChildBorder;

    private bool _visible = false;
    protected Context Context;
    
    public Widget(Context context, string? Name = null, bool AutoRegister = true)
    {
        this.Name = Name ?? $"{DateTime.UtcNow.ToBinary()}";
        context.Logger.Debug($"Initializing Widget {Name}{(AutoRegister ? " (Auto)" : string.Empty)}");
        if (AutoRegister)
        {
            context.Events["widgetReg"].Children.Add(this.Name, new Event(new Dictionary<string, Event>
            {
                { "open", new Event() },
                { "close", new Event() },
                { "toggle", new Event() }
            }));
            context.WidgetRegistry.Add(Name ?? $"{DateTime.UtcNow.Millisecond}", this);
            context.Events["onMainMenuBar"]["Debug"].Hook += RenderOnMainMenuBar_Debug;
        }

        this.Context = context;
    }
    
    private dynamic? RenderOnMainMenuBar_Debug(params dynamic[]? args)
    {
        ImGui.MenuItem($"Open {Name.Replace("#", @"\#")}", string.Empty, ref Visible);
        return null;
    }

    public abstract void RenderContent();

    public virtual void Update(float delta)
    {
    }

    public virtual void Render()
    {
        // Detect direct visibility changes and trigger appropriate event
        if (_visible != Visible)
        {
            if (Visible) Open();
            else Close();
            _visible = Visible;
        }

        // Render
        Action renderFunc = RenderMode switch
        {
            WidgetRenderMode.Window => () => RenderInWindow(),
            WidgetRenderMode.Child => () => RenderInChild(),
            WidgetRenderMode.Popup => () => RenderInPopup(),
            WidgetRenderMode.Modal => () => RenderInModal(),
            _ => () => RenderContent()
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
        if (Context.Events["widgetReg"].Children.ContainsKey(Name))
            Context.Events["widgetReg"][Name]["open"].Invoke(this);
    }

    public void Close()
    {
        Visible = false;
        if (Context.Events["widgetReg"].Children.ContainsKey(Name))
            Context.Events["widgetReg"][Name]["close"].Invoke(this);
    }

    public void Toggle()
    {
        Visible = !Visible;
        if (!Context.Events["widgetReg"].Children.ContainsKey(Name))
            return;
        if (Visible)
            Context.Events["widgetReg"][Name]["close"].Invoke(this);
        else
            Context.Events["widgetReg"][Name]["open"].Invoke(this);
        Context.Events["widgetReg"][Name]["toggle"].Invoke(this);
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