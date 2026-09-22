//
//  PhoneSessionReceiver.swift
//  IFPAWatch
//

import Foundation
import WatchConnectivity
import WidgetKit

/// Receives the selected IFPA player id from the phone and stores it where the
/// complication looks for it.
///
/// App groups are per-device containers, so the phone's group is not visible
/// from the watch. The phone pushes the id over WatchConnectivity and this
/// writes it into the watch's own group container, which is the same suite the
/// complication's `@AppStorage` reads.
final class PhoneSessionReceiver: NSObject, ObservableObject {
    static let shared = PhoneSessionReceiver()

    private static let playerIdKey = "PlayerId"
    private static let suiteName = "group.com.edgiardina.ifpa"

    @Published private(set) var playerId: Int

    private override init() {
        playerId = UserDefaults(suiteName: Self.suiteName)?.integer(forKey: Self.playerIdKey) ?? 0
        super.init()
    }

    func activate() {
        guard WCSession.isSupported() else { return }
        let session = WCSession.default
        session.delegate = self
        session.activate()
    }

    /// Stores a newly received id and asks WidgetKit to redraw. An absent or
    /// unchanged value is ignored so the complication is not reloaded for
    /// nothing.
    private func store(_ incoming: Int) {
        guard incoming > 0, incoming != playerId else { return }
        UserDefaults(suiteName: Self.suiteName)?.set(incoming, forKey: Self.playerIdKey)
        DispatchQueue.main.async {
            self.playerId = incoming
            WidgetCenter.shared.reloadAllTimelines()
        }
    }

    /// The phone may send the id as a number or as a string, so accept both.
    private func readPlayerId(from payload: [String: Any]) -> Int? {
        if let value = payload[Self.playerIdKey] as? Int { return value }
        if let text = payload[Self.playerIdKey] as? String { return Int(text) }
        return nil
    }
}

extension PhoneSessionReceiver: WCSessionDelegate {
    func session(_ session: WCSession,
                 activationDidCompleteWith activationState: WCSessionActivationState,
                 error: Error?) {
        // The phone's last application context survives activation, so read it
        // now rather than waiting for the next change.
        if let id = readPlayerId(from: session.receivedApplicationContext) {
            store(id)
        }
    }

    func session(_ session: WCSession, didReceiveApplicationContext applicationContext: [String: Any]) {
        if let id = readPlayerId(from: applicationContext) {
            store(id)
        }
    }

    func session(_ session: WCSession, didReceiveUserInfo userInfo: [String: Any] = [:]) {
        if let id = readPlayerId(from: userInfo) {
            store(id)
        }
    }
}
