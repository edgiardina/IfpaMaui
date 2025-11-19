using System;
using Microsoft.Maui.Controls;

namespace IfpaMaui.Platforms.iOS
{
    // NOTE: You must define IAppIndexingProvider and IAppLinks in your shared project
    public class IOSAppIndexingProvider : IAppIndexingProvider
    {
        public IAppLinks AppLinks => new IOSAppLinks();
    }
}
