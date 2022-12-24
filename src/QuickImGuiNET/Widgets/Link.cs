using System.Diagnostics;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace QuickImGuiNET;
public static partial class Widgets
{
    public static void Link(string url, string displayText)
    {
        // Calculate the rect of the element.
        // Since the color needs to update before drawing the text
        // we don't actually know the exact size and position of the element we're working with
        // so an estimate is calculated here and used to run "early" checks on the element before it actually exists
        var min = ImGui.GetWindowPos() + ImGui.GetCursorPos();
        var max = min + ImGui.CalcTextSize(displayText);
        uint col;
            
        if (ImGui.IsMouseHoveringRect(min, max))
        {
            // "early" Hover/Click handler based on calculated rect
            col = 0xFF_FF91D7;
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                Process.Start(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? "start"          //win
                        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                            ? "open"       //osx
                            : "xdg-open",  //linux
                    url
                );
            ImGui.SetTooltip($"Open Link in Browser\n\t{url}");
        }
        else
        {
            // Calculate draw-list position for underline
            col = 0xFF_FFAEA3;
            min.Y = max.Y;
            ImGui.GetWindowDrawList().AddLine(min, max, col, 1f);
        }
            
        // Finally actually render the display text
        ImGui.PushStyleColor(ImGuiCol.Text, col);
        ImGui.Text(displayText);
        ImGui.PopStyleColor();
    }
}
