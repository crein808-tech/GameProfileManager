# Game Profile Manager

A unified desktop tool for managing NVIDIA driver profiles, DLSS/FSR/XeSS DLL versions, and DLSSTweaks — all from one window.
Something I had a need for and hyperfixated on while leveraging Ai. Not meant to be cutting edge, just working. Please let me know of any changes you would like to see.

PC gamers who tweak GPU settings typically juggle three separate tools: [NVIDIA Profile Inspector](https://github.com/Orbmu2k/nvidiaProfileInspector), [DLSS Swapper](https://github.com/beeradmoore/dlss-swapper), and [DLSSTweaks](https://github.com/emoose/DLSSTweaks). Game Profile Manager brings them together around a shared game list so you can configure everything in one place.

![.NET 8](https://img.shields.io/badge/.NET-8.0-purple) ![Windows](https://img.shields.io/badge/platform-Windows-blue) ![License](https://img.shields.io/badge/license-MIT-green)

## Features

### NVIDIA Profile Builder
- Visual editor with friendly dropdowns instead of raw hex values
- Generates `.nip` profile files compatible with NVIDIA Profile Inspector
- No executable binding conflicts — profiles import cleanly alongside existing NVIDIA profiles

### DLL Swap (DLSS / FSR / XeSS)
- Browse and swap upscaling DLLs to any version from the [DLSS Swapper manifest](https://github.com/beeradmoore/dlss-swapper)
- Supports DLSS (Super Resolution, Frame Gen, Ray Reconstruction), AMD FSR 3.1, and Intel XeSS
- Automatic backup before every swap — one-click revert to original
- **Anti-cheat safety**: automatically blocks DLL swaps for games with Easy Anti-Cheat, BattlEye, or Vanguard

### DLSSTweaks Integration
- Deploy and remove [DLSSTweaks](https://github.com/emoose/DLSSTweaks) (by emoose) to any game directory
- Built-in config editor — no more hand-editing INI files
- Force DLAA, override quality presets, tweak scaling ratios, and more

### General
- Automatic Steam game detection + manual game folder picker
- Persistent settings — remembers your manual games, tool paths, and window layout
- DLL descriptions in dropdowns so you know what each technology does

## Requirements

- Windows 10/11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- [NVIDIA Profile Inspector](https://github.com/Orbmu2k/nvidiaProfileInspector) (for NPI features)
- [DLSSTweaks files](https://github.com/emoose/DLSSTweaks) — `dxgi.dll` + `dlsstweaks.ini` (for DLSSTweaks features)

## Setup

1. Download the latest release from the [Releases](../../releases) page (or build from source — see below).
2. Run `GameProfileManager.exe`.
3. On first launch, configure your tool paths by editing the settings file at:
   ```
   %LocalAppData%\GameProfileManager\settings.json
   ```
   Set the paths to your local copies of the tools:
   ```json
   {
     "NpiExePath": "C:\\Tools\\nvidiaProfileInspector.exe",
     "DlssTweaksDll": "C:\\Tools\\dxgi.dll",
     "DlssTweaksIni": "C:\\Tools\\dlsstweaks.ini"
   }
   ```
4. Restart the app. Your Steam games will be detected automatically. Use **Add Game** for non-Steam games.

## Building from Source

```bash
git clone https://github.com/youruser/GameProfileManager.git
cd GameProfileManager
dotnet build
```

Output: `bin/Debug/net8.0-windows/GameProfileManager.exe`

## How It Works

- **NPI profiles** are generated as `.nip` XML files with decimal SettingID/SettingValue (as NPI's deserializer expects). You import them manually through NPI's GUI.
- **DLL swaps** download versioned DLLs from the public DLSS Swapper manifest, verify the MD5 hash, back up the original, and replace it in the game directory.
- **DLSSTweaks** is deployed by copying the proxy DLL and config INI into the game directory. The built-in editor reads/writes the INI directly.

## Credits

This tool wraps and integrates the work of others:

- [NVIDIA Profile Inspector](https://github.com/Orbmu2k/nvidiaProfileInspector) by Orbmu2k
- [DLSS Swapper](https://github.com/beeradmoore/dlss-swapper) by beeradmoore (manifest source)
- [DLSSTweaks](https://github.com/emoose/DLSSTweaks) by emoose

## License

MIT
