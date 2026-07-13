using System;
using System.Threading.Tasks;

namespace Ifpa.Services
{
    public interface IDeepLinkService
    {
        Task HandleDeepLink(Uri uri);
        Task HandleAppAction(string actionId);
    }
}