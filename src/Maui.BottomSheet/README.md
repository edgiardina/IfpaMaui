# Plugin.Maui.BottomSheet (vendored)

This folder holds a copy of the source of [Plugin.Maui.BottomSheet](https://github.com/lucacivale/Maui.BottomSheet) by Luca Civale (MIT license, see `LICENSE`). The app uses this copy through a `ProjectReference`. It does not use the NuGet package.

## Source

- Fork: https://github.com/edgiardina/Maui.BottomSheet
- Branch: `ifpa`
- Base: upstream `main` at `11e0581` (version 10.0.7 plus 6 upstream commits)

The `ifpa` branch adds these fixes to upstream `main`:

| Fork branch | Fix | Upstream |
| --- | --- | --- |
| `fix/isopen-false-ignores-iscancelable` | `IsOpen = false` closes a sheet when `IsCancelable` is false. Also fixes an iOS crash when the sheet opens again. | Issue 257, PR 258 |
| `fix/nonmodal-touch-popups` | On Android, a non-modal sheet sends touches to the popups below it. Toolbar overflow menu items work again, and a touch outside a popup closes it. | Not sent |

## Changes to the upstream project files

- The projects target `net11.0-android` and `net11.0-ios` only.
- The Android minimum is API 24, the .NET 11 minimum.
- `Directory.Build.props` and `Directory.Packages.props` in this folder replace the upstream files. They turn off the analyzers and packaging.
- The Windows project, the samples and the tests are not in this copy.
- Keep the package versions in `Directory.Packages.props` the same as in `src/IfpaMaui/IfpaMaui.csproj`.

## Update this copy

1. Rebase or merge the fix branches in the fork onto the new upstream `main`.
2. Merge them into the `ifpa` branch.
3. Copy `src/Plugin.BottomSheet`, `src/Plugin.BottomSheet.Android`, `src/Plugin.BottomSheet.iOSMacCatalyst` and `src/Plugin.Maui.BottomSheet` from the `ifpa` branch into this folder.
4. Do the project file changes again.
5. Update the base commit in this file.

When upstream releases all the fixes, remove this folder and use the NuGet package again.
