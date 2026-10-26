# BundleToolGUI

A small Windows GUI for turning an Android App Bundle (`.aab`) into a universal APK, with an option to install it on a connected Android device.

## Download

Run [`BundleToolGUI.exe`](BundleToolGUI.exe).

The C# rewrite is available at [`BundleToolGUIReplacement.cs`](BundleToolGUIReplacement.cs).

## How to use it

1. Open the app and choose an `.aab` file.
2. Choose a signing key:
   - **Debug keystore** creates and uses a local debug key.
   - **Custom keystore** lets you select your own keystore, enter its password, and provide or fetch the alias.
3. Choose **Build APK** to create the files, or **Build and Install** to build and install the APK on a connected device.
4. By default, the output files are saved directly in the same directory as the selected AAB. You can choose another output folder if you prefer. The app opens the output folder automatically when the build or installation completes.

The output names match the AAB's base name. For example, `MyApp.aab` produces `MyApp.apk` and `MyApp.apks`.

## First run and requirements

The app does not need `python39.dll`. On first use it downloads and caches:

- Temurin Java 17
- Google Bundletool 1.18.3
- Android platform-tools (ADB), when you choose **Build and Install**

An internet connection is needed for the initial downloads. For installation, connect an Android device, enable USB debugging, and authorize the computer when prompted. The app targets Windows 10/11 and uses the .NET Framework components available on Windows.

## About this rewrite

AI was used to inspect the original Python-based app and rewrite its recovered behavior in C#. The original executable contained compiled Python bytecode rather than the original source, so this is a reconstruction of the app's workflow, not a line-for-line source translation.



