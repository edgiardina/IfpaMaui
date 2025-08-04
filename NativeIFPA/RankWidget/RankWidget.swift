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
        switch family {
        case .systemSmall:
            VStack(alignment: .center, spacing: 4) {
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
            }.padding()
        case .systemMedium:
            HStack(alignment: .center, spacing: 16) {
                if let photoData = entry.profilePhotoData, let uiImage = UIImage(data: photoData) {
                    Image(uiImage: uiImage)
                        .resizable()
                        .aspectRatio(contentMode: .fit)
                        .frame(width: 80, height: 80)
                        .clipShape(RoundedRectangle(cornerRadius: 20, style: .continuous))
                }
                VStack(alignment: .center, spacing: 4) {
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
            }.frame(maxHeight: .infinity, alignment: .center)
            .padding()
        case .systemLarge:
            HStack(alignment: .center, spacing: 20) {
                if let photoData = entry.profilePhotoData, let uiImage = UIImage(data: photoData) {
                    Image(uiImage: uiImage)
                        .resizable()
                        .aspectRatio(contentMode: .fit)
                        .frame(width: 120, height: 120)
                        .clipShape(RoundedRectangle(cornerRadius: 20, style: .continuous))
                }
                VStack(alignment: .leading, spacing: 8) {
                    Text("\(entry.player?.player.first?.firstName ?? "") \(entry.player?.player.first?.lastName ?? "")")
                        .foregroundColor(.gray)
                        .font(.title2)
                    Text(Int(entry.player?.player.first?.playerStats.system.open.currentRank ?? "0")?.ordinal ?? "")
                        .foregroundColor(.white)
                        .bold()
                        .font(.system(size: 44))
                    Text("Points: \(entry.player?.player.first?.playerStats.system.open.currentPoints ?? "")")
                        .foregroundColor(.gray)
                        .font(.body)
                    Text("Efficiency Rank: \(Int(entry.player?.player.first?.playerStats.system.open.efficiencyRank ?? "0")?.ordinal ?? "")")
                        .foregroundColor(.gray)
                        .font(.body)
                    // Additional stats example
                    if let stats = entry.player?.player.first?.playerStats.system.open {
                        Text("Events Played: \(stats.totalEventsAllTime ?? "-")")
                            .foregroundColor(.gray)
                            .font(.body)
                        Text("Best Finish: \(stats.bestFinish ?? "-")")
                            .foregroundColor(.gray)
                            .font(.body)
                    }
                }
            }.padding()
        default:
            // Fallback for other families
            VStack {
                Text("IFPA Rank")
                    .foregroundColor(.white)
            }.padding()
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
