#nullable enable
using UnityEditor;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Import settings for the fetched audio, applied by rule rather than by hand.
    ///
    /// WHY THIS EXISTS. Unity's default for an .ogg is Vorbis, DecompressOnLoad. For the foley
    /// that is correct and cheap. For the beds it is a disaster that never announces itself: the
    /// woodland loop is a three-megabyte file and about a hundred and sixty seconds of stereo
    /// PCM, so "decompress on load" means tens of megabytes of RAM per bed, three beds live at
    /// once, on a platform whose perf floor is an Android handset. Nothing errors. The game just
    /// costs a great deal more memory than it looks like it should.
    ///
    /// Done as a postprocessor and not as checked-in .meta values because a re-run of
    /// tools/audio/fetch-sounds.py brings in files that have never been imported, and a rule
    /// covers those while a hand-edited meta does not. Same reason UrpSetup builds the pipeline
    /// assets in code: a clean checkout must be able to rebuild the project without clicking.
    /// </summary>
    public sealed class AudioImportRules : AssetPostprocessor
    {
        private const string AudioRoot = "Assets/Resources/Audio/";

        /// <summary>Slot folders whose clips are long, looping and played one at a time.</summary>
        private static readonly string[] Beds =
        {
            "weather/rain-heavy", "weather/rain-light", "weather/wind-trees",
            "ambience/woodland-day", "ambience/woodland-dusk", "ambience/lake-shore",
            "crowd/crowd-murmur-distant",
        };

        private void OnPreprocessAudio()
        {
            var path = assetPath.Replace('\\', '/');
            if (!path.StartsWith(AudioRoot, System.StringComparison.OrdinalIgnoreCase)) return;

            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;

            if (IsBed(path))
            {
                // Streamed: a bed is read start to finish exactly once per loop, which is the
                // access pattern streaming is for, and it costs a buffer instead of a decode.
                settings.loadType = AudioClipLoadType.Streaming;
                settings.quality = 0.55f;       // it is a bed under gunfire, not a listening test
                // In Unity 6 this is a per-platform sample setting, not a property on the
                // importer; the importer-level one still compiles but is obsolete and ignored.
                settings.preloadAudioData = false;
            }
            else
            {
                // Foley fires on a frame and must already be in memory. These are tens of
                // kilobytes each; decompressing them all is free.
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.quality = 0.7f;
                settings.preloadAudioData = true;
            }

            // Mono for everything. The beds are 2D and a stereo bed is twice the data for a
            // width the player cannot use; the foley is the player's own boots, which are 2D on
            // purpose (see Ambience.MakeOneShot).
            importer.forceToMono = true;
            importer.defaultSampleSettings = settings;
        }

        private static bool IsBed(string path)
        {
            foreach (var slot in Beds)
                if (path.StartsWith(AudioRoot + slot, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
