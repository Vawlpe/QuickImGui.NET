using ImGuiNET;
using System.Numerics;
using System.Reflection;
using VRBK = QuickImGuiNET.Veldrid;
using Serilog;

namespace QuickImGuiNET.Example.Veldrid;

public class Program
{
    public static readonly string[] defaultArgs = new string[] { "1280", "720", "-1" };
    public static VRBK.Backend backend;

    public static void Main(string[] args)
    {
        // Temporary default logger
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}") // Log to console
            .WriteTo.File("QIMGUIN.log",
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}") // Log to file
            .MinimumLevel.Debug() // Set minimum logging level
            .CreateLogger();
        
        // Read CLI Args
        //TODO Load Config file first, then CLI args
        if (args.Length != defaultArgs.Length)
            args = args.Concat(defaultArgs.Skip(args.Length)).ToArray();
        int width = int.Parse(args[0]);
        int height = int.Parse(args[1]);
        int veldridBackendIndex = int.Parse(args[2]);
        
        Log.Logger.Information($"QUIMGUIN v0.1 - ({width}x{height})"
                               + $"\n\t- VeldridBackendIndex: {veldridBackendIndex}");
        Log.Logger.Information("Initializing Veldrid Backend");
        backend = new VRBK.Backend(width, height, veldridBackendIndex);
        
        // TODO Use Config info to set up proper logger
        Log.Logger.Information("Initializing proper logger");
        backend.Logger = new LoggerConfiguration()
            .WriteTo
            .Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}") // Log to console
            .WriteTo.File("QIMGUIN.log",
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}") // Log to file
            .MinimumLevel.Debug() // Set minimum logging level
            .CreateLogger();
        
        backend.Logger.Information("Setting up basic Events");
        backend.Events = new() {
            { "onMainMenuBar", new(new() {
                { "Debug", new() }
            })},
            { "widgetReg", new() }
        };
        
        backend.Logger.Information("Initializing Widget Registry");
        backend.WidgetReg = new();
        new ExampleWidget(backend, "ExampleWidget##example00") {
            Visible        = true,
            RenderMode     = WidgetRenderMode.Window,
            Position       = new(100, 100),
            Size           = new(150, 150),
            SizeCond       = ImGuiCond.FirstUseEver,
            PositionCond   = ImGuiCond.FirstUseEver,
            IconRenderSize = new(128, 128)
        };
        new ExampleWidget(backend, "ExampleWidget##example01") {
            Visible        = true,
            RenderMode     = WidgetRenderMode.Window,
            Position       = new(255, 100),
            Size           = new(275, 275),
            SizeCond       = ImGuiCond.FirstUseEver,
            PositionCond   = ImGuiCond.FirstUseEver,
            IconRenderSize = new(256, 256)
        };
        new Widgets.FileManager(backend, "FileManager##example00") {
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
                {"Images", new() { ".png", ".jpg", ".jpeg" }},
                {"All",    new() { "*"                  }}
            },
            CurrentFTQuery  = "All"
        };
        new Widgets.MemoryEditor(backend, "MemoryView##example00") {
            Visible              = false,
            RenderMode           = WidgetRenderMode.Window,
            Position             = new(100, 380),
            Size                 = new(500, 360),
            SizeCond             = ImGuiCond.FirstUseEver,
            PositionCond         = ImGuiCond.FirstUseEver,
            WindowFlags          = ImGuiWindowFlags.NoScrollbar,
            ReadOnly = false,
            Cols = 16,
            OptShowOptions = true,
            OptShowAscii = true,
            OptGreyOutZeroes = true,
            OptUpperCaseHex = true,
            OptMidColsCount = 8,
            OptAddrDigitsCount = 0,
            OptFooterExtraHeight = 0,
            HighlightColor = 0xFF_FFFF32,
            Data = new byte[]
            {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F
            }
        };

        backend.Logger.Information("Run Backend loop");
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
