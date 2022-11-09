using ImGuiNET;
using System.Numerics;
using System.Reflection;
using VRBK = QuickImGuiNET.Veldrid;

namespace QuickImGuiNET.Example.Veldrid;

public class Program
{
    public static readonly string[] defaultArgs = new string[] { "1280", "720", "-1" };
    public static VRBK.Backend backend;

    public static void Main(string[] args)
    {
        if (args.Length != defaultArgs.Length)
            args = args.Concat(defaultArgs.Skip(args.Length)).ToArray();
        int width = int.Parse(args[0]);
        int height = int.Parse(args[1]);
        int veldridBackendIndex = int.Parse(args[2]);

        backend = new VRBK.Backend(width, height, veldridBackendIndex);
        
        backend.Events = new() {
            { "onMainMenuBar", new(new() {
                { "Debug", new() }
            })},
            { "widgetReg", new() }
        };

        backend.WidgetReg = new() {};
        new ExampleWidget(backend, "ExampleWidget##example001") {
            Visible        = true,
            RenderMode     = WidgetRenderMode.Window,
            Position       = new(100, 100),
            Size           = new(150, 200),
            SizeCond       = ImGuiCond.FirstUseEver,
            PositionCond   = ImGuiCond.FirstUseEver,
            IconRenderSize = new(128, 128)
        };
        new ExampleWidget(backend, "ExampleWidget##example002") {
            Visible        = true,
            RenderMode     = WidgetRenderMode.Window,
            Position       = new(100, 310),
            Size           = new(275, 320),
            SizeCond       = ImGuiCond.FirstUseEver,
            PositionCond   = ImGuiCond.FirstUseEver,
            IconRenderSize = new(256, 256)
        };
        new Widgets.FileManager(backend, "FileManager##example001") {
            Visible         = false,
            RenderMode      = WidgetRenderMode.Modal,
            Position        = ImGui.GetMainViewport().GetWorkCenter() - new Vector2(250, 250),
            Size            = new(500, 500),
            SizeCond        = ImGuiCond.FirstUseEver,
            PositionCond    = ImGuiCond.FirstUseEver,
            Mode            = Widgets.FileManager.SelectionMode.SaveFile,
            CurrentPath     = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? Path.GetFullPath("~"),
            ShowHiddenFiles = true,
            ShowSystemFiles = true,
            FileTypeQueries = new() {
                {"Images", new() { "*.png", "*.jpg", "*.jpeg" }},
                {"All",    new() { "*"                        }}
            },
            CurrentFTQuery  = "All"
        };

        backend.Run(Draw, UpdateCallback: Update);
    }

    public static bool showDemoWindow = false;

    public static void Draw()
    {
        ImGui.DockSpaceOverViewport();
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Debug"))
            {
                ImGui.MenuItem("Show Demo Window", String.Empty, ref showDemoWindow);
                backend.Events["onMainMenuBar"]["Debug"].Invoke();
                ImGui.EndMenu();
            }

            backend.Events["onMainMenuBar"].Invoke();
            ImGui.EndMainMenuBar();
        }

        if (showDemoWindow)
            ImGui.ShowDemoWindow(ref showDemoWindow);

        foreach (var widget in backend.WidgetReg.Values)
            widget.Render();
    }

    public static void Update(float deltaSeconds)
    {
        foreach (var widget in backend.WidgetReg.Values)
            widget.Update(deltaSeconds);
    }
}
