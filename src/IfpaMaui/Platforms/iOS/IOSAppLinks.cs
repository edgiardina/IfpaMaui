using System;
using System.Threading.Tasks;
using CoreSpotlight;
using Foundation;
using ObjCRuntime;
using UIKit;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;

namespace IfpaMaui.Platforms.iOS
{
    internal class IOSAppLinks : IAppLinks
    {
        public async void DeregisterLink(IAppLinkEntry appLink)
        {
            if (string.IsNullOrWhiteSpace(appLink.AppLinkUri?.ToString()))
                throw new ArgumentNullException("AppLinkUri");

            await RemoveLinkAsync(appLink.AppLinkUri?.ToString());
        }

        public async void DeregisterLink(Uri uri)
        {
            if (string.IsNullOrWhiteSpace(uri?.ToString()))
                throw new ArgumentNullException(nameof(uri));

            await RemoveLinkAsync(uri.ToString());
        }

        public async void RegisterLink(IAppLinkEntry appLink)
        {
            if (string.IsNullOrWhiteSpace(appLink.AppLinkUri?.ToString()))
                throw new ArgumentNullException("AppLinkUri");

            await AddLinkAsync(appLink);
        }

        public async void DeregisterAll()
        {
            await ClearIndexedDataAsync();
        }

        static async Task AddLinkAsync(IAppLinkEntry deepLinkUri)
        {
            var appDomain = NSBundle.MainBundle.BundleIdentifier;
            string contentType, associatedWebPage;
            bool shouldAddToPublicIndex;

            TryGetValues(deepLinkUri, out contentType, out associatedWebPage, out shouldAddToPublicIndex);

            var id = deepLinkUri.AppLinkUri.ToString();

            var searchableAttributeSet = await GetAttributeSet(deepLinkUri, contentType, id);
            var searchItem = new CSSearchableItem(id, appDomain, searchableAttributeSet);
            await IndexItemAsync(searchItem);

            var activity = new NSUserActivity($"{appDomain}.{contentType}")
            {
                Title = deepLinkUri.Title,
                EligibleForSearch = true
            };

            if (!string.IsNullOrEmpty(associatedWebPage))
                activity.WebPageUrl = new NSUrl(associatedWebPage);

            activity.EligibleForPublicIndexing = shouldAddToPublicIndex;

            activity.UserInfo = GetUserInfoForActivity(deepLinkUri);
            activity.ContentAttributeSet = searchableAttributeSet;

            if (deepLinkUri.IsLinkActive)
                activity.BecomeCurrent();

            if (deepLinkUri is AppLinkEntry aL)
            {
                aL.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == AppLinkEntry.IsLinkActiveProperty.PropertyName)
                    {
                        if (aL.IsLinkActive)
                            activity.BecomeCurrent();
                        else
                            activity.ResignCurrent();
                    }
                };
            }
        }

        static Task<bool> ClearIndexedDataAsync()
        {
            var tcs = new TaskCompletionSource<bool>();

            if (CSSearchableIndex.IsIndexingAvailable)
                CSSearchableIndex
                    .DefaultSearchableIndex
                    .DeleteAll(error => tcs.TrySetResult(error == null));
            else
                tcs.TrySetResult(false);

            return tcs.Task;
        }

        static async Task<CSSearchableItemAttributeSet> GetAttributeSet(
                                                            IAppLinkEntry deepLinkUri,
                                                            string contentType,
                                                            string id)
        {
#pragma warning disable CA1416, CA1422
            var searchableAttributeSet = new CSSearchableItemAttributeSet(contentType)
            {
                RelatedUniqueIdentifier = id,
                Title = deepLinkUri.Title,
                ContentDescription = deepLinkUri.Description,
                Url = new NSUrl(deepLinkUri.AppLinkUri.ToString())
            };
#pragma warning restore CA1416, CA1422

            if (deepLinkUri.Thumbnail is not null)
            {
                var mauiContext = Application.Current?.Handler?.MauiContext
                    ?? throw new InvalidOperationException("MauiContext is not available.");

                var imageSource = (Microsoft.Maui.IImageSource)deepLinkUri.Thumbnail;

                // IMPORTANT: GetPlatformImageAsync returns IImageSourceServiceResult<T> 
                // which is IDisposable, not IAsyncDisposable → use *using*, NOT await using
                var result = await imageSource.GetPlatformImageAsync(mauiContext);

                using (result)
                {
                    if (result?.Value is not UIImage uiimage)
                        throw new InvalidOperationException("AppLinkEntry Thumbnail must be set to a valid source");

                    searchableAttributeSet.ThumbnailData = uiimage.AsPNG();
                }
            }

            return searchableAttributeSet;
        }


        static NSMutableDictionary GetUserInfoForActivity(IAppLinkEntry deepLinkUri)
        {
            var info = new NSMutableDictionary();
            info.Add(new NSString("link"), new NSString(deepLinkUri.AppLinkUri.ToString()));

            foreach (var item in deepLinkUri.KeyValues)
                info.Add(new NSString(item.Key), new NSString(item.Value));

            return info;
        }

        static Task<bool> IndexItemAsync(CSSearchableItem searchItem)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (CSSearchableIndex.IsIndexingAvailable)
            {
                CSSearchableIndex
                    .DefaultSearchableIndex
                    .Index(new[] { searchItem }, error => tcs.TrySetResult(error == null));
            }
            else
            {
                tcs.SetResult(false);
            }

            return tcs.Task;
        }

        static Task<bool> RemoveLinkAsync(string identifier)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (CSSearchableIndex.IsIndexingAvailable)
            {
                CSSearchableIndex
                    .DefaultSearchableIndex
                    .Delete(new[] { identifier }, error => tcs.TrySetResult(error == null));
            }
            else
            {
                tcs.SetResult(false);
            }

            return tcs.Task;
        }

        static void TryGetValues(
            IAppLinkEntry deepLinkUri,
            out string contentType,
            out string associatedWebPage,
            out bool shouldAddToPublicIndex)
        {
            contentType = string.Empty;
            associatedWebPage = string.Empty;
            shouldAddToPublicIndex = false;
            var publicIndex = string.Empty;

            if (!deepLinkUri.KeyValues.TryGetValue(nameof(contentType), out contentType))
                contentType = "View";

            if (deepLinkUri.KeyValues.TryGetValue(nameof(publicIndex), out publicIndex))
                bool.TryParse(publicIndex, out shouldAddToPublicIndex);

            deepLinkUri.KeyValues.TryGetValue(nameof(associatedWebPage), out associatedWebPage);
        }
    }
}
