# Getting ONE/ONE prompts into Suno on Android

Status: 2026-10-11. The goal is to move four things (Lyrics, Styles, Exclude styles, slider values) from the
ONE/ONE page into the Suno Android app with as few taps as possible, and to be using it within days.

## 1. What is technically possible on Android today

Legend: **verified** = read from a primary source or well-documented API; **reported** = from secondary press
or vendor docs; **unverified** = reasoning or memory, confirm on a device.

### (a) Installed PWA in split-screen beside Suno, one-tap "copy next field"

- Chrome on Android installs a PWA as a WebAPK, which is a real Android app entry in Recents and can be put in
  split-screen like any other app. web.dev lists "sharing the screen with other app windows, as in Android
  split-screen mode" as a property of installed PWA windows. **verified** (web.dev Window management guide)
- Multi-window/split-screen has been a platform feature since Android 7.0; launchers decide the gesture to
  enter it (usually: Recents -> long-press the app icon -> "Split screen"/"Open in split screen view").
  **verified** (source.android.com split-screen doc)
- The manifest cannot set a preferred window size; the user drags the divider. **verified** (web.dev)
- Clipboard writes from a page need a user gesture (`navigator.clipboard.writeText` inside a click handler),
  which the one-button sequence satisfies. **verified** (standard Clipboard API behaviour)
- Install requires HTTPS plus a manifest and a service worker; `file://` does not install as a standalone app.
  **verified** (Chrome install criteria)
- Cost: one tap here + one long-press-paste in Suno per field, three fields, no permissions, no sideloading,
  no policy exposure. Works on every Android phone and on iPhone too (Safari Add to Home Screen, minus split-screen).

### (b) Floating overlay / bubble native app (SYSTEM_ALERT_WINDOW)

- `TYPE_APPLICATION_OVERLAY` windows still work; the user grants "Display over other apps". **verified**
  (framework API, unchanged through API 35)
- Google said in 2019 that Bubbles would eventually replace SYSTEM_ALERT_WINDOW; that deprecation has not
  happened as of 2025 and Bubbles remain a notification-conversation feature tied to the user's notification
  settings. **reported** (XDA 2019; Flutter plugin docs note the Android 11+ "All conversations can bubble"
  dependency)
- Android 15 added "Display over other apps" to the Restricted Settings list for sideloaded apps; the user must
  do App info -> three dots -> "Allow restricted settings" first. **reported** (Android Authority, Sept 2024;
  Zebra enterprise docs describe the same unlock flow)
- An overlay alone still cannot type into Suno. It only keeps the text visible and puts it on the clipboard;
  the paste is still manual. So it is strictly a UX improvement over (a) only if (c) is added.

### (c) Accessibility service that sets Suno's EditText directly (ACTION_SET_TEXT)

- The API exists and works cross-app: a service with `canRetrieveWindowContent` walks the active window's
  node tree, finds an editable node and calls `performAction(ACTION_SET_TEXT, bundle)`; empty text clears
  the field and the cursor goes to the end. **verified** (AccessibilityNodeInfo reference; AOSP source
  comments; many automation write-ups, e.g. the WhatsApp automation example)
- Whether Suno's boxes accept ACTION_SET_TEXT depends on Suno's UI toolkit. Native EditText does; Flutter and
  React Native text fields generally do too, but it is **unverified** for Suno specifically. The scaffold
  falls back to clipboard + manual paste if the action returns false.
- Sideloading: since Android 13, a sideloaded app's accessibility service is a "Restricted setting"; the user
  unlocks it once via App info -> three dots -> "Allow restricted settings" (PIN confirmation). Apps installed
  via `adb` or via the session-based installer do not even show the restriction. **reported** (9to5Google,
  Esper, BleepingComputer, CleanBrowsing guide)
- Google Play: AccessibilityService use by anything that is not an accessibility tool requires the Permission
  Declaration Form plus a prominent in-app disclosure and consent; Google lists "automation tools" explicitly
  as not accessibility tools. Undeclared use can suspend the app and the developer account. **verified**
  (Play Console help, "Use of the AccessibilityService API"). Conclusion: viable only as a personal sideloaded
  APK, not as a Play listing.

### (d) Clipboard: multi-item, Android 13 preview, keyboard clipboard history

- Android 13+ shows a clipboard overlay (preview + edit) after every copy. It is cosmetic for this use case
  and cannot be disabled. **reported** (9to5Google, ZDNet, Android Police)
- There is no system multi-clip API: `ClipData` can hold several items but every consumer pastes item 0.
  **verified** (ClipboardManager API)
- Gboard and Samsung Keyboard keep a clipboard history (Gboard: about an hour unless pinned). The user could
  copy all three fields in a row and pick each from the keyboard's clipboard panel while in Suno. That is a
  real option and needs nothing from us; the "Copy all as one block" output is also a fine single clip.
  **reported** (several Gboard guides, inconsistent on menu paths)

### (e) suno.com in a mobile browser + userscript/extension

