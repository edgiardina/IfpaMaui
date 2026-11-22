using CoreGraphics;
using Foundation;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Platform;
using UIKit;

namespace Ifpa.Platforms.Services;

/// <summary>
/// Draws FontImageSource glyphs the way MAUI 10.0.110 and earlier did.
/// </summary>
/// <remarks>
/// WORKAROUND for https://github.com/dotnet/maui/issues/40494. MAUI 10.0.120+ (dotnet/maui#39230)
/// re-centers FontImageSource glyphs on their ink bounds on iOS 26+, which pushes our Fluent toolbar
/// icons ~3.8 pt above the center of their glass buttons. This is a copy of the pre-#39230 drawing
/// code from src/Core/src/ImageSources/iOS/ImageSourceExtensions.cs (tag 10.0.110). Remove this class
/// and its registration in MauiProgram once #40494 is fixed and the app is on a MAUI version with the fix.
/// </remarks>
public class UncenteredFontImageSourceService : FontImageSourceService
{
    public UncenteredFontImageSourceService(IFontManager fontManager, ILogger<FontImageSourceService> logger = null)
        : base(fontManager, logger)
    {
    }

    public override Task<IImageSourceServiceResult<UIImage>> GetImageAsync(IImageSource imageSource, float scale = 1, CancellationToken cancellationToken = default)
    {
        var fontImageSource = (IFontImageSource)imageSource;
        if (fontImageSource.IsEmpty)
            return Task.FromResult<IImageSourceServiceResult<UIImage>>(null);

        try
        {
            var image = RenderGlyph(fontImageSource, scale)
                ?? throw new InvalidOperationException("Unable to generate font image.");

            return Task.FromResult<IImageSourceServiceResult<UIImage>>(
                new ImageSourceServiceResult(image, true, () => image.Dispose()));
        }
        catch (Exception ex)
        {
            Logger?.LogWarning(ex, "Unable to generate font image '{Glyph}'.", fontImageSource.Glyph);
            throw;
        }
    }

    UIImage RenderGlyph(IFontImageSource imageSource, float scale)
    {
        if (string.IsNullOrWhiteSpace(imageSource.Glyph))
            return null;

        var font = FontManager.GetFont(imageSource.Font);
        var color = (imageSource.Color ?? Colors.White).ToPlatform();
        var glyph = (NSString)imageSource.Glyph;

        var attString = new NSAttributedString(imageSource.Glyph, font, color);
        var imageSize = glyph.GetSizeUsingAttributes(attString.GetUIKitAttributes(0, out _));

        if (imageSize.Width <= 0 || imageSize.Height <= 0)
            return null;

        var renderer = new UIGraphicsImageRenderer(imageSize, new UIGraphicsImageRendererFormat
        {
            Opaque = false,
            Scale = scale,
        });

        // Centers the font's line box (ascent + descent), not the glyph's ink. That's the 10.0.110 behavior.
        return renderer.CreateImage(_ =>
        {
            var boundingRect = attString.GetBoundingRect(imageSize, 0, new NSStringDrawingContext());
            attString.DrawString(new CGRect(
                imageSize.Width / 2 - boundingRect.Size.Width / 2,
                imageSize.Height / 2 - boundingRect.Size.Height / 2,
                imageSize.Width,
                imageSize.Height));
        })
        // Automatic keeps a color-less glyph a template image so it takes the bar tint; an explicit
        // color has to stay AlwaysOriginal or UIKit re-tints it.
        .ImageWithRenderingMode(imageSource.Color == null ? UIImageRenderingMode.Automatic : UIImageRenderingMode.AlwaysOriginal);
    }
}
