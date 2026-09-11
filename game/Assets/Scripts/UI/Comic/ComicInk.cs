#nullable enable
using System;
using UnityEngine;

namespace Cipher.Game.UI.Comic
{
    /// <summary>
    /// The comic-page drawing kit for OnGUI.
    ///
    /// ROADMAP-2026-09-12 §5.3 asks the screens to stop being RPG lists: "hard 3 px border, off-white
    /// paper fill, halftone in the gutters, one amber accent, unequal boxes like a page layout". This
    /// is the primitive set that makes that cheap enough to use everywhere — Panel, Halftone, Stamp,
    /// Pencil, Gauge, LeaderLine, Caption — plus the ink figure the kit page is built out of.
    ///
    /// Every texture is generated in code, like WorldBackdrop's ground: there are still no art files
    /// in this project and a clean checkout rebuilds the whole look. They are built lazily on first
    /// draw (a Texture2D cannot be created before Unity is up) and cached in statics.
    ///
    /// DOMAIN RELOAD: the statics are wiped on reload but the textures are not necessarily, so every
    /// accessor tests `!= null` — which is Unity's overloaded null, and therefore also true for a
    /// texture that was destroyed under us. The cache heals itself rather than throwing on the first
    /// OnGUI after a script recompile.
    ///
    /// Everything takes ComicRect so the page layouts stay engine-free; conversion happens here.
    /// </summary>
    public static class ComicInk
    {
        // ------------------------------------------------------------------ palette

        /// <summary>Newsprint, slightly warm. Not white — white reads as a dialog box, not a page.</summary>
        public static readonly Color Paper = new Color32(0xF2, 0xEE, 0xE4, 0xFF);

        /// <summary>Not pure black. Printed ink never is, and 0,0,0 next to warm paper looks like a bug.</summary>
        public static readonly Color Ink = new Color32(0x14, 0x12, 0x10, 0xFF);

        /// <summary>
        /// The ONE accent, and it is the implant light at the temple (ADR-003). Spending it anywhere
        /// else costs it the meaning it has in the world.
        /// </summary>
        public static readonly Color Amber = new Color32(0xFF, 0x9E, 0x1F, 0xFF);

        /// <summary>Locked, unaffordable, not-yours. Drawn, not printed.</summary>
        public static readonly Color PencilGrey = new Color32(0x8C, 0x88, 0x80, 0xFF);

        /// <summary>Hard 3 px. The brief's number, and it is what makes the page read as printed.</summary>
        public const float BorderWidth = 3f;

        /// <summary>How far the panel's shadow block is thrown. Down and right, one light, like a page.</summary>
        public const float ShadowOffset = 6f;

        // ------------------------------------------------------------------ generated textures

        private static Texture2D? _paperTex;
        private static Texture2D? _inkTex;
        private static Texture2D? _pencilTex;
        private static Texture2D? _discTex;
        private static Texture2D? _stampTex;
        private static Texture2D[]? _halftones;

        /// <summary>Density steps baked as separate screens. A halftone varies dot SIZE, not opacity.</summary>
        private static readonly float[] HalftoneDensities = { 0.08f, 0.20f, 0.35f, 0.50f, 0.68f, 0.85f };

        private static Texture2D PaperTex => _paperTex != null ? _paperTex : (_paperTex = BuildPaper());
        private static Texture2D InkTex => _inkTex != null ? _inkTex : (_inkTex = BuildInk());
        private static Texture2D PencilTex => _pencilTex != null ? _pencilTex : (_pencilTex = BuildPencilHatch());
        private static Texture2D DiscTex => _discTex != null ? _discTex : (_discTex = BuildDisc());
        private static Texture2D StampTex => _stampTex != null ? _stampTex : (_stampTex = BuildStampRing());

