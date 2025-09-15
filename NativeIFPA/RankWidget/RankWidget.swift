//
//  RankWidget.swift
//  RankWidget
//
//  Created by Ed Giardina on 6/9/23.
//

import WidgetKit
import SwiftUI
import Intents

struct Provider: IntentTimelineProvider {
    
    @AppStorage("PlayerId", store: UserDefaults(suiteName: "group.com.edgiardina.ifpa")) private var playerId = 2

    func placeholder(in context: Context) -> IfpaPlayerEntry {
        return IfpaPlayerEntry(date: Date())
    }

    func getSnapshot(for configuration: ConfigurationIntent, in context: Context, completion: @escaping (IfpaPlayerEntry) -> ()) {
        Task {
            guard let player = try? await IfpaPlayer.getPlayerById(from: playerId) else { return }
            let photoData = await fetchProfilePhotoData(from: player.player.first?.profilePhoto)
            let entry = IfpaPlayerEntry(date: Date(), player: player, profilePhotoData: photoData)
            completion(entry)
        }
    }

    func getTimeline(for configuration: ConfigurationIntent, in context: Context, completion: @escaping (Timeline<Entry>) -> ()) {
        Task {
            guard let player = try? await IfpaPlayer.getPlayerById(from: playerId) else { return }
            let photoData = await fetchProfilePhotoData(from: player.player.first?.profilePhoto)
            let entry = IfpaPlayerEntry(date: Date(), player: player, profilePhotoData: photoData)
            
            let timeline = Timeline(entries: [entry], policy: .atEnd)
            completion(timeline)
        }
    }

    private func fetchProfilePhotoData(from urlString: String?) async -> Data? {
        guard let urlString = urlString, let url = URL(string: urlString) else { return nil }
        do {
            let (data, _) = try await URLSession.shared.data(from: url)
            return data
        } catch {
            return nil
        }
    }
}

struct IfpaPlayerEntry: TimelineEntry {
    let date: Date
    var player: IfpaPlayer?
    var profilePhotoData: Data?
}

struct RankWidgetEntryView : View {
    let entry: IfpaPlayerEntry
    @Environment(\.widgetFamily) var family

