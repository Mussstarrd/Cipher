# ONE/ONE Filler (Android, native fallback)

A tiny sideloaded app that floats a strip of chips (`LYRICS` / `STYLES` / `EXCL`) over the Suno app and
writes each field straight into the Suno text box you tapped. No copy-paste, no app switching.

It is the **fallback** to the PWA + split-screen flow described in `../docs/android-plan.md`. Build it only
if the paste sequence turns out to be too slow for you. It cannot be built in this repo's CI (no Android SDK);
build it on a machine with Android Studio.

## What it does

1. `MainActivity` receives the prompt block (the ONE/ONE page's **Share** button, or **Copy all as one block**
   then step 3 in the app) and stores it in SharedPreferences. `Prompt.kt` parses the `LYRICS` / `STYLES` /
   `EXCLUDE STYLES` / `SLIDERS` headers.
2. `BubbleService` is a foreground service that draws a draggable overlay strip (`TYPE_APPLICATION_OVERLAY`,
   needs "Display over other apps").
3. `SunoFillService` is an `AccessibilityService` scoped to `com.suno.android` only. On a chip tap it finds the
   input-focused editable node and performs `ACTION_SET_TEXT`. If that fails (toolkits that only honour paste),
   the chip falls back to copying the field to the clipboard and tells you to long-press and Paste.

The sliders are not automated: the strip shows the values, you drag them in Suno.

## Build

```
# Android Studio (Ladybug or newer): File -> Open -> this folder, then Build -> Build APK(s).
# Or on the command line with a JDK 17 and the Android SDK installed:
cd android
gradle wrapper --gradle-version 8.9      # once; the wrapper jar is not committed
./gradlew assembleDebug
adb install -r app/build/outputs/apk/debug/app-debug.apk
```

No library dependencies, so the first sync only downloads the Android Gradle plugin and Kotlin.

## First run on the phone (Android 13 or newer)

1. Open **ONE/ONE Filler**. Tap **1** and allow "Display over other apps".
2. Tap **2**. Because the app is sideloaded, Android 13+ greys the service out as a *Restricted setting*.
   Unlock it: **Settings -> Apps -> ONE/ONE Filler -> (three dots) -> Allow restricted settings**, confirm with
   your PIN, then come back and turn **ONE/ONE Suno filler** on. (If the app does not appear in the Accessibility
   list at all, reinstall the APK by tapping the file in a file manager rather than via `adb`, so Android
   registers it as a user-installed app.)
3. In the ONE/ONE page, roll, then tap **Share** and pick ONE/ONE Filler (or **Copy all as one block** and use
   step **3** in the app).
4. Tap **4**: the strip appears and Suno opens. In Suno, tap the Lyrics box, tap `LYRICS` on the strip. Tap the
   Styles box, tap `STYLES`. Open Advanced Options, tap Exclude styles, tap `EXCL`. Set the sliders. Tap `1/1`
   to dismiss the strip.

## Unverified assumptions (check on a device)

- Suno's Android package name is assumed to be `com.suno.android`. Confirm with
  `adb shell pm list packages | grep -i suno` and change `SunoFillService.SUNO_PACKAGE` and
  `res/xml/suno_fill_service.xml` if it differs.
- Suno's text boxes are assumed to expose `isEditable` nodes that accept `ACTION_SET_TEXT`. If Suno is a
  Flutter or React Native app this usually still works, but if it does not the clipboard fallback kicks in.

## Google Play

Do not publish this as-is. An accessibility service used for anything other than helping people with
disabilities must pass Play's Permission Declaration Form with a prominent in-app disclosure, and "automation
tools" are explicitly not treated as accessibility tools. It is fine as a personal sideloaded APK.
