# Builder notes — ambiguities resolved

Each item: what the spec left open, the simplest reading I took, and where it lives.

1. **Path fields refuse non-existent files.** Spec: "server verifies it exists; 'file not
   found' is a sentence". I read that as: a path that does not exist is rejected with the
   sentence and *not* saved, rather than saved with a warning. Applies to stage 1, 2, 5
   (shots), 6 (final). `tracks._check_path_fields`.
2. **Cut and Assemble mark their stage done automatically** on success, since the action
   is the whole stage. Checklist stages still need an explicit Mark done. Reopen is
   always available. `media.cut_clip`, `media.assemble`.
3. **Shot list per variant.** Spec says "3–5 prompt variants ... plus a shot list". Each
   variant carries its own shot list built from its subject/setting/lighting so "Use
   this" saves a matching pair. `prompts.generate` → `variant["shots"]`.
4. **Loop vs trim in Assemble.** Spec: trim with `-shortest`, loop with `-stream_loop` if
   the video is shorter. The final mux always uses `-stream_loop -1` on the concatenated
   intermediate plus `-shortest`, which covers both cases without probing durations in a
   pure command builder. Verified here: 4 s of video over 7 s audio → 7.0 s output; 4 s
   over 2 s → 2.0 s. `media.build_assemble_cmd`.
5. **"Use this" replaces earlier chosen prompts** instead of accumulating them; the
   Drafts list stays short. `server.App.choose_prompt`.
6. **Metadata regenerate replaces unchosen drafts** of the four metadata kinds; nothing is
   marked chosen for metadata, so Regenerate discards edits. The UI button says so.
7. **Platform format reminders are generic** ("check the current limit on the upload
   screen") rather than quoting numbers that go stale, per the no-invented-facts rule.
   `db.DEFAULT_PLATFORMS`.
8. **Distributor upload URL is blank by default**; the owner sets theirs in Settings.
9. **Track edit.** Spec lists Notes as editable; I also expose an "Edit details" form on
   the track page because the generators depend on genre/mood/energy and typos would
   otherwise be permanent. No delete; status `archived` exists in the API (PATCH) but has
   no UI button (backlog).
10. **CLI `cut --src`** accepts an explicit audio file so the clip can be cut before the
    download/master path is recorded (also used by tests). `prompts --setting/--aspect/
    --shot-len`, `assemble --aspect`, `meta --explicit` are the same kind of thin flags.
11. **Browser auto-open** on `serve` (spec §8.1 "browser opens the Dashboard") can be
    turned off with `--no-browser` or `RELEASELINE_NO_BROWSER=1`.
12. **Request log** is quiet for 2xx and prints one line per 4xx/5xx; tracebacks for 500s
    go to the terminal only.
13. **Settings file corrupt** → every API call that needs settings returns the sentence
    with the path; the Dashboard itself still loads because it only needs the database.
14. **Type-beat rule** covers genre families hip hop/trap/rap/boom bap/phonk, drill/grime,
    R&B/soul, lo-fi/chillhop. The title is "<Title> | <Mood> <Genre> Type Beat <year>"
    without "[FREE]": whether a beat is free is a licensing choice, left to the owner.
15. **Hashtag counts**: YouTube up to 4, TikTok up to 8, Instagram up to 15. Chosen as
    sane defaults, not platform rules.

## Added after review round 1 (qa-01, ux-01)

16. **Cut checks the source length first.** With ffmpeg present, `cut_clip` runs ffprobe
    (falling back to `ffmpeg -i`) on the source. Start at or past the end → sentence, no
    files, stage unchanged. Start inside but length past the end → length clamped to what
    is left (and the fade shortened to a quarter of that if it no longer fits) with the
    note "The clip will be M:SS long because the file ends there." appended to the
    message. After every cut the WAV is verified (≥ 1 KB and a probed duration > 0) before
    it is renamed into place; otherwise outputs are deleted and a sentence is returned.
    If neither ffprobe nor ffmpeg can report a duration, the pre-check is skipped and the
    post-check still applies. `media.cut_clip`, `media.check_clip_output`.
17. **Slug chosen inside the write transaction.** `create_track` does BEGIN IMMEDIATE,
    then picks the free slug, makes the folders, inserts. Concurrent same-title creates
    serialize instead of colliding. The library folder check is still DB-only (an existing
    folder with no track is reused), see the Dashboard notice below.
18. **Request validation is central.** `tracks.text_value` (string, ≤ 4,000 chars; feedback
    text ≤ 20,000), `tracks.id_value` (digits, ≤ 18 of them, else 404) and
    `server.App._text/_number` reject wrong JSON types with "The field 'x' should be
    text / a number." Non-string scalars are no longer coerced with `str()`.
19. **ffmpeg stderr is filtered** (`media.stderr_tail`): progress lines (`frame=`,
    `size=`, `time=`…) are dropped and `[name @ 0x…]` prefixes lose the address; the last
    four meaningful lines are shown.
20. **argparse errors exit 1** via a `Parser.error` override (usage + "Error: …"). Exit 2
    stays for unexpected exceptions. A Python < 3.10 check runs in `__main__` before any
    other import.
21. **Dashboard notice for a recreated database.** `db.connect` remembers database files
    this process created from nothing; when that is true and `library/` has folders no
    track points at, `/api/dashboard` carries `notice` and the UI shows an amber card.
    Chosen over scanning folders on every request unconditionally so a fresh install
    never shows it.
22. **UI**: `busy()` always restores a button's label; metadata boxes render only in
    stage 7 (the Drafts card links there); variants stay visible after Use this with the
    chosen one marked, and the Drafts card refreshes in place; empty path/URL saves are
    "Enter … first." or "… cleared."; the Suno link is a text field with `inputmode=url`;
    the upload table stacks into blocks under 600 px.
