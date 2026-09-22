//
//  IFPAWatchApp.swift
//  IFPAWatch
//

import SwiftUI

@main
struct IFPAWatchApp: App {
    private let session = PhoneSessionReceiver.shared

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(session)
                .onAppear { session.activate() }
        }
    }
}