- Kiwi Browser (the usual way to run Chrome extensions on Android) was discontinued in January 2025; its
  extension code moved into Microsoft Edge Canary. **reported** (AlternativeTo, ReclaimTheNet)
- Firefox for Android runs Violentmonkey and Tampermonkey (Tampermonkey 5.5.0, May 2026). **verified**
  (addons.mozilla.org listings)
- A userscript could fill suno.com's create form (set the native value setter, dispatch `input`, which is
  what React-controlled textareas need). It depends on suno.com's DOM, which is unversioned and changes
  without notice, and the user would have to use suno.com in Firefox instead of the Suno app. **unverified**
  for the current suno.com markup. Fragile; not recommended as the primary path.

### (f) Suno public API, deep link or share target

- Suno has no public, self-serve API. In July 2026 its CPO announced an invite-only API partner program for
  apps that send prompts and receive audio; no docs, pricing or timeline are public. Third-party "Suno API"
  wrappers are unofficial and a terms-of-service risk. **reported** (Digital Music News, Music Business
  Worldwide, July 2026)
- No evidence of a `suno.com/create?...` URL that pre-fills fields, and no evidence that the Suno Android app
  registers an `ACTION_SEND` share target for text. **unverified** (nothing found; worth checking the app's
  manifest with `aapt dump xmltree` on a device, which takes five minutes and would make Share-to-Suno the
  best possible flow if it exists)

## 2. Decision

**Primary: the installed PWA in split-screen with a one-tap paste sequence.** It ships now, needs no
permissions, no sideloading, no Play policy exposure, and works on every Android phone. Friction is three taps
here and three long-press pastes in Suno, with the bar always telling you what is on the clipboard and where it
goes. The whole flow is under 30 seconds.

**Fallback: a sideloaded native "Filler" app** (scaffolded in `android/`): a floating chip strip over Suno
plus an accessibility service scoped to `com.suno.android` that writes the focused box with `ACTION_SET_TEXT`.
It removes the pastes and the app switching, at the cost of a one-time Android Studio build, two permission
unlocks (Restricted settings), and an unverified assumption about Suno's toolkit. It is not publishable on
Play without a declaration review, which is fine for a personal tool.

Rejected: Suno API (does not exist publicly), deep links (none found), suno.com userscripts (fragile and the
user wants the app), overlay without accessibility (does not remove the paste).

## 3. What was built

- `src/one/manifest.webmanifest`: name, standalone display, dark theme/background, id/scope, icons.
- `src/one/sw.js`: service worker; caches the page, manifest and icons, network-first so new builds show up,
  Google Fonts cached on first use. Stamped with a hash of the built page so each build replaces the cache.
- `src/one/icon.js`: draws the 192 and 512 px PNG icons in pure Node (no dependencies).
- `build.js`: emits `dist/one.webmanifest`, `dist/one-sw.js`, `dist/one-icon-192.png`, `dist/one-icon-512.png`.
- `src/one/template.html`: PWA head tags, `data-field` markers on the four sections, the pinned paste bar,
  split-screen (short viewport) styles, active/pasted field highlighting.
- `src/one/app.js`: the paste sequence (Lyrics -> Styles -> Exclude styles -> slider values, resets on every
  roll and on Restart), "Copy all as one block", Web Share of the block, service-worker registration and the
  Chrome "Install app" prompt.
- `.github/workflows/pages.yml`: deploys `dist/` to GitHub Pages on push to `main` (needs one-time setup).
- `android/`: Kotlin scaffold of the fallback app with a README (build steps, permission unlocks, assumptions).

The block format (also what the Android app parses):

```
LYRICS
...

STYLES
...

EXCLUDE STYLES
...

SLIDERS
Weirdness 40% · Style Influence 70%
Model v6 · Variety Off · Max Mode On · Instrumental On
```

## 4. Install and use on the phone

### One-time: put the page on HTTPS

1. Push this branch and merge to `main`.
2. GitHub -> repo **Settings -> Pages -> Build and deployment -> Source: GitHub Actions**.
3. The `pages` workflow runs tests, builds and publishes `dist/`. The page is at
   `https://<user>.github.io/Cipher/one.html`. (Any static host works; Netlify/Vercel drag-and-drop of `dist/`
   is the alternative.)

### One-time: install on Android (Chrome)

1. Open the URL in Chrome. Tap the **Install app** button in the bottom bar if Chrome offers it; otherwise
   Chrome menu (three dots) -> **Add to Home screen** -> **Install**.
2. An **ONE/ONE** icon appears in the launcher and in the app drawer; it opens full-screen without browser
   chrome and works offline.

### Every time: split-screen beside Suno

1. Open **Suno**, go to Create, switch to Custom.
2. Open **Recents** (swipe up and hold, or the square button). Long-press the Suno icon at the top of its card
   -> **Split screen** (Samsung: "Open in split screen view"). Pick **ONE/ONE** for the other half.
   Drag the divider so Suno gets a little more than half.