        private static Texture2D HalftoneTex(int level)
        {
            if (_halftones == null || _halftones.Length != HalftoneDensities.Length || _halftones[0] == null)
            {
                _halftones = new Texture2D[HalftoneDensities.Length];
                for (int i = 0; i < HalftoneDensities.Length; i++)
                    _halftones[i] = BuildHalftone(HalftoneDensities[i]);
            }
            return _halftones[Mathf.Clamp(level, 0, _halftones.Length - 1)];
        }

        /// <summary>
        /// Build every generated texture now. Optional: the accessors build on demand anyway. Call it
        /// once at startup so the first time a player opens the kit screen mid-firefight it does not
        /// pay for eight SetPixels32 passes on that frame.
        /// </summary>
        public static void Warm()
        {
            _ = PaperTex;
            _ = InkTex;
            _ = PencilTex;
            _ = DiscTex;
            _ = StampTex;
            for (int i = 0; i < HalftoneDensities.Length; i++) _ = HalftoneTex(i);
        }

        /// <summary>
        /// Explicit teardown. Not required — the textures are HideAndDontSave and the lazy accessors
        /// tolerate them vanishing — but a test or a tool that churns them can ask for the memory back.
        /// </summary>
        public static void Release()
        {
            Kill(ref _paperTex);
            Kill(ref _inkTex);
            Kill(ref _pencilTex);
            Kill(ref _discTex);
            Kill(ref _stampTex);
            if (_halftones != null)
            {
                foreach (var t in _halftones)
                {
                    if (t != null) UnityEngine.Object.DestroyImmediate(t);
                }
                _halftones = null;
            }
            _titleStyle = _bodyStyle = _captionStyle = _stampStyle = _pencilStyle = _bigStyle = _tinyStyle = null;
        }

        private static void Kill(ref Texture2D? tex)
        {
            if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
            tex = null;
        }

