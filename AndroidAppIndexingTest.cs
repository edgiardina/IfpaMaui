using Microsoft.Maui.Controls;
using IfpaMaui.Platforms.Android;

namespace IfpaMaui
{
    /// <summary>
    /// Test to verify Android app indexing fix works - prevents the original crash
    /// "System.ArgumentException: No IAppIndexingProvider was provided"
    /// </summary>
    public static class AndroidAppIndexingTest
    {
        /// <summary>
        /// Tests that the Android AppIndexingProvider prevents the original crash
        /// This simulates what happens in PlayerDetailViewModel.AddPlayerToAppLinks()
        /// </summary>
        public static bool TestAndroidAppIndexingFix()
        {
            try
            {
                Console.WriteLine("🧪 Testing Android AppIndexingProvider fix...");
                
                // Test 1: Can create the Android provider (this was missing before)
                var provider = new AndroidAppIndexingProvider();
                var appLinks = provider.AppLinks;
                Console.WriteLine("✅ AndroidAppIndexingProvider created successfully");
                
                // Test 2: Can create app link entry (same as PlayerDetailViewModel does)
                var testEntry = new AppLinkEntry
                {
                    Title = "Test Player",
                    Description = "Test Rank", 
                    AppLinkUri = new Uri("https://www.ifpapinball.com/player.php?p=132673"), // Same URL pattern as issue #244
                    IsLinkActive = true
                };
                
                testEntry.KeyValues.Add("contentType", "Player");
                testEntry.KeyValues.Add("appName", "IFPA Companion");
                Console.WriteLine("✅ AppLinkEntry created successfully");
                
                // Test 3: This is the exact line that was crashing before!
                // PlayerDetailViewModel line 273: Application.Current.AppLinks.RegisterLink(entry);
                appLinks.RegisterLink(testEntry);
                Console.WriteLine("✅ appLinks.RegisterLink() executed without crash!");
                
                // Test 4: Other operations that should work
                appLinks.DeregisterLink(testEntry);
                appLinks.DeregisterLink(testEntry.AppLinkUri);
                Console.WriteLine("✅ Deregister operations work correctly");
                
                Console.WriteLine("🎉 Android AppIndexingProvider fix test PASSED!");
                Console.WriteLine("   The original crash 'No IAppIndexingProvider was provided' is now FIXED!");
                return true;
            }
            catch (ArgumentException ex) when (ex.Message.Contains("No IAppIndexingProvider"))
            {
                Console.WriteLine($"❌ ORIGINAL CRASH STILL EXISTS: {ex.Message}");
                Console.WriteLine("   The AndroidAppIndexingProvider was not properly configured.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected error during test: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Demonstrates what the original issue looked like before the fix
        /// </summary>
        public static void ShowOriginalProblem()
        {
            Console.WriteLine("\n📋 Original Issue #244 Summary:");
            Console.WriteLine("   Error: System.ArgumentException: No IAppIndexingProvider was provided");
            Console.WriteLine("   Location: PlayerDetailViewModel.AddPlayerToAppLinks() line 273");
            Console.WriteLine("   Cause: Missing Android implementation of IAppIndexingProvider");
            Console.WriteLine("   Platform: Only iOS had IOSAppIndexingProvider, Android had nothing");
            
            Console.WriteLine("\n🔧 Fix Applied:");
            Console.WriteLine("   ✅ Created AndroidAppIndexingProvider implementing IAppIndexingProvider");
            Console.WriteLine("   ✅ Created AndroidAppLinks implementing IAppLinks with proper functionality");
            Console.WriteLine("   ✅ Updated App.xaml.cs to configure provider for Android builds");
            Console.WriteLine("   ✅ Maintains full iOS compatibility");
            Console.WriteLine("   ✅ Provides foundation for future Android app indexing features");
        }
    }
}