3. In ONE/ONE: pick a genre, tap **ROLL**. The bar reads **COPY LYRICS 1/3**.
4. Tap it. Tap into Suno's Lyrics box, long-press -> **Paste**. The bar now reads **COPY STYLES 2/3**.
5. Tap it, paste into Styles. Tap **COPY EXCLUDE STYLES 3/3**, open Suno's Advanced Options, paste into Exclude
   styles.
6. The bar now shows the slider values and settings (Weirdness, Style Influence, model, Variety, Max Mode,
   Instrumental). Set them in Suno. Tap the bar to start over, or Roll again.

Alternatives in the same bar: **Copy all as one block** (one clip; split it in Suno or keep it in Notes) and
**Share** (sends the block to any app, including the Filler app below).

## 5. Roadmap for the native piece

1. On a phone with Suno installed, confirm the package name (`adb shell pm list packages | grep -i suno`) and
   dump its manifest to check for an `ACTION_SEND` text filter or a `suno://` scheme. If either exists, add a
   direct Share/deep-link button to the page and the native app may be unnecessary.
2. Build `android/` in Android Studio, sideload, unlock the two restricted settings, and test `ACTION_SET_TEXT`
   on each of the three Suno boxes. If Suno's toolkit rejects it, switch `SunoFillService.setFocusedText` to
   `ACTION_PASTE` after setting the clipboard (one extra line).
3. If the focused-box approach is reliable, try auto-targeting: walk the tree for editable nodes in order
   (Suno's order is Lyrics, Styles, Exclude) and fill all three with one chip tap.
4. Optional: receive the block over Web Share Target in the PWA direction too (so a share from Suno's result
   page can log which prompt produced it).
5. Never ship the accessibility service to Google Play without the declaration form; keep it sideloaded or
   distribute as an APK release on GitHub.

## Sources

- web.dev, Window management for PWAs: https://web.dev/learn/pwa/windows
- AOSP, Split-screen interactions: https://source.android.com/docs/core/display/split-screen
- Android reference, AccessibilityNodeInfo (ACTION_SET_TEXT): https://developer.android.com/reference/android/view/accessibility/AccessibilityNodeInfo
- Android guide, Create your own accessibility service: https://developer.android.com/guide/topics/ui/accessibility/service
- Google Play Console help, Use of the AccessibilityService API: https://support.google.com/googleplay/android-developer/answer/10964491
- XDA, Google trying to limit apps that use an Accessibility Service: https://www.xda-developers.com/google-trying-limit-apps-accessibility-service/
- 9to5Google, Android 13 restricts sideloaded apps' accessibility access: https://9to5google.com/2022/05/06/android-13-sideloaded-accessibility/
- Esper, Android 13 sideloading restriction: https://esper.io/blog/android-13-sideloading-restriction-harder-malware-abuse-accessibility-apis
- BleepingComputer, malware bypass of Restricted Settings: https://www.bleepingcomputer.com/news/security/malware-devs-already-bypassed-android-13s-new-security-feature/
- CleanBrowsing, Allow restricted settings on Android 13+: https://cleanbrowsing.org/support/mobile/disable-restricted-settings-android
- Android Authority, Android 15 restricted settings for sideloaded apps: https://androidauthority.com/android-15-restricted-settings-sideloading-3481098
- Zebra docs, Display over other apps on Android 15 (restricted settings unlock): https://docs.zebra.com/us/en/solutions/wcc/ptt-express-ig/c-ptte-android-ig-installation/t-zcc-installing-on-android-15-devices.html
- XDA, Bubbles to replace the overlay API (2019): https://www.xda-developers.com/android-q-system-alert-window-deprecate-bubbles
- 9to5Google, Android 13 clipboard overlay: https://9to5google.com/2022/04/26/android-13-clipboard-overlay/
- ZDNet, Android 13 clipboard editor: https://www.zdnet.com.au/article/how-to-use-the-android-13-clipboard-editor/
- AlternativeTo, Kiwi Browser shuts down, extensions move to Edge Canary: https://www.alternativeto.net/news/2025/1/kiwi-browser-discontinued-explore-alternatives-for-extension-support-and-security/
- Mozilla add-ons, Violentmonkey (Firefox for Android): https://addons.mozilla.org/en-US/firefox/addon/violentmonkey/
- Mozilla add-ons, Tampermonkey for Android versions: https://addons.mozilla.org/fr/android/addon/tampermonkey/versions/
- Digital Music News, Suno opening an API partner program (July 2026): https://www.digitalmusicnews.com/2026/07/03/suno-is-opening-an-api-partner-program/
- Music Business Worldwide, Suno explores developer API: https://musicbusinessworldwide.com/suno-explores-developer-api-seeking-apps-that-unlock-experiences-generative-music-makes-possible-for-the-first-time
- Blake Crosley, Suno's Style field and Style Influence slider (field layout incl. Exclude under Advanced Options): https://blakecrosley.com/blog/suno-style-field-style-influence
- Hackernoon, automating WhatsApp with an accessibility service (ACTION_SET_TEXT example): https://sia.hackernoon.com/i-automated-my-whatsapp-chats-on-android-and-heres-how-you-can-too-kh4g319g