        private static Texture2D New(int w, int h, string name, bool alpha)
        {
            return new Texture2D(w, h, alpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, mipChain: false)
            {
                name = name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        /// <summary>Newsprint: the paper colour plus a little fibre, so a large fill is not a flat slab.</summary>
        private static Texture2D BuildPaper()
        {
            const int size = 128;
            var tex = New(size, size, "ComicPaper", alpha: false);
            var px = new Color32[size * size];
            var rng = new Rng(0xC0FFEEUL);

            for (int i = 0; i < px.Length; i++)
            {
                // Two grains: a fine one for tooth, a rare dark fleck for pulp.
                float tooth = 0.972f + rng.NextFloat() * 0.028f;
                var c = Paper * tooth;
                if (rng.NextFloat() > 0.9965f) c *= 0.88f;
                c.a = 1f;
                px[i] = c;
            }

            tex.SetPixels32(px);
            tex.Apply(false);
            return tex;
        }

        /// <summary>Ink with a little tooth, so a 3 px rule is not a machine-perfect 3 px rule.</summary>
        private static Texture2D BuildInk()
        {
            const int size = 64;
            var tex = New(size, size, "ComicInk", alpha: false);
            var px = new Color32[size * size];
            var rng = new Rng(0x17AB9C3UL);
            for (int i = 0; i < px.Length; i++)
            {
                var c = Ink * (0.86f + rng.NextFloat() * 0.30f);
                c.a = 1f;
                px[i] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(false);
            return tex;
        }

        /// <summary>
        /// A staggered dot screen. Staggered rather than a straight square lattice because that is
        /// what a 45-degree screen looks like, and it tiles seamlessly with an even row count — a
        /// rotated lattice does not.
        /// </summary>
        private static Texture2D BuildHalftone(float density)
        {
            const int size = 32;
            const int pitch = 8; // 4 rows of 4 dots; even, so the stagger wraps
            var tex = New(size, size, $"ComicHalftone{density:F2}", alpha: true);
            var px = new Color32[size * size];

            float radius = pitch * 0.5f * Mathf.Sqrt(Mathf.Clamp01(density)) * 1.28f;
            float r2 = radius * radius;

            for (int y = 0; y < size; y++)
            {
                int row = y / pitch;
                for (int x = 0; x < size; x++)
                {
                    // Squared distance to the nearest dot centre in this row and the rows either
                    // side. Checking the neighbours is what keeps the dots round across a row seam.
                    float best = float.MaxValue;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int rr = row + dy;
                        float ox = (rr & 1) == 0 ? 0f : pitch * 0.5f;   // odd rows are staggered
                        float cy = (rr + 0.5f) * pitch;
                        float cx = Mathf.Round((x - ox - pitch * 0.5f) / pitch) * pitch + ox + pitch * 0.5f;
                        float ddx = x - cx, ddy = y - cy;
                        best = Mathf.Min(best, ddx * ddx + ddy * ddy);
                    }

                    // One pixel of feather: a hard-aliased dot at this size reads as noise.
                    // White with alpha, so the same screen can be printed in ink over paper or in
                    // paper over a black gutter; the caller supplies the colour.
                    var c = Color.white;
                    c.a = Mathf.Clamp01((r2 - best) * 0.5f);
                    px[y * size + x] = c;
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false);
            return tex;
        }

        /// <summary>Faint single-direction hatch. Pencil is a different mark from ink, not paler ink.</summary>
        private static Texture2D BuildPencilHatch()
        {
            const int size = 16;
            var tex = New(size, size, "ComicPencil", alpha: true);
            var px = new Color32[size * size];
            var rng = new Rng(0x9E3779B9UL);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // A 45-degree stroke every four pixels, broken up so it looks drawn.
                    bool stroke = ((x + y) & 3) == 0;
                    var c = Color.white; // tinted at draw time, like the halftone screens
                    c.a = stroke ? 0.30f + rng.NextFloat() * 0.20f : 0f;
                    px[y * size + x] = c;
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false);
            return tex;
        }

        /// <summary>A filled circle with a one-pixel feather. Heads, hands, wheels, anchor pips.</summary>
        private static Texture2D BuildDisc()
        {
            const int size = 64;
            var tex = New(size, size, "ComicDisc", alpha: true);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[size * size];
            const float r = size * 0.5f - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - size * 0.5f + 0.5f;
                    float dy = y - size * 0.5f + 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    var c = Color.white;
                    c.a = Mathf.Clamp01(r - d);
                    px[y * size + x] = c;
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false);
            return tex;
        }

        /// <summary>
        /// A rubber-stamp ring: a heavy rectangular band eaten away by noise, so the ink looks pressed
        /// rather than filled. White, tinted at draw time — it is the only thing that gets the amber.
        /// </summary>
        private static Texture2D BuildStampRing()
        {
            const int w = 128, h = 64;
            var tex = New(w, h, "ComicStamp", alpha: true);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[w * h];
            var rng = new Rng(0x5747A3UL);

            const int band = 5; // the frame's thickness in texels
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    bool onFrame = edge < band;

                    // Distress: pinholes in the band and a few stray specks inside it.
                    float n = rng.NextFloat();
                    float a = onFrame ? (n > 0.16f ? 1f : 0.15f) : (n > 0.992f ? 0.35f : 0f);

                    var c = Color.white;
                    c.a = a;
                    px[y * w + x] = c;
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false);
            return tex;
        }

        // ------------------------------------------------------------------ styles

        private static GUIStyle? _titleStyle, _bodyStyle, _captionStyle, _stampStyle, _pencilStyle, _bigStyle, _tinyStyle;

        private static GUIStyle Style(ref GUIStyle? slot, int size, FontStyle font, TextAnchor anchor, Color colour)
        {
            if (slot != null) return slot;
            var made = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = font,
                alignment = anchor,
                wordWrap = false,
                richText = false,
            };
            made.normal.textColor = colour;
            slot = made;
            return made;
        }

