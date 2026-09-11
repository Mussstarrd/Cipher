#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Sim.Core;

namespace Cipher.Game.Match
{
    /// <summary>
    /// Between waves, two people get out of the truck and fix things. ADR-009, the owner's own
    /// image: "maybe in between rounds she comes out with my 9-year-old to do repairs."
    ///
    /// The setup window is currently the only part of the loop where nothing is at stake. This is
    /// what puts something there, and it does it without adding a fail state: they are never
    /// killed. When enemies get close they abandon the job and scramble back to the truck, and the
    /// repair simply does not happen. **The stake is the repair, not the family.**
    ///
    /// That is a deliberate line and ADR-009 draws it: the two-year-old never leaves the truck, and
    /// neither child is ever a timer, an escort or a fetch objective. What the player is buying
    /// with a clean perimeter is the right to have their emplacements fixed, which is a real
    /// decision with a real cost and does not require anyone's daughter to be killable.
    ///
    /// Pure C#, no UnityEngine, so it is testable without a scene.
    /// </summary>
    public sealed class RepairCrew
    {
        /// <summary>One thing that could be repaired, as the crew sees it.</summary>
        public readonly struct Job
        {
            /// <summary>Caller's own index, handed back so it knows what was worked on.</summary>
            public readonly int Index;
            public readonly Vec2 Position;
            /// <summary>0 = wrecked, 1 = pristine. The crew walks to the worst one it can reach.</summary>
            public readonly float Condition01;

            public Job(int index, Vec2 position, float condition01)
            {
                Index = index;
                Position = position;
                Condition01 = condition01;
            }
        }

        public enum Stance
        {
            /// <summary>In the truck, where they belong.</summary>
            Inside,
            Walking,
            Working,
            /// <summary>Job abandoned or window closed; heading back.</summary>
            Returning,
        }

        /// <summary>Metres per second. Slower than the hero: one of them is nine.</summary>
        public float WalkSpeed { get; set; } = 2.4f;

        /// <summary>Repair delivered per second while working, in the caller's own units.</summary>
        public float RepairPerSecond { get; set; } = 7f;

        /// <summary>Close enough to work on it.</summary>
        public float WorkRange { get; set; } = 1.1f;

        /// <summary>
        /// Enemies inside this many cells of the job send them back. Generous on purpose: the point
        /// is that they leave EARLY, the way people do, not that the player gets to cut it fine.
        /// </summary>
        public float DangerRadius { get; set; } = 11f;

        /// <summary>Nothing below this is worth coming out for.</summary>
        public float WorthLeavingFor { get; set; } = 0.92f;

        /// <summary>How far they will walk from the truck. They do not cross the map.</summary>
        public float Leash { get; set; } = 26f;

        public Vec2 TruckPosition { get; set; }
        public Stance State { get; private set; } = Stance.Inside;
        public Vec2 AdultPosition { get; private set; }
        public int JobIndex { get; private set; } = -1;
        public bool Outside => State != Stance.Inside;

        /// <summary>
        /// The nine-year-old trails a step behind and to one side, carrying whatever is being
        /// carried. Derived rather than simulated: two bodies pathing independently would be two
        /// chances to get stuck on a wall for no gain the player can see.
        /// </summary>
        public Vec2 ChildPosition
        {
            get
            {
                Vec2 back = _facing;
                if (back.X * back.X + back.Y * back.Y < 1e-6f) back = new Vec2(0f, 1f);
                back = back.Normalized();
                return new Vec2(AdultPosition.X - back.X * 0.75f + back.Y * 0.45f,
                                AdultPosition.Y - back.Y * 0.75f - back.X * 0.45f);
            }
        }

        /// <summary>True on the tick they gave up and ran, so the game can say something about it.</summary>
        public bool AbandonedThisTick { get; private set; }

        /// <summary>Total repair delivered this position, for the after-action line.</summary>
        public float RepairDelivered { get; private set; }

        private Vec2 _facing = new Vec2(0f, 1f);
        private Vec2 _jobPosition;

        public void Reset(Vec2 truck)
        {
            TruckPosition = truck;
            AdultPosition = truck;
            State = Stance.Inside;
            JobIndex = -1;
            RepairDelivered = 0f;
            AbandonedThisTick = false;
        }

