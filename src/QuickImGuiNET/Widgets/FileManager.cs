using ImGuiNET;
using System.Numerics;

namespace QuickImGuiNET;

public static partial class Widgets
{
    public class FileManager : Widget
    {
        public FileManager(Backend backend, string Name, bool AutoRegister = true) : base(backend, Name, AutoRegister)
        {
            backend.Events["onMainMenuBar"]["Debug"].Hook += RenderOnMainMenuBar_Debug;
            //backend.Events["widgetReg"][Name]["close"].Hook += (fm) => { Console.WriteLine($"Selected {fm?[0].Selected}"); return null; };
            
            //Create ConfirmPrompt Widget w/o auto-registration
            Prompt = new ConfirmPrompt(backend, $"{Name}_ConfirmPrompt001", false) {
                Visible       = false,
                RenderMode    = WidgetRenderMode.Modal,
                Position      = ImGui.GetMainViewport().GetWorkCenter() - new Vector2(150, 50),
                PositionCond  = ImGuiCond.Appearing,
                WindowFlags   = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize,
                Size          = new(300, 100),
                //FIXME: Calling .Close() instead of manually setting Visible triggers
                //      the event twice, perhaps a quirk with Widget.RenderInModal() ?
                OkHandler     = () => backend.WidgetReg[Name].Visible = false,
                CancelHandler = () => {},
                Prompt        = "A file with that path already exists, are you sure you want to save over it?",
                ButtonOK      = "Save",
                ButtonCancel  = "Cancel"
            };
            //Manually register events for prompt, we only need "close" but if all 3 aren't registered it'll complain
            backend.Events["widgetReg"].Children.Add($"{Name}_ConfirmPrompt001",
                new(new() {
                    { "open", new() },
                    { "close", new() },
                    { "toggle", new() }
                }));
            backend.Events["widgetReg"][$"{Name}_ConfirmPrompt001"]["close"].Hook += (p) => { (p?[0].OkOrCancel ? p?[0].OkHandler : p?[0].CancelHandler)?.Invoke(); return null; };
            
            this.backend = backend;
        }

        public string Selected = String.Empty;
        public SelectionMode Mode;
        public string CurrentPath;
        public bool ShowHiddenFiles;
        public bool ShowSystemFiles;
        public Dictionary<string, List<string>> FileTypeQueries;
        public string CurrentFTQuery;

        private DirectoryInfo[]? FoldersFound;
        private FileInfo[]? FilesFound;
        private readonly Backend backend;
        private ConfirmPrompt Prompt;

        public override void RenderContent()
        {
            //TODO Better path display and input
            ImGui.Text(CurrentPath);
            if (!ImGui.BeginListBox(String.Empty, ImGui.GetWindowContentRegionMax() - new Vector2(50, 75)))
                return;

            // File/dir list
            if (FilesFound is null || FoldersFound is null) {
                ImGui.Text($"Refreshing...");
                RefreshFiles();
            } else {
                if (Directory.Exists(Path.GetFullPath(Path.Combine(CurrentPath, ".."))) && ImGui.Selectable("..")) {
                    CurrentPath = Path.GetFullPath(Path.Combine(CurrentPath, ".."));
                    RefreshFiles();
                }

                foreach (FileSystemInfo fi in FoldersFound.Concat(
                        Mode.HasFlag(SelectionMode.Folder)
                        ? new FileSystemInfo[] {}
                        : (FileSystemInfo[])FilesFound)) {
                    ImGui.PushStyleColor(ImGuiCol.Text,
                        fi.Attributes.HasFlag(FileAttributes.Hidden)    ? 0xFF_AAAAAA :
                        fi.Attributes.HasFlag(FileAttributes.System)    ? 0xFF_5511CC :
                        fi.Attributes.HasFlag(FileAttributes.Directory) ? 0xFF_4FE9FC :
                        ImGui.GetColorU32(ImGuiCol.Text)
                    );

                    // Left-Click
                    if (ImGui.Selectable(fi.Name, Selected == fi.FullName))
                        Selected = fi.FullName;
                    ImGui.PopStyleColor();

                    // Double-Click
                    if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) { 
                        if (fi.Attributes.HasFlag(FileAttributes.Directory)) {
                            CurrentPath = fi.FullName;
                            Selected = String.Empty;
                            RefreshFiles();
                        } else if (Mode.HasFlag(SelectionMode.Save)) 
                             Prompt.Open();
                        else Close();
                        break;
                    }

                    // Right-Click
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) 
                        Selected = String.Empty;
                }
                ImGui.EndListBox();
            }

            // Buttons
            if (ImGui.Button("Cancel")) {
                Selected = String.Empty;
                Close();
            }
            ImGui.SameLine();
            ImGui.BeginDisabled(Selected != String.Empty);
            if (ImGui.Button(Mode.HasFlag(SelectionMode.Save) ? "Save" : "Open")) {
                if (Directory.Exists(Selected)) {
                    CurrentPath = Selected;
                    Selected = String.Empty;
                    RefreshFiles();
                } else if (Mode.HasFlag(SelectionMode.Save))
                        Prompt.Open();
                else Close();
            }
            ImGui.EndDisabled();

            //TODO "CurrentFTQuery" Dropdown
            
            // Render the confirmation prompt inside this one so they stack properly
            // This would break if the prompt was auto-registered to backend.WidgetReg
            // as that would try to draw the confirmation prompt on it's own instead of here
            Prompt.Render();
        }

        public dynamic? RenderOnMainMenuBar_Debug(params dynamic[]? args)
        {
            ImGui.MenuItem($"Open {Name.Replace("#", @"\#")}", String.Empty, ref Visible);
            return null;
        }
        private void RefreshFiles()
        {
            FileSystemInfo[] FSIs = new DirectoryInfo(CurrentPath).GetFileSystemInfos(String.Join(", ", FileTypeQueries[CurrentFTQuery]), new EnumerationOptions() {
                AttributesToSkip =
                    (!ShowHiddenFiles                    ? FileAttributes.Hidden    : 0) |
                    (!ShowSystemFiles                    ? FileAttributes.System    : 0) |
                    (Mode.HasFlag(SelectionMode.Folder)  ? FileAttributes.Normal    : 0)
            });

            FoldersFound = FSIs            .Where((x) => x.Attributes.HasFlag(FileAttributes.Directory)) .Select((d) => (DirectoryInfo)d).ToArray();
            FilesFound   = FSIs.Except(FSIs.Where((x) => x.Attributes.HasFlag(FileAttributes.Directory))).Select((f) => (FileInfo     )f).ToArray();
        }

        public enum SelectionMode
        {
            Open, File = 0b00, Save = 0b10, Folder = 0b01,
            //--------------------------------------------
            OpenFile   = Open|File,
            OpenFolder = Open|Folder,
            SaveFile   = Save|File,
            SaveFolder = Save|Folder
        }
    }
}