        private static GUIStyle TitleStyle => Style(ref _titleStyle, 19, FontStyle.Bold, TextAnchor.MiddleLeft, Ink);
        private static GUIStyle BodyStyle => Style(ref _bodyStyle, 16, FontStyle.Normal, TextAnchor.MiddleLeft, Ink);
        private static GUIStyle TinyStyle => Style(ref _tinyStyle, 13, FontStyle.Normal, TextAnchor.MiddleLeft, Ink);
        private static GUIStyle CaptionStyle => Style(ref _captionStyle, 18, FontStyle.Bold, TextAnchor.MiddleLeft, Ink);
        private static GUIStyle StampStyle => Style(ref _stampStyle, 20, FontStyle.Bold, TextAnchor.MiddleCenter, Amber);
        private static GUIStyle PencilStyle => Style(ref _pencilStyle, 16, FontStyle.Normal, TextAnchor.MiddleLeft, PencilGrey);
        private static GUIStyle BigStyle => Style(ref _bigStyle, 54, FontStyle.Bold, TextAnchor.MiddleCenter, Ink);

        // ------------------------------------------------------------------ raw primitives

        public static Rect ToRect(ComicRect r) => new Rect(r.X, r.Y, r.W, r.H);

        /// <summary>Flat colour fill. The base of everything else here.</summary>
        public static void Fill(ComicRect r, Color colour)
        {
            if (r.IsEmpty) return;
            var prev = GUI.color;
            GUI.color = colour;
            GUI.DrawTexture(ToRect(r), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        /// <summary>A solid block of ink, with the ink texture's tooth tiled across it.</summary>
        public static void InkBlock(ComicRect r) => Tile(r, InkTex, Color.white, 48f);

        /// <summary>Paper fill, fibre and all.</summary>
        public static void PaperFill(ComicRect r) => Tile(r, PaperTex, Color.white, 128f);

        private static void Tile(ComicRect r, Texture2D tex, Color tint, float tileSize)
        {
            if (r.IsEmpty) return;
            var prev = GUI.color;
            GUI.color = tint;
            // Tiling by texCoords, not by stretching: the grain must be the same size in every panel
            // regardless of how big the panel is, or the big panels look blurry and the small ones busy.
            GUI.DrawTextureWithTexCoords(ToRect(r), tex, new Rect(r.X / tileSize, r.Y / tileSize, r.W / tileSize, r.H / tileSize));
            GUI.color = prev;
        }

        /// <summary>Four ink rules around a rect, drawn inward so the border never grows the rect.</summary>
        public static void Border(ComicRect r, float thickness)
        {
            if (r.IsEmpty) return;
            InkBlock(new ComicRect(r.X, r.Y, r.W, thickness));
            InkBlock(new ComicRect(r.X, r.Bottom - thickness, r.W, thickness));
            InkBlock(new ComicRect(r.X, r.Y, thickness, r.H));
            InkBlock(new ComicRect(r.Right - thickness, r.Y, thickness, r.H));
        }

        public static void Disc(ComicRect r, Color colour)
        {
            if (r.IsEmpty) return;
            var prev = GUI.color;
            GUI.color = colour;
            GUI.DrawTexture(ToRect(r), DiscTex);
            GUI.color = prev;
        }

        /// <summary>An outlined circle: a disc with a paper disc punched out of it.</summary>
        public static void Ring(ComicRect r, float thickness, Color colour)
        {
            Disc(r, colour);
            Disc(r.Inset(thickness), Paper);
        }

        public static void Label(ComicRect r, string text, GUIStyle style)
        {
            if (string.IsNullOrEmpty(text)) return;
            GUI.Label(ToRect(r), text, style);
        }

        /// <summary>Body copy in ink. Exposed so a page can write a line without reaching for a style.</summary>
        public static void Body(ComicRect r, string text, bool centred = false)
        {
            var s = BodyStyle;
            var was = s.alignment;
            s.alignment = centred ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            Label(r, text, s);
            s.alignment = was;
        }

        /// <summary>Small print: quantities, costs, the second line on a card.</summary>
        public static void Small(ComicRect r, string text, bool centred = false)
        {
            var s = TinyStyle;
            var was = s.alignment;
            s.alignment = centred ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            Label(r, text, s);
            s.alignment = was;
        }

        /// <summary>The one big number on a page, in ink or in amber. Used for the kit page's ±.</summary>
        public static void BigNumber(ComicRect r, string text, Color colour)
        {
            var s = BigStyle;
            var was = s.normal.textColor;
            s.normal.textColor = colour;
            Label(r, text, s);
            s.normal.textColor = was;
        }

        // ------------------------------------------------------------------ the kit

        /// <summary>
        /// A page panel: shadow block, paper, hard border, optional title bar.
        ///
        /// The shadow is a solid offset block rather than a soft gradient because a comic page is
        /// printed — a blur here is the single fastest way to make this look like a 2009 web UI.
        /// </summary>
        public static void Panel(ComicRect r, string? title = null)
        {
            if (r.IsEmpty) return;

            InkBlock(r.Offset(ShadowOffset, ShadowOffset));
            PaperFill(r);
            Border(r, BorderWidth);

            if (string.IsNullOrEmpty(title)) return;

            // Title sits in its own band with a rule under it: a panel's caption, not a window's
            // titlebar. Nothing is rounded and nothing is tinted.
            var band = new ComicRect(r.X + BorderWidth, r.Y + BorderWidth, r.W - BorderWidth * 2f, 28f);
            Label(band.Inset(9f, 0f, 6f, 0f), title!.ToUpperInvariant(), TitleStyle);
            InkBlock(new ComicRect(band.X, band.Bottom, band.W, BorderWidth));
        }

        /// <summary>
        /// A halftone screen over a rect. Density picks a dot SIZE, which is what a printer varies;
        /// fading opacity instead would just make grey, and grey is what we are avoiding.
        /// Use it for gutters, the shadowed side of the figure, and the empty half of a gauge.
        /// </summary>
        public static void Halftone(ComicRect r, float density) => Halftone(r, density, Ink);

        /// <summary>Halftone in a chosen colour — paper dots over a black gutter, for instance.</summary>
        public static void Halftone(ComicRect r, float density, Color colour)
        {
            if (r.IsEmpty || density <= 0.001f) return;
            int level = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(density) * (HalftoneDensities.Length - 1)), 0, HalftoneDensities.Length - 1);
            Tile(r, HalftoneTex(level), colour, 32f);
        }

