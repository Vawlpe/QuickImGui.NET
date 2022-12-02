using ImGuiNET;

namespace QuickImGuiNET;

public static partial class Widgets
{
    public class ConfirmPrompt : Widget
    {
        public string ButtonCancel = "Cancel";

        public string ButtonOK = "Ok";
        public Action CancelHandler = () => { };
        public Action OkHandler = () => { };
        public bool OkOrCancel;
        public string Prompt = "test";

        public ConfirmPrompt(Backend backend, string Name, bool AutoRegister = true) : base(backend, Name, AutoRegister) { }

        public override void RenderContent()
        {
            ImGui.TextWrapped(Prompt);
            if (ImGui.Button(ButtonCancel))
            {
                OkOrCancel = false;
                Close();
            }

            ImGui.SameLine();
            if (ImGui.Button(ButtonOK))
            {
                OkOrCancel = true;
                Close();
            }
        }
    }
}