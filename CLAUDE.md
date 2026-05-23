# IFPA MAUI App — Agent Instructions

## AI Agent Interaction Preferences

- Provide direct, actionable responses without validation requests or meta-commentary
- No confirmation prompts like "If you'd like that, just say 'Yes...'" or similar forced-choice confirmations
- Assume the developer is capable of deciding next steps without nudges or permission-seeking prompts
- Use terse, direct instructions — follow-up questions will be asked if needed
- No commentary on behavior, thought process, or tool usage unless explicitly requested
- NEVER USE "You're absolutely right" or any other filler
- Assume the user could be wrong and say so, if so
- Remove filler statements and implied permission requests from responses

## Project Overview

**IFPA (International Flipper Pinball Association) Companion App** — a .NET MAUI cross-platform mobile app targeting iOS and Android. Provides tournament information, rankings, player statistics, and news from the IFPA pinball community.

- Package ID: `com.edgiardina.ifpa`
- Primary API host: `api.ifpapinball.com`

## Technology Stack

- **.NET 10** (iOS/Android only — no Windows/Mac targets)
- **.NET MAUI** with Shell navigation
- **MVVM** via CommunityToolkit.Mvvm
- **Dependency Injection** via Microsoft.Extensions.DependencyInjection
- **SQLite** for local data storage
- **Serilog** + **Microsoft.Extensions.Logging** (`ILogger<T>`) for structured logging
- **Shiny** for background services and notifications
- `CommunityToolkit.Maui` & `CommunityToolkit.Mvvm`
- `PinballApi` for IFPA API integration
- `LiveChartsCore.SkiaSharpView.Maui` for charts
- `Syncfusion.Maui.Toolkit` for UI components
- `The49.Maui.BottomSheet` for modal presentations

## Logging — Always Use the Structured Logger

**Always prefer `ILogger<T>` (Microsoft.Extensions.Logging) over any platform-specific logging API.**

- Inject `ILogger<T>` via constructor DI for instance classes
- For static methods (e.g., Android widget `RequestUpdate` helpers), resolve from DI: `IPlatformApplication.Current?.Services?.GetService<ILogger<T>>()`
- Never use `Android.Util.Log`, `Console.WriteLine`, `Debug.WriteLine`, or `Serilog.Log.Logger` directly in application code
- Use structured message templates: `logger.LogDebug("Loaded {Count} items", count)` — not string interpolation
- The only exception is `ILogger` not yet being available (e.g., very early bootstrap) — document why if so

### Correct pattern
```csharp
// Instance class
public class MyService
{
    private readonly ILogger<MyService> _logger;
    public MyService(ILogger<MyService> logger) => _logger = logger;

    public void DoWork() => _logger.LogDebug("Did {Thing}", thing);
}

// Static helper (Android widget pattern)
public static void RequestUpdate(Context context)
{
    var logger = IPlatformApplication.Current?.Services?.GetService<ILogger<MyWidget>>();
    logger?.LogDebug("RequestUpdate called — {Count} widget(s)", ids?.Length ?? 0);
}
```

### Wrong
```csharp
Android.Util.Log.Debug("tag", "message");   // ❌ platform-specific
Log.Logger.Information("message");           // ❌ Serilog static
Console.WriteLine("message");               // ❌
```

## Project Structure

### ViewModels
- All inherit from `BaseViewModel`
- Use `[ObservableProperty]` for bindable properties, `[RelayCommand]` for commands
- Registered via `AddAllFromNamespace<BaseViewModel>()`
- Always handle exceptions; use `logger.LogError(ex, "message")` with context

### Views (Pages)
- Registered via `AddAllFromNamespace<RankingsPage>()`
- Shell navigation: `await Shell.Current.GoToAsync($"route?param={value}")`
- Use `[QueryProperty]` for nav parameters
- Use compiled bindings: `x:DataType`

### Services
- Interface-based singletons
- `IPinballRankingApi` wrapped by `CachingPinballRankingApi`
- Cross-platform event signaling uses `WeakReferenceMessenger` (CommunityToolkit.Mvvm) — avoid `#if ANDROID` callsites in shared Views

### Android Widgets
- Implemented in C# under `Platforms/Android/Widgets/`
- Use `WeakReferenceMessenger` messages fired from shared code; `MainActivity` registers handlers that call static `RequestUpdate` on each widget
- Current messages: `MyStatsPlayerChangedMessage` (rank widget), `CalendarFilterChangedMessage` (calendar widget)

## Coding Standards

