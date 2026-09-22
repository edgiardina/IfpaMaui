//
//  ContentView.swift
//  IFPAWatch
//

import SwiftUI

/// The watch app fetches the player itself rather than mirroring the
/// complication, so it can show the same rank detail the large iPhone widget
/// does. The IFPA number stays at the top because reading it out at a
/// registration desk is the reason to raise a wrist.
struct ContentView: View {
    @EnvironmentObject private var session: PhoneSessionReceiver

    @State private var player: Player?
    @State private var isLoading = false
    @State private var failed = false

    var body: some View {
        ScrollView {
            if session.playerId == 0 {
                noPlayer
            } else {
                VStack(alignment: .leading, spacing: 10) {
                    numberHeader
                    body(for: player)
                }
                .frame(maxWidth: .infinity, alignment: .leading)
            }
        }
        .task(id: session.playerId) {
            await load()
        }
    }

    // MARK: States

    @ViewBuilder
    private func body(for player: Player?) -> some View {
        if let player = player {
            details(for: player)
        } else if isLoading {
            ProgressView()
                .frame(maxWidth: .infinity)
                .padding(.vertical, 18)
        } else if failed {
            VStack(alignment: .leading, spacing: 6) {
                Text("Could not load rank")
                    .font(.system(.footnote, design: .rounded).weight(.semibold))
                Text("Check your connection and try again.")
                    .font(.caption2)
                    .foregroundStyle(.secondary)
                Button("Retry") {
                    Task { await load() }
                }
                .buttonStyle(.bordered)
                .padding(.top, 2)
            }
        }
    }

    private var noPlayer: some View {
        VStack(spacing: 6) {
            Text("IFPA")
                .font(.system(.caption2, design: .rounded).weight(.semibold))
                .tracking(1.1)
                .foregroundStyle(.secondary)
            Text("No player yet")
                .font(.system(.headline, design: .rounded))
            Text("Open My Stats on your iPhone and choose a player.")
                .font(.footnote)
                .foregroundStyle(.secondary)
                .multilineTextAlignment(.center)
        }
        .padding()
    }

    // MARK: Pieces

    /// One line, so it reads the way a player says it at a desk and leaves the
    /// rank detail below the fold less often.
    private var numberHeader: some View {
        HStack(alignment: .firstTextBaseline, spacing: 5) {
            Text("IFPA")
                .font(.system(size: 10, weight: .semibold, design: .rounded))
                .tracking(1.2)
                .foregroundStyle(.secondary)

            // `verbatim:` on purpose. Text's LocalizedStringKey initialiser
            // locale-formats an interpolated Int, which turned IFPA number
            // 63251 into "63,251". It is an identifier, not a quantity, and a
            // player has to read the digits aloud.
            Text(verbatim: "#\(session.playerId)")
                .font(.system(.title3, design: .rounded).weight(.heavy))
                .monospacedDigit()
                .lineLimit(1)
                .minimumScaleFactor(0.5)
        }
    }

    @ViewBuilder
    private func details(for player: Player) -> some View {
        let stats = player.openStats

        VStack(alignment: .leading, spacing: 8) {
            if !player.displayName.isEmpty {
                Text(player.displayName.uppercased())
                    .font(.system(.caption2, design: .rounded).weight(.semibold))
                    .tracking(0.6)
                    .foregroundStyle(.secondary)
                    .lineLimit(2)
                    .minimumScaleFactor(0.7)
            }

            HStack(alignment: .firstTextBaseline, spacing: 6) {
                Text(IfpaStat.ordinal(stats?.currentRank))
                    .font(.system(.title, design: .rounded).weight(.heavy))
                    .lineLimit(1)
                    .minimumScaleFactor(0.5)
                Text("\(IfpaStat.points(stats?.currentPoints)) pts")
                    .font(.caption2)
                    .foregroundStyle(.secondary)
                    .lineLimit(1)
                    .minimumScaleFactor(0.7)
            }

            Divider()

            // The same six figures the large iPhone widget shows.
            row("Eff. Pct", IfpaStat.percent(stats?.efficiencyValue))
            row("Eff. Rank", IfpaStat.ordinal(stats?.efficiencyRank))
            row("Events", IfpaStat.count(stats?.totalEventsAllTime))
            row("Best Finish", IfpaStat.ordinal(stats?.bestFinish))
            row("Avg Finish", IfpaStat.ordinal(stats?.averageFinish))
            row("Highest Rank", IfpaStat.ordinal(stats?.highestRank))
        }
    }

    private func row(_ label: String, _ value: String) -> some View {
        HStack(alignment: .firstTextBaseline, spacing: 6) {
            Text(label)
                .font(.caption2)
                .foregroundStyle(.secondary)
                .lineLimit(1)
            Spacer(minLength: 6)
            Text(value)
                .font(.system(.caption, design: .rounded).weight(.semibold))
                .monospacedDigit()
                .lineLimit(1)
        }
    }

    // MARK: Loading

    @MainActor
    private func load() async {
        guard session.playerId > 0 else { return }

        isLoading = true
        failed = false
        do {
            let result = try await IfpaPlayer.getPlayerById(from: session.playerId)
            player = result.firstPlayer
            failed = (player == nil)
        } catch {
            failed = true
        }
        isLoading = false
    }
}