        /// <summary>
        /// An amber rubber stamp, slightly off-square. The tilt is the whole trick: a stamp that lines
        /// up with the panel it is on reads as a button, and a button is a list item again.
        /// </summary>
        public static void Stamp(ComicRect r, string text)
        {
            if (r.IsEmpty) return;

            var saved = GUI.matrix;
            // Deterministic per-position tilt rather than random, so a redraw does not jitter and a
            // screenshot is reproducible.
            float tilt = -5.5f + (Mathf.Abs(r.X * 0.37f + r.Y * 0.11f) % 3f);
            GUIUtility.RotateAroundPivot(tilt, new Vector2(r.CenterX, r.CenterY));

            var prev = GUI.color;
            GUI.color = Amber;
            GUI.DrawTexture(ToRect(r), StampTex);
            GUI.color = prev;

            Label(r, text.ToUpperInvariant(), StampStyle);

            GUI.matrix = saved;
        }

        /// <summary>
        /// Locked / unaffordable / not-yet. Hatched, hairline-bordered, grey text. It is on the page
        /// and it is plainly not yours, and the player learns that without a legend.
        /// </summary>
        public static void Pencil(ComicRect r, string text)
        {
            if (r.IsEmpty) return;
            Tile(r, PencilTex, PencilGrey, 16f);
            var c = PencilGrey;
            c.a = 0.55f;
            Fill(new ComicRect(r.X, r.Y, r.W, 1f), c);
            Fill(new ComicRect(r.X, r.Bottom - 1f, r.W, 1f), c);
            Fill(new ComicRect(r.X, r.Y, 1f, r.H), c);
            Fill(new ComicRect(r.Right - 1f, r.Y, 1f, r.H), c);
            Label(r.Inset(9f, 0f, 6f, 0f), text, PencilStyle);
        }

