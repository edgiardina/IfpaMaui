# IFPA MAUI App - Development Guide

## 🚨 CRITICAL: Always Work in Branches

**NEVER work directly on main unless specifically asked to do otherwise.**

Before starting any work:
```bash
git checkout -b feature/your-feature-name
git push -u origin feature/your-feature-name
```

## 🚀 App Launch & Testing Workflow

### 1. Pre-Flight Checks
```bash
# Check if emulator is running
adb devices

# If no devices, start Android emulator first
# (Use Android Studio Device Manager or command line)
```

### 2. Build & Deploy
```bash
# Clean build (if needed)
dotnet clean

# Build and run on Android
dotnet run --framework net10.0-android

# Expected output: "deployed successfully" with process ID
```

### 3. Monitor Logs (CRITICAL - Always Do This)
```bash
# Start continuous log monitoring in separate terminal
adb logcat -s mono-stdout:* ActivityManager:* AndroidRuntime:* System.err:*

# Alternative: Filter for crashes only
adb logcat -s AndroidRuntime:E System.err:E
```

### 4. Verify App is Running
```bash
# Check if app process is active
adb shell ps | grep ifpamaui

# Expected: Process ID should match deployment output
# Example: u0_a123   8368  1247 com.companyname.ifpamaui
```

## 📱 Screenshot Workflow

### Basic Screenshots
```bash
# Take screenshot
adb exec-out screencap -p > screenshot-$(Get-Date -Format "yyyy-MM-dd-HHmmss").png

# Alternative with timestamp
adb shell screencap -p /sdcard/screenshot.png && adb pull /sdcard/screenshot.png screenshot-current.png
```

### Navigation & Testing Screenshots
```bash
# 1. Home screen
adb exec-out screencap -p > screenshot-home.png

# 2. Navigate to Rankings (if testing player pages)
adb shell input tap 400 600  # Adjust coordinates as needed

# 3. Take screenshot after navigation
adb exec-out screencap -p > screenshot-rankings.png

# 4. Navigate to specific player (search or tap)
# Use input text for search: adb shell input text "player name"
# Use input tap for UI elements: adb shell input tap X Y

# 5. Screenshot final result
adb exec-out screencap -p > screenshot-player-detail.png
```

## 🔍 Common Navigation Patterns

### Accessing Player Detail Page
1. **Via Search**: 
   - Tap search icon/field
   - Input player name: `adb shell input text "playerName"`
   - Tap search result

2. **Via Rankings**:
   - Navigate to Rankings tab
   - Scroll and tap on player entry
   - Use coordinate taps: `adb shell input tap X Y`

3. **Direct Navigation** (for testing):
   - Consider adding test navigation commands in app
   - Use Shell.GoToAsync with test player IDs

### Screen Coordinates Helper
```bash
# Get screen resolution
adb shell wm size

# Get touch coordinates (enable Developer Options > Pointer Location)
# Common areas:
# - Tab bar: Usually bottom 100px
# - Search: Often top-right corner
# - List items: Center of visible area
```

## 🐛 Troubleshooting Guide

### App Won't Start
```bash
# Check build errors
dotnet build --verbosity normal

# Check deployment logs
adb logcat -s *:E

# Force stop and retry
adb shell am force-stop com.companyname.ifpamaui
dotnet run --framework net10.0-android
```

### App Crashes
```bash
# Monitor crash logs
adb logcat -s AndroidRuntime:E System.err:E

# Common issues:
# - Missing XAML resources (StaticResource not found)
# - Null reference exceptions in ViewModels
# - Navigation parameter issues
```

### XAML Resource Issues
- **Error**: `StaticResource not found for key 'ResourceName'`
- **Solution**: Check Resources/Styles/Colors.xaml for available resources
- **Fix**: Use existing resources or add new ones to Colors.xaml
- **Common fixes**:
  - `CardBackgroundDark` → Use `SecondaryBanner` or define new resource
  - `Gray200/600/900` → Use `PrimaryTextColor`, `SecondaryTextColor`, etc.

### Navigation Issues
```bash
# Check Shell routes registration
# Look in AppShell.xaml and MauiProgram.cs

# Test direct navigation from logs
# Shell.GoToAsync should show in mono-stdout logs
```

## ⚡ Performance Tips

### Fast Development Cycle
1. Use hot reload when possible: `dotnet watch run`
2. Keep adb logcat running in separate terminal
3. Use specific log filters to reduce noise
4. Take screenshots at each major step
5. Keep emulator running between builds

### Screenshot Organization
```bash
# Use descriptive names
screenshot-before-changes.png
screenshot-after-hero-section.png
screenshot-final-result.png
screenshot-error-state.png
```

## 📋 Testing Checklist

Before considering work complete:
- [ ] App builds without errors
- [ ] App launches and shows process ID
- [ ] No crash logs in adb logcat
- [ ] Can navigate to target feature
- [ ] Screenshots taken of key states
- [ ] Feature works as expected
- [ ] No regressions in basic navigation

## 🎯 Page-Specific Testing

### Player Detail Page
```bash
# Navigate via search
adb shell input text "player name"
adb shell input keyevent 66  # Enter key
# Screenshot result

# Navigate via rankings
# Tap rankings → scroll → tap player
# Screenshot result

# Test features
# - Hero section display
# - Statistics cards
# - Action buttons functionality
# - Chart rendering
# - Theme switching
```

## 💡 Pro Tips

1. **Always monitor logs** - Start `adb logcat` before any app interaction
2. **Take screenshots early** - Capture states before and after changes
3. **Use descriptive git commits** - Include what was tested and how
4. **Test on clean deployment** - Restart app to verify changes persist
5. **Check both themes** - Light and dark mode compatibility
6. **Verify data loading** - Test with real API responses when possible

## 🔧 Emergency Recovery

If everything breaks:
```bash
# Nuclear option - clean slate
adb shell am force-stop com.companyname.ifpamaui
adb uninstall com.companyname.ifpamaui
dotnet clean
dotnet build
dotnet run --framework net10.0-android
```

Remember: **Screenshot everything, monitor logs always, work in branches.**