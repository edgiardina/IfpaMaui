namespace Ifpa.Models
{
    // Minimal stand-in for the app's Ifpa.Models.Settings.
    //
    // The real Settings is a static class that pulls in MAUI (Preferences, FileSystem) and
    // several app services. The linked SQLiteCacheProvider only reads a single member from it —
    // Settings.CacheDuration — so this shim provides just that, with the same value the app uses.
    // This lets the cache provider compile and run on a plain desktop test runner without MAUI.
    internal static class Settings
    {
        public static TimeSpan CacheDuration = TimeSpan.FromDays(30);
    }
}
