//
//  ContentView.swift
//  IFPAWatch
//

import SwiftUI

/// The watch app itself leads with the IFPA number, because reading that
/// number out at a registration desk is the reason to raise your wrist. Rank
/// lives on the complication.
struct ContentView: View {
    @EnvironmentObject private var session: PhoneSessionReceiver

    var body: some View {
        VStack(spacing: 6) {
            Text("IFPA")
                .font(.system(.caption2, design: .rounded).weight(.semibold))
                .tracking(1.1)
                .foregroundStyle(.secondary)

            if session.playerId > 0 {
                // `verbatim:` on purpose. Text's LocalizedStringKey initializer
                // locale-formats an interpolated Int, which turns IFPA number
                // 63251 into "63,251". It is an identifier, not a quantity, and
                // a player has to read the digits out at a registration desk.
                Text(verbatim: "#\(session.playerId)")
                    .font(.system(.title, design: .rounded).weight(.heavy))
                    .monospacedDigit()
                    .lineLimit(1)
                    .minimumScaleFactor(0.5)

                Text("Add the IFPA complication to a watch face for your rank.")
                    .font(.footnote)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
            } else {
                Text("No player yet")
                    .font(.system(.headline, design: .rounded))

                Text("Open My Stats on your iPhone and choose a player.")
                    .font(.footnote)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
            }
        }
        .padding()
    }
}
