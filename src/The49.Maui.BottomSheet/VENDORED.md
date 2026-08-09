# Vendored: The49.Maui.BottomSheet (iOS 26 sheet-sizing fix)

This folder is a vendored copy of [The49.Maui.BottomSheet](https://github.com/the49ltd/The49.Maui.BottomSheet)
(v8.0.3, MIT — see `LICENSE.md`). Upstream is dormant (last release February 2024,
targeting `net8.0-ios17.2`) and has no iOS 26 build, so the source lives in-tree.

## Why

On iOS 26 the calendar-event sheet (which opens at the **medium** detent) rendered as an
inset card whose square bottom corners the device's rounded display then clipped, with
the map showing in a margin down both sides. A pristine native `UISheetPresentationController`
sheet on iOS 26 does **not** do this: it is edge-to-edge, and its bottom corners follow the
device curve automatically.

## Root cause

iOS 26 sizes (and insets) a sheet from its content's measured size. The49 sizes the content
view to the **tallest** detent height so that dragging between detents does not re-layout the
content. On iOS 26 that oversized/overflowing content makes the system present the sheet as an
inset card instead of the native edge-to-edge sheet.

## Changes vs upstream 8.0.3

- **`The49.Maui.BottomSheet.csproj`** — retargeted `net8.0-*` → `net10.0-android;net10.0-ios`
  and stripped NuGet packaging metadata (this is a `ProjectReference`, not a package).
- **`Models/ContentDetent.cs`** — `Measure(...)` now returns `Size` in .NET 10, so
  `r.Request.Height` → `r.Height`.
- **`Platforms/iOS/BottomSheetContainer.cs`** — on iOS 26, size the content view to the card
  (`_view.Frame = Bounds`) instead of the tallest-detent height. This lets iOS render the
  native edge-to-edge sheet and handle the bottom corners per the device. Earlier iOS keeps
  the original (no-relayout) behavior. This is the whole fix — no corner masking, no
  device-specific radii.

The behavior change is gated behind `OperatingSystem.IsIOSVersionAtLeast(26)`, so iOS 16–18
are unchanged.

The upstream-shaped version of the fix is offered back as a pull request:
https://github.com/the49ltd/The49.Maui.BottomSheet/pull/160
