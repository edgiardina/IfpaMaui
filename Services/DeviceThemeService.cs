namespace Ifpa.Services
{
    public interface IDeviceThemeService
    {
        /// <summary>
        ///  Get the current theme, light or dark, not unspecified!
        /// </summary>
        AppTheme GetRequestedTheme();

        /// <summary>
        ///  force a reload of the current theme, in case user chose system default, this will trigger an update from platform specific code
        /// </summary>
        void ReloadRequestedTheme();

        /// <summary>
        /// Get the current theme settings, light, dark or unspecified
        /// </summary>
        AppTheme GetThemeSettings();
        void SaveThemeSettings(AppTheme theme);
    }

    public partial class DeviceThemeService(IPreferences preferences) : IDeviceThemeService
    {
        private readonly IPreferences _preferences = preferences;

        static DeviceThemeService currentImplementation;
        public static DeviceThemeService Instance => currentImplementation ??= new DeviceThemeService(Preferences.Default);


        private AppTheme? runTimeTheme = null;

        private const string IdAppearance = "IdAppearanceV3";

        public void SaveThemeSettings(AppTheme theme)
        {
            int saveValue = (int)theme;
            _preferences.Set(IdAppearance, saveValue);
            ReloadRequestedTheme();
        }

        public void ReloadRequestedTheme()
        {
            try
            {
                var theme = (AppTheme)_preferences.Get(IdAppearance, (int)AppTheme.Unspecified);

                if (theme == AppTheme.Unspecified)
                {
                    theme = GetPlatformTheme();
                }

                runTimeTheme = theme;

                if (Application.Current != null)
                {
                    Application.Current.UserAppTheme = runTimeTheme.Value == AppTheme.Dark ? AppTheme.Dark : AppTheme.Light;
                }
            }
            catch
            {
                runTimeTheme = AppTheme.Light;

                if (Application.Current != null)
                {
                    Application.Current.UserAppTheme = AppTheme.Light;
                }
            }
        }

        public AppTheme GetThemeSettings()
        {
            try
            {
                var theme = (AppTheme)_preferences.Get(IdAppearance, (int)AppTheme.Unspecified);
                return theme;
            }
            catch
            {
                return AppTheme.Light;
            }
        }

        public AppTheme GetRequestedTheme()
        {
            if (runTimeTheme != null && runTimeTheme != AppTheme.Unspecified)
            {
                return runTimeTheme.Value;
            }

            try
            {
                runTimeTheme = (AppTheme)_preferences.Get(IdAppearance, (int)AppTheme.Unspecified);
                if (runTimeTheme == AppTheme.Unspecified)
                {
                    runTimeTheme = GetPlatformTheme();
                }
                return runTimeTheme.Value;
            }
            catch
            {
                return GetPlatformTheme();
            }
        }

        public AppTheme GetPlatformTheme()
        {
#if IOS
            if (UIKit.UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
#pragma warning disable CA1416 // Validate platform compatibility
                var scene = UIKit.UIApplication.SharedApplication.ConnectedScenes.ToArray().FirstOrDefault();
#pragma warning restore CA1416 // Validate platform compatibility
                if (scene != null)
                {
                    var windowScene = (UIKit.UIWindowScene)scene;
#pragma warning disable CA1416 // Validate platform compatibility
                    var userInterfaceStyle = windowScene.TraitCollection.UserInterfaceStyle;
#pragma warning restore CA1416 // Validate platform compatibility

                    return userInterfaceStyle == UIKit.UIUserInterfaceStyle.Dark ? AppTheme.Dark : AppTheme.Light;
                }
            }
            return AppTheme.Light;
#else
            return AppInfo.RequestedTheme;
#endif 
        }
    }
}
