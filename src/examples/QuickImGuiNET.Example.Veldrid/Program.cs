using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Serilog;
using Tomlyn;
using VRBK = QuickImGuiNET.Veldrid;

namespace QuickImGuiNET.Example.Veldrid;

public class Program
{
    public static Backend backend;

    public static bool showDemoWindow;

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
        Log.Information("QUIMGUIN v0.1");

        // Re-usable vars for cfg sinks/sources
        var cfg_toml = new Config.Toml("QUIMGUIN.cfg", ref backend);
        var cfg_cli = new Config.Cli(args, ref backend);

        // Backend shadow ctor
        Log.Logger.Information("Initializing Veldrid Backend");
        backend = new VRBK.Backend
        {
            // Add Config to shadow backend
            Config = new Config
            {
                _default = Toml.ToModel(""),
                Sinks = new IConfigSink[]
                {
                    cfg_toml,
                    cfg_cli
                },
                Sources = new IConfigSource[]
                {
                    cfg_toml,
                    cfg_cli
                }
            },
            // Add Logger to shadow backend
            Logger = new LoggerConfiguration()
                .WriteTo
                .Console( // Log to console
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("QIMGUIN.log", // Log to file
                    outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .MinimumLevel.Debug() // Set minimum logging level
                .CreateLogger(),
            // Add Events to shadow backend
            Events = new Dictionary<string, Event>
            {
                {
                    "onMainMenuBar", new Event(new Dictionary<string, Event>
                    {
                        { "Debug", new Event() }
                    })
                },
                { "widgetReg", new Event() }
            },
            // Add Widget Registry to shadow backend
            WidgetReg = new Dictionary<string, Widget>()
        };

        // Loading config sources
        backend.Logger.Information($"Loading default config + ({backend.Config.Sources.Length}) source(s)");
        backend.Config.LoadDefault();
        backend.Config.From(backend.Config.Sources[0]);
        backend.Config.From(backend.Config.Sources[1]);

        backend.Logger.Information("Config Done, ready to initialize"
                                   + $"\n\t- MainViewportSize: ({backend.Config["window"]["width"]}x{backend.Config["window"]["height"]})"
                                   + $"\n\t- VeldridBackendIndex: {backend.Config["veldrid"]["backend"]}");

        // Initialize shadow -> ready backend
        backend.Init();

        // Auto-register widgets to backend
        new ExampleWidget(backend, "ExampleWidget##example00")
        {
            Visible = true,
            RenderMode = WidgetRenderMode.Window,
            Position = new Vector2(100, 100),
            Size = new Vector2(150, 150),
            SizeCond = ImGuiCond.FirstUseEver,
            PositionCond = ImGuiCond.FirstUseEver,
            IconRenderSize = new Vector2(128, 128)
        };
        new ExampleWidget(backend, "ExampleWidget##example01")
        {
            Visible = true,
            RenderMode = WidgetRenderMode.Window,
            Position = new Vector2(255, 100),
            Size = new Vector2(275, 275),
            SizeCond = ImGuiCond.FirstUseEver,
            PositionCond = ImGuiCond.FirstUseEver,
            IconRenderSize = new Vector2(256, 256)
        };
        new Widgets.FileManager(backend, "FileManager##example00")
        {
            Visible = false,
            RenderMode = WidgetRenderMode.Modal,
            Position = ImGui.GetMainViewport().GetWorkCenter() - new Vector2(250, 250),
            Size = new Vector2(500, 500),
            SizeCond = ImGuiCond.FirstUseEver,
            PositionCond = ImGuiCond.FirstUseEver,
            Mode = Widgets.FileManager.SelectionMode.SaveFile,
            CurrentPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? Path.GetFullPath("~"),
            ShowHiddenFiles = true,
            ShowSystemFiles = true,
            FileTypeQueries = new Dictionary<string, List<string>>
            {
                { "Images", new List<string> { ".png", ".jpg", ".jpeg" } },
                { "All", new List<string> { "*" } }
            },
            CurrentFTQuery = "All"
        };
        new Widgets.MemoryEditor(backend, "MemoryView##example00")
        {
            Visible = false,
            RenderMode = WidgetRenderMode.Window,
            Position = new Vector2(100, 380),
            Size = new Vector2(500, 360),
            SizeCond = ImGuiCond.FirstUseEver,
            PositionCond = ImGuiCond.FirstUseEver,
            WindowFlags = ImGuiWindowFlags.NoScrollbar,
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

        // Run backend loop and handle errors
        backend.Logger.Information("Run Backend loop");
        try
        {
            backend.Run(Draw, UpdateCallback: Update);
        }
        catch (Exception e)
        {
            backend.Logger.Error(e.ToString());
        }
        finally
        {
            backend.Logger.Information($"EXITING: {Environment.ExitCode}");
        }
    }

    public static void Draw()
    {
        ImGui.DockSpaceOverViewport();
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Debug"))
            {
                ImGui.MenuItem("Show Demo Window", string.Empty, ref showDemoWindow);
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