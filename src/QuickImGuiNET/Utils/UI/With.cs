namespace QuickImGuiNET.Utils;
using ImGuiNET;
public static partial class UI
{
    public enum WithFailFlags {
        None        = 0x00,
        SkipEffect  = 0x01,
        SkipElement = 0x02,
        Stop        = 0x04,
        Custom      = 0x08
    }
    public enum WithUndoFlags {
        None          = 0x00,
        Before        = 0x01,
        After         = 0x02,
        Once          = 0x04
    }
    public enum WithEffectFlags {
        None          = 0x00,
        Before        = 0x01,
        After         = 0x02,
        Once          = 0x04
    }
    public struct WithFlags {
        public WithFailFlags   FailFlags;
        public WithUndoFlags   UndoFlags;
        public WithEffectFlags EffectFlags;
    }

    public static void With(Action Effect, Action? UndoEffect, Action? CustomFail, WithFlags Flags, Func<bool>? Condition, params Action[] Elements)
    {
        WithFailFlags   Ffail = Flags.FailFlags;
        WithUndoFlags   Fundo = Flags.UndoFlags;
        WithEffectFlags Feffe = Flags.EffectFlags;

        if (Condition is null)
                Condition = () => true;
        if (UndoEffect is null)
            UndoEffect = () => {};
        if (CustomFail is null)
            CustomFail = () => {};

        if (Feffe.HasFlag(WithEffectFlags.Once | WithEffectFlags.Before))
            if (!(Ffail.HasFlag(WithFailFlags.SkipEffect) && !Condition()))
                Effect();
        
        if (Fundo.HasFlag(WithUndoFlags.Once | WithUndoFlags.Before))
            UndoEffect();

        for (int i = 0; i < Elements.Length; i++)
        {
            if (Ffail.HasFlag(WithFailFlags.Stop) && !Condition())
                break;

            if (Ffail.HasFlag(WithFailFlags.Custom) && !Condition())
                CustomFail();

            if (!Feffe.HasFlag(WithEffectFlags.Once) && Feffe.HasFlag(WithEffectFlags.Before))
                if (!(Ffail.HasFlag(WithFailFlags.SkipEffect) && !Condition()))
                    Effect();
            
            if (!Fundo.HasFlag(WithUndoFlags.Once) && Fundo.HasFlag(WithUndoFlags.Before))
                UndoEffect();

            if (!(Ffail.HasFlag(WithFailFlags.SkipElement) && !Condition()))
                Elements[i]();

            if (!Feffe.HasFlag(WithEffectFlags.Once) && Feffe.HasFlag(WithEffectFlags.After))
                if (!(Ffail.HasFlag(WithFailFlags.SkipEffect) && !Condition()))
                    Effect();
            
            if (!Fundo.HasFlag(WithUndoFlags.Once) && Fundo.HasFlag(WithUndoFlags.After))
                UndoEffect();
        }

        if (Feffe.HasFlag(WithEffectFlags.Once | WithEffectFlags.After))
            if (!(Ffail.HasFlag(WithFailFlags.SkipEffect) && !Condition()))
                Effect();

        if (Fundo.HasFlag(WithUndoFlags.Once | WithUndoFlags.After))
            UndoEffect();
    }

    public static void WithSameLine(WithFlags Flags, Func<bool>? Condition, params Action[] Elements) => With(
        () => ImGui.SameLine(),
        () => ImGui.Dummy(new(0,0)),
        null,
        new () {
            FailFlags   = Flags.FailFlags,
            UndoFlags   = Flags.UndoFlags   | WithUndoFlags.After   | WithUndoFlags.Once,
            EffectFlags = Flags.EffectFlags | WithEffectFlags.After
        },
        Condition, Elements
    );
    public static void WithDisabled(WithFlags Flags, Func<bool>? Condition, params Action[] Elements) => With(
        () => ImGui.BeginDisabled(Condition is null ? true : !Condition()),
        () => ImGui.EndDisabled(),
        null,
        new() {
            FailFlags   = Flags.FailFlags,
            UndoFlags   = Flags.UndoFlags   | WithUndoFlags.After    | WithUndoFlags.Once,
            EffectFlags = Flags.EffectFlags | WithEffectFlags.Before | WithEffectFlags.Once
        },
        Condition, Elements
    );
    public static void WithColors(WithFlags Flags, Func<bool>? Condition, (ImGuiCol, uint)[] Colors, params Action[] Elements) => With(
        () => Array.ForEach(Colors, (c) => ImGui.PushStyleColor(c.Item1, c.Item2)),
        () => ImGui.PopStyleColor(Colors.Length),
        () => ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.Text)),
        new() { 
            FailFlags   = Flags.FailFlags   | WithFailFlags.Custom   | WithFailFlags.SkipEffect,
            UndoFlags   = Flags.UndoFlags   | WithUndoFlags.After,
            EffectFlags = Flags.EffectFlags | WithEffectFlags.Before
        },
        Condition, Elements
    );
    public static void WithStyles(WithFlags Flags, Func<bool>? Condition, (ImGuiStyleVar, dynamic)[] Colors, params Action[] Elements) => With(
        () => Array.ForEach(Colors, (c) => ImGui.PushStyleVar(c.Item1, c.Item2)),
        () => ImGui.PopStyleVar(Colors.Length),
        () => ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha),
        new() { 
            FailFlags   = Flags.FailFlags   | WithFailFlags.Custom,
            UndoFlags   = Flags.UndoFlags   | WithUndoFlags.After,
            EffectFlags = Flags.EffectFlags | WithEffectFlags.Before
        },
        Condition, Elements
    );
}