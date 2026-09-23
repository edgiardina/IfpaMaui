//
//  CalendarWidget.swift
//  RankWidget
//
//  Month calendar of upcoming IFPA tournaments near the location set in the
//  app's Calendar tab. Days with an event are filled, and the larger sizes
//  list the next few events beside or below the month.
//
//  iOS only. The watch targets do not compile this file.
//

import WidgetKit
import SwiftUI
import CoreLocation

// MARK: - Timeline

struct CalendarEntry: TimelineEntry {
    let date: Date
    let filter: CalendarFilter
    /// Nil when no search has ever succeeded for this filter. Empty when the
    /// search succeeded and found nothing.
    var tournaments: [IfpaTournament]?
}

struct CalendarProvider: TimelineProvider {

    private static let cachedPayloadKey = "CachedCalendarPayload"
    private static let cachedSignatureKey = "CachedCalendarPayloadFilter"

    private var sharedDefaults: UserDefaults? {
        return UserDefaults(suiteName: CalendarFilter.suiteName)
    }

    func placeholder(in context: Context) -> CalendarEntry {
        return CalendarEntry(date: Date(), filter: .fallback, tournaments: [])
    }

    func getSnapshot(in context: Context, completion: @escaping (CalendarEntry) -> ()) {
        Task {
            completion(await loadEntry(at: Date()))
        }
    }

    /// One entry now and one at each of the next two midnights, so the
    /// "today" ring moves on time even when the refresh runs late. The
    /// entries share one search, and the view drops events that have ended.
    func getTimeline(in context: Context, completion: @escaping (Timeline<CalendarEntry>) -> ()) {
        Task {
            let now = Date()
            let entry = await loadEntry(at: now)

            let calendar = Calendar.current
            let today = calendar.startOfDay(for: now)
            let midnights = (1...2).compactMap { calendar.date(byAdding: .day, value: $0, to: today) }
            let entries = [entry] + midnights.map {
                CalendarEntry(date: $0, filter: entry.filter, tournaments: entry.tournaments)
            }

            // The calendar changes slowly. A few hours also bounds how long a
            // filter change in the app waits before the widget shows it.
            let refresh = calendar.date(byAdding: .hour, value: 3, to: now) ?? now.addingTimeInterval(3 * 3600)
            completion(Timeline(entries: entries, policy: .after(refresh)))
        }
    }

    /// Always produces an entry. A failed search falls back to the last one
    /// that succeeded for the same filter.
    func loadEntry(at now: Date) async -> CalendarEntry {
        let filter = CalendarFilter.load()
        let today = Calendar.current.startOfDay(for: now)

        do {
            let coordinate = await CalendarGeocoder.coordinate(for: filter.location)
            let data = try await TournamentSearch.payload(filter: filter, coordinate: coordinate, from: today)
            let tournaments = try TournamentSearch.decode(data)

            sharedDefaults?.set(data, forKey: Self.cachedPayloadKey)
            sharedDefaults?.set(filter.signature, forKey: Self.cachedSignatureKey)

            return CalendarEntry(date: now, filter: filter, tournaments: tournaments)
        } catch {
            return CalendarEntry(date: now, filter: filter, tournaments: cachedTournaments(for: filter))
        }
    }

    private func cachedTournaments(for filter: CalendarFilter) -> [IfpaTournament]? {
        guard let defaults = sharedDefaults,
              defaults.string(forKey: Self.cachedSignatureKey) == filter.signature,
              let data = defaults.data(forKey: Self.cachedPayloadKey) else {
            return nil
        }
        return try? TournamentSearch.decode(data)
    }
}

// MARK: - Brand

/// Same navy ground as the rank widget, so the two sit together on a Home
/// Screen. Gold marks a day with an event.
private enum CalendarBrand {
    static let ground = Color(hex: 0x062C53)
    static let primary = Color.white
    static let secondary = Color.white.opacity(0.68)
    static let tertiary = Color.white.opacity(0.4)
    static let accent = Color(hex: 0xF2B233)
}

/// Opens the app on the Calendar tab. DeepLinkService maps this path to the
/// tab route.
private let calendarTabUrl = URL(string: "https://www.ifpapinball.com/calendar")

// MARK: - Month grid

struct MonthGrid: View {
    let today: Date
    let eventDays: Set<Date>
    let dayFont: Font
    let weekdayFont: Font

    private var calendar: Calendar { Calendar.current }

