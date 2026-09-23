namespace Ifpa.Interfaces
{
    /// <summary>
    /// Shares a link with a rich preview (title and image) where the platform supports it.
    /// </summary>
    public interface ILinkShareService
    {
        Task ShareLinkAsync(string url, string title, string imageUrl);
    }
}