        /// <summary>
        /// An ink gauge. Heavy outline, amber fill, ticks every tenth with hand-varied lengths, and
        /// halftone over the part you have not used. Reads at a glance across a room, which is the
        /// bar the owner's "4% full" complaint set.
        /// </summary>
        public static void Gauge(ComicRect r, float fraction, string label)
        {
            if (r.IsEmpty) return;
            fraction = Mathf.Clamp01(fraction);

            var labelRect = new ComicRect(r.X, r.Y, r.W, 20f);
            Label(labelRect, label.ToUpperInvariant(), TitleStyle);

            var bar = new ComicRect(r.X, r.Y + 22f, r.W, Mathf.Max(14f, r.H - 24f));
            PaperFill(bar);
            Halftone(bar, 0.2f);

            var filled = new ComicRect(bar.X, bar.Y, bar.W * fraction, bar.H);
            Fill(filled.Inset(BorderWidth), Amber);

            // Ticks. The 1.6px jitter is deliberate: a perfectly even ruler reads as a progress bar,
            // and this is supposed to look like it was drawn on the side of the truck.
            for (int i = 1; i < 10; i++)
            {
                float x = bar.X + bar.W * (i / 10f);
                float len = (i % 5 == 0 ? 0.62f : 0.34f) * bar.H;
                float jitter = ((i * 37) % 5) * 0.4f;
                InkBlock(new ComicRect(x, bar.Bottom - len - jitter, 2f, len));
            }

            Border(bar, BorderWidth);
        }

        /// <summary>
        /// The line from a slot box to the place on the body it refers to, with a pip at the landing
        /// point. Rotating a thin rect is the only way to get an arbitrary line out of OnGUI.
        /// </summary>
        public static void LeaderLine(ComicPoint from, ComicPoint to)
        {
            float dx = to.X - from.X;
            float dy = to.Y - from.Y;
            float length = Mathf.Sqrt(dx * dx + dy * dy);
            if (length < 0.5f) return;

            var saved = GUI.matrix;
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle, new Vector2(from.X, from.Y));
            InkBlock(new ComicRect(from.X, from.Y - 1f, length, 2f));
            GUI.matrix = saved;

