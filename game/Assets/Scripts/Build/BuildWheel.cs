#nullable enable
using System;

namespace Cipher.Game.Build
{
    /// <summary>
    /// Selection maths for the radial build menu. Owner's call, 2026-09-11:
    ///
    ///   "Build should be lb button opens build wheel instead of tiny text prompts in top corner"
    ///
    /// He is right that a row of text in the corner is not a menu, it is a legend. A wheel puts the
    /// options where the thumb already is and makes the choice muscle memory rather than reading.
    ///
    /// Kept as pure maths, separate from the drawing, so the selection rule is unit tested and the
    /// OnGUI code stays dumb. Nothing here knows what a turret is.
    /// </summary>
    public sealed class BuildWheel
    {
        /// <summary>Stick deflection below this leaves the selection alone rather than snapping.</summary>
        public float DeadZone { get; set; } = 0.35f;

        public int OptionCount { get; private set; }
        public bool IsOpen { get; private set; }

        /// <summary>Currently highlighted option, or -1 when the stick is centred and nothing was chosen.</summary>
        public int Selected { get; private set; } = -1;

        /// <summary>Where the last aim vector pointed, in radians. Used to draw the pointer.</summary>
        public float AimAngle { get; private set; }
        public bool HasAim { get; private set; }

        public void Open(int optionCount, int startSelection = -1)
        {
            OptionCount = Math.Max(0, optionCount);
            IsOpen = OptionCount > 0;
            Selected = (startSelection >= 0 && startSelection < OptionCount) ? startSelection : -1;
            HasAim = false;
            AimAngle = 0f;
        }

        /// <summary>Closes the wheel and reports what was highlighted, or -1 for nothing.</summary>
        public int Close()
        {
            IsOpen = false;
            HasAim = false;
            int chosen = Selected;
            return chosen;
        }

        /// <summary>
        /// Feeds a stick or mouse direction in. Y is up-positive. Inside the dead zone the previous
        /// selection is kept, which stops a thumb drifting back to centre from clearing the choice.
        /// </summary>
        public void Aim(float x, float y)
        {
            if (!IsOpen || OptionCount <= 0) return;

            float magnitude = MathF.Sqrt(x * x + y * y);
            if (magnitude < DeadZone) { HasAim = false; return; }

            // Screen-style angle: 0 at twelve o'clock, increasing clockwise, which is how the
            // options are laid out on the drawn wheel.
            float angle = MathF.Atan2(x, y);
            if (angle < 0f) angle += MathF.PI * 2f;

            AimAngle = angle;
            HasAim = true;
            Selected = IndexForAngle(angle, OptionCount);
        }

        /// <summary>Which wedge an angle falls in. Wedges are centred on their option's angle.</summary>
        public static int IndexForAngle(float angle, int optionCount)
        {
            if (optionCount <= 0) return -1;
            float wedge = MathF.PI * 2f / optionCount;
            float shifted = angle + wedge * 0.5f;
            if (shifted >= MathF.PI * 2f) shifted -= MathF.PI * 2f;
            int index = (int)(shifted / wedge);
            return index >= optionCount ? 0 : index;
        }

        /// <summary>The angle an option sits at, for drawing. Twelve o'clock is option zero.</summary>
        public static float AngleForIndex(int index, int optionCount)
            => optionCount <= 0 ? 0f : index * (MathF.PI * 2f / optionCount);

        /// <summary>Keyboard and shoulder-button fallback for players not using a stick.</summary>
        public void Step(int delta)
        {
            if (!IsOpen || OptionCount <= 0 || delta == 0) return;
            int start = Selected < 0 ? 0 : Selected;
            int next = (start + delta) % OptionCount;
            if (next < 0) next += OptionCount;
            Selected = next;
        }
    }
}
