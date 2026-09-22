//
//  RankWidget.swift
//  RankWidget
//
//  Created by Ed Giardina on 6/9/23.
//

import WidgetKit
import SwiftUI
import Intents

/// Shared timeline state and loading. The timeline protocol itself differs by
/// platform, so the conformances live in the extensions below: iOS keeps the
/// configurable intent, and watchOS uses a static configuration because the
/// player arrives from the phone over WatchConnectivity and there is nothing
/// to configure on a watch face.
///
/// On the watch the app-group suite is the watch's own container, written by
/// the watch app when it receives the id from the phone. App groups are
/// per-device, so this is deliberately not the phone's container.
struct Provider {

    @AppStorage("PlayerId", store: UserDefaults(suiteName: "group.com.edgiardina.ifpa")) private var playerId = 2

    /// A single entry with `.atEnd` leaves the next refresh undefined, so a
    /// failed fetch can stay on screen until something else wakes the widget.
    /// Ask for a refresh an hour out instead.
    func nextRefresh(after date: Date) -> Date {
        return Calendar.current.date(byAdding: .hour, value: 1, to: date)
            ?? date.addingTimeInterval(3600)
    }

    /// Always produces an entry. A failed fetch yields an entry with no player,
    /// which the view renders as the "not available" placeholder rather than
    /// leaving the completion handler uncalled.
    func loadEntry() async -> IfpaPlayerEntry {
        do {
            let player = try await IfpaPlayer.getPlayerById(from: playerId)
            let photoData = await fetchProfilePhotoData(from: player.firstPlayer?.profilePhoto)
            return IfpaPlayerEntry(date: Date(), player: player, profilePhotoData: photoData)
        } catch {
            return IfpaPlayerEntry(date: Date())
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

#if os(watchOS)

extension Provider: TimelineProvider {
    func placeholder(in context: Context) -> IfpaPlayerEntry {
        return IfpaPlayerEntry(date: Date())
    }

    func getSnapshot(in context: Context, completion: @escaping (IfpaPlayerEntry) -> ()) {
        Task {
            completion(await loadEntry())
        }
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<IfpaPlayerEntry>) -> ()) {
        Task {
            let entry = await loadEntry()
            completion(Timeline(entries: [entry], policy: .after(nextRefresh(after: entry.date))))
        }
    }
}

#else

extension Provider: IntentTimelineProvider {
    func placeholder(in context: Context) -> IfpaPlayerEntry {
        return IfpaPlayerEntry(date: Date())
    }

    func getSnapshot(for configuration: ConfigurationIntent, in context: Context, completion: @escaping (IfpaPlayerEntry) -> ()) {
        Task {
            completion(await loadEntry())
        }
    }

    func getTimeline(for configuration: ConfigurationIntent, in context: Context, completion: @escaping (Timeline<Entry>) -> ()) {
        Task {
            let entry = await loadEntry()
            completion(Timeline(entries: [entry], policy: .after(nextRefresh(after: entry.date))))
        }
    }
}

#endif

struct IfpaPlayerEntry: TimelineEntry {
    let date: Date
    var player: IfpaPlayer?
    var profilePhotoData: Data?
}

// MARK: - Brand

/// The widget commits to IFPA navy in both light and dark rather than adapting,
/// the way most team and league widgets do. Text tints are derived for that
/// ground, so they only need to hold contrast against one colour.
private enum Brand {
    static let ground = Color(hex: 0x062C53)
    static let primary = Color.white
    static let secondary = Color.white.opacity(0.68)
    static let tertiary = Color.white.opacity(0.52)
}

// MARK: - Entry view

struct RankWidgetEntryView : View {
    let entry: IfpaPlayerEntry
    @Environment(\.widgetFamily) var family

    private var playerRecord: Player? { entry.player?.firstPlayer }

    /// Open-system stats. Absent for a player who is not ranked in that system.
    private var openStats: PlayerStatsOpen? { playerRecord?.openStats }

    private var nameText: String { playerRecord?.displayName ?? "" }
    private var rankText: String { IfpaStat.ordinal(openStats?.currentRank) }

    /// Bare rank with no ordinal suffix. The circular Lock Screen family is
    /// only ~51pt across on its inscribed square, so the suffix costs width it
    /// does not have.
    private var rankNumberText: String { IfpaStat.ungrouped(openStats?.currentRank) }

    /// "IFPA #63251", or bare "IFPA" when the number is unknown. This is the
    /// value a player reads out to staff at tournament registration, so the
    /// compact families lead with it rather than with the player's own name.
    private var numberLabel: String {
        guard let number = playerRecord?.playerID, !number.isEmpty else { return "IFPA" }
        return "IFPA #\(number)"
    }
    private var pointsText: String { IfpaStat.points(openStats?.currentPoints) }

    private var isAccessory: Bool {
        switch family {
        case .accessoryCircular, .accessoryRectangular, .accessoryInline: return true
        default: return false
        }
    }

    var body: some View {
        layout
            .modifier(LegacyContentMargins(isAccessory: isAccessory))
            .modifier(WidgetGround(isAccessory: isAccessory, family: family))
    }

    @ViewBuilder
    private var layout: some View {
        if playerRecord == nil {
            unavailable
        } else {
            switch family {
            case .systemSmall:
                smallLayout
            case .systemMedium:
                mediumLayout
            case .systemLarge, .systemExtraLarge:
                largeLayout
            case .accessoryCircular:
                circularLayout
            case .accessoryRectangular:
                rectangularLayout
            case .accessoryInline:
                Text("IFPA \(rankText)")
            #if os(watchOS)
            case .accessoryCorner:
                cornerLayout
            #endif
            @unknown default:
                smallLayout
            }
        }
    }

    // MARK: Unavailable

    @ViewBuilder
    private var unavailable: some View {
        switch family {
        case .accessoryInline:
            Text("IFPA \(IfpaStat.missing)")
        case .accessoryCircular:
            Text(IfpaStat.missing)
                .font(.system(.title3, design: .rounded).weight(.semibold))
        case .accessoryRectangular:
            Text("No IFPA player data")
                .font(.system(.caption, design: .rounded))
        default:
            Text("Player data not available.")
                .font(.system(.footnote, design: .rounded))
                .foregroundStyle(Brand.secondary)
                .multilineTextAlignment(.center)
                .padding()
        }
    }

    // MARK: Brand mark

    /// Wordmark plus the player's IFPA number. Players read that number aloud
    /// to registration staff at events, so it is set for legibility rather
    /// than as chrome: brighter than the wordmark, monospaced digits, and no
    /// letter spacing. Sitting in the header keeps it at every system size
    /// without taking room from the rank.
    private func brandMark(size: CGFloat) -> some View {
        HStack(spacing: 5) {
            Image("ifpa_icon")
                .resizable()
                .aspectRatio(contentMode: .fit)
                .frame(width: size, height: size)
            Text("IFPA")
                .font(.system(.caption2, design: .rounded).weight(.semibold))
                .tracking(1.1)
                .foregroundStyle(Brand.tertiary)
            if let number = playerRecord?.playerID, !number.isEmpty {
                Text("#\(number)")
                    .font(.system(.caption, design: .rounded).weight(.semibold))
                    .monospacedDigit()
                    .foregroundStyle(Brand.secondary)
                    .lineLimit(1)
                    .minimumScaleFactor(0.8)
            }
        }
        // No negative padding. The previous layout pulled the mark 8pt outside
        // the content area, where the widget's corner radius clipped it.
        .frame(maxWidth: .infinity, alignment: .leading)
    }

    // MARK: Hero stack

    /// Name, rank and points. `minimumScaleFactor` lets a long name shrink
    /// instead of truncating, and the semantic fonts track Dynamic Type.
    private func hero(nameFont: Font, rankFont: Font, pointsFont: Font, alignment: HorizontalAlignment) -> some View {
        let textAlignment: TextAlignment = alignment == .center ? .center : .leading

        return VStack(alignment: alignment, spacing: 3) {
            Text(nameText.uppercased())
                .font(nameFont.weight(.semibold))
                .tracking(0.7)
                .foregroundStyle(Brand.secondary)
                // Two lines, so an unusually long name wraps and shrinks
                // rather than being cut off mid-surname. Ordinary names still
                // occupy one line.
                .lineLimit(2)
                .minimumScaleFactor(0.55)
                .multilineTextAlignment(textAlignment)

            Text(rankText)
                .font(rankFont.weight(.heavy))
                .foregroundStyle(Brand.primary)
                .lineLimit(1)
                .minimumScaleFactor(0.5)

            Text("\(pointsText) pts")
                .font(pointsFont)
                .foregroundStyle(Brand.secondary)
                .lineLimit(1)
                .minimumScaleFactor(0.7)
        }
    }

    // MARK: Photo

    private var photoImage: UIImage? {
        guard let data = entry.profilePhotoData else { return nil }
        return UIImage(data: data)
    }

    @ViewBuilder
    private func photo(side: CGFloat) -> some View {
        if let image = photoImage {
            Image(uiImage: image)
                .resizable()
                .aspectRatio(contentMode: .fill)
                .frame(width: side, height: side)
                .clipShape(RoundedRectangle(cornerRadius: side * 0.18, style: .continuous))
        }
    }

    // MARK: System layouts

    /// Mark pinned top, hero centred in the space that remains, so the widget
    /// no longer leaves its bottom third empty.
    private var smallLayout: some View {
        VStack(spacing: 0) {
            brandMark(size: 14)
            Spacer(minLength: 2)
            hero(nameFont: .system(.caption2, design: .rounded),
                 rankFont: .system(.largeTitle, design: .rounded),
                 pointsFont: .system(.caption2, design: .rounded),
                 alignment: .center)
            Spacer(minLength: 2)
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity)
        .padding(.vertical, 2)
    }

    private var mediumLayout: some View {
        VStack(spacing: 0) {
            brandMark(size: 14)
            Spacer(minLength: 4)
            // Two equal columns, each centring its own content. Giving the
            // photo a fixed width instead made it hug the leading edge while
            // the hero absorbed all the remaining space.
            HStack(spacing: 12) {
                if photoImage != nil {
                    photo(side: 96)
                        .frame(maxWidth: .infinity)
                }
                hero(nameFont: .system(.footnote, design: .rounded),
                     rankFont: .system(.largeTitle, design: .rounded),
                     pointsFont: .system(.footnote, design: .rounded),
                     alignment: .center)
                .frame(maxWidth: .infinity)
            }
            Spacer(minLength: 4)
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity)
        .padding(.vertical, 2)
    }

    private var largeLayout: some View {
        VStack(alignment: .leading, spacing: 0) {
            brandMark(size: 16)

            Spacer(minLength: 8)

            // Two equal columns, matching the medium layout.
            HStack(spacing: 12) {
                if photoImage != nil {
                    photo(side: 112)
                        .frame(maxWidth: .infinity)
                }
                hero(nameFont: .system(.subheadline, design: .rounded),
                     rankFont: .system(.largeTitle, design: .rounded),
                     pointsFont: .system(.subheadline, design: .rounded),
                     alignment: .center)
                .frame(maxWidth: .infinity)
            }

            Spacer(minLength: 12)

            Divider().overlay(Brand.tertiary)

            Spacer(minLength: 8)

            statsGrid

            Spacer(minLength: 4)
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .top)
    }

    /// Two columns with a real gutter. The previous grid gave the columns no
    /// horizontal spacing, so a value in the left column butted against the
    /// label in the right one.
    private var statsGrid: some View {
        let stats: [(String, String)] = [
            ("Eff. Pct", IfpaStat.percent(openStats?.efficiencyValue)),
            ("Eff. Rank", IfpaStat.ordinal(openStats?.efficiencyRank)),
            ("Events", IfpaStat.count(openStats?.totalEventsAllTime)),
            ("Best Finish", IfpaStat.ordinal(openStats?.bestFinish)),
            ("Avg Finish", IfpaStat.ordinal(openStats?.averageFinish)),
            ("Highest Rank", IfpaStat.ordinal(openStats?.highestRank)),
        ]

        return LazyVGrid(
            columns: [
                GridItem(.flexible(), spacing: 18, alignment: .leading),
                GridItem(.flexible(), spacing: 18, alignment: .leading),
            ],
            alignment: .leading,
            spacing: 10
        ) {
            ForEach(stats, id: \.0) { stat in
                HStack(alignment: .firstTextBaseline, spacing: 6) {
                    Text(stat.0)
                        .font(.system(.caption, design: .rounded))
                        .foregroundStyle(Brand.secondary)
                        .lineLimit(1)
                        .minimumScaleFactor(0.8)
                    Spacer(minLength: 4)
                    Text(stat.1)
                        .font(.system(.caption, design: .rounded).weight(.semibold))
                        .foregroundStyle(Brand.primary)
                        .monospacedDigit()
                        .lineLimit(1)
                }
            }
        }
    }

    // MARK: Lock Screen layouts
    //
    // Accessory widgets are rendered monochrome by the system, so these carry
    // no colour of their own and rely on shape and weight instead.

    private var circularLayout: some View {
        VStack(spacing: -2) {
            Text("IFPA")
                .font(.system(size: 9, weight: .semibold, design: .rounded))
                .tracking(0.6)
            Text(rankNumberText)
                .font(.system(.title2, design: .rounded).weight(.heavy))
                .lineLimit(1)
                .minimumScaleFactor(0.35)
        }
        .padding(.horizontal, 7)
        .widgetAccentable()
    }

    #if os(watchOS)
    /// Watch-face corner. The curved label is where the IFPA number goes,
    /// since that is the thing a player needs to read out at a registration
    /// desk and the corner has no room for it inline.
    private var cornerLayout: some View {
        Text(rankNumberText)
            .font(.system(.title3, design: .rounded).weight(.heavy))
            .lineLimit(1)
            .minimumScaleFactor(0.4)
            .widgetLabel(numberLabel)
    }
    #endif

    private var rectangularLayout: some View {
        VStack(alignment: .center, spacing: 1) {
            // The number, not the player's own name. This is the family with
            // room for it, and reading it out at a registration desk is the
            // reason it is on a wrist at all.
            Text(numberLabel)
                .font(.system(.caption2, design: .rounded).weight(.semibold))
                .monospacedDigit()
                .lineLimit(1)
                .minimumScaleFactor(0.7)
                .widgetAccentable()
            Text(rankText)
                .font(.system(.title3, design: .rounded).weight(.heavy))
                .lineLimit(1)
            Text("\(pointsText) pts")
                .font(.system(.caption2, design: .rounded))
                .lineLimit(1)
                .minimumScaleFactor(0.7)
        }
        .multilineTextAlignment(.center)
        .frame(maxWidth: .infinity, maxHeight: .infinity)
    }
}

// MARK: - Content margins

/// iOS 17 insets widget content automatically. iOS 16 does not, and the
/// deployment target is 16.2, so supply the inset ourselves on the older OS
/// only — otherwise content sits flush against the corner radius there, which
/// clipped the profile photo and the brand mark.
private struct LegacyContentMargins: ViewModifier {
    let isAccessory: Bool

    func body(content: Content) -> some View {
        if #available(iOS 17.0, watchOS 10.0, *) {
            content
        } else if isAccessory {
            content
        } else {
            content.padding(16)
        }
    }
}

// MARK: - Container background

/// System families keep the navy ground. Accessory families must stay
/// transparent so the Lock Screen shows through, and the circular family gets
/// the standard translucent platter.
private struct WidgetGround: ViewModifier {
    let isAccessory: Bool
    let family: WidgetFamily

