# Wiring the atmosphere into the bootstrap

`Weather` / `RainFall` / `Ambience` are built and tested but **not wired**: `FloodBootstrap.cs` is
the owner's file and this branch does not touch it. Three edits connect the lot. They were applied
locally, built, and used to photograph all five conditions, then reverted — so they are known to
compile and render, not guessed.

Everything goes through one facade (`Atmosphere`) on purpose. The weather is four systems that have
to agree — lighting, fog and sky, rain, audio beds — and the failure worth designing against is
three of them changing at a position boundary and the fourth not.

## 1. The field

Next to the other audio state, around line 228:

```csharp
        // ---- audio ----
        private SoundBank _sfx = null!;
        private Atmosphere _atmos = null!;          // <-- add
```

## 2. Build: replace the fixed dusk

In `BuildSceneObjects()`, around line 568. `_scenario` is already loaded by this point (`Awake`
loads it before `BuildSceneObjects`, deliberately), so the seed is available:

```csharp
-           ApplyOvercastWinter();
+           _atmos = Atmosphere.Create(transform, _camera, _scenario.DirectorSeed);
            ApplyColourGrade();
```

`ApplyOvercastWinter()` and the `DuskHaze` constant then have no callers and can be deleted. Their
numbers live on unchanged as `Weather.Profile(Sky.DuskClear)`, and `WeatherTests` asserts them
constant-for-constant — if that test ever fails, the game's default look has changed.

`_sun` is still wanted by anything that aims at the light; take it from `_atmos.Sun`.

## 3. Per frame

In the audio block, immediately after `_sfx.Update(dt)` (around line 1257):

```csharp
            _sfx.SetHordeIntensity(_hordeIntensity);
            _sfx.Update(dt);
            _atmos.Update(Time.unscaledDeltaTime, _camera, _hordeIntensity,
                          ToWorld(_hero.Position, 0f));
```

**Unscaled on purpose.** Adrenaline focus slows the world; slowing the rain and the wind with it
turns a tactical pause into an underwater one. Same rule `AdrenalineFocus` already follows.

The hero position is passed rather than a speed: `Atmosphere` derives the speed itself (and
discards the teleport on a position change), so the footstep cadence cannot be wired wrongly.

## 4. Optional — a new position gets new weather

Wherever the scenario changes (`FallBack()`, after `_scenario = LoadScenario(next)`):

```csharp
            _atmos.Reseed(_scenario.DirectorSeed, _camera);
```

Without this the campaign runs under whatever the first position rolled, which is not wrong, just
less interesting. With it, each position has its own remembered sky. Note this is a **crossfade**,
not a cut — the player drove here, it is a continuous moment.

## Also worth knowing

- **Mute/volume** are `_atmos.SetMuted(bool)` and `_atmos.SetVolume(float)`, to be hung off
  whatever the pause menu already does to `SoundBank.Muted` / `MasterVolume`.
- **`-exodus-weather <name>`** forces a condition (`OvercastNoon`, `Sunrise`, `DuskClear`,
  `DuskRain`, `Fog`) and overrides the seed. It is read inside `Weather.Resolve`, so it needs no
  harness change. A nonsense name warns and falls back to the seed rather than failing a capture.
- **The seeded pick preserves the shipped look by default**: scenario seed `20260910` (The Gate)
  rolls `DuskClear`.
