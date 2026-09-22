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

            if (!session.WatchAppInstalled)
            {
                // Nothing can be delivered yet. WatchStateDidChange fires when
                // the watch app is installed, and sends then.
                logger?.LogDebug("Watch app is not installed, deferring send of player {PlayerId}", playerId);
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

            // The application context waits for the watch app to run. A
            // complication transfer additionally wakes the extension, so the
            // face updates without the user opening the watch app. It has a
            // daily budget, so it complements the context rather than
            // replacing it.
            if (session.ComplicationEnabled)
            {
                session.TransferCurrentComplicationUserInfo(context);
                logger?.LogDebug("Also sent player {PlayerId} over the complication channel", playerId);
            }
        }

        /// <summary>
        /// Fires when the watch is paired or unpaired, when the watch app is
        /// installed or removed, and when a complication is added to a face.
        /// </summary>
        /// <remarks>
        /// This is the fix for a player that was already chosen before the
        /// watch app existed. Installing the watch app raises neither
        /// activation nor a player change, so without this the id was never
        /// sent and the watch sat empty until the player was changed.
        /// </remarks>
        // The binding keeps the Session prefix here, unlike
        // DidBecomeInactive and DidDeactivate.
        public override void SessionWatchStateDidChange(WCSession session)
        {
            logger?.LogDebug(
                "Watch state changed — paired={Paired}, appInstalled={Installed}, complication={Complication}",
                session.Paired, session.WatchAppInstalled, session.ComplicationEnabled);

            SendPlayerId();
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
