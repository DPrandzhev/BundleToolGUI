# BundleToolGUI


This is a Python-free Windows replacement for the recovered AAB-to-APK GUI.

- `BundleToolGUI_Final.exe` is the updated app. It does not need `python39.dll` and explicitly enables TLS 1.2 for downloads.
- The first build downloads Temurin Java 17 and Google Bundletool 1.18.3 from their official sources into `%LOCALAPPDATA%\BundleToolGUI`.
- `Build and Install` also downloads Android platform-tools (ADB). USB debugging must be enabled and the phone must authorize this computer.
- Choose a debug keystore or provide a custom keystore. The output files use the input AAB's basename: `<name>.apk` and `<name>.apks`.
- `BundleToolGUIReplacement.cs` is the C# source.

The executable targets Windows and uses the .NET Framework components included with current Windows 10/11. It compiled successfully, but I have not run an end-to-end conversion.