    var body: some View {
        Group {
            if #available(iOS 17.0, *) {
                contentForFamily()
                    .containerBackground(Color(hex: 0x062C53), for: .widget)
            } else {
                ZStack {
                    ContainerRelativeShape()
                        .fill(Color(hex: 0x062C53))
                    contentForFamily()
                }
            }
        }
    }

    @ViewBuilder
    private func contentForFamily() -> some View {
        if entry.player == nil {
            VStack {
                Text("Player data not available.")
                    .foregroundColor(.gray)
                    .font(.headline)
            }.padding()
        } else {
            switch family {
            case .systemSmall:
                ZStack(alignment: .topLeading) {
                    Image("ifpa_icon")
                        .resizable()
                        .frame(width: 20, height: 20)
                        .padding([.top, .leading], -8)
                    VStack(alignment: .center, spacing: 4) {
                        Spacer().frame(height: 16)
                        Text("# \(entry.player?.player.first?.playerID ?? "-")")
                            .foregroundColor(.gray)
                            .font(.caption2)
                        Text("\(entry.player?.player.first?.firstName ?? "") \(entry.player?.player.first?.lastName ?? "")")
                            .foregroundColor(.gray)
                            .font(.caption)
                        Text(Int(entry.player?.player.first?.playerStats.system.open.currentRank ?? "0")?.ordinal ?? "")
                            .foregroundColor(.white)
                            .bold()
                            .font(.system(size: 28))
                        Text(entry.player?.player.first?.playerStats.system.open.currentPoints ?? "")
                            .foregroundColor(.gray)
                            .font(.caption2)
                    }
                    .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .top)
                    .padding([.top, .leading], 0)
                }
            case .systemMedium:
                ZStack(alignment: .topLeading) {
                    Image("ifpa_icon")
                        .resizable()
                        .frame(width: 20, height: 20)
                        .padding([.top, .leading], -8)
                    HStack(alignment: .center, spacing: 16) {
                        if let photoData = entry.profilePhotoData, let uiImage = UIImage(data: photoData) {
                            Image(uiImage: uiImage)
                                .resizable()
                                .aspectRatio(contentMode: .fit)
                                .frame(width: 120, height: 120)
                                .clipShape(RoundedRectangle(cornerRadius: 20, style: .continuous))
                        }
                        VStack(alignment: .center, spacing: 4) {
                            Text("# \(entry.player?.player.first?.playerID ?? "-")")
                                .foregroundColor(.gray)
                                .font(.caption2)
                            Text("\(entry.player?.player.first?.firstName ?? "") \(entry.player?.player.first?.lastName ?? "")")
                                .foregroundColor(.gray)
                                .font(.headline)
                            Text(Int(entry.player?.player.first?.playerStats.system.open.currentRank ?? "0")?.ordinal ?? "")
                                .foregroundColor(.white)
                                .bold()
                                .font(.system(size: 36))
                            Text(entry.player?.player.first?.playerStats.system.open.currentPoints ?? "")
                                .foregroundColor(.gray)
                                .font(.subheadline)
                        }
                    }
                    .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .top)
                    .padding([.top, .leading], 0)
                }
            case .systemLarge:
                ZStack(alignment: .topLeading) {
                    Image("ifpa_icon")
                        .resizable()
                        .frame(width: 20, height: 20)
                        .padding([.top, .leading], -8)
                    VStack(alignment: .leading, spacing: 16) {
                        // Top half: image, id, name, rank, points in a row
                        HStack(alignment: .center, spacing: 16) {
                            if let photoData = entry.profilePhotoData, let uiImage = UIImage(data: photoData) {
                                Image(uiImage: uiImage)
                                    .resizable()
                                    .aspectRatio(contentMode: .fit)
                                    .frame(width: 120, height: 120)
                                    .clipShape(RoundedRectangle(cornerRadius: 20, style: .continuous))
                            }
                            VStack(alignment: .leading, spacing: 4) {
                                Text("# \(entry.player?.player.first?.playerID ?? "-")")
                                    .foregroundColor(.gray)
                                    .font(.caption2)
                                Text("\(entry.player?.player.first?.firstName ?? "") \(entry.player?.player.first?.lastName ?? "")")
                                    .foregroundColor(.gray)
                                    .font(.title2)
                                Text(Int(entry.player?.player.first?.playerStats.system.open.currentRank ?? "0")?.ordinal ?? "")
                                    .foregroundColor(.white)
                                    .bold()
                                    .font(.system(size: 44))
                                Text(entry.player?.player.first?.playerStats.system.open.currentPoints ?? "")
                                    .foregroundColor(.gray)
                                    .font(.title3)
                            }
                        }
                        // Bottom half: two-column grid of stats/info
                        let stats: [(String, String)] = [
                            ("Eff. Prct", entry.player?.player.first?.playerStats.system.open.efficiencyValue ?? "-"),
                            ("Eff. Rank", Int(entry.player?.player.first?.playerStats.system.open.efficiencyRank ?? "0")?.ordinal ?? "-"),
                            ("Events Played", entry.player?.player.first?.playerStats.system.open.totalEventsAllTime ?? "-"),
                            ("Best Finish", entry.player?.player.first?.playerStats.system.open.bestFinish ?? "-"),
                            ("Avg Finish", entry.player?.player.first?.playerStats.system.open.averageFinish ?? "-"),
                            ("Highest Rank", Int(entry.player?.player.first?.playerStats.system.open.highestRank ?? "0")?.ordinal ?? "-"),
                        ]
                        LazyVGrid(columns: [GridItem(.flexible(), alignment: .top), GridItem(.flexible(), alignment: .top)], spacing: 8) {
                            ForEach(stats, id: \.0) { stat in
                                HStack(alignment: .top) {
                                    Text(stat.0)
                                        .foregroundColor(.gray)
                                        .font(.body)
                                    Spacer(minLength: 8)
                                    Text(stat.1)
                                        .foregroundColor(.white)
                                        .font(.body)
                                }
                            }
                        }
                    }
                    .frame(maxHeight: .infinity, alignment: .top)
                    .padding()
                }
            default:
                // Fallback for other families
                VStack {
                    Text("IFPA Rank")
                        .foregroundColor(.white)
                }.padding()
            }
        }
    }
}

struct RankWidget: Widget {
    let kind: String = "RankWidget"

    var body: some WidgetConfiguration {
        IntentConfiguration(kind: kind, intent: ConfigurationIntent.self, provider: Provider()) { entry in
            RankWidgetEntryView(entry: entry)
        }
        .configurationDisplayName("IFPA Rank") // Changed title
        .description("Show Current My Stats Player's Rank")
    }
}

/*
struct RankWidget_Previews: PreviewProvider {
    static var previews: some View {
        RankWidgetEntryView(entry: IfpaPlayerEntry(date: Date(), player: IfpaPlayer(player: <#[Player]#>)))
            .previewContext(WidgetPreviewContext(family: .systemSmall))
    }
}*/


extension Color {
    init(hex: UInt, alpha: Double = 1) {
        self.init(
            .sRGB,
            red: Double((hex >> 16) & 0xff) / 255,
            green: Double((hex >> 08) & 0xff) / 255,
            blue: Double((hex >> 00) & 0xff) / 255,
            opacity: alpha
        )
    }
}

extension Int {

    var ordinal: String {
        var suffix: String
        let ones: Int = self % 10
        let tens: Int = (self/10) % 10
        if tens == 1 {
            suffix = "th"
        } else if ones == 1 {
            suffix = "st"
        } else if ones == 2 {
            suffix = "nd"
        } else if ones == 3 {
            suffix = "rd"
        } else {
            suffix = "th"
        }
        return "\(self)\(suffix)"
    }

}
