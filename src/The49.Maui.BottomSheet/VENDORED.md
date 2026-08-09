# Vendored: The49.Maui.BottomSheet (iOS 26 floating-card fix)

This folder is a vendored copy of [The49.Maui.BottomSheet](https://github.com/the49ltd/The49.Maui.BottomSheet)
(v8.0.3, MIT — see `LICENSE.md`). Upstream is dormant (last release February 2024,
targeting `net8.0-ios17.2`) and has no iOS 26 build, so the source lives in-tree.

## Why

On iOS 26 a partial-height sheet (the calendar-event sheet opens at the **medium**
detent) is rendered as a floating card, but on iPhone the system keeps the card
**attached to the bottom edge with square corners**. The device's rounded display then
clips those bottom corners. See the app's calendar-event page.

## Changes vs upstream 8.0.3

- **`The49.Maui.BottomSheet.csproj`** — retargeted `net8.0-*` → `net10.0-android;net10.0-ios`
  and stripped NuGet packaging metadata (this is a `ProjectReference`, not a package).
- **`Models/ContentDetent.cs`** — `Measure(...)` now returns `Size` in .NET 10, so
  `r.Request.Height` → `r.Height`.
- **`Platforms/iOS/BottomSheetContainer.cs`** — on iOS 26, lift the card above the
  home-indicator curve and mask all four corners round so nothing is clipped
  (`ApplyFloatingCardMask`). Fullscreen stays edge-to-edge; older iOS is unchanged.
- **`Platforms/iOS/BottomSheetViewController.cs`** — on iOS 26, keep the presented view
  transparent so the lifted card floats over the content behind the sheet.

The iOS behavior change is gated behind `OperatingSystem.IsIOSVersionAtLeast(26)`, so
iOS 16–18 keep their original edge-attached rendering.

The upstream-shaped version of the fix is offered back as a pull request:
https://github.com/the49ltd/The49.Maui.BottomSheet/pull/160