            Disc(new ComicRect(to.X - 4f, to.Y - 4f, 8f, 8f), Ink);
        }

        /// <summary>
        /// A comic caption box: paper, hard border, and a solid ink tab down the left edge. The tab is
        /// what says "narration" rather than "field label".
        /// </summary>
        public static void Caption(ComicRect r, string text)
        {
            if (r.IsEmpty) return;
            InkBlock(r.Offset(4f, 4f));
            PaperFill(r);
            Border(r, BorderWidth);
            InkBlock(new ComicRect(r.X, r.Y, 10f, r.H));
            Label(r.Inset(20f, 0f, 8f, 0f), text, CaptionStyle);
        }

        // ------------------------------------------------------------------ the figure

        /// <summary>
        /// The paper doll: head, neck, torso, two arms, two legs, hands and feet, and a halftone
        /// screen down one side for the light. Rects, discs and rotated rules only — which is the
        /// whole reason the kit page can exist with no art files.
        ///
        /// Anchors in KitPageLayout.Anchor are expressed as fractions of this same rect, so the two
        /// must be changed together; the leader lines land on the body because of that agreement.
        /// </summary>
        public static void Figure(ComicRect r)
        {
            if (r.IsEmpty) return;

            float w = r.W, h = r.H;
            float cx = r.X + w * 0.5f;

            float headD = Mathf.Min(w * 0.30f, h * 0.16f);
            var head = new ComicRect(cx - headD * 0.5f, r.Y + h * 0.02f, headD, headD);

            float torsoW = w * 0.42f;
            var torso = new ComicRect(cx - torsoW * 0.5f, head.Bottom + h * 0.05f, torsoW, h * 0.34f);

            // Light comes from the left, as it does in ApplyOvercastWinter; the screen goes on the
            // right so the UI and the world agree about where the sun is.
            // The head is an outline only. A halftone square laid over a circle spills past it, and a
            // clipped screen is not worth a stencil pass in OnGUI for one 30px shape.
            Disc(head, Ink);
            Disc(head.Inset(3f), Paper);

            InkBlock(new ComicRect(cx - 3f, head.Bottom - 2f, 6f, h * 0.05f + 4f)); // neck

            PaperFill(torso);
            Border(torso, 3f);
            Halftone(new ComicRect(torso.CenterX + torso.W * 0.12f, torso.Y + 3f, torso.W * 0.38f - 3f, torso.H - 6f), 0.35f);

            // Arms and legs as rotated rules with a disc at each end — a jointed stick figure under a
            // solid torso reads as a body, and is four draw calls instead of a mesh.
            float armLen = h * 0.26f;
            Limb(new ComicPoint(torso.X + 4f, torso.Y + 8f), 118f, armLen, 5f);
            Limb(new ComicPoint(torso.Right - 4f, torso.Y + 8f), 62f, armLen, 5f);

            float legLen = h * 0.30f;
            Limb(new ComicPoint(cx - torsoW * 0.22f, torso.Bottom - 2f), 100f, legLen, 6f);
            Limb(new ComicPoint(cx + torsoW * 0.22f, torso.Bottom - 2f), 80f, legLen, 6f);

            // Hands and feet, at the anchor points the slot boxes aim for.
            Disc(new ComicRect(r.X + w * 0.20f - 7f, r.Y + h * 0.50f - 7f, 14f, 14f), Ink);
            Disc(new ComicRect(r.X + w * 0.80f - 7f, r.Y + h * 0.50f - 7f, 14f, 14f), Ink);
            InkBlock(new ComicRect(r.X + w * 0.40f - 13f, r.Y + h * 0.93f - 5f, 26f, 9f));
            InkBlock(new ComicRect(r.X + w * 0.60f - 13f, r.Y + h * 0.93f - 5f, 26f, 9f));
        }

        private static void Limb(ComicPoint from, float angleDeg, float length, float thickness)
        {
            var saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(angleDeg, new Vector2(from.X, from.Y));
            InkBlock(new ComicRect(from.X, from.Y - thickness * 0.5f, length, thickness));
            GUI.matrix = saved;
        }

        // ------------------------------------------------------------------ selection

        /// <summary>
        /// What the cursor is on. A thick amber bracket at the corners rather than a filled highlight:
        /// a fill would hide the picture, and the picture is the point of these screens.
        /// </summary>
        public static void Cursor(ComicRect r)
        {
            if (r.IsEmpty) return;
            var box = r.Inset(-5f);
            float arm = Mathf.Min(20f, Mathf.Min(box.W, box.H) * 0.4f);
            const float t = 4f;

            Fill(new ComicRect(box.X, box.Y, arm, t), Amber);
            Fill(new ComicRect(box.X, box.Y, t, arm), Amber);
            Fill(new ComicRect(box.Right - arm, box.Y, arm, t), Amber);
            Fill(new ComicRect(box.Right - t, box.Y, t, arm), Amber);
            Fill(new ComicRect(box.X, box.Bottom - t, arm, t), Amber);
            Fill(new ComicRect(box.X, box.Bottom - arm, t, arm), Amber);
            Fill(new ComicRect(box.Right - arm, box.Bottom - t, arm, t), Amber);
            Fill(new ComicRect(box.Right - t, box.Bottom - arm, t, arm), Amber);
        }

        /// <summary>Deterministic generator, so every generated texture is identical on every run.</summary>
        private struct Rng
        {
            private ulong _s;
            public Rng(ulong seed) { _s = seed == 0 ? 0x9E3779B97F4A7C15UL : seed; }

            private ulong Next()
            {
                _s ^= _s << 13; _s ^= _s >> 7; _s ^= _s << 17;
                return _s;
            }

            public float NextFloat() => (Next() >> 40) * (1f / 16777216f);
        }
    }
}
