using ImGuiNET;


namespace QuickImGuiNET;
public static partial class Widgets
{
    public class ConfirmPrompt : Widget
    {
        public ConfirmPrompt(Backend backend, string Name, bool AutoRegister = true) : base(backend, Name, AutoRegister)
        {
            backend.Events["onMainMenuBar"]["Debug"].Hook += RenderOnMainMenuBar_Debug;
        }
        public override void RenderContent()
        {
            ImGui.TextWrapped(Prompt);
            if (ImGui.Button(ButtonCancel)) {
                OkOrCancel = false;
                Close();
            }
            ImGui.SameLine();
            if (ImGui.Button(ButtonOK)) {
                OkOrCancel = true;
                Close();
            }
        }

        public dynamic? RenderOnMainMenuBar_Debug(params dynamic[]? args)
        {
            ImGui.MenuItem($"Open {Name.Replace("#", @"\#")}", String.Empty, ref Visible);
            return null;
        }
        public string ButtonOK = "Ok";
        public string ButtonCancel = "Cancel";
        public string Prompt = "test";
        public bool OkOrCancel;
        public Action OkHandler = () => {};
        public Action CancelHandler = () => {};
    }
}