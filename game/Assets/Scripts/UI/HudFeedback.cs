#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game.UI
{
    /// <summary>
    /// The parts of the HUD that answer "what is happening to me": a wave card, a health vignette,
    /// a damage-direction wedge, and world-space markers with a distance. Phase B of the roadmap.
    ///
    /// Owner, three playtests running: "I don't understand", "I can't tell", "there's no way to
    /// know". He has never once said "that killed me" because the game never told him what did.
    /// Everything in here is a way of telling him without a paragraph.
    ///
    /// Immediate-mode, drawn inside the bootstrap's DPI-scaled OnGUI matrix. Textures are generated,
    /// never loaded. The styling is plain for now; the comic-page toolkit restyles these when it
    /// lands, so the DRAWING is isolated here and the DATA stays in the models that own it.
    /// </summary>
    public sealed class HudFeedback
    {
        // ---------------------------------------------------------------- wave cards

        private string _cardTitle = "";
        private string _cardBody = "";
        private float _cardLeft;
        private float _cardTotal = 1f;

        /// <summary>
        /// A big centred card: WAVE 3, WAVE CLEARED, LAST WAVE CALLED, PACK UP. Two lines at most.
        /// Only one at a time; a new one replaces the old, because two cards fighting reads as
        /// noise and the newest fact is the one that matters.
        /// </summary>
        public void ShowCard(string title, string body, float seconds = 2.8f)
        {
            _cardTitle = title;
            _cardBody = body;
            _cardLeft = seconds;
            _cardTotal = Mathf.Max(0.1f, seconds);
        }

        /// <summary>Whether a card is on screen. Exposed so the timing is testable without a scene.</summary>
        public bool CardVisible => _cardLeft > 0f;
        public string CardTitle => _cardTitle;

        // ---------------------------------------------------------------- damage direction

        private struct Wedge { public float Angle; public float Left; }
        private readonly List<Wedge> _wedges = new List<Wedge>(6);
        private float _hurtPulse;

        /// <summary>How many distinct directions are currently being shouted about.</summary>
        public int WedgeCount => _wedges.Count;
        /// <summary>The screen-space bearing of wedge <paramref name="i"/>, 0 = straight ahead, clockwise.</summary>
        public float WedgeAngle(int i) => _wedges[i].Angle;

        /// <summary>
        /// Something hurt the player from <paramref name="fromWorld"/>. A wedge appears on the screen
        /// edge on that side and fades. The camera's yaw is what turns a world direction into a
        /// screen side, so it is passed in rather than looked up.
        /// </summary>
        public void NoteDamage(Vector3 heroWorld, Vector3 fromWorld, float cameraYawDegrees)
        {
            var d = fromWorld - heroWorld;
            d.y = 0f;
            if (d.sqrMagnitude < 1e-4f) { _hurtPulse = 1f; return; }

            // Angle on screen: 0 is up (ahead of the camera), clockwise.
            float worldAngle = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float screenAngle = Mathf.DeltaAngle(cameraYawDegrees, worldAngle);

            // Merge with a wedge already pointing the same way rather than stacking a new one.
            for (int i = 0; i < _wedges.Count; i++)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(_wedges[i].Angle, screenAngle)) < 25f)
                {
                    _wedges[i] = new Wedge { Angle = screenAngle, Left = 1f };
                    _hurtPulse = 1f;
                    return;
                }
            }
            if (_wedges.Count >= 6) _wedges.RemoveAt(0);
            _wedges.Add(new Wedge { Angle = screenAngle, Left = 1f });
            _hurtPulse = 1f;
        }

        // ---------------------------------------------------------------- tick

        public void Tick(float dt)
        {
            if (_cardLeft > 0f) _cardLeft -= dt;
            if (_hurtPulse > 0f) _hurtPulse = Mathf.Max(0f, _hurtPulse - dt * 2.2f);
            for (int i = _wedges.Count - 1; i >= 0; i--)
            {
                var w = _wedges[i];
                w.Left -= dt * 1.4f;
                if (w.Left <= 0f) _wedges.RemoveAt(i); else _wedges[i] = w;
            }
        }

        // ---------------------------------------------------------------- draw

        private Texture2D? _vignette;
        private Texture2D? _wedgeTex;
        private GUIStyle? _cardTitleStyle;
        private GUIStyle? _cardBodyStyle;
        private GUIStyle? _markerStyle;

        /// <summary>Everything, in back-to-front order. Call inside the scaled GUI matrix.</summary>
        public void Draw(float uiW, float uiH, float healthFraction)
        {
            DrawVignette(uiW, uiH, healthFraction);
            DrawWedges(uiW, uiH);
            DrawCard(uiW, uiH);
        }

        /// <summary>
        /// Red at the edges when hurt. Strength is (1 - health) squared plus a pulse on each hit, so
        /// a scratch barely shows and near-death is unmistakable without covering the field.
        /// </summary>
        private void DrawVignette(float uiW, float uiH, float healthFraction)
        {
            float low = 1f - Mathf.Clamp01(healthFraction);
            float strength = low * low * 0.85f + _hurtPulse * 0.45f;
            if (strength <= 0.01f) return;

            _vignette ??= BuildVignette(256);
            var prev = GUI.color;
            GUI.color = new Color(0.65f, 0.05f, 0.03f, Mathf.Clamp01(strength));
            GUI.DrawTexture(new Rect(0f, 0f, uiW, uiH), _vignette);
            GUI.color = prev;
        }

        /// <summary>A hard ink wedge on the edge of the screen, on the side the hit came from.</summary>
        private void DrawWedges(float uiW, float uiH)
        {
            if (_wedges.Count == 0) return;
            _wedgeTex ??= BuildWedge(64);

            float cx = uiW * 0.5f, cy = uiH * 0.5f;
            float radius = Mathf.Min(uiW, uiH) * 0.42f;
            var prev = GUI.color;
            var prevMatrix = GUI.matrix;

            for (int i = 0; i < _wedges.Count; i++)
            {
                var w = _wedges[i];
                float a = w.Angle * Mathf.Deg2Rad;
                float x = cx + Mathf.Sin(a) * radius;
                float y = cy - Mathf.Cos(a) * radius;
                float size = 46f + 24f * w.Left;

                GUI.color = new Color(0.85f, 0.10f, 0.08f, Mathf.Clamp01(w.Left * 1.3f));
                GUIUtility.RotateAroundPivot(w.Angle, new Vector2(x, y));
                GUI.DrawTexture(new Rect(x - size * 0.5f, y - size * 0.5f, size, size), _wedgeTex);
                GUI.matrix = prevMatrix;
            }
            GUI.color = prev;
        }

        private void DrawCard(float uiW, float uiH)
        {
            if (_cardLeft <= 0f) return;

            _cardTitleStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 42, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
            };
            _cardBodyStyle ??= new GUIStyle(GUI.skin.label)
            {
                // WORD WRAP, or a long body runs off both edges of the card and off the screen.
                // The mission brief is two sentences; every card before it was four words, which is
                // why nobody noticed the card could not hold a sentence.
                fontSize = 18, alignment = TextAnchor.UpperCenter, wordWrap = true,
            };

            // In fast, out slow: the eye catches the arrival and the fade is not a second event.
            float t = _cardLeft / _cardTotal;
            float alpha = t > 0.85f ? (1f - t) / 0.15f : Mathf.Clamp01(t / 0.35f);
            float rise = (1f - alpha) * 14f;

            // The card is sized to what it holds. A fixed 96px box was fine for "WAVE 3" and threw
            // the mission brief across the whole screen.
            const float Width = 620f;
            float bodyHeight = _cardBody.Length == 0
                ? 0f
                : _cardBodyStyle.CalcHeight(new GUIContent(_cardBody), Width - 48f);
            float height = 58f + bodyHeight + (bodyHeight > 0f ? 14f : 0f);

            float x = uiW * 0.5f - Width * 0.5f;
            float y = uiH * 0.20f + rise;

            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.62f * alpha);
            GUI.DrawTexture(new Rect(x, y, Width, height), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            _cardTitleStyle.normal.textColor = new Color(1f, 0.93f, 0.78f, alpha);
            _cardBodyStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f, alpha);
            GUI.Label(new Rect(x, y + 6f, Width, 50f), _cardTitle, _cardTitleStyle);
            if (bodyHeight > 0f)
                GUI.Label(new Rect(x + 24f, y + 56f, Width - 48f, bodyHeight), _cardBody, _cardBodyStyle);
            GUI.color = prev;
        }

        /// <summary>
        /// A label on a thing in the world; when the thing is off screen, an arrow at the screen
        /// edge with the distance. This is how the player finds the vault, the truck and the
        /// generator without a paragraph: the screen points at them.
        /// </summary>
        public void DrawMarker(Camera camera, float uiScale, float uiW, float uiH,
                               Vector3 world, string label, Color colour, float metres)
        {
            _markerStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
            };
            _markerStyle.normal.textColor = colour;

            var screen = camera.WorldToScreenPoint(world);
            float x = screen.x / uiScale;
            float y = (Screen.height - screen.y) / uiScale;
            bool behind = screen.z <= 0f;
            bool onScreen = !behind && x >= 40f && x <= uiW - 40f && y >= 40f && y <= uiH - 40f;

            if (onScreen)
            {
                GUI.Label(new Rect(x - 120f, y - 30f, 240f, 22f), label, _markerStyle);
                return;
            }

            // Off screen: project the direction from the centre to the edge and draw an arrow.
            if (behind) { x = uiW - x; y = uiH - y; }
            float cx = uiW * 0.5f, cy = uiH * 0.5f;
            var dir = new Vector2(x - cx, y - cy);
            if (dir.sqrMagnitude < 1e-3f) dir = Vector2.up;
            dir.Normalize();

            // Intersect with the padded screen rectangle.
            float pad = 58f;
            float hx = (uiW * 0.5f - pad) / Mathf.Max(1e-3f, Mathf.Abs(dir.x));
            float hy = (uiH * 0.5f - pad) / Mathf.Max(1e-3f, Mathf.Abs(dir.y));
            float k = Mathf.Min(hx, hy);
            float ex = cx + dir.x * k, ey = cy + dir.y * k;

            float angle = Mathf.Atan2(dir.x, -dir.y) * Mathf.Rad2Deg;
            var prevMatrix = GUI.matrix;
            var prev = GUI.color;
            _wedgeTex ??= BuildWedge(64);
            GUI.color = colour;
            GUIUtility.RotateAroundPivot(angle, new Vector2(ex, ey));
            GUI.DrawTexture(new Rect(ex - 16f, ey - 16f, 32f, 32f), _wedgeTex);
            GUI.matrix = prevMatrix;
            GUI.color = prev;

            GUI.Label(new Rect(ex - 120f, ey + 14f, 240f, 22f), $"{label}  {metres:F0}m", _markerStyle);
        }

        /// <summary>
        /// A small bar floating over something in the world. Discreet on purpose -- the owner asked
        /// for health bars "nice and discreetly", and four hundred of them is a spreadsheet, not a
        /// battlefield.
        ///
        /// Three rules keep it quiet:
        ///  - a full bar is not drawn at all, so an untouched crowd shows nothing;
        ///  - it fades out with distance and vanishes past <paramref name="maxMetres"/>;
        ///  - it is one flat ink rectangle with a fill, no frame, no gradient, no number.
        ///
        /// Returns false when nothing was drawn, so callers can skip their own work.
        /// </summary>
        public bool DrawHealthBar(Camera camera, float uiScale, float uiW, float uiH,
                                  Vector3 world, float fraction01, float metres,
                                  float width = 34f, float maxMetres = 45f, Color? tint = null)
        {
            if (camera == null) return false;
            fraction01 = Mathf.Clamp01(fraction01);
            if (fraction01 >= 0.999f) return false;
            if (metres > maxMetres) return false;

            var screen = camera.WorldToScreenPoint(world);
            if (screen.z <= 0f) return false;

            float x = screen.x / uiScale;
            float y = (Screen.height - screen.y) / uiScale;
            if (x < -width || x > uiW + width || y < -12f || y > uiH + 12f) return false;

            // Fades over the last third of its range rather than popping out of existence.
            float near = Mathf.Clamp01(1f - Mathf.InverseLerp(maxMetres * 0.66f, maxMetres, metres));
            if (near <= 0.02f) return false;

            // Smaller with distance too, so a crowd at the gate does not turn into a wall of bars.
            float scale = Mathf.Lerp(0.72f, 1f, near);
            float w = width * scale;
            float h = 3.5f * scale;
            var rect = new Rect(x - w * 0.5f, y, w, h);

            var prev = GUI.color;
            GUI.color = new Color(0.05f, 0.05f, 0.06f, 0.55f * near);
            GUI.DrawTexture(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f),
                            Texture2D.whiteTexture);

            var colour = tint ?? Color.Lerp(new Color(0.90f, 0.32f, 0.22f),
                                            new Color(0.62f, 0.78f, 0.45f), fraction01);
            colour.a = near;
            GUI.color = colour;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * fraction01, rect.height),
                            Texture2D.whiteTexture);
            GUI.color = prev;
            return true;
        }

        // ---------------------------------------------------------------- generated textures

        private static Texture2D BuildVignette(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                name = "HudVignette", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f;
                    float v = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    // Clear through the middle, hard-ish edge: an ink wash, not a photographic glow.
                    float a = Mathf.Clamp01((r - 0.55f) / 0.5f);
                    a = a * a;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        /// <summary>An upward-pointing solid triangle. Rotated at draw time.</summary>
        private static Texture2D BuildWedge(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                name = "HudWedge", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size - 0.5f;          // -0.5..0.5
                    float v = 1f - (y + 0.5f) / size;             // 1 at top of texture
                    // Inside the triangle: apex at v=1, base at v=0.15, half-width grows with 1-v.
                    float halfWidth = (1f - v) * 0.5f;
                    bool inside = v > 0.15f && v <= 1f && Mathf.Abs(u) <= halfWidth * 0.85f;
                    px[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }
    }
}
