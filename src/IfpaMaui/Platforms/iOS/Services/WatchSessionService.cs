using CommunityToolkit.Mvvm.Messaging;
using Foundation;
using Ifpa.Models;
using Microsoft.Extensions.Logging;
using WatchConnectivity;

namespace Ifpa.Platforms.Services
{
    /// <summary>
    /// Sends the My Stats player id to the paired Apple Watch.
    /// </summary>
    /// <remarks>
    /// App groups are per-device containers, so the watch cannot read the
    /// phone's shared container. WatchConnectivity carries the id across, and
    /// the watch app writes it into its own container, which is where the rank
    /// complication already looks.
    /// </remarks>
    public class WatchSessionService : WCSessionDelegate
    {
        private const string PlayerIdKey = "PlayerId";

        private readonly ILogger<WatchSessionService> logger;

        public WatchSessionService(ILogger<WatchSessionService> logger)
        {
            this.logger = logger;
        }

        public void Start()
        {
            if (!WCSession.IsSupported)
            {
                logger?.LogDebug("WatchConnectivity is not supported on this device");
                return;
            }

            var session = WCSession.DefaultSession;
            session.Delegate = this;
            session.ActivateSession();

            if (!WeakReferenceMessenger.Default.IsRegistered<MyStatsPlayerChangedMessage>(this))
            {
                WeakReferenceMessenger.Default.Register<MyStatsPlayerChangedMessage>(this, (_, _) =>
                {
                    logger?.LogDebug("MyStatsPlayerChangedMessage received — sending player id to the watch");
                    SendPlayerId();
                });
            }
        }

        /// <summary>
        /// Sends the current player id as the application context. A context
        /// replaces any previous one and is held until the watch reads it, so a
        /// watch that is switched off still receives the latest value when it
        /// next wakes. That makes it the right choice over a live message.
        /// </summary>
        public void SendPlayerId()
        {
            var playerId = Settings.MyStatsPlayerId;
            if (playerId == 0)
            {
                logger?.LogDebug("No My Stats player is set, so nothing to send to the watch");
                return;
            }

            var session = WCSession.DefaultSession;

            if (session.ActivationState != WCSessionActivationState.Activated)
            {
                logger?.LogDebug("Watch session is not active yet, deferring send of player {PlayerId}", playerId);
                return;
            }

            if (!session.Paired)
            {
                logger?.LogDebug("No paired watch, skipping send of player {PlayerId}", playerId);
                return;
            }

            var context = new NSDictionary<NSString, NSObject>(
                new NSString(PlayerIdKey),
                NSNumber.FromInt32(playerId));

            if (session.UpdateApplicationContext(context, out var error))
            {
                logger?.LogDebug("Sent player {PlayerId} to the watch", playerId);
            }
            else
            {
                logger?.LogError("Could not send player {PlayerId} to the watch: {Error}",
                    playerId, error?.LocalizedDescription);
            }
        }

        public override void ActivationDidComplete(WCSession session,
            WCSessionActivationState activationState, NSError error)
        {
            if (error != null)
            {
                logger?.LogError("Watch session activation failed: {Error}", error.LocalizedDescription);
                return;
            }

            logger?.LogDebug("Watch session activated with state {State}", activationState);

            // Send on activation as well as on change, so a watch that was
            // paired or reinstalled after the player was chosen still gets it.
            SendPlayerId();
        }

        public override void DidBecomeInactive(WCSession session)
        {
            logger?.LogDebug("Watch session became inactive");
        }

        public override void DidDeactivate(WCSession session)
        {
            logger?.LogDebug("Watch session deactivated — reactivating for the next watch");

            // Reactivating is what lets a switch to a different paired watch
            // keep working.
            WCSession.DefaultSession.ActivateSession();
        }
    }
}
