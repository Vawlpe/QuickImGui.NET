using ImGuiNET;
using System.Reflection;
using System.Numerics;
using QuickImGuiNET.Utils;

namespace QuickImGuiNET.Example.Veldrid;

public class ExampleWidget : Widget
{
    public Texture IconTexture;
    public Vector2 IconRenderSize;
    public bool TestEnabled;
    public bool TestClicked;
    public int TestDragVal;
    public float TestCountdown = 0;
    public int TestCooldownMult;
    public ExampleWidget() : base()
    {
        IconTexture = Texture.Bind(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Icon.png"), Program.backend);
    }
    public override void RenderContent()
    {
        ImGui.Text("Hello World!");
        ImGui.Image(IconTexture.ID, IconRenderSize);

        if (TestCooldownMult < 5) {

            UI.WithDisabled(new UI.WithFlags(), () => TestCountdown == 0,
                () => ImGui.Checkbox(TestCountdown > 0 ? $"Random Cooldown({TestCountdown:F0}s left...)" : "You can click now lol~!", ref TestEnabled)
            );

            UI.WithDisabled(new UI.WithFlags(), () => TestEnabled,
                () => UI.WithColors(new UI.WithFlags(),
                    () => TestDragVal > 25,
                    new (ImGuiCol,uint)[] { (ImGuiCol.Text, 0xFF3EAFFC) },
                    () => UI.WithColors(new UI.WithFlags(),
                        () => TestDragVal > 60,
                        new (ImGuiCol,uint)[] { (ImGuiCol.Text, 0xFF0000C4) },
                        () => UI.WithSameLine(new UI.WithFlags(), null,
                            () => { if (ImGui.Button("Clicc to show thingy idk"))
                                TestClicked = true;
                            }, () => { if (TestClicked)
                                ImGui.DragInt(String.Empty, ref TestDragVal, 0.1f, 0, 100, TestDragVal switch {
                                    <=10  => "really? (00-10)",
                                    <=25  => "okay~!  (11-25)",
                                    <=40  => "nice~!! (26-40)",
                                    <=50  => "poggers (41-50)",
                                    <=60  => "E       (51-60)",
                                    <=75  => "ohno-   (61-75)",
                                    <=100 => "YES~!   (76-100)",
                                    _     => "[INVALID]"
                                });
                            }
                        )
                    )
                )
            );

            if (TestDragVal >= 100) {
                TestClicked = false;
                TestDragVal = 0;
                TestEnabled = false;
                TestCooldownMult += 1;
                TestCountdown = 10 * TestCooldownMult;
            }

            if (TestCountdown > 0)
                TestCountdown -= (1f/60f);
            if (TestCountdown < 0)
                TestCountdown = 0;
        }
        else {
            ImGui.Text("Lorem ipsum dolor sit amet...");
            ImGui.Text("You did the dumb UI thing 5 times, nice, now read the code bish...");
            UI.WithColors(new UI.WithFlags(), null,
                new (ImGuiCol,uint)[] {
                    (ImGuiCol.Text, 0xFF117DC1)
                },
                () => ImGui.Text("- \"Yes the code sucks, idc\"")
            );
        }
    }
}