    /// Weeks of the current month, padded with nil on either side so every
    /// row has seven cells. The first column follows the locale's first
    /// weekday.
    private var weeks: [[Date?]] {
        guard let month = calendar.dateInterval(of: .month, for: today),
              let dayCount = calendar.range(of: .day, in: .month, for: today)?.count else {
            return []
        }

        let leading = (calendar.component(.weekday, from: month.start) - calendar.firstWeekday + 7) % 7
        var cells: [Date?] = Array(repeating: nil, count: leading)
        for offset in 0..<dayCount {
            cells.append(calendar.date(byAdding: .day, value: offset, to: month.start))
        }
        while cells.count % 7 != 0 {
            cells.append(nil)
        }

        return stride(from: 0, to: cells.count, by: 7).map { Array(cells[$0..<($0 + 7)]) }
    }

    private var weekdaySymbols: [String] {
        let symbols = calendar.veryShortStandaloneWeekdaySymbols
        let shift = calendar.firstWeekday - 1
        return Array(symbols[shift...] + symbols[..<shift])
    }

    var body: some View {
        VStack(spacing: 1) {
            HStack(spacing: 0) {
                ForEach(Array(weekdaySymbols.enumerated()), id: \.offset) { _, symbol in
                    Text(symbol)
                        .font(weekdayFont.weight(.semibold))
                        .foregroundStyle(CalendarBrand.tertiary)
                        .frame(maxWidth: .infinity)
                }
            }

            ForEach(Array(weeks.enumerated()), id: \.offset) { _, week in
                HStack(spacing: 0) {
                    ForEach(Array(week.enumerated()), id: \.offset) { _, day in
                        cell(for: day)
                    }
                }
                .frame(maxHeight: .infinity)
            }
        }
    }

    @ViewBuilder
    private func cell(for day: Date?) -> some View {
        if let day = day {
            let isToday = calendar.isDate(day, inSameDayAs: today)
            let hasEvent = eventDays.contains(day)
            let isPast = day < calendar.startOfDay(for: today)

            ZStack {
                if hasEvent {
                    Circle().fill(CalendarBrand.accent)
                }
                if isToday {
                    Circle().strokeBorder(CalendarBrand.primary, lineWidth: 1.5)
                }
                Text("\(calendar.component(.day, from: day))")
                    .font(dayFont.weight(hasEvent || isToday ? .bold : .regular))
                    .monospacedDigit()
                    .foregroundStyle(hasEvent ? CalendarBrand.ground
                                     : isPast ? CalendarBrand.tertiary
                                     : CalendarBrand.primary)
                    .lineLimit(1)
                    .minimumScaleFactor(0.6)
            }
            // Square cells, so a filled day reads as a circle and not an
            // ellipse, whatever the widget's aspect ratio.
            .aspectRatio(1, contentMode: .fit)
            // Capped, so the large size shows a calendar and not a row of
            // discs.
            .frame(maxWidth: 30, maxHeight: 30)
            .padding(1)
            .frame(maxWidth: .infinity, maxHeight: .infinity)
        } else {
            Color.clear
                .frame(maxWidth: .infinity, maxHeight: .infinity)
        }
    }
}

// MARK: - Tournament row

private struct TournamentRow: View {
    let tournament: IfpaTournament
    let nameFont: Font
    let detailFont: Font

    private var monthText: String {
        tournament.startDay.formatted(.dateTime.month(.abbreviated)).uppercased()
    }

    private var dayText: String {
        tournament.startDay.formatted(.dateTime.day())
    }

    /// Place, led by the date range for a multi-day event.
    private var detailText: String {
        var parts: [String] = []
        if tournament.endDay > tournament.startDay {
            parts.append((tournament.startDay..<tournament.endDay)
                .formatted(.interval.month(.abbreviated).day()))
        }
        if !tournament.place.isEmpty {
            parts.append(tournament.place)
        }
        return parts.joined(separator: " · ")
    }

    var body: some View {
        HStack(spacing: 8) {
            VStack(spacing: -1) {
                Text(monthText)
                    .font(.system(size: 9, weight: .bold, design: .rounded))
                    .foregroundStyle(CalendarBrand.accent)
                Text(dayText)
                    .font(.system(.callout, design: .rounded).weight(.bold))
                    .monospacedDigit()
                    .foregroundStyle(CalendarBrand.primary)
            }
            .frame(width: 28)

            VStack(alignment: .leading, spacing: 1) {
                Text(tournament.name)
                    .font(nameFont.weight(.semibold))
                    .foregroundStyle(CalendarBrand.primary)
                    .lineLimit(1)
                if !detailText.isEmpty {
                    Text(detailText)
                        .font(detailFont)
                        .foregroundStyle(CalendarBrand.secondary)
                        .lineLimit(1)
                }
            }
            .frame(maxWidth: .infinity, alignment: .leading)
        }
    }
}

