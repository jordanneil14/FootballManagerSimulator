# Football Manager Simulator (Android)

A football management simulation game, built as a single .NET MAUI Blazor Hybrid app for Android.
The UI is Blazor (Razor components running in an on-device WebView) and the whole simulation runs
locally — no server, no internet required.

## Project

`FootballManagerSimulator.Maui` (`net10.0-android`) contains everything:

- **Game logic** — `Models/`, `Helpers/`, `Factories/`, `Competitions/`, `Events/`, `Services/` (incl. `GameEngine`), game `State`.
- **UI** — `Components/` (Razor pages, layouts) and `wwwroot/app.css`.
- **Platform glue** — `MauiAppStorage` reads the bundled `settings.json` / `playerData.json`
  (in `Resources/Raw/`) and stores save games in the app's private data directory.

## Prerequisites

Installed on this machine:
- the `maui-android` .NET workload,
- the Android SDK (`%LOCALAPPDATA%\Android\Sdk`),
- a JDK (`%LOCALAPPDATA%\Android\jdk`).

`Directory.Build.props` at the repo root points the build at those two locations automatically,
so you don't need to pass `-p:AndroidSdkDirectory` / `-p:JavaSdkDirectory` on the command line.

## Build a debug APK

```bash
dotnet build FootballManagerSimulator.Maui -f net10.0-android -c Debug
```

Output APK:

```
FootballManagerSimulator.Maui\bin\Debug\net10.0-android\com.jordanneil.pocketgaffer-Signed.apk
```

Debug builds embed the .NET assemblies in the APK (`EmbedAssembliesIntoApk` in the csproj), so it is
self-contained: copy it to an Android phone and install it (enable "install from unknown sources").
Without that setting, a debug APK has no app code and crashes on launch when installed by hand.

## Deploy to a connected device / emulator

```bash
dotnet build FootballManagerSimulator.Maui -t:Run -f net10.0-android
```

## Publish for the Play Store

```bash
dotnet publish FootballManagerSimulator.Maui -f net10.0-android -c Release
```

Sign the resulting app bundle with your own keystore. (The debug APK above is signed with the debug
key — fine for local installs/testing, not for store submission.)

- **App ID:** `com.jordanneil.pocketgaffer`
- **Display name:** Pocket Gaffer
