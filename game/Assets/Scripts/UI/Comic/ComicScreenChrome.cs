#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cipher.Game.UI.Comic
{
    /// <summary>
    /// Reads the devices and hands back a <see cref="ComicNav"/>. The only engine-facing half of the
    /// three screen controllers, and the only part of them that a unit test cannot reach — which is
    /// why it contains no decisions at all beyond "which physical control means which intent".
    ///
    /// One instance per screen, because the stick repeat is stateful and two open panels must not
    /// share a latch.
    /// </summary>
    public sealed class ComicInput
    {
        private readonly AxisRepeat _vertical = new AxisRepeat();
        private readonly AxisRepeat _horizontal = new AxisRepeat();

        /// <summary>True when a pad is plugged in. Every prompt on every page is chosen by this.</summary>
        public static bool PadPresent => Gamepad.current != null;

        /// <summary>Clear the held direction. Called when a screen opens, so an off-centre stick is quiet.</summary>
        public void Reset()
        {
            _vertical.Reset();
            _horizontal.Reset();
        }

        /// <summary>
        /// One frame of intent. Unscaled time on purpose: build mode and these panels can be up while
        /// adrenaline focus is slowing the world, and a menu that scrolls at 0.3x is a menu that feels
        /// broken.
        /// </summary>
        public ComicNav Read()
        {
            var pad = Gamepad.current;
            var kb = Keyboard.current;
            float dt = Time.unscaledDeltaTime;

            var nav = new ComicNav();

            // The LEFT STICK scrolls, with a hold repeat. The owner asked for this by name:
            // "up on the d-pad opens this skills list and then my left analog control scrolling
            // through it".
            if (pad != null)
            {
                var stick = pad.leftStick.ReadValue();
                int v = _vertical.Step(stick.y, dt);
                int h = _horizontal.Step(stick.x, dt);
                if (v > 0) nav.Up = true;
                if (v < 0) nav.Down = true;
                if (h < 0) nav.Left = true;
                if (h > 0) nav.Right = true;

                // The d-pad works too. It is safe now that an open page consumes every input: the
                // press that opened the page is consumed by the bootstrap's own early-out, so the
                // next press can only ever mean "move".
                if (pad.dpad.up.wasPressedThisFrame) nav.Up = true;
                if (pad.dpad.down.wasPressedThisFrame) nav.Down = true;
                if (pad.dpad.left.wasPressedThisFrame) nav.Left = true;
                if (pad.dpad.right.wasPressedThisFrame) nav.Right = true;

                if (pad.buttonSouth.wasPressedThisFrame) nav.Confirm = true;
                // B and Back both close. Two ways out of a modal screen is never the wrong number.
                if (pad.buttonEast.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame) nav.Cancel = true;
                if (pad.buttonWest.wasPressedThisFrame) nav.Secondary = true;
                if (pad.buttonNorth.wasPressedThisFrame) nav.Leave = true;
            }

            if (kb != null)
            {
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) nav.Up = true;
                if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) nav.Down = true;
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) nav.Left = true;
                if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) nav.Right = true;

                if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) nav.Confirm = true;
                if (kb.escapeKey.wasPressedThisFrame) nav.Cancel = true;
                if (kb.fKey.wasPressedThisFrame) nav.Secondary = true;
                if (kb.lKey.wasPressedThisFrame) nav.Leave = true;
            }

            return nav;
        }
    }

    /// <summary>
    /// The furniture every comic screen shares: the black gutter under the page, the page rect
    /// itself, and the two bands at the foot — one sentence of plain English, then the prompts.
    ///
    /// The sentence is not decoration. The owner's complaint was not that these screens were ugly
    /// first, it was that he did not know what he was looking at: "I don't understand where I'm
    /// picking up gear from how my inventory works". A page that answers that in one line at the
    /// bottom answers it every time it is opened, which no tutorial does.
    /// </summary>
    public static class ComicScreenChrome
    {
        /// <summary>Height of the explain + prompt block at the foot of every page.</summary>
        public const float FooterHeight = 96f;

        private const float Margin = 14f;

        /// <summary>Two lines of caption. One sentence at 1600 wide is two at 1280.</summary>
        private const float ExplainHeight = 52f;

        /// <summary>The rect the page proper gets: the screen minus a margin and the footer.</summary>
        public static ComicRect PageRect(float uiW, float uiH)
            => new ComicRect(Margin, Margin, uiW - Margin * 2f, uiH - Margin * 2f - FooterHeight);

        /// <summary>Where the footer goes, directly under <see cref="PageRect"/>.</summary>
        public static ComicRect FooterRect(float uiW, float uiH)
        {
            var page = PageRect(uiW, uiH);
            return new ComicRect(page.X, page.Bottom + 8f, page.W, FooterHeight - 8f);
        }

        /// <summary>Blacks the whole screen out before the page is drawn over it.</summary>
        public static void Backdrop(float uiW, float uiH)
            => ComicPages.Gutter(new ComicRect(0f, 0f, uiW, uiH));

        /// <summary>
        /// The caption box carries the explanation and the paper strip under it carries the controls.
        /// Two bands rather than one line because a sentence and a key list read at different speeds,
        /// and jamming them together is how the old screens became "computer gibberish".
        /// </summary>
        public static void Footer(ComicRect r, string explain, string prompts)
        {
            if (r.IsEmpty) return;

            ComicInk.Caption(new ComicRect(r.X, r.Y, r.W, ExplainHeight), explain, wrap: true);

            var strip = new ComicRect(r.X, r.Y + ExplainHeight + 4f, r.W, r.H - ExplainHeight - 4f);
            if (strip.IsEmpty) return;
            ComicInk.PaperFill(strip);
            ComicInk.Border(strip, 2f);
            ComicInk.Small(strip.Inset(12f, 0f, 12f, 0f), prompts);
        }
    }
}
