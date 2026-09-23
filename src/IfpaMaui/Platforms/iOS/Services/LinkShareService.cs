using Foundation;
using Ifpa.Interfaces;
using LinkPresentation;
using Microsoft.Extensions.Logging;
using UIKit;

namespace Ifpa.Platforms.Services
{
    /// <summary>
    /// Presents the iOS share sheet with LPLinkMetadata, so the sheet header and the
    /// Messages bubble show the title and image instead of a bare URL.
    /// </summary>
    public class LinkShareService : ILinkShareService
    {
        static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(3) };

        readonly ILogger<LinkShareService> logger;

        public LinkShareService(ILogger<LinkShareService> logger)
        {
            this.logger = logger;
        }

        public async Task ShareLinkAsync(string url, string title, string imageUrl)
        {
            var nsUrl = new NSUrl(url);
            var metadata = new LPLinkMetadata
            {
                OriginalUrl = nsUrl,
                Url = nsUrl,
                Title = title,
            };

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                // The sheet opens right away. iOS asks the providers for the image
                // and shows it in the header when the download completes.
                var image = new Lazy<Task<UIImage>>(() => LoadImageAsync(imageUrl));
                metadata.ImageProvider = CreateImageProvider(image);
                metadata.IconProvider = CreateImageProvider(image);
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var presenter = Platform.GetCurrentUIViewController();
                if (presenter == null)
                {
                    logger.LogWarning("No view controller to present the share sheet for {Url}", url);
                    return;
                }

                var activityController = new UIActivityViewController(
                    [new LinkItemSource(nsUrl, metadata)], null);

                // iPad presents the share sheet as a popover and needs an anchor.
                if (activityController.PopoverPresentationController is { } popover)
                {
                    popover.SourceView = presenter.View;
                    var bounds = presenter.View!.Bounds;
                    popover.SourceRect = new CoreGraphics.CGRect(bounds.Width / 2, bounds.Height / 2, 0, 0);
                    popover.PermittedArrowDirections = 0;
                }

                presenter.PresentViewController(activityController, true, null);
            });
        }

        static NSItemProvider CreateImageProvider(Lazy<Task<UIImage>> image)
        {
            var provider = new NSItemProvider();
            provider.RegisterObject(new ObjCRuntime.Class(typeof(UIImage)), NSItemProviderRepresentationVisibility.All, completion =>
            {
                image.Value.ContinueWith(t =>
                {
                    var result = t.IsCompletedSuccessfully ? t.Result : null;
                    completion(result, result == null ? new NSError(NSError.CocoaErrorDomain, 0) : null);
                });
                return null;
            });
            return provider;
        }

        async Task<UIImage> LoadImageAsync(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return null;

            try
            {
                var bytes = await http.GetByteArrayAsync(imageUrl);
                return UIImage.LoadFromData(NSData.FromArray(bytes));
            }
            catch (Exception ex)
            {
                // A missing preview image must not block the share.
                logger.LogWarning(ex, "Could not load share preview image {ImageUrl}", imageUrl);
                return null;
            }
        }

        sealed class LinkItemSource : UIActivityItemSource
        {
            readonly NSUrl url;
            readonly LPLinkMetadata metadata;

            public LinkItemSource(NSUrl url, LPLinkMetadata metadata)
            {
                this.url = url;
                this.metadata = metadata;
            }

            public override NSObject GetPlaceholderData(UIActivityViewController activityViewController) => url;

            public override NSObject GetItemForActivity(UIActivityViewController activityViewController, NSString activityType) => url;

            public override LPLinkMetadata GetLinkMetadata(UIActivityViewController activityViewController) => metadata;
        }
    }
}