// MARK: - Entry view

struct CalendarWidgetEntryView: View {
    let entry: CalendarEntry
    @Environment(\.widgetFamily) var family

    private var calendar: Calendar { Calendar.current }
    private var today: Date { calendar.startOfDay(for: entry.date) }

    /// Events that have not ended, soonest first.
    private var upcoming: [IfpaTournament] {
        (entry.tournaments ?? []).filter { $0.endDay >= today }
    }

    /// Every day covered by an upcoming event, as local midnights to match
    /// the grid's cells.
    private var eventDays: Set<Date> {
        var days = Set<Date>()
        for tournament in upcoming {
            var day = calendar.startOfDay(for: tournament.startDay)
            let last = calendar.startOfDay(for: tournament.endDay)
            while day <= last {
                days.insert(day)
                guard let next = calendar.date(byAdding: .day, value: 1, to: day) else { break }
                day = next
            }
        }
        return days
    }

    private var isAccessory: Bool {
        family == .accessoryRectangular
    }

    var body: some View {
        layout
            .modifier(CalendarGround(isAccessory: isAccessory))
    }

    @ViewBuilder
    private var layout: some View {
        switch family {
        case .systemMedium:
            mediumLayout
        case .systemLarge, .systemExtraLarge:
            largeLayout
        case .accessoryRectangular:
            rectangularLayout
        default:
            smallLayout
        }
    }

    // MARK: Pieces

    private func monthTitle(_ format: Date.FormatStyle) -> some View {
        Text(today.formatted(format).uppercased())
            .font(.system(.caption2, design: .rounded).weight(.bold))
            .tracking(0.8)
            .foregroundStyle(CalendarBrand.accent)
            .lineLimit(1)
    }

    private var icon: some View {
        Image("ifpa_icon")
            .resizable()
            .aspectRatio(contentMode: .fit)
            .frame(width: 14, height: 14)
    }

    private func grid(dayFont: Font, weekdayFont: Font) -> some View {
        MonthGrid(today: today, eventDays: eventDays, dayFont: dayFont, weekdayFont: weekdayFont)
    }

    /// "150 mi · Chicago, Il", so it is clear which search the dots are for.
    private var filterText: String {
        "\(entry.filter.distanceMiles) mi · \(entry.filter.location)"
    }

    /// Shown in place of the list when there is nothing to list.
    @ViewBuilder
    private var emptyMessage: some View {
        let text = entry.tournaments == nil
            ? "Tournaments not available."
            : "No tournaments within \(entry.filter.distanceMiles) mi of \(entry.filter.location)."
        Text(text)
            .font(.system(.caption, design: .rounded))
            .foregroundStyle(CalendarBrand.secondary)
            .multilineTextAlignment(.leading)
            .frame(maxWidth: .infinity, alignment: .leading)
    }

    private func list(limit: Int, nameFont: Font, detailFont: Font, spacing: CGFloat) -> some View {
        VStack(alignment: .leading, spacing: spacing) {
            if upcoming.isEmpty {
                emptyMessage
            } else {
                ForEach(upcoming.prefix(limit)) { tournament in
                    if let url = tournament.url {
                        Link(destination: url) {
                            TournamentRow(tournament: tournament, nameFont: nameFont, detailFont: detailFont)
                        }
                    } else {
                        TournamentRow(tournament: tournament, nameFont: nameFont, detailFont: detailFont)
                    }
                }
            }
        }
    }

    // MARK: System layouts

    /// The month on its own. A tap opens the Calendar tab, since there is no
    /// room to tell events apart.
    private var smallLayout: some View {
        VStack(spacing: 3) {
            HStack(spacing: 4) {
                monthTitle(.dateTime.month(.wide))
                Spacer(minLength: 0)
                icon
            }
            grid(dayFont: .system(size: 10, design: .rounded),
                 weekdayFont: .system(size: 8, design: .rounded))
        }
        .widgetURL(calendarTabUrl)
    }