    func body(content: Content) -> some View {
        if #available(iOS 17.0, watchOS 10.0, *) {
            if family == .accessoryCircular {
                content.containerBackground(for: .widget) { AccessoryWidgetBackground() }
            } else if isAccessory {
                content.containerBackground(for: .widget) { Color.clear }
            } else {
                content.containerBackground(for: .widget) { Brand.ground }
            }
        } else {
            if family == .accessoryCircular {
                ZStack {
                    AccessoryWidgetBackground()
                    content
                }
            } else if isAccessory {
                content
            } else {
                ZStack {
                    ContainerRelativeShape().fill(Brand.ground)
                    content
                }
            }
        }
    }
}

// MARK: - Widget

struct RankWidget: Widget {
    let kind: String = "RankWidget"

    var body: some WidgetConfiguration {
        #if os(watchOS)
        // Static rather than intent-configured: the watch takes its player
        // from the phone over WatchConnectivity, so a watch-face picker would
        // have nothing to offer.
        StaticConfiguration(kind: kind, provider: Provider()) { entry in
            RankWidgetEntryView(entry: entry)
        }
        .configurationDisplayName("IFPA Rank")
        .description("Show Current My Stats Player's Rank")
        .supportedFamilies([
            .accessoryCircular,
            .accessoryRectangular,
            .accessoryInline,
            .accessoryCorner,
        ])
        #else
        IntentConfiguration(kind: kind, intent: ConfigurationIntent.self, provider: Provider()) { entry in
            RankWidgetEntryView(entry: entry)
        }
        .configurationDisplayName("IFPA Rank")
        .description("Show Current My Stats Player's Rank")
        .supportedFamilies([
            .systemSmall,
            .systemMedium,
            .systemLarge,
            .accessoryCircular,
            .accessoryRectangular,
            .accessoryInline,
        ])
        #endif
    }
}

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

