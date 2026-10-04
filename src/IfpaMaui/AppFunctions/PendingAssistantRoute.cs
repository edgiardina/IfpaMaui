namespace Ifpa.AppFunctions;

/// <summary>
/// Holds the page that the app shows when the user opens it from an assistant answer.
/// iOS does not tell the app that the user tapped the Siri result. The tap only brings the
/// app to the foreground, so the app shows the page if it becomes active soon after the answer.
/// </summary>
public class PendingAssistantRoute
{
    static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(30);

    readonly Lock sync = new();
    string route;
    DateTimeOffset expires;

    public void Set(string route)
    {
        lock (sync)
        {
            this.route = route;
            expires = DateTimeOffset.UtcNow + Lifetime;
        }
    }

    /// <summary>Returns the route one time, or null when there is no route or it is too old.</summary>
    public string Take()
    {
        lock (sync)
        {
            var result = DateTimeOffset.UtcNow <= expires ? route : null;
            route = null;
            return result;
        }
    }
}
