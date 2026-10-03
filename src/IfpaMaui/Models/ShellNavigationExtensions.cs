using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.Web;

namespace Ifpa.Models
{
    public static class ShellNavigationExtensions
    {
        /// <summary>
        /// Navigates to a relative route. If a page for the same route and query is already in the current
        /// navigation stack, pops back to it instead of pushing a duplicate.
        /// </summary>
        public static Task GoToAndPopExistingAsync(this Shell shell, string route)
        {
            var pagesToPop = PagesAboveExisting(shell, route);

            if (pagesToPop == 0)
                return Task.CompletedTask;

            if (pagesToPop > 0)
                return shell.GoToAsync(string.Join("/", Enumerable.Repeat("..", pagesToPop.Value)));

            return shell.GoToAsync(route);
        }

        private static int? PagesAboveExisting(Shell shell, string route)
        {
            var parts = route.Split('?', 2);
            var routeName = parts[0];

            // Only single-segment relative routes (e.g. "player-details?playerId=1") can be matched to one page
            if (string.IsNullOrEmpty(routeName) || routeName.Contains('/'))
                return null;

            var stack = shell.CurrentItem?.CurrentItem?.Navigation.NavigationStack;
            if (stack == null)
                return null;

            var query = HttpUtility.ParseQueryString(parts.Length > 1 ? parts[1] : string.Empty);

            for (int i = stack.Count - 1; i >= 0; i--)
            {
                if (stack[i] is Page page && Routing.GetRoute(page) == routeName && QueryMatches(page, query))
                    return stack.Count - 1 - i;
            }

            return null;
        }

        // Compares each query parameter against the page property Shell bound it to via [QueryProperty]
        private static bool QueryMatches(Page page, NameValueCollection query)
        {
            var pageType = page.GetType();
            var queryProperties = pageType.GetCustomAttributes<QueryPropertyAttribute>().ToList();

            foreach (var key in query.AllKeys)
            {
                if (key == null)
                    continue;

                var propertyName = queryProperties.FirstOrDefault(a => a.QueryId == key)?.Name;
                var value = propertyName == null ? null : pageType.GetProperty(propertyName)?.GetValue(page);

                if (value == null || Convert.ToString(value, CultureInfo.InvariantCulture) != query[key])
                    return false;
            }

            return true;
        }
    }
}
