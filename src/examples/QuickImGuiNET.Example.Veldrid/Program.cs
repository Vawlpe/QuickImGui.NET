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
            {"onMainMenuBar", new(new() {
                {"Debug", new()}
            })}
        };

        widgets = new() {
            new ExampleWidget(backend) {
                Visible = true,
                RenderMode = WidgetRenderMode.Window,
                Name = "ExampleWidget##example001",
                Position = new Vector2(100, 100),
                Size = new Vector2(150, 200),
                SizeCond = ImGuiCond.FirstUseEver,
                PositionCond = ImGuiCond.FirstUseEver,
                IconRenderSize = new Vector2(128, 128)
            },
            new ExampleWidget(backend) {
                Visible = true,
                RenderMode = WidgetRenderMode.Window,
                Name = "ExampleWidget##example002",
                Position = new Vector2(100, 310),
                Size = new Vector2(275, 320),
                SizeCond = ImGuiCond.FirstUseEver,
                PositionCond = ImGuiCond.FirstUseEver,
                IconRenderSize = new Vector2(256, 256)
            },
            new ExampleWidget(backend) {
                Visible = true,
                RenderMode = WidgetRenderMode.Window,
                Name = "ExampleWidget##example003",
                Position = new Vector2(100, 100),
                Size = new Vector2(150, 200),
                SizeCond = ImGuiCond.FirstUseEver,
                PositionCond = ImGuiCond.FirstUseEver,
                IconRenderSize = new Vector2(512, 512)
            },
            new Widgets.FileManager(backend) {
                Visible = true,
                RenderMode = WidgetRenderMode.Modal,
                Name = "FileManager##example001",
                Position = ImGui.GetMainViewport().GetWorkCenter() - new Vector2(250, 250),
                Size = new Vector2(500, 500),
                SizeCond = ImGuiCond.FirstUseEver,
                PositionCond = ImGuiCond.FirstUseEver,
                Mode = Widgets.FileManager.SelectionMode.OpenFile,
                CurrentPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? Path.GetFullPath("~"),
                ShowHiddenFiles = true, ShowSystemFiles = true,
                FileTypeQueries = new() {
                    {"Images", new() { "*.png", "*.jpg", "*.jped" }},
                    {"All",    new() { "*"                        }}
                },
                CurrentFTQuery = "All",
                CloseCallback = (w) => Console.WriteLine($"Selected: {(w as Widgets.FileManager)?.Selected}")
            }
        };

        backend.Run(Draw, UpdateCallback: Update);
    }

    public static bool showDemoWindow = false;
    public static List<Widget> widgets = new();
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

        foreach (var widget in widgets)
            widget.Render();
    }

    public static void Update(float deltaSeconds)
    {
        foreach (var widget in widgets)
            widget.Update(deltaSeconds);
    }
}
