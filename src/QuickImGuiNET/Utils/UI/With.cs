namespace QuickImGuiNET.Utils;
using ImGuiNET;
public static partial class UI
{
    public struct WithFlags {
        public FailFlags   Fail;
        public EffectFlags Effect;
        public EffectFlags Undo;

        public enum FailFlags
        {
            //10101
            Ignore = 0b00, Skip = 0b01, Stop = 0b10,
            Element = 0b100, Effect = 0b1000, Undo = 0b10000,
            Custom = 0b100000,
            //-----------------------------------------------
            SkipElement       = Skip|Element,
            SkipEffect        = Skip|Effect,
            SkipUndo          = Skip|Undo,
            CustomSkipElement = Custom|Skip|Element,
            CustomSkipEffect  = Custom|Skip|Effect,
            CustomSkipUndo    = Custom|Skip|Undo,
            CustomStop        = Custom|Stop,
        }
        public enum EffectFlags
        {
            Ignore = 0b00, Before = 0b01, After = 0b10, Once = 0b100,
            //-------------------------------------------------------
            OnceBefore = Once|Before,
            OnceAfter  = Once|After,
            Around     = Before|After,
            OnceAround = Once|Around
        }
    }

    public static void With(WithFlags Flags, Action Effect,  Action? UndoEffect = null, Action? CustomFail = null, Func<bool>? Condition = null, params Action[] Elements)
    {
        Condition  ??= () => true;
        UndoEffect ??= () => {};
        CustomFail ??= () => {};
        List<Action> Callbacks = new();

        // Cache re-usable flag queries
        bool hasSkipEffect  = Flags.Fail.HasFlag(WithFlags.FailFlags.SkipEffect);
        bool hasSkipUndo    = Flags.Fail.HasFlag(WithFlags.FailFlags.SkipUndo);
        bool hasSkipElement = Flags.Fail.HasFlag(WithFlags.FailFlags.SkipElement);
        bool hasStop        = Flags.Fail.HasFlag(WithFlags.FailFlags.Stop);
        bool hasCustom      = Flags.Fail.HasFlag(WithFlags.FailFlags.Custom);

        // ONCE-BEFORE
        // (condition cache)
        bool failed      = !Condition();
        bool skipEffect  = hasSkipEffect && failed;
        bool skipUndo    = hasSkipUndo   && failed;
        bool skipElement = false;
        
        if (Flags.Effect.HasFlag(WithFlags.EffectFlags.OnceBefore) && !skipEffect)
            Callbacks.Add(Effect);
        if (Flags.Undo.HasFlag(WithFlags.EffectFlags.OnceBefore) && !skipUndo)
            Callbacks.Add(UndoEffect);

        for (int i = 0; i < Elements.Length; i++)
        {
            // BEFORE
            failed = !Condition();
            if (hasCustom && failed)
                Callbacks.Add(CustomFail);
            if (hasStop && failed)
                break;
            skipEffect  = hasSkipEffect  && failed;
            skipUndo    = hasSkipUndo    && failed;
            skipElement = hasSkipElement && failed;

            if ((Flags.Effect ^ WithFlags.EffectFlags.Once).HasFlag(WithFlags.EffectFlags.OnceBefore) && !skipEffect)
                Callbacks.Add(Effect);
            if ((Flags.Undo ^ WithFlags.EffectFlags.Once).HasFlag(WithFlags.EffectFlags.OnceBefore) && !skipUndo)
                Callbacks.Add(UndoEffect);

            // Element
            if (!skipElement) Callbacks.Add(Elements[i]);

            // AFTER
            if ((Flags.Effect ^ WithFlags.EffectFlags.Once).HasFlag(WithFlags.EffectFlags.OnceAfter) && !skipEffect)
                Callbacks.Add(Effect);
            if ((Flags.Undo ^ WithFlags.EffectFlags.Once).HasFlag(WithFlags.EffectFlags.OnceAfter) && !skipUndo)
                Callbacks.Add(UndoEffect);
        }

        // ONCE-AFTER
        failed      = !Condition();
        skipEffect  = hasSkipEffect && failed;
        skipUndo    = hasSkipUndo   && failed;

        if (Flags.Effect.HasFlag(WithFlags.EffectFlags.OnceAfter) && !skipEffect)
            Callbacks.Add(Effect);
        if (Flags.Undo.HasFlag(WithFlags.EffectFlags.OnceAfter) && !skipUndo)
            Callbacks.Add(UndoEffect);

        // Execute
        Array.ForEach(Callbacks.ToArray(), (c) => c());
    }

    public static void WithSameLine(WithFlags Flags, Func<bool>? Condition, params Action[] Elements) => With(
        Flags: new() {
            Fail   = Flags.Fail,
            Effect = Flags.Effect | WithFlags.EffectFlags.After,
            Undo   = Flags.Undo   | WithFlags.EffectFlags.OnceAfter
        },
        Effect:     () => ImGui.SameLine(),
        UndoEffect: () => ImGui.Dummy(new(0,0)),
        Condition: Condition, Elements: Elements
    );
    public static void WithDisabled(WithFlags Flags, Func<bool>? Condition, params Action[] Elements) => With(
        Flags: new() {
            Fail   = Flags.Fail,
            Effect = Flags.Effect | WithFlags.EffectFlags.OnceBefore,
            Undo   = Flags.Undo   | WithFlags.EffectFlags.OnceAfter
        },
        Effect:     () => ImGui.BeginDisabled(Condition is null ? true : !Condition()),
        UndoEffect: () => ImGui.EndDisabled(),
        Condition: Condition, Elements: Elements
    );
    public static void WithColors(WithFlags Flags, Func<bool>? Condition, (ImGuiCol, uint)[] Colors, params Action[] Elements) => With(
        Flags: new() { 
            Fail   = Flags.Fail   | WithFlags.FailFlags.CustomSkipEffect,
            Effect = Flags.Effect | WithFlags.EffectFlags.Before,
            Undo   = Flags.Undo   | WithFlags.EffectFlags.After
        },
        Effect:     () => Array.ForEach(Colors, (c) => ImGui.PushStyleColor(c.Item1, c.Item2)),
        UndoEffect: () => ImGui.PopStyleColor(Colors.Length),
        CustomFail: () => ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.Text)),
        Condition: Condition, Elements: Elements
    );
    public static void WithStyles(WithFlags Flags, Func<bool>? Condition, (ImGuiStyleVar, dynamic)[] Styles, params Action[] Elements) => With(
        Flags: new() { 
            Fail   = Flags.Fail   | WithFlags.FailFlags.CustomSkipEffect,
            Effect = Flags.Effect | WithFlags.EffectFlags.Before,
            Undo   = Flags.Undo   | WithFlags.EffectFlags.After
        },
        Effect:     () => Array.ForEach(Styles, (c) => ImGui.PushStyleVar(c.Item1, c.Item2)),
        UndoEffect: () => ImGui.PopStyleVar(Styles.Length),
        CustomFail: () => ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha),
        Condition: Condition, Elements: Elements
    );
}