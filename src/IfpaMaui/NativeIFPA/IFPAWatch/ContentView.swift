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

    /// watchOS gives a ScrollView no side inset of its own, so values ended up
    /// against the bezel where the curved display clips them.
    private let sideInset: CGFloat = 8

    var body: some View {
        ScrollView {
            if session.playerId == 0 {
                noPlayer
            } else {
                VStack(alignment: .leading, spacing: 5) {
                    identity
                    content(for: player)
                }
                .frame(maxWidth: .infinity, alignment: .leading)
                .padding(.horizontal, sideInset)
                .padding(.bottom, 6)
            }
        }
        .task(id: session.playerId) {
            await load()
        }
    }

    // MARK: States

    @ViewBuilder
    private func content(for player: Player?) -> some View {
        if let player = player {
            rank(for: player)
            Divider().padding(.vertical, -1)
            stats(for: player)
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

    /// Number and name. One line for the number so it reads the way a player
    /// says it out loud.
    private var identity: some View {
        VStack(alignment: .leading, spacing: 1) {
            HStack(alignment: .firstTextBaseline, spacing: 5) {
                // The same mark the iPhone widget shows. Inline, so it costs
                // horizontal room the header has and no vertical room it does not.
                Image("ifpa_icon")
                    .resizable()
                    .aspectRatio(contentMode: .fit)
                    .frame(width: 13, height: 13)
                    .alignmentGuide(.firstTextBaseline) { $0[.bottom] - 1 }

                Text("IFPA")
                    .font(.system(size: 10, weight: .semibold, design: .rounded))
                    .tracking(1.2)
                    .foregroundStyle(.secondary)

                // `verbatim:` on purpose. Text's LocalizedStringKey initialiser
                // locale-formats an interpolated Int, which turned IFPA number
                // 63251 into "63,251". It is an identifier, not a quantity, and
                // a player has to read the digits aloud.
                Text(verbatim: "#\(session.playerId)")
                    .font(.system(.title3, design: .rounded).weight(.heavy))
                    .monospacedDigit()
                    .lineLimit(1)
                    .minimumScaleFactor(0.5)
            }

            if let name = player?.displayName, !name.isEmpty {
                Text(name.uppercased())
                    .font(.system(size: 11, weight: .semibold, design: .rounded))
                    .tracking(0.5)
                    .foregroundStyle(.secondary)
                    .lineLimit(1)
                    .minimumScaleFactor(0.6)
            }
        }
    }

    /// Rank and points share a line. That was cramped while the stats were six
    /// full-width rows, but the two-column grid leaves the rank only about half
    /// the width, and reclaiming the line is what lets every stat fit on screen.
    private func rank(for player: Player) -> some View {
        let stats = player.openStats
        return HStack(alignment: .firstTextBaseline, spacing: 7) {
            Text(IfpaStat.ordinal(stats?.currentRank))
                .font(.system(.title2, design: .rounded).weight(.heavy))
                .lineLimit(1)
                .minimumScaleFactor(0.5)
            Spacer(minLength: 4)
            Text("\(IfpaStat.points(stats?.currentPoints)) pts")
                .font(.system(size: 12, design: .rounded))
                .foregroundStyle(.secondary)
                .lineLimit(1)
                .minimumScaleFactor(0.7)
        }
    }

    /// The same six figures the large iPhone widget shows, paired into two
    /// columns like that widget's grid. Six single rows overflowed the screen
    /// and left the last of them sliced by the bottom edge; three paired rows
    /// fit without scrolling.
    private func stats(for player: Player) -> some View {
        let stats = player.openStats
        let cells: [(String, String)] = [
            ("Efficiency", IfpaStat.percent(stats?.efficiencyValue)),
            ("Eff. rank", IfpaStat.ordinal(stats?.efficiencyRank)),
            ("Events", IfpaStat.count(stats?.totalEventsAllTime)),
            ("Best", IfpaStat.ordinal(stats?.bestFinish)),
            ("Average", IfpaStat.ordinal(stats?.averageFinish)),
            ("Peak rank", IfpaStat.ordinal(stats?.highestRank)),
        ]

        return VStack(alignment: .leading, spacing: 4) {
            ForEach(Array(stride(from: 0, to: cells.count, by: 2)), id: \.self) { index in
                HStack(alignment: .top, spacing: 10) {
                    cell(cells[index], alignment: .leading)
                    if index + 1 < cells.count {
                        cell(cells[index + 1], alignment: .trailing)
                    }
                }
            }
        }
    }

    /// Label above value, so a long ordinal like 18943rd has the full column
    /// width rather than competing with its own label on one line.
    private func cell(_ stat: (String, String), alignment: HorizontalAlignment) -> some View {
        VStack(alignment: alignment, spacing: -3) {
            Text(stat.0)
                .font(.system(size: 11))
                .foregroundStyle(.secondary)
                .lineLimit(1)
                .minimumScaleFactor(0.8)
            Text(stat.1)
                .font(.system(size: 14, weight: .semibold, design: .rounded))
                .monospacedDigit()
                .lineLimit(1)
                .minimumScaleFactor(0.6)
        }
        .frame(maxWidth: .infinity,
               alignment: alignment == .trailing ? .trailing : .leading)
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
