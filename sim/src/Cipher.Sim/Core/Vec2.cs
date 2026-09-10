#nullable enable
using System;

namespace Cipher.Sim.Core
{
    /// <summary>
    /// Minimal 2D vector. Lives here (not UnityEngine.Vector2) so the sim core
    /// stays engine-free; the Unity adapter converts at the boundary.
    /// </summary>
    public readonly struct Vec2 : IEquatable<Vec2>
    {
        public readonly float X;
        public readonly float Y;

        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vec2 Zero => new Vec2(0f, 0f);

        public float LengthSquared => X * X + Y * Y;

        public float Length => MathF.Sqrt(LengthSquared);

        /// <summary>Returns a unit vector, or Zero when the vector is degenerate.</summary>
        public Vec2 Normalized()
        {
            float len = Length;
            return len > 1e-8f ? new Vec2(X / len, Y / len) : Zero;
        }

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.X * s, a.Y * s);

        public static float DistanceSquared(Vec2 a, Vec2 b) => (a - b).LengthSquared;
        public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;

        public bool Equals(Vec2 other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object? obj) => obj is Vec2 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X:F3}, {Y:F3})";
    }
}
