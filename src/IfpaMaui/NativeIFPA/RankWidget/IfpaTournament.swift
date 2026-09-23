//
//  IfpaTournament.swift
//  RankWidget
//
//  Models and loading for the IFPA `tournament/search` endpoint, used by the
//  calendar widget.
//
//  The search runs with the same filter as the app's Calendar tab. The app
//  mirrors that filter into the shared app group (see Settings.cs), so the
//  widget and the tab show the same events.
//

import Foundation
import CoreLocation

// MARK: - Filter

/// The Calendar tab's filter, as the app writes it into the app group.
struct CalendarFilter: Equatable {
    static let suiteName = "group.com.edgiardina.ifpa"

    // Keys match Settings.cs. The app writes them with MAUI Preferences,
    // which stores plain NSUserDefaults values under the key as given.
    private static let locationKey = "CalendarLocation"
    private static let distanceKey = "CalendarDistance"
    private static let rankingSystemKey = "CalendarRankingSystem"
    private static let showLeaguesKey = "CalendarShowLeagues"

    var location: String
    var distanceMiles: Int
    /// "All", "Main" or "Women", the values of the app's picker.
    var rankingSystem: String
    var showLeagues: Bool

    /// The app's own defaults, for a phone that has never opened the filter.
    static let fallback = CalendarFilter(location: "Chicago, Il",
                                         distanceMiles: 150,
                                         rankingSystem: "All",
                                         showLeagues: false)

    static func load() -> CalendarFilter {
        guard let defaults = UserDefaults(suiteName: suiteName) else { return fallback }

        var filter = fallback
        if let location = defaults.string(forKey: locationKey),
           !location.trimmingCharacters(in: .whitespaces).isEmpty {
            filter.location = location
        }
        let distance = defaults.integer(forKey: distanceKey)
        if distance > 0 {
            filter.distanceMiles = distance
        }
        if let system = defaults.string(forKey: rankingSystemKey), !system.isEmpty {
            filter.rankingSystem = system
        }
        filter.showLeagues = defaults.bool(forKey: showLeaguesKey)
        return filter
    }

    /// Identifies a cached payload, so a filter change never shows events
    /// from the previous search.
    var signature: String {
        return "\(location)|\(distanceMiles)|\(rankingSystem)|\(showLeagues)"
    }
}

// MARK: - Tournament

struct IfpaTournament: Identifiable {
    let id: Int
    let name: String
    let city: String?
    let stateprov: String?
    /// Local midnight of the first day.
    let startDay: Date
    /// Local midnight of the last day. Equal to `startDay` for a one-day event.
    let endDay: Date

    var place: String {
        return [city, stateprov]
            .compactMap { $0 }
            .filter { !$0.isEmpty }
            .joined(separator: ", ")
    }

    /// The app routes this URL through DeepLinkService, the same as a shared
    /// tournament link.
    var url: URL? {
        return URL(string: "https://www.ifpapinball.com/tournaments/view.php?t=\(id)")
    }

    func covers(_ day: Date) -> Bool {
        return day >= startDay && day <= endDay
    }
}

/// Decodes the raw search result. Every field goes through `looseString`,
/// the same as the player model, so a tournament with an odd field is dropped
/// on its own instead of failing the whole list.
private struct TournamentSearchResponse: Decodable {
    let tournaments: [RawTournament]

    enum CodingKeys: String, CodingKey {
        case tournaments
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        tournaments = container.optionalValue([RawTournament].self, .tournaments) ?? []
    }
}

private struct RawTournament: Decodable {
    let id: String?
    let name: String?
    let city: String?
    let stateprov: String?
    let startDate: String?
    let endDate: String?

    enum CodingKeys: String, CodingKey {
        case id = "tournament_id"
        case name = "tournament_name"
        case city
        case stateprov
        case startDate = "event_start_date"
        case endDate = "event_end_date"
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = container.looseString(.id)
        name = container.looseString(.name)
        city = container.looseString(.city)
        stateprov = container.looseString(.stateprov)
        startDate = container.looseString(.startDate)
        endDate = container.looseString(.endDate)
    }
}

// MARK: - Search

enum TournamentSearch {

    /// How far ahead the widget looks. Enough to fill the rest of this month
    /// and the upcoming list when the month is nearly over.
    static let daysAhead = 62

