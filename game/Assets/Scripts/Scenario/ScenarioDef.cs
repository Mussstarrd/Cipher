#nullable enable
using System.Collections.Generic;
using Cipher.Game.Match;
using Cipher.Sim.Grid;

namespace Cipher.Game.Scenarios
{
    /// <summary>A rectangle of wall, in cells.</summary>
    public readonly struct WallRect
    {
        public readonly int X, Y, Width, Height;
        public readonly WallKind Kind;

        public WallRect(int x, int y, int width, int height, WallKind kind)
        {
            X = x; Y = y; Width = width; Height = height; Kind = kind;
        }
    }

    public readonly struct Cell
    {
        public readonly int X, Y;
        public Cell(int x, int y) { X = x; Y = y; }
    }

    /// <summary>A spawn point, named so a brief and a HUD can refer to it ("west gate").</summary>
    public readonly struct SpawnPoint
    {
        public readonly int X, Y;
        public readonly string Gate;
        public SpawnPoint(int x, int y, string gate) { X = x; Y = y; Gate = gate; }
    }

    public sealed class MapDef
    {
        public int Width { get; set; } = 64;
        public int Height { get; set; } = 48;
        /// <summary>Named starting layout the bootstrap knows how to build before walls are applied.</summary>
        public string Preset { get; set; } = "arena";
        public List<WallRect> Walls { get; } = new List<WallRect>();
    }

    /// <summary>
    /// What a mission is made of, as data.
    ///
    /// Schema and field names follow docs/design/progression-and-campaign.md §6. Everything the
    /// schema defines is parsed even where nothing consumes it yet, because the file on disk is the
    /// contract: dropping a field at read time means a mission author writes something that is
    /// silently ignored, which is worse than a field that is faithfully loaded and waiting.
    /// Where that is the case it is called out on the member.
    /// </summary>
    public sealed class ScenarioDef
    {
        public const int SupportedSchema = 1;

        public int Schema { get; set; } = SupportedSchema;
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public int Tier { get; set; } = 1;
        public string Brief { get; set; } = "";

        public MapDef Map { get; } = new MapDef();
        public Cell HeroSpawn { get; set; }
        public List<SpawnPoint> SpawnCells { get; } = new List<SpawnPoint>();
        public Cell Vault { get; set; }
        public int VaultHp { get; set; } = 25;

        /// <summary>
        /// Parsed, not yet consumed: Structure/Process/Crew actors are the subject of the objectives
        /// that need them (ProtectActors, HoldUntil, KeepCrewAlive), and none of those exist in the
        /// game yet. <see cref="ObjectiveFactory"/> refuses those types by name rather than
        /// pretending, so a mission using one fails to load instead of quietly being unwinnable.
        /// </summary>
        public List<ActorDef> Actors { get; } = new List<ActorDef>();

        public EconomyConfig Economy { get; set; } = new EconomyConfig();
        public DirectorConfig Director { get; set; } = new DirectorConfig();
        /// <summary>Seed for this mission's spawn director. Same seed, same match.</summary>
        public ulong DirectorSeed { get; set; } = 20260910UL;

        public List<WaveSpec> Waves { get; } = new List<WaveSpec>();
        public List<int> SafeZoneAfterWaves { get; } = new List<int>();
        public List<ObjectiveDef> Objectives { get; } = new List<ObjectiveDef>();
        public RewardDef Rewards { get; } = new RewardDef();
        public MedalsDef Medals { get; } = new MedalsDef();

        /// <summary>The wave table, in the shape <see cref="MatchState"/> wants.</summary>
        public IReadOnlyList<WaveDef> ToWaveTable()
        {
            var waves = new List<WaveDef>(Waves.Count);
            foreach (var w in Waves) waves.Add(new WaveDef(w.Count, w.SpawnPerSecond, w.SetupSeconds));
            return waves;
        }
    }

    /// <summary>
    /// One wave. <see cref="Mix"/> is parsed from the file but the spawn director is still global
    /// rather than per-wave, so it does not yet steer archetype selection; it is kept because it is
    /// part of the schema on disk and the director becoming per-wave is a change here, not a change
    /// to every mission file.
    /// </summary>
    public sealed class WaveSpec
    {
        public float SetupSeconds { get; set; } = 20f;
        public int Count { get; set; } = 100;
        public float SpawnPerSecond { get; set; } = 8f;
        public Dictionary<string, float> Mix { get; } = new Dictionary<string, float>();
    }

    public enum ActorKind { Structure, Process, Crew }

    public sealed class ActorDef
    {
        public string Id { get; set; } = "";
        public ActorKind Kind { get; set; } = ActorKind.Structure;
        public int X { get; set; }
        public int Y { get; set; }
        public int Hp { get; set; } = 100;
        public float DurationSeconds { get; set; }
        public float RequiresHeroWithin { get; set; }
    }

    /// <summary>An objective as written in the file: a type plus its parameters, unresolved.</summary>
    public sealed class ObjectiveDef
    {
        public string Type { get; set; } = "";
        public int Count { get; set; }
        public float Seconds { get; set; }
        public int MinHp { get; set; }
        public int MinAlive { get; set; }
        public string ActorId { get; set; } = "";
    }

    public sealed class DropDef
    {
        public string Slot { get; set; } = "";
        public string Rarity { get; set; } = "";
        public int ItemLevel { get; set; } = 1;
    }

    public sealed class RewardDef
    {
        public int Scrip { get; set; }
        public List<DropDef> GuaranteedDrops { get; } = new List<DropDef>();
        public List<string> Unlocks { get; } = new List<string>();
    }

    /// <summary>A medal condition as written in the file. Evaluated at the end of a match.</summary>
    public sealed class Criterion
    {
        public string Type { get; set; } = "";
        public int Value { get; set; }
        public float Seconds { get; set; }
    }

    public sealed class MedalsDef
    {
        public List<Criterion> Bronze { get; } = new List<Criterion>();
        public List<Criterion> Silver { get; } = new List<Criterion>();
        public List<Criterion> Gold { get; } = new List<Criterion>();
    }
}