        /// <summary>
        /// Advances the crew. <paramref name="windowOpen"/> is the caller's judgement about whether
        /// it is safe to be out at all -- in practice "we are between waves and the mission is still
        /// running". <paramref name="enemiesWithin"/> is the same pressure query the actor system
        /// uses, so the crew and the objectives agree about what "close" means.
        ///
        /// Returns the repair delivered this tick; <see cref="JobIndex"/> says to what.
        /// </summary>
        public float Tick(float dt, bool windowOpen, IReadOnlyList<Job> jobs,
                          Func<Vec2, float, int> enemiesWithin)
        {
            AbandonedThisTick = false;
            if (dt <= 0f) return 0f;
            if (enemiesWithin == null) throw new ArgumentNullException(nameof(enemiesWithin));

            // Anything that makes it unsafe or pointless sends them home, from any state.
            if (!windowOpen)
            {
                GoHome(dt);
                return 0f;
            }

            if (State == Stance.Inside)
            {
                int pick = ChooseJob(jobs, enemiesWithin);
                if (pick < 0) return 0f;
                JobIndex = jobs[pick].Index;
                _jobPosition = jobs[pick].Position;
                State = Stance.Walking;
            }

            if (State == Stance.Walking || State == Stance.Working)
            {
                // They watch the job, not their own feet: something walking toward the turret they
                // are about to kneel behind is the thing that turns them round.
                if (enemiesWithin(_jobPosition, DangerRadius) > 0
                    || enemiesWithin(AdultPosition, DangerRadius) > 0)
                {
                    AbandonedThisTick = State == Stance.Working || State == Stance.Walking;
                    JobIndex = -1;
                    State = Stance.Returning;
                }
            }

            switch (State)
            {
                case Stance.Walking:
                {
                    if (StepToward(_jobPosition, dt) <= WorkRange) State = Stance.Working;
                    return 0f;
                }

                case Stance.Working:
                {
                    // Finished, or someone else fixed it: go and find the next worst thing.
                    if (!StillWorthDoing(jobs))
                    {
                        JobIndex = -1;
                        State = Stance.Inside;   // re-picks next tick, or stays in if nothing is left
                        return 0f;
                    }
                    float done = RepairPerSecond * dt;
                    RepairDelivered += done;
                    return done;
                }

                case Stance.Returning:
                {
                    GoHome(dt);
                    return 0f;
                }
            }

            return 0f;
        }

        private void GoHome(float dt)
        {
            JobIndex = -1;
            if (State == Stance.Inside) return;
            State = Stance.Returning;
            if (StepToward(TruckPosition, dt) <= 0.6f)
            {
                State = Stance.Inside;
                AdultPosition = TruckPosition;
            }
        }

        /// <summary>Moves toward a point and returns the distance remaining after the step.</summary>
        private float StepToward(Vec2 target, float dt)
        {
            Vec2 d = new Vec2(target.X - AdultPosition.X, target.Y - AdultPosition.Y);
            float dist = MathF.Sqrt(d.X * d.X + d.Y * d.Y);
            if (dist <= 1e-4f) return 0f;

            _facing = new Vec2(d.X / dist, d.Y / dist);
            float step = MathF.Min(WalkSpeed * dt, dist);
            AdultPosition = new Vec2(AdultPosition.X + _facing.X * step,
                                     AdultPosition.Y + _facing.Y * step);
            return dist - step;
        }

        /// <summary>The worst thing within the leash that nothing is standing near.</summary>
        private int ChooseJob(IReadOnlyList<Job> jobs, Func<Vec2, float, int> enemiesWithin)
        {
            if (jobs == null || jobs.Count == 0) return -1;

            int best = -1;
            float worst = WorthLeavingFor;
            for (int i = 0; i < jobs.Count; i++)
            {
                var j = jobs[i];
                if (j.Condition01 >= worst) continue;
                if (Vec2.DistanceSquared(j.Position, TruckPosition) > Leash * Leash) continue;
                if (enemiesWithin(j.Position, DangerRadius) > 0) continue;
                worst = j.Condition01;
                best = i;
            }
            return best;
        }

        private bool StillWorthDoing(IReadOnlyList<Job> jobs)
        {
            if (jobs == null) return false;
            for (int i = 0; i < jobs.Count; i++)
                if (jobs[i].Index == JobIndex) return jobs[i].Condition01 < 1f;
            return false;   // it is gone: destroyed, sold, or packed onto the truck
        }
    }
}
