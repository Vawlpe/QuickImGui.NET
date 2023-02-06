using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using VR = Veldrid;

namespace QuickImGuiNET.Veldrid;

public class InputManager : IInputManager
{
    private bool _altDown;
    private bool _controlDown;
    private bool _shiftDown;
    private bool _winKeyDown;
    
    public void UpdateInput(dynamic inputData)
    {
        var io = ImGui.GetIO();
        VR.InputSnapshot snapshot = inputData;
        io.MousePos = snapshot.MousePosition;
        io.MouseWheel = snapshot.WheelDelta;

        // Determine if any of the mouse buttons were pressed during this input period, even if they are no longer held.
        var leftPressed = false;
        var middlePressed = false;
        var rightPressed = false;
        foreach (var me in snapshot.MouseEvents)
            if (me.Down)
                switch (me.MouseButton)
                {
                    case VR.MouseButton.Left:
                        leftPressed = true;
                        break;
                    case VR.MouseButton.Middle:
                        middlePressed = true;
                        break;
                    case VR.MouseButton.Right:
                        rightPressed = true;
                        break;
                }

        io.MouseDown[0] = leftPressed || snapshot.IsMouseDown(VR.MouseButton.Left);
        io.MouseDown[1] = middlePressed || snapshot.IsMouseDown(VR.MouseButton.Right);
        io.MouseDown[2] = rightPressed || snapshot.IsMouseDown(VR.MouseButton.Middle);

        if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
            unsafe
            {
                int x, y;
                var buttons = SDL2Extensions.SDL_GetGlobalMouseState(&x, &y);
                io.MouseDown[0] = (buttons & 0b0001) != 0;
                io.MouseDown[1] = (buttons & 0b0010) != 0;
                io.MouseDown[2] = (buttons & 0b0100) != 0;
                io.MousePos = new Vector2(x, y);
            }

        var keyCharPresses = snapshot.KeyCharPresses;
        foreach (var c in keyCharPresses) io.AddInputCharacter(c);

        var keyEvents = snapshot.KeyEvents;
        foreach (var keyEvent in keyEvents)
        {
            io.KeysDown[(int)keyEvent.Key] = keyEvent.Down;
            switch (keyEvent.Key)
            {
                case VR.Key.ControlLeft:
                    _controlDown = keyEvent.Down;
                    break;
                case VR.Key.ShiftLeft:
                    _shiftDown = keyEvent.Down;
                    break;
                case VR.Key.AltLeft:
                    _altDown = keyEvent.Down;
                    break;
                case VR.Key.WinLeft:
                    _winKeyDown = keyEvent.Down;
                    break;
            }
        }

        io.KeyCtrl = _controlDown;
        io.KeyAlt = _altDown;
        io.KeyShift = _shiftDown;
        io.KeySuper = _winKeyDown;

        var viewports = ImGui.GetPlatformIO().Viewports;
        for (var i = 1; i < viewports.Size; i++)
        {
            var v = viewports[i];
            var window = (ImGuiWindow)GCHandle.FromIntPtr(v.PlatformUserData).Target;
            window.Update();
        }
    }

    public void SetKeyMappings()
    {
        var io = ImGui.GetIO();
        io.KeyMap[(int)ImGuiKey.Tab] = (int)VR.Key.Tab;
        io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)VR.Key.Left;
        io.KeyMap[(int)ImGuiKey.RightArrow] = (int)VR.Key.Right;
        io.KeyMap[(int)ImGuiKey.UpArrow] = (int)VR.Key.Up;
        io.KeyMap[(int)ImGuiKey.DownArrow] = (int)VR.Key.Down;
        io.KeyMap[(int)ImGuiKey.PageUp] = (int)VR.Key.PageUp;
        io.KeyMap[(int)ImGuiKey.PageDown] = (int)VR.Key.PageDown;
        io.KeyMap[(int)ImGuiKey.Home] = (int)VR.Key.Home;
        io.KeyMap[(int)ImGuiKey.End] = (int)VR.Key.End;
        io.KeyMap[(int)ImGuiKey.Delete] = (int)VR.Key.Delete;
        io.KeyMap[(int)ImGuiKey.Backspace] = (int)VR.Key.BackSpace;
        io.KeyMap[(int)ImGuiKey.Enter] = (int)VR.Key.Enter;
        io.KeyMap[(int)ImGuiKey.Escape] = (int)VR.Key.Escape;
        io.KeyMap[(int)ImGuiKey.Space] = (int)VR.Key.Space;
        io.KeyMap[(int)ImGuiKey.A] = (int)VR.Key.A;
        io.KeyMap[(int)ImGuiKey.C] = (int)VR.Key.C;
        io.KeyMap[(int)ImGuiKey.V] = (int)VR.Key.V;
        io.KeyMap[(int)ImGuiKey.X] = (int)VR.Key.X;
        io.KeyMap[(int)ImGuiKey.Y] = (int)VR.Key.Y;
        io.KeyMap[(int)ImGuiKey.Z] = (int)VR.Key.Z;
        io.KeyMap[(int)ImGuiKey.Space] = (int)VR.Key.Space;
    }
}