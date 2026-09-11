# Four packs to download by hand (about 90 seconds)

**These are free, CC0, and they are the answer to "did our library not have better humanoid
models".** It did not — because nobody had fetched the packs that had them. Robots, mechs, monsters
and a turret pack have been sitting on the free shelf the whole time.

**`tools/art/fetch-free-packs.sh` cannot get them any more.** Google rebuilt the Drive folder page
to render entirely in JavaScript, so the scraper that fetched the first six packs now sees a shell
with no file list in it — it reports `[dir?]` for a few subfolders and downloads nothing. The
existing six packs on disk were fetched before that change. Fixing the scraper means either a
headless browser or a Google Cloud API key, and neither is worth it for four downloads.

## What to click

| Pack | Page | Why we want it |
|---|---|---|
| **Animated Monsters** | https://quaternius.com/packs/animatedmonster.html | **The Collector** (ADR-011) — the owner's "butcher from Diablo, huge belly, three times the size of a person". Currently built from primitives. |
| **Animated Robot** | https://quaternius.com/packs/animatedrobot.html | The **hacked service humanoids** (ADR-003, ~30% of a wave). Currently primitives with a procedural gait. |
| **Animated Mech** | https://quaternius.com/packs/animatedmech.html | A heavier machine class, and a second silhouette for the Collector to be picked from. |
| **Turret Pack** | https://quaternius.com/packs/turretpack.html | The emplacements. The salvage look is deliberate and good, but worth comparing against. |

On each page: scroll to the **Download** button, which opens a Google Drive folder. Download the
whole folder (Drive offers a zip), or just the `FBX/` directory inside it.

## Where to put them

Unzip into `art/free/<name>/`, using exactly these folder names so the rest of the tooling finds
them:

```
art/free/monsters/
art/free/robots/
art/free/mechs/
art/free/turrets/
```

`art/free/` is gitignored on purpose — these are a re-downloadable input, not project source, and
committing several hundred megabytes of third-party meshes would be wrong even under CC0.

Tell me when they are there and I will wire them: the animation path in CLAUDE.md is a minefield
(five approaches failed silently before one worked) and these packs ship each character twice, so
which file gets used is not obvious.

## The licence, for the record

CC0 1.0 Universal, stated in each pack's own `License.txt` and on its page: free in personal,
educational and **commercial** games, no credit required. Raw assets may not be resold or
repackaged, which we never do. Same terms as the six packs already in use.
