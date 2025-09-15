# GitHub Copilot Instructions for IFPA .NET MAUI App

## AI Agent Interaction Preferences

### Response Style
- Provide direct, actionable responses without validation requests or meta-commentary
- No confirmation prompts like "If you'd like that, just say 'Yes...'" or similar forced-choice confirmations
- Assume the developer is capable of deciding next steps without nudges or permission-seeking prompts
- Use terse, direct instructions - follow-up questions will be asked if needed
- No commentary on behavior, thought process, or tool usage unless explicitly requested
- NEVER USE "You're absolutely right" or any other garbage as response
- Assume the user could be wrong and say so, if so
- Remove filler statements and implied permission requests from responses

## Project Overview
This is the **IFPA (International Flipper Pinball Association) Companion App** - a .NET MAUI cross-platform mobile application targeting iOS and Android. The app provides pinball players with tournament information, rankings, player statistics, and news from the IFPA community.

## Technology Stack
- **.NET 9** (Primary target framework) (iOS/Android only)
- **.NET MAUI** (Multi-platform App UI)
- **MVVM Pattern** with CommunityToolkit.Mvvm
- **Shell Navigation** with routing
- **Dependency Injection** with Microsoft.Extensions.DependencyInjection
- **SQLite** for local data storage
- **Serilog** for logging
- **Shiny** for background services and notifications

## Key Dependencies
- `CommunityToolkit.Maui` & `CommunityToolkit.Mvvm` for MAUI extensions and MVVM
- `PinballApi` (v3.1.15) for IFPA API integration
- `System.ServiceModel.Syndication` for RSS/blog feeds
- `LiveChartsCore.SkiaSharpView.Maui` for data visualization
- `Syncfusion.Maui.Toolkit` for UI components
- `The49.Maui.BottomSheet` for modal presentations

## Project Structure Guidelines

### ViewModels
- All ViewModels inherit from `BaseViewModel`
- Use `[ObservableProperty]` attributes for bindable properties
- Use `[RelayCommand]` attributes for commands
- ViewModels are registered in DI container via `AddAllFromNamespace<BaseViewModel>()`
- Follow async/await patterns for data operations
- Always handle exceptions and use logging

### Views (Pages)
- All pages registered in DI container via `AddAllFromNamespace<RankingsPage>()`
- Use Shell navigation: `await Shell.Current.GoToAsync($"route?param={value}")`
- Implement `[QueryProperty]` attributes for navigation parameters
- Use data binding with `x:DataType` for compiled bindings
- Follow MAUI styling patterns with ResourceDictionaries

### Navigation
- Shell-based navigation with route registration
- Use route parameters for passing data between pages
- Example: `await Shell.Current.GoToAsync($"player-details?playerId={playerId}")`

### Services
- Interface-based services registered as singletons
- `IPinballRankingApi` wrapped with `CachingPinballRankingApi`
- `NotificationService` for push notifications
- `BlogPostService` for RSS feed processing

## Coding Standards

### C# Conventions
- Use modern C# features (.NET 9)
- Follow Microsoft naming conventions
- Use `partial` classes for source generators (MVVM)
- Include XML documentation for public APIs
- Use `using` statements for resource disposal
- Prefer `async/await` over `.Result` or `.Wait()`

### XAML Conventions
- Use compiled bindings with `x:DataType`
- Implement proper styling with ResourceDictionaries
- Use `AppThemeBinding` for light/dark theme support
- Follow accessibility best practices
- Use FluentUI icons: `FluentIcon.IconName` with `FluentRegular`/`FluentFilled` fonts
- Implement proper layout containers for responsive design

### UI considerations
- Try to create a UI which looks "default" or "native" on the platform. My original inspiration was the iOS Mail app.
- When possible, use Fluent UI icons for consistency
- If an icon doesn't exist in IconFonts.xaml, examine appropriate hex codes here: https://github.com/AathifMahir/MauiIcons/blob/master/src/MauiIcons.Fluent/Icons/FluentIcons.cs
- Avoid using images, which may bloat the app size, whenever possible.

### Error Handling
- Always wrap operations that can throw in try-catch blocks from the ViewModel; never let exceptions crash the app
- Use structured logging with Serilog
- Display user-friendly error messages via `Shell.Current.DisplayAlert`
- Log exceptions with context using `logger.LogError(ex, "message")`

### Performance
- Use caching appropriately (see `CachingPinballRankingApi`)
- Implement proper loading states with `IsBusy` properties
- Use `CollectionView` instead of `ListView` for better performance
- Optimize images and use vector icons where possible

## Platform-Specific Considerations
- Target iOS 16.0+ and Android API 21+
- Use conditional compilation for platform-specific code: `#if IOS`, `#if ANDROID`
- Handle platform-specific renderers in `ConfigureMauiHandlers`
- iOS widgets implemented in Swift (NativeIFPA project)
- Android widgets implemented in C# (Platforms/Android/Widgets)

## API Integration
- Use `IPinballRankingApi` for all IFPA data
- API responses are cached via `CachingPinballRankingApi`
- Handle API rate limiting and network failures gracefully
- Use proper HttpClient configuration with User-Agent headers

## Localization
- Use resource files (`Strings.resx`) for all user-facing text
- Access strings via `Strings.ResourceKey` pattern
- Support for multiple languages through resource files

## Background Services
- Use Shiny framework for background jobs
- `NotificationJob` handles background notifications
- Implement proper lifecycle management for services

## Data Binding Patterns
- Use `ObservableProperty` for simple properties
- Use `ObservableCollection<T>` for lists
- Implement `INotifyPropertyChanged` via CommunityToolkit.Mvvm
- Use converters for data transformation in XAML

## Testing Considerations
- Services are interface-based for easy mocking
- ViewModels are testable through dependency injection
- Use proper separation of concerns for unit testing

## Common Patterns to Follow

### ViewModel Pattern
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
            // Load data
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

### Navigation Pattern
```csharp
[RelayCommand]
public async Task NavigateToDetail(int id)
{
    await Shell.Current.GoToAsync($"detail-page?id={id}");
}

[QueryProperty(nameof(Id), "id")]
public partial class DetailViewModel : BaseViewModel
{
    [ObservableProperty]
    private int id;
}
```

### XAML Styling Pattern
```xaml
<ContentPage x:DataType="vm:SampleViewModel">
    <ContentPage.Resources>
        <ResourceDictionary>
            <Style x:Key="HeaderStyle" TargetType="Label">
                <Setter Property="FontSize" Value="24" />
                <Setter Property="FontAttributes" Value="Bold" />
            </Style>
        </ResourceDictionary>
    </ContentPage.Resources>
</ContentPage>
```

## Important Notes
- Always use .NET MAUI APIs, never Xamarin.Forms
- Prefer CommunityToolkit.Mvvm over manual INotifyPropertyChanged implementation
- Use proper resource management and disposal patterns
- Follow mobile-first design principles
- Test on both iOS and Android platforms
- Consider accessibility in all UI implementations