    /// The endpoint sends a calendar date with no zone. Parse only the date
    /// part, in the phone's zone, so an event never slides to the previous
    /// day in the western hemisphere.
    private static let dayFormatter: DateFormatter = {
        let formatter = DateFormatter()
        formatter.calendar = Calendar(identifier: .gregorian)
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.timeZone = TimeZone.current
        formatter.dateFormat = "yyyy-MM-dd"
        return formatter
    }()

    static func day(from raw: String?) -> Date? {
        guard let raw = raw, raw.count >= 10 else { return nil }
        let datePart = String(raw.prefix(10))
        guard datePart != "0000-00-00" else { return nil }
        return dayFormatter.date(from: datePart)
    }

    static func payload(filter: CalendarFilter, coordinate: CLLocationCoordinate2D, from today: Date) async throws -> Data {
        guard let apiKey = Bundle.main.object(forInfoDictionaryKey: "IFPAApiKey") as? String,
              !apiKey.isEmpty else {
            throw IfpaError.missingApiKey
        }

        let end = Calendar.current.date(byAdding: .day, value: daysAhead, to: today) ?? today

        // Same parameters as PinballApi's TournamentSearch, which the app's
        // Calendar tab calls.
        var items = [
            URLQueryItem(name: "api_key", value: apiKey),
            URLQueryItem(name: "latitude", value: String(coordinate.latitude)),
            URLQueryItem(name: "longitude", value: String(coordinate.longitude)),
            URLQueryItem(name: "m", value: String(filter.distanceMiles)),
            URLQueryItem(name: "radius", value: String(filter.distanceMiles)),
            URLQueryItem(name: "start_date", value: dayFormatter.string(from: today)),
            URLQueryItem(name: "end_date", value: dayFormatter.string(from: end)),
            URLQueryItem(name: "total", value: "250"),
        ]
        if filter.rankingSystem != "All" {
            items.append(URLQueryItem(name: "rank_type", value: filter.rankingSystem.uppercased()))
        }
        if !filter.showLeagues {
            items.append(URLQueryItem(name: "event_type", value: "TOURNAMENT"))
        }

        var components = URLComponents(string: "https://api.ifpapinball.com/tournament/search")
        components?.queryItems = items

        guard let url = components?.url else {
            throw IfpaError.invalidUrl
        }

        let (data, response) = try await URLSession.shared.data(from: url)

        if let http = response as? HTTPURLResponse, !(200..<300).contains(http.statusCode) {
            throw IfpaError.httpStatus(http.statusCode)
        }

        return data
    }

    /// Sorted by start day, then name. Tournaments with no id, no name or no
    /// start date are dropped.
    static func decode(_ data: Data) throws -> [IfpaTournament] {
        let response = try JSONDecoder().decode(TournamentSearchResponse.self, from: data)

        return response.tournaments
            .compactMap { raw -> IfpaTournament? in
                guard let idText = raw.id, let id = Int(idText),
                      let name = raw.name, !name.isEmpty,
                      let start = day(from: raw.startDate) else {
                    return nil
                }
                let end = day(from: raw.endDate).map { max($0, start) } ?? start
                return IfpaTournament(id: id,
                                      name: name,
                                      city: raw.city,
                                      stateprov: raw.stateprov,
                                      startDay: start,
                                      endDay: end)
            }
            .sorted { ($0.startDay, $0.name) < ($1.startDay, $1.name) }
    }
}

// MARK: - Geocoding

enum CalendarGeocoder {
    private static let cacheKey = "CachedCalendarGeocode"

    /// Chicago, the same default the app uses when geocoding fails.
    static let fallback = CLLocationCoordinate2D(latitude: 41.8781, longitude: -87.6298)

    /// Resolves the filter's free-text location. A result is cached against
    /// the text, because the location changes rarely and the geocoder is
    /// rate limited.
    static func coordinate(for address: String) async -> CLLocationCoordinate2D {
        let defaults = UserDefaults(suiteName: CalendarFilter.suiteName)

        if let cached = defaults?.dictionary(forKey: cacheKey),
           cached["address"] as? String == address,
           let latitude = cached["latitude"] as? Double,
           let longitude = cached["longitude"] as? Double {
            return CLLocationCoordinate2D(latitude: latitude, longitude: longitude)
        }

        do {
            let placemarks = try await CLGeocoder().geocodeAddressString(address)
            if let coordinate = placemarks.first?.location?.coordinate {
                defaults?.set(["address": address,
                               "latitude": coordinate.latitude,
                               "longitude": coordinate.longitude],
                              forKey: cacheKey)
                return coordinate
            }
        } catch {
            // Fall through to the default below.
        }
        return fallback
    }
}