### C#
- Modern C# / .NET 10 features
- Microsoft naming conventions
- `partial` classes for source generators
- `async/await` over `.Result` / `.Wait()`
- `using` statements for disposables

### XAML
- Compiled bindings with `x:DataType`
- `AppThemeBinding` for light/dark support
- FluentUI icons: `FluentIcon.IconName` with `FluentRegular`/`FluentFilled` fonts
- If an icon isn't in `IconFonts.xaml`, find hex codes at: https://github.com/AathifMahir/MauiIcons/blob/master/src/MauiIcons.Fluent/Icons/FluentIcons.cs

### UI
- Default/native look; original inspiration was the iOS Mail app
- Avoid images that bloat app size; prefer vector icons

### Error Handling
- Wrap in try-catch inside ViewModels; don't let exceptions crash the app
- `logger.LogError(ex, "context message")`
- User-facing errors via `Shell.Current.DisplayAlert`

### Performance
- Use `CachingPinballRankingApi` for caching
- `CollectionView` over `ListView`
- Proper loading states with `IsBusy`

## Platform Specifics

- iOS 16.0+, Android API 21+
- Conditional compilation: `#if IOS`, `#if ANDROID` — only use in `Platforms/` folders, not shared Views
- iOS widgets: Swift (NativeIFPA project)
- Android widgets: C# (`Platforms/Android/Widgets/`)
- Android SSL: `api.ifpapinball.com` uses a ZeroSSL/Sectigo cert not in older Android trust stores — handled via `network_security_config.xml` + bundled `sectigo_root_r46.pem`

## Localization

- All user-facing strings via `Strings.resx`
- Access as `Strings.ResourceKey`

## Background Services

- Shiny framework for background jobs
- `NotificationJob` handles background notifications

## Branch Workflow

**NEVER work directly on `main` unless explicitly asked.**

```bash
git checkout -b feature/your-feature-name
git push -u origin feature/your-feature-name
```

## Build & Deploy to Android Emulator

**Always deploy via dotnet toolchain, not raw `adb install`.** Raw `adb install` installs the APK but skips the Fast Deployment assembly push, causing the app to crash at launch with "No assemblies found".

```bash
# Correct — handles Fast Deployment
dotnet build -t:Run -f net10.0-android -p:AndroidAttachDebugger=false

# If XA3006 typemaps error (stale artifact), add --no-incremental
dotnet build -t:Run -f net10.0-android -p:AndroidAttachDebugger=false --no-incremental

# To uninstall before a clean deploy
$env:PATH = "$env:LOCALAPPDATA\Android\Sdk\platform-tools;$env:PATH"
adb -s emulator-5554 uninstall com.edgiardina.ifpa
```

## Log Monitoring

```powershell
$env:PATH = "$env:LOCALAPPDATA\Android\Sdk\platform-tools;$env:PATH"

# Clear before a test
adb -s emulator-5554 logcat -c

# Dump filtered crash info
$result = adb -s emulator-5554 logcat -d 2>&1
($result -split "`n") | Where-Object { $_ -match "FATAL|AndroidRuntime|com.edgiardina" -and $_ -notmatch "AppsFilter|BLOCKED" }

# App-level structured logs (Serilog output)
($result -split "`n") | Where-Object { $_ -match "com\.edgiardina\.ifpa.*DBG|INF|WRN|ERR" -and $_ -notmatch "AppsFilter" }
```

## Common ViewModel Pattern

```csharp
public partial class SampleViewModel : BaseViewModel
{
    [ObservableProperty]
    private string title = "Sample";

    [RelayCommand]
    private async Task LoadData()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            // load data
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading data");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

## Troubleshooting

### App crashes at launch — "No assemblies found"
Fast Deployment mismatch. Uninstall and redeploy via `dotnet build -t:Run` (not `adb install`).

### XA3006 — typemaps.x86_64.ll not found
Stale build artifact. Add `--no-incremental` to the build command.

### SSL / CertPathValidatorException on api.ifpapinball.com
The Sectigo root cert is bundled in `Platforms/Android/Resources/raw/sectigo_root_r46.pem` and trusted via `network_security_config.xml`. If this error reappears, confirm the manifest still references `android:networkSecurityConfig="@xml/network_security_config"`.

### MauiOnBackPressedCallback crash after adb install
Stale incremental install. Run `adb uninstall com.edgiardina.ifpa` then redeploy via dotnet toolchain.

### AppsFilter noise drowning logcat output
Filter it out: `Where-Object { $_ -notmatch "AppsFilter|BLOCKED" }`
