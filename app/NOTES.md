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