    /// Month on the left, the next three events on the right, the same split
    /// as the system Calendar widget.
    private var mediumLayout: some View {
        HStack(alignment: .top, spacing: 14) {
            VStack(spacing: 3) {
                HStack(spacing: 4) {
                    monthTitle(.dateTime.month(.wide))
                    Spacer(minLength: 0)
                }
                grid(dayFont: .system(size: 10, design: .rounded),
                     weekdayFont: .system(size: 8, design: .rounded))
            }
            .frame(width: 128)

            VStack(alignment: .leading, spacing: 6) {
                HStack(spacing: 4) {
                    Text("UP NEXT")
                        .font(.system(.caption2, design: .rounded).weight(.bold))
                        .tracking(0.8)
                        .foregroundStyle(CalendarBrand.tertiary)
                    Spacer(minLength: 0)
                    icon
                }
                list(limit: 3,
                     nameFont: .system(.caption, design: .rounded),
                     detailFont: .system(.caption2, design: .rounded),
                     spacing: 6)
            }
            .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .top)
        }
        .widgetURL(calendarTabUrl)
    }

    private var largeLayout: some View {
        VStack(alignment: .leading, spacing: 6) {
            HStack(spacing: 5) {
                icon
                monthTitle(.dateTime.month(.wide).year())
                Spacer(minLength: 4)
                Text(filterText)
                    .font(.system(.caption2, design: .rounded))
                    .foregroundStyle(CalendarBrand.secondary)
                    .lineLimit(1)
                    .truncationMode(.middle)
            }

            // The list takes its natural height and the month fills the rest,
            // so the calendar stays the larger part of the widget.
            grid(dayFont: .system(.footnote, design: .rounded),
                 weekdayFont: .system(size: 10, design: .rounded))
                .frame(maxHeight: .infinity)

            Divider().overlay(CalendarBrand.tertiary)

            list(limit: 3,
                 nameFont: .system(.footnote, design: .rounded),
                 detailFont: .system(.caption, design: .rounded),
                 spacing: 7)
        }
        .widgetURL(calendarTabUrl)
    }

    // MARK: Lock Screen

    /// The next event. Rendered monochrome by the system, so it relies on
    /// weight rather than colour.
    @ViewBuilder
    private var rectangularLayout: some View {
        if let next = upcoming.first {
            VStack(alignment: .leading, spacing: 0) {
                Text(next.startDay.formatted(.dateTime.weekday(.abbreviated).month(.abbreviated).day()))
                    .font(.system(.caption2, design: .rounded).weight(.semibold))
                    .widgetAccentable()
                    .lineLimit(1)
                Text(next.name)
                    .font(.system(.headline, design: .rounded))
                    .lineLimit(2)
                    .minimumScaleFactor(0.8)
                if !next.place.isEmpty {
                    Text(next.place)
                        .font(.system(.caption2, design: .rounded))
                        .lineLimit(1)
                }
            }
            .frame(maxWidth: .infinity, alignment: .leading)
            .widgetURL(next.url)
        } else {
            VStack(alignment: .leading, spacing: 0) {
                Text("IFPA Calendar")
                    .font(.system(.caption2, design: .rounded).weight(.semibold))
                    .widgetAccentable()
                Text(entry.tournaments == nil ? "Not available" : "No upcoming tournaments")
                    .font(.system(.footnote, design: .rounded))
            }
            .frame(maxWidth: .infinity, alignment: .leading)
            .widgetURL(calendarTabUrl)
        }
    }
}

// MARK: - Container background

/// Navy ground for the Home Screen sizes, transparent on the Lock Screen.
/// iOS 16 has no container background and does not inset widget content, so
/// the older path supplies both.
private struct CalendarGround: ViewModifier {
    let isAccessory: Bool

    func body(content: Content) -> some View {
        if #available(iOS 17.0, *) {
            if isAccessory {
                content.containerBackground(for: .widget) { Color.clear }
            } else {
                content.containerBackground(for: .widget) { CalendarBrand.ground }
            }
        } else if isAccessory {
            content
        } else {
            ZStack {
                ContainerRelativeShape().fill(CalendarBrand.ground)
                content.padding(16)
            }
        }
    }
}

// MARK: - Widget

struct CalendarWidget: Widget {
    let kind: String = "CalendarWidget"

    var body: some WidgetConfiguration {
        StaticConfiguration(kind: kind, provider: CalendarProvider()) { entry in
            CalendarWidgetEntryView(entry: entry)
        }
        .configurationDisplayName("IFPA Calendar")
        .description("Upcoming tournaments near your Calendar location.")
        .supportedFamilies([
            .systemSmall,
            .systemMedium,
            .systemLarge,
            .accessoryRectangular,
        ])
    }
}
