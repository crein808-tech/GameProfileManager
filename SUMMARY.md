# Project State Summary
Last updated: 2026-04-24

## Current Task
GitHub-ready. Code review complete, all critical/medium issues fixed. ModernWpf UI integrated. GPU-aware presets added.

## Decisions Made
- Stack: WPF / C# / .NET 8 (Windows desktop GUI)
- UI: ModernWpf (Fluent dark theme, accent #89B4FA)
- NPI integration: NO silent/programmatic import. NPI v2.4.0.31 `-silentImport` flag does not work reliably. Workflow is: generate .nip file → user imports manually via NPI GUI.
- NPI .nip format: SettingID and SettingValue must be **decimal uint** (not hex). NPI uses .NET XML deserialization which calls `ParseUInt32` with default (decimal) parsing.
- NPI .nip profiles generated WITHOUT executable binding to avoid "already in use" conflicts with existing NVIDIA profiles.
- DLSS/FSR/XeSS swap reimplemented (no CLI available in DLSS Swapper)
- DLSSTweaks integration via file deployment (ini + DLL drop into game dir)
- Revert strategy: automatic backup snapshots before every DLL swap
- DLSS manifest source: `https://beeradmoore.github.io/dlss-swapper/manifest.json`
- Game detection: Steam auto-detect + manual folder picker
- Anti-cheat safety: hard-block DLL swaps for EAC/BattlEye/Vanguard games
- GPU hardware detection via WMI, classified into Budget/Mid/High/Ultra tiers
- NPI presets: Performance, Balanced, Quality, Ultra Quality (last only for High+ GPUs)
- Settings persisted to %LocalAppData%\GameProfileManager\settings.json
- Build output: C:\dev\gpm-build\ (avoids Google Drive sync conflicts)

## Feature Status
| Feature | Status |
|---------|--------|
| Steam game detection | Verified working |
| Manual game add | Verified working |
| Game search/filter | Verified working |
| NPI Profile Builder (visual editor) | Verified working |
| NPI GPU-tier presets | Implemented |
| NPI .nip save | Verified working (decimal format confirmed) |
| NPI Open in NPI workflow | Verified working (profile persists after restart) |
| DLL detection/scan | Verified working |
| DLL version list (manifest) | Verified working |
| DLL swap | Verified working |
| DLL revert | Verified working |
| DLL type/version descriptions | Implemented |
| DLSSTweaks deploy | Verified working |
| DLSSTweaks config editor | Verified working (auto-close after save) |
| DLSSTweaks remove | Verified working |
| DLSSTweaks GPU-tier tips | Implemented |
| Anti-cheat detection | Code-complete, untested |
| Backup manager (DLL backups) | Verified working |
| Settings persistence | Implemented (manual games, tool paths, window state) |
| GPU hardware detection | Implemented (WMI, name-based tier classification) |
| ModernWpf UI | Integrated (Fluent dark theme) |

## Code Review Fixes Applied (2026-04-24)
1. Process handle leak in NpiService.LaunchNpi() — now disposed
2. Unused constructor parameter in NpiService — removed
3. Fire-and-forget async in DllRevert_Click — now properly awaited
4. HttpClient timeouts — 30s for manifest, 60s for DLL downloads
5. Steam ACF parsing crash on locked/deleted files — wrapped in try-catch
6. DllTypeMap consolidated from 3 parallel dictionaries into single record source
7. Redundant hardcoded Steam path removed (dynamic Environment.GetFolderPath only)

## Next Action
1. Create GitHub repo and push
2. Take screenshots for README
3. Build release binary
4. Settings UI for tool paths (currently editable only via settings.json)
