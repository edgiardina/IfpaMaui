using Ifpa.Interfaces;

namespace Ifpa.Platforms.Services
{
    public class LinkShareService : ILinkShareService
    {
        public Task ShareLinkAsync(string url, string title, string imageUrl) =>
            Share.RequestAsync(new ShareTextRequest
            {
                Uri = url,
                Title = title,
            });
    }
}
