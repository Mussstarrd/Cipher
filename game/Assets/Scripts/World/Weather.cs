#nullable enable
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cipher.Game
{
    /// <summary>Which sky a position is fought under.</summary>
    /// <remarks>
    /// Deliberately five, not a continuum. A slider between "noon" and "dusk" produces a hundred
    /// in-between skies nobody authored and none of them read as a time of day; five hand-tuned
    /// conditions each read instantly, which is the whole point. Add a sixth by authoring it, not
    /// by interpolating.
    /// </remarks>
    public enum Sky
    {
        /// <summary>Flat winter overcast, sun high and weak. The "nothing is wrong yet" sky.</summary>
        OvercastNoon,
        /// <summary>Low sun from the east, gold on cold blue, ground mist. Hopeful, and a lie.</summary>
        Sunrise,
        /// <summary>The shipped look: raking warm key, cold fill, long shadows west.</summary>
        DuskClear,
        /// <summary>Dusk with the warmth rained out of it. Grey, short sightlines, thunder.</summary>
        DuskRain,
        /// <summary>Thick fog. No readable sun, no horizon, the treeline gone. The scary one.</summary>
        Fog,
    }

    /// <summary>
    /// Everything a <see cref="Sky"/> is, as plain data: the sun, the ambient trilight, the fog,
    /// the procedural sky's five colours, and the names of the audio beds that belong under it.
    ///
    /// LIGHT, FOG AND SKY MOVE TOGETHER OR NOT AT ALL. This is the rule the original
    /// ApplyOvercastWinter comment was written to protect and it survives here unchanged. Warming
    /// the sun without cooling the ambient gives you a sepia filter; thickening the fog without
    /// recolouring the sky gives you grey soup with a sunset pasted behind it. Every field below
    /// was picked against the other fields, so change them in sets.
    ///
    /// Pure data, no UnityEngine behaviour, so the tests can assert on it without a scene.
    /// </summary>
    public sealed class WeatherProfile
    {
        public Sky Sky;
        public string Name = "";

        // ---- sun -------------------------------------------------------------------
        /// <summary>Euler rotation of the directional light. X is elevation, Y is compass.</summary>
        public Vector3 SunEuler;
        public Color SunColor;
        public float SunIntensity;
        /// <summary>Overcast and fog get weak shadows because a diffuse sky does not cast hard ones.</summary>
        public float ShadowStrength;

        // ---- ambient trilight ------------------------------------------------------
        public Color AmbientSky;
        public Color AmbientEquator;
        public Color AmbientGround;

        // ---- fog -------------------------------------------------------------------
        /// <summary>The colour distance dissolves into. Must match the sky's horizon band.</summary>
        public Color FogColor;
        /// <summary>ExponentialSquared density. This is the single biggest lever on sightline.</summary>
        public float FogDensity;

        // ---- procedural sky (Exodus/ComicSky) --------------------------------------
        public Color SkyTop;
        public Color SkyHorizon;
        public Color CloudLight;
        public Color CloudDark;
        public float CloudCover;
        public float SkyBands;

        // ---- weather effects -------------------------------------------------------
        /// <summary>0 = dry. Drives the instanced rain and the rain audio bed together.</summary>
        public float Rain;
        /// <summary>0..1. How hard the wind bed blows and how far the rain slants.</summary>
        public float Wind;
        /// <summary>Distant thunder one-shots, on a long random timer.</summary>
        public bool Thunder;

        // ---- audio beds (slot folders under Resources/Audio) ------------------------
        /// <summary>Weather layer: a rain or wind loop. Slot name, e.g. "weather/rain-heavy".</summary>
        public string WeatherBed = "";
        /// <summary>Place layer: which woodland this hour of day sounds like.</summary>
        public string PlaceBed = "";
        /// <summary>How loud the weather layer sits under everything else.</summary>
        public float WeatherBedVolume;
        /// <summary>How loud the place layer sits. Quiet: this is a bed, not a feature.</summary>
        public float PlaceBedVolume;
    }

    /// <summary>
    /// Picks and applies the weather for a position.
    ///
    /// SEEDED, NOT RANDOM. The condition comes from the scenario's director seed, so The Gate is
    /// the same dusk every single time it loads and the mission after it is reliably something
    /// else. That is a design property (a position should have a remembered feel) and a working
    /// property (two screenshots of the same scenario are comparable, which is the rule that has
    /// earned its place in this repo more than once).
    /// </summary>
    public static class Weather
    {
        /// <summary>Command-line override so a named condition can be summoned for a screenshot.</summary>
        public const string OverrideArg = "-exodus-weather";

        /// <summary>
        /// Weights, in <see cref="Sky"/> order. Dusk clear is the shipped look and stays the most
        /// common; the two dramatic conditions (rain, fog) together are about a third, which is
        /// often enough to be a surprise and rare enough to stay one.
        /// </summary>
        private static readonly float[] Weights = { 0.20f, 0.12f, 0.34f, 0.20f, 0.14f };

        /// <summary>
        /// The condition for a seed. Pure and total: every ulong maps to exactly one sky.
        /// </summary>
        public static Sky Pick(ulong seed)
        {
            // SplitMix64 finalizer. The scenario seeds are human-authored dates (20260910) which
            // differ in their low bits only, so a modulo of the raw seed would put every mission
            // under nearly the same sky. This scrambles before we sample.
            ulong z = seed + 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;

            float roll = (z >> 11) * (1.0f / 9007199254740992.0f);   // 53 bits -> [0,1)
            float acc = 0f;
            for (int i = 0; i < Weights.Length; i++)
            {
                acc += Weights[i];
                if (roll < acc) return (Sky)i;
            }
            return Sky.DuskClear;
        }

        /// <summary>
        /// Reads <c>-exodus-weather &lt;name&gt;</c>. Returns null when absent or unparseable.
        /// Pure, so the argument parsing is tested rather than trusted.
        /// </summary>
        public static Sky? FromCommandLine(string[]? args)
        {
            if (args == null) return null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], OverrideArg, StringComparison.OrdinalIgnoreCase)) continue;
                if (Enum.TryParse<Sky>(args[i + 1], ignoreCase: true, out var sky)) return sky;
                Debug.LogWarning($"[Weather] {OverrideArg} '{args[i + 1]}' is not a condition; using the seed.");
                return null;
            }
            return null;
        }

        /// <summary>The override if one was given on the command line, else the seeded pick.</summary>
        public static Sky Resolve(ulong seed)
        {
            return FromCommandLine(Environment.GetCommandLineArgs()) ?? Pick(seed);
        }

        /// <summary>The authored numbers for a condition. Pure; the tests read these directly.</summary>
        public static WeatherProfile Profile(Sky sky)
        {
            switch (sky)
            {
                // -------------------------------------------------------- overcast noon
                // Winter overcast: the sun is up but it is a lamp behind a sheet. Shadows are
                // weak and short, contrast is low, and the whole frame sits in a narrow band of
                // grey-green. The danger of this one is that it looks like the lighting broke,
                // so the cloud cover is pushed high to give the sky something to DO.
                case Sky.OvercastNoon:
                    return new WeatherProfile
                    {
                        Sky = Sky.OvercastNoon,
                        Name = "overcast noon",
                        SunEuler = new Vector3(58f, -35f, 0f),
                        SunColor = new Color(0.92f, 0.94f, 0.98f),
                        SunIntensity = 0.95f,
                        ShadowStrength = 0.45f,
                        AmbientSky = new Color(0.55f, 0.60f, 0.68f),
                        AmbientEquator = new Color(0.52f, 0.52f, 0.52f),
                        AmbientGround = new Color(0.30f, 0.26f, 0.21f),
                        FogColor = new Color(0.72f, 0.74f, 0.76f),
                        FogDensity = 0.0090f,
                        SkyTop = new Color(0.46f, 0.53f, 0.62f),
                        SkyHorizon = new Color(0.78f, 0.80f, 0.82f),
                        CloudLight = new Color(0.88f, 0.89f, 0.90f),
                        CloudDark = new Color(0.56f, 0.59f, 0.65f),
                        CloudCover = 0.80f,
                        SkyBands = 5f,
                        Rain = 0f,
                        Wind = 0.45f,
                        Thunder = false,
                        WeatherBed = "weather/wind-trees",
                        PlaceBed = "ambience/woodland-day",
                        WeatherBedVolume = 0.20f,
                        PlaceBedVolume = 0.15f,
                    };

                // -------------------------------------------------------------- sunrise
                // The mirror of dusk: same raking geometry, opposite compass, and the mist is
                // ground mist rather than haze so it is thicker than the evening's. Sunrise in
                // this game is a lie -- it is the sky you get on the mission where you still
                // think you are going to hold -- so it is the prettiest of the five on purpose.
                case Sky.Sunrise:
                    return new WeatherProfile
                    {
                        Sky = Sky.Sunrise,
                        Name = "sunrise",
                        SunEuler = new Vector3(9f, 74f, 0f),
                        SunColor = new Color(1.00f, 0.72f, 0.52f),
                        SunIntensity = 1.15f,
                        ShadowStrength = 0.75f,
                        AmbientSky = new Color(0.34f, 0.40f, 0.58f),
                        AmbientEquator = new Color(0.44f, 0.40f, 0.44f),
                        AmbientGround = new Color(0.26f, 0.20f, 0.16f),
                        FogColor = new Color(0.70f, 0.60f, 0.60f),
                        FogDensity = 0.0170f,
                        SkyTop = new Color(0.20f, 0.28f, 0.46f),
                        SkyHorizon = new Color(0.95f, 0.68f, 0.55f),
                        CloudLight = new Color(0.98f, 0.78f, 0.66f),
                        CloudDark = new Color(0.44f, 0.40f, 0.52f),
                        CloudCover = 0.42f,
                        SkyBands = 7f,
                        Rain = 0f,
                        Wind = 0.25f,
                        Thunder = false,
                        WeatherBed = "weather/wind-trees",
                        PlaceBed = "ambience/woodland-day",
                        WeatherBedVolume = 0.14f,
                        PlaceBedVolume = 0.20f,
                    };

                // ------------------------------------------------------------ dusk rain
                // Dusk with the key light rained out. The mistake to avoid is keeping the warm
                // sun and adding rain on top: real rain kills the sun, so the intensity halves
                // and the colour goes neutral, and ALL the warmth left in frame comes from the
                // ground bounce. Fog nearly doubles, which is what makes rain feel close.
                case Sky.DuskRain:
                    return new WeatherProfile
                    {
                        Sky = Sky.DuskRain,
                        Name = "dusk, raining",
                        SunEuler = new Vector3(13f, -58f, 0f),
                        SunColor = new Color(0.72f, 0.72f, 0.80f),
                        SunIntensity = 0.55f,
                        ShadowStrength = 0.35f,
                        AmbientSky = new Color(0.30f, 0.34f, 0.42f),
                        AmbientEquator = new Color(0.30f, 0.30f, 0.34f),
                        AmbientGround = new Color(0.18f, 0.16f, 0.15f),
                        FogColor = new Color(0.42f, 0.43f, 0.48f),
                        FogDensity = 0.0240f,
                        SkyTop = new Color(0.12f, 0.14f, 0.20f),
                        SkyHorizon = new Color(0.40f, 0.40f, 0.45f),
                        CloudLight = new Color(0.52f, 0.53f, 0.58f),
                        CloudDark = new Color(0.22f, 0.23f, 0.29f),
                        CloudCover = 0.92f,
                        SkyBands = 4f,
                        Rain = 1.0f,
                        Wind = 0.80f,
                        Thunder = true,
                        WeatherBed = "weather/rain-heavy",
                        PlaceBed = "ambience/woodland-dusk",
                        WeatherBedVolume = 0.34f,
                        PlaceBedVolume = 0.08f,
                    };

                // ------------------------------------------------------------------ fog
                // No readable sun, no horizon, no treeline. Everything pale and close. The sun
                // is kept faintly on so the cel shader still has a light direction to band
                // against -- kill it entirely and every character goes flat and the ink outline
                // is the only thing left describing them.
                case Sky.Fog:
                    return new WeatherProfile
                    {
                        Sky = Sky.Fog,
                        Name = "fog",
                        SunEuler = new Vector3(30f, -20f, 0f),
                        SunColor = new Color(0.82f, 0.84f, 0.86f),
                        SunIntensity = 0.60f,
                        ShadowStrength = 0.20f,
                        AmbientSky = new Color(0.62f, 0.64f, 0.66f),
                        AmbientEquator = new Color(0.60f, 0.60f, 0.60f),
                        AmbientGround = new Color(0.36f, 0.33f, 0.29f),
                        FogColor = new Color(0.76f, 0.77f, 0.76f),
                        FogDensity = 0.0380f,
                        SkyTop = new Color(0.62f, 0.65f, 0.68f),
                        SkyHorizon = new Color(0.80f, 0.80f, 0.78f),
                        CloudLight = new Color(0.84f, 0.84f, 0.83f),
                        CloudDark = new Color(0.68f, 0.69f, 0.70f),
                        CloudCover = 0.70f,
                        SkyBands = 3f,
                        Rain = 0f,
                        Wind = 0.15f,
                        Thunder = false,
                        WeatherBed = "weather/wind-trees",
                        PlaceBed = "ambience/woodland-dusk",
                        WeatherBedVolume = 0.10f,
                        PlaceBedVolume = 0.22f,
                    };

                // ----------------------------------------------------------- dusk clear
                // THE SHIPPED LOOK, PRESERVED NUMBER FOR NUMBER from ApplyOvercastWinter. It is
                // the most common condition and the one the art was tuned against; if a change
                // here makes the game look different, that is a regression, not a weather.
                default:
                    return new WeatherProfile
                    {
                        Sky = Sky.DuskClear,
                        Name = "dusk",
                        SunEuler = new Vector3(13f, -58f, 0f),
                        SunColor = new Color(1.00f, 0.80f, 0.57f),
                        SunIntensity = 1.30f,
                        ShadowStrength = 0.80f,
                        AmbientSky = new Color(0.30f, 0.36f, 0.50f),
                        AmbientEquator = new Color(0.38f, 0.36f, 0.40f),
                        AmbientGround = new Color(0.24f, 0.18f, 0.14f),
                        FogColor = new Color(0.62f, 0.52f, 0.47f),
                        FogDensity = 0.0135f,
                        SkyTop = new Color(0.17f, 0.21f, 0.34f),
                        SkyHorizon = new Color(0.86f, 0.61f, 0.40f),
                        CloudLight = new Color(0.93f, 0.72f, 0.55f),
                        CloudDark = new Color(0.40f, 0.36f, 0.46f),
                        CloudCover = 0.55f,
                        SkyBands = 6f,
                        Rain = 0f,
                        Wind = 0.35f,
                        Thunder = false,
                        WeatherBed = "weather/wind-trees",
                        PlaceBed = "ambience/woodland-dusk",
                        WeatherBedVolume = 0.18f,
                        PlaceBedVolume = 0.18f,
                    };
            }
        }

        /// <summary>
        /// Builds (or re-aims) the sun and writes the profile into RenderSettings and the sky
        /// material. Safe to call again for a new condition: it reuses the light it was given.
        /// </summary>
        /// <returns>The directional light, so the caller can keep holding it.</returns>
        public static Light Apply(WeatherProfile p, Camera? camera, Light? existingSun = null)
        {
            var light = existingSun;
            if (light == null)
            {
                var go = new GameObject("Sun");
                light = go.AddComponent<Light>();
                light.type = LightType.Directional;
            }

            light.transform.rotation = Quaternion.Euler(p.SunEuler);
            light.color = p.SunColor;
            light.intensity = p.SunIntensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = p.ShadowStrength;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = p.AmbientSky;
            RenderSettings.ambientEquatorColor = p.AmbientEquator;
            RenderSettings.ambientGroundColor = p.AmbientGround;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = p.FogColor;
            RenderSettings.fogDensity = p.FogDensity;

            // A banded procedural sky rather than a flat clear colour. A smooth or empty sky over
            // cel-shaded ground is the fastest way to make stylised art look unfinished, because
            // the eye reads the mismatch before it reads anything else.
            var skyShader = Shader.Find("Exodus/ComicSky");
            if (skyShader != null)
            {
                // Reuse the existing sky material where there is one: a new Material per position
                // change leaks one per mission across a campaign.
                var sky = RenderSettings.skybox;
                if (sky == null || sky.shader != skyShader) sky = new Material(skyShader);

                sky.SetColor("_SkyTop", p.SkyTop);
                sky.SetColor("_SkyHorizon", p.SkyHorizon);
                sky.SetColor("_CloudLight", p.CloudLight);
                sky.SetColor("_CloudDark", p.CloudDark);
                sky.SetColor("_GroundHaze", p.FogColor);
                sky.SetFloat("_CloudCover", p.CloudCover);
                sky.SetFloat("_Bands", p.SkyBands);
                RenderSettings.skybox = sky;

                if (camera != null) camera.clearFlags = CameraClearFlags.Skybox;
            }
            else if (camera != null)
            {
                Debug.LogWarning("[Sky] Exodus/ComicSky not found; falling back to a flat clear");
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = p.FogColor;
            }

            return light;
        }
    }
}
