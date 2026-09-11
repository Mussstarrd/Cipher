#nullable enable
using System;

namespace Cipher.Game.UI.Comic
{
    /// <summary>
    /// A rectangle in the DPI-scaled UI space (see FloodBootstrap.BeginScaledUi).
    ///
    /// Why not UnityEngine.Rect: the three page layouts are the load-bearing part of this toolkit
    /// and they have to be unit-testable the way PauseMenuModel is — plain C#, no engine. ComicInk
    /// takes ComicRect on every entry point and converts once at the draw call, so the integrator
    /// never sees the seam.
    ///
    /// Y grows downward, matching OnGUI.
    /// </summary>
    public readonly struct ComicRect : IEquatable<ComicRect>
    {
        public readonly float X;
        public readonly float Y;
        public readonly float W;
        public readonly float H;

        public ComicRect(float x, float y, float w, float h)
        {
            X = x;
            Y = y;
            W = Math.Max(0f, w);
            H = Math.Max(0f, h);
        }

        public static readonly ComicRect Zero = new ComicRect(0f, 0f, 0f, 0f);

        public float Right => X + W;
        public float Bottom => Y + H;
        public float CenterX => X + W * 0.5f;
        public float CenterY => Y + H * 0.5f;
        public ComicPoint Center => new ComicPoint(CenterX, CenterY);
        public bool IsEmpty => W <= 0f || H <= 0f;

        /// <summary>Shrink on all four sides. Negative grows, which is how the drop shadow is built.</summary>
        public ComicRect Inset(float amount) => Inset(amount, amount, amount, amount);

        public ComicRect Inset(float left, float top, float right, float bottom) =>
            new ComicRect(X + left, Y + top, W - left - right, H - top - bottom);

        public ComicRect Offset(float dx, float dy) => new ComicRect(X + dx, Y + dy, W, H);

        /// <summary>
        /// Touching edges do NOT count as overlapping. Panels on a comic page sit gutter-to-gutter,
        /// so the tests that assert "these must not overlap" would be unusable otherwise.
        /// </summary>
        public bool Overlaps(ComicRect other)
        {
            if (IsEmpty || other.IsEmpty) return false;
            return X < other.Right && other.X < Right && Y < other.Bottom && other.Y < Bottom;
        }

        public bool Contains(ComicPoint p) => p.X >= X && p.X <= Right && p.Y >= Y && p.Y <= Bottom;

        /// <summary>True when this sits entirely inside <paramref name="outer"/>, edges allowed to touch.</summary>
        public bool IsInside(ComicRect outer) =>
            X >= outer.X - Epsilon && Y >= outer.Y - Epsilon &&
            Right <= outer.Right + Epsilon && Bottom <= outer.Bottom + Epsilon;

        /// <summary>Float slop for the containment assertions; layout maths accumulates a little.</summary>
        private const float Epsilon = 0.001f;

        /// <summary>Slice a vertical column out of this rect. Used to build the page's unequal columns.</summary>
        public ComicRect Column(float fromFraction, float toFraction) =>
            new ComicRect(X + W * fromFraction, Y, W * (toFraction - fromFraction), H);

        /// <summary>Slice a horizontal band out of this rect.</summary>
        public ComicRect Row(float fromFraction, float toFraction) =>
            new ComicRect(X, Y + H * fromFraction, W, H * (toFraction - fromFraction));

        public bool Equals(ComicRect other) =>
            X.Equals(other.X) && Y.Equals(other.Y) && W.Equals(other.W) && H.Equals(other.H);

        public override bool Equals(object? obj) => obj is ComicRect r && Equals(r);

        public override int GetHashCode() =>
            X.GetHashCode() ^ (Y.GetHashCode() << 2) ^ (W.GetHashCode() >> 2) ^ (H.GetHashCode() >> 1);

        public override string ToString() => $"({X:F1},{Y:F1} {W:F1}x{H:F1})";
    }

    /// <summary>A point in the same space. Leader lines need endpoints that are not rectangles.</summary>
    public readonly struct ComicPoint
    {
        public readonly float X;
        public readonly float Y;

        public ComicPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"({X:F1},{Y:F1})";
    }
}
