using ImGuiNET;
using System.Numerics;

namespace QuickImGuiNET;

public static partial class Widgets {
    public class FileManager : Widget
    {
        public FileManager(Backend backend, string ID) : base(backend, ID) {
            backend.Events["onMainMenuBar"]["Debug"].Hook += RenderOnMainMenuBar_Debug;
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

        public override void RenderContent()
        {
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
                        } else Close();
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
            Utils.UI.WithDisabled(new Utils.UI.WithFlags(),
                () => Mode.HasFlag(SelectionMode.Folder) != File.Exists(Selected),
                () => { if (ImGui.Button(Mode.HasFlag(SelectionMode.Save) ? "Save" : "Open")) {
                            if (Selected == String.Empty) {
                                Selected = CurrentPath;
                                Close();
                            } else if (Directory.Exists(Selected)) {
                                CurrentPath = Selected;
                                Selected = String.Empty;
                                RefreshFiles();
                            }
                        }
                }
            );
        }

        public dynamic? RenderOnMainMenuBar_Debug(params dynamic[]? args)
        {
            ImGui.MenuItem($"Open {Name}", String.Empty, ref Visible);
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
