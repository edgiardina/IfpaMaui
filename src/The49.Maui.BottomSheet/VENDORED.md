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
- **`Platforms/iOS/BottomSheetContainer.cs`** — on iOS 26, mask the card so its **bottom
  corners** are rounded enough to sit inside the device's rounded display instead of being
  clipped (`ApplyFloatingCardMask`, `BuildCardPath` with independent top/bottom radii). The
  top corners keep the sheet's own `CornerRadius`; fullscreen is unchanged; older iOS is
  unchanged. (An earlier attempt lifted the card off the bottom, but that exposed the system
  sheet's shadow/material below it — rounding the corners in place avoids that.)
- **`Platforms/iOS/BottomSheetViewController.cs`** — on iOS 26, keep the presented view
  transparent so the rounded corner cut-outs show the content behind the sheet.

The iOS behavior change is gated behind `OperatingSystem.IsIOSVersionAtLeast(26)`, so
iOS 16–18 keep their original edge-attached rendering.

The upstream-shaped version of the fix is offered back as a pull request:
https://github.com/the49ltd/The49.Maui.BottomSheet/pull/160
