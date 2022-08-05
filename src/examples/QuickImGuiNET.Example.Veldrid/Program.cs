using ImGuiNET;
using VRBK = QuickImGuiNET.Veldrid;

namespace QuickImGuiNET.Example.Veldrid;

public class Program
{
    public static readonly string[] defaultArgs = new string[] { "640", "480", "-1" };
    public static void Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Invalid arguments. Usage: QuickImGuiNET.Example.Veldrid.exe <width> <height> <veldridBacknedIndex>");
            args = defaultArgs;
        }
        int width = int.Parse(args[0]);
        int height = int.Parse(args[1]);
        int veldridBackendIndex = int.Parse(args[2]);

        var backend = new VRBK.Backend(width, height, veldridBackendIndex);

        backend.Run(DrawUI);
    }

    public static bool showDemoWindow = false;
    public static void DrawUI()
    {
        ImGui.DockSpaceOverViewport(ImGui.GetMainViewport());
        if (ImGui.BeginMainMenuBar())
        {
            ImGui.MenuItem("Show Demo Window", "", ref showDemoWindow);
            ImGui.EndMainMenuBar();
        }

        if (showDemoWindow)
            ImGui.ShowDemoWindow(ref showDemoWindow);

        if (!ImGui.Begin("QuickImGuiNET.Example.Veldrid"))
            return;

        ImGui.Text("Hello World!");
        ImGui.End();
    }
}