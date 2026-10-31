# Exclude Styles presets

Rules (see `reference/suno-field-spec.md`): bare nouns, comma-separated, never negated ("vocals", not "no vocals"); the field is a bias, not a ban; hands-on testers report it bites hardest with a short list, so the generator defaults to ~8 terms and you trim to 4 if Suno ignores it. Never exclude something the Style field asks for (do not exclude "808" on a trap beat). Limit 1,000 characters.

## Core vocal guards (always present)

| Preset | Terms | When |
|---|---|---|
| `short` | `vocals, humming, ad-libs, vocal chops` | Suno is ignoring a longer list, or the Style field is already very specific |
| `default` (core + lane) | core 4 + first 4 lane guards (~8 terms) | Every generated block |
| `full` (belt and braces) | `vocals, lyrics, singing, rap, spoken word, humming, ad-libs, vocal chops, choir, EDM build, piano ballad, quantized rigid drums, drift phonk, cinematic trailer` + lane guards | A lane keeps leaking vocals or drifting genre; `--excludePreset full` |
| `lead` core (phonk lane) | `lead vocals, humming, singing, rapping` | Lanes that use chopped and screwed sample textures as instruments; excluding "vocal chops" would fight the Style field |

## Lane guards

| Lane | Guards (priority order; default takes the first 4) |
|---|---|
| connor-price | drift phonk, EDM build, dubstep, lo-fi hiss, dark, aggressive distortion, slow ballad, orchestral |
| paak-pocket | trap hi-hats, EDM, quantized drum machine, 808 trap bass, autotune, synthwave, metal, smooth jazz elevator |
| jid-dark | drift phonk, EDM build, cinematic trailer, happy, major key pop, smooth jazz, lo-fi chill, dubstep |
| phonk-experimental | drift phonk, Brazilian funk, EDM build, sidechain pump, cowbell lead melody, clean modern mix, house, dubstep |
| hybrid | drift phonk, Brazilian funk, EDM build, cowbell lead melody, quantized rigid drums, smooth jazz, cinematic trailer, dubstep |

## Situational add-ons (`--exclude "..."`)

| Problem heard | Add |
|---|---|
| Wordless "ooh" pads or sustained vowels | `wordless vocals, choir, vocal pad` |
| Spoken intro / DJ drop | `spoken intro, DJ tags, crowd noise` |
| Sung hook on a bouncy beat | `pop vocals, autotune, chorus vocals` |
| Beat turns into an EDM drop | `EDM drops, supersaw, sidechain pump, big room` |
| Beat turns into a trailer cue | `cinematic trailer, epic orchestra, braams` |
| Rigid drum machine on a live lane | `quantized drums, drum machine, trap hi-hats` |
| Phonk slides into drift phonk | `drift phonk, cowbell melody, Brazilian funk, gym music` |
| Too clean for lo-fi lanes | `clean modern mix, polished pop production` |
| Guitar appears uninvited | `acoustic guitar, electric guitar solo` |
| Piano ballad drift | `piano ballad, solo piano, slow ballad` |
