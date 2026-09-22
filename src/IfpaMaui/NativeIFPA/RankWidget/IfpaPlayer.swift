//
//  IfpaPlayer.swift
//  RankWidget
//
//  Models for the IFPA `player/{id}` endpoint.
//
//  The endpoint returns almost every scalar as a quoted string, and the set of
//  fields it populates changes per player: a player who is not in the Open
//  system has no `player_stats.system.open` object at all, and sparse players
//  send null or an empty string where an established player sends a number.
//  Decoding is therefore total on purpose. Every scalar goes through
//  `looseString`, every field is optional, and a shape change surfaces as a
//  missing value in the widget instead of a thrown error.
//

import Foundation

// MARK: - Errors

enum IfpaError: Error {
    case missingApiKey
    case invalidUrl
    case httpStatus(Int)
}

// MARK: - Loose scalar decoding

/// Decodes a JSON string, number, bool, or null into an optional string.
/// The IFPA API is not consistent about which of those it sends for a given
/// field, so the widget accepts all of them rather than crashing on the ones
/// it did not expect.
struct LooseScalar: Decodable {
    let text: String?

    init(from decoder: Decoder) throws {
        let container = try decoder.singleValueContainer()

        if container.decodeNil() {
            text = nil
        } else if let string = try? container.decode(String.self) {
            text = string
        } else if let int = try? container.decode(Int.self) {
            text = String(int)
        } else if let double = try? container.decode(Double.self) {
            text = String(double)
        } else if let bool = try? container.decode(Bool.self) {
            text = bool ? "true" : "false"
        } else {
            text = nil
        }
    }
}

extension KeyedDecodingContainer {
    /// Reads a scalar without throwing. A missing key, a null, or an
    /// unexpected JSON type all yield nil.
    func looseString(_ key: Key) -> String? {
        guard let scalar = try? decodeIfPresent(LooseScalar.self, forKey: key) else { return nil }
        return scalar.text
    }

    /// Reads a nested value without throwing, so one malformed sub-object
    /// cannot fail the whole player.
    func optionalValue<T: Decodable>(_ type: T.Type, _ key: Key) -> T? {
        return (try? decodeIfPresent(type, forKey: key)) ?? nil
    }
}

// MARK: - IfpaPlayer

struct IfpaPlayer: Decodable {
    let player: [Player]

    /// The endpoint wraps a single player in a one-element array.
    var firstPlayer: Player? { player.first }

    enum CodingKeys: String, CodingKey {
        case player
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        player = container.optionalValue([Player].self, .player) ?? []
    }

    static func getPlayerById(from playerId: Int) async throws -> IfpaPlayer {
        guard let apiKey = Bundle.main.object(forInfoDictionaryKey: "IFPAApiKey") as? String,
              !apiKey.isEmpty else {
            throw IfpaError.missingApiKey
        }

        var components = URLComponents(string: "https://api.ifpapinball.com/player/\(playerId)")
        components?.queryItems = [URLQueryItem(name: "api_key", value: apiKey)]

        guard let url = components?.url else {
            throw IfpaError.invalidUrl
        }

        let (data, response) = try await URLSession.shared.data(from: url)

        // The API answers an unknown player id with 401, not 404, so any
        // non-success status has to be treated as "no player".
        if let http = response as? HTTPURLResponse, !(200..<300).contains(http.statusCode) {
            throw IfpaError.httpStatus(http.statusCode)
        }

        return try JSONDecoder().decode(IfpaPlayer.self, from: data)
    }
}

// MARK: - Player

struct Player: Decodable {
    let playerID: String?
    let firstName: String?
    let lastName: String?
    let initials: String?
    let excludedFlag: String?
    let age: String?
    let city: String?
    let stateprov: String?
    let countryName: String?
    let countryCode: String?
    let ifpaRegistered: String?
    let womensFlag: String?
    let profilePhoto: String?
    let matchplayEvents: MatchplayEvents?
    let twitchUsername: String?
    let pinsideUsername: String?
    let playerStats: PlayerStats?
    let series: [Series]?

    /// Stats for the Open (Main) system, absent for players who are not
    /// ranked in it.
    var openStats: PlayerStatsOpen? { playerStats?.system?.open }

    var displayName: String {
        return [firstName, lastName]
            .compactMap { $0 }
            .filter { !$0.isEmpty }
            .joined(separator: " ")
    }

    enum CodingKeys: String, CodingKey {
        case playerID = "player_id"
        case firstName = "first_name"
        case lastName = "last_name"
        case initials
        case excludedFlag = "excluded_flag"
        case age
        case city
        case stateprov
        case countryName = "country_name"
        case countryCode = "country_code"
        case ifpaRegistered = "ifpa_registered"
        case womensFlag = "womens_flag"
        case profilePhoto = "profile_photo"
        case matchplayEvents = "matchplay_events"
        case twitchUsername = "twitch_username"
        case pinsideUsername = "pinside_username"
        case playerStats = "player_stats"
        case series
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)

        playerID = container.looseString(.playerID)
        firstName = container.looseString(.firstName)
        lastName = container.looseString(.lastName)
        initials = container.looseString(.initials)
        excludedFlag = container.looseString(.excludedFlag)
        age = container.looseString(.age)
        city = container.looseString(.city)
        stateprov = container.looseString(.stateprov)
        countryName = container.looseString(.countryName)
        countryCode = container.looseString(.countryCode)
        ifpaRegistered = container.looseString(.ifpaRegistered)
        womensFlag = container.looseString(.womensFlag)
        profilePhoto = container.looseString(.profilePhoto)
        twitchUsername = container.looseString(.twitchUsername)
        pinsideUsername = container.looseString(.pinsideUsername)

        matchplayEvents = container.optionalValue(MatchplayEvents.self, .matchplayEvents)
        playerStats = container.optionalValue(PlayerStats.self, .playerStats)
        series = container.optionalValue([Series].self, .series)
    }
}

// MARK: - MatchplayEvents

struct MatchplayEvents: Decodable {
    let id: String?
    let rating: String?
    let rank: String?

    enum CodingKeys: String, CodingKey {
        case id, rating, rank
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = container.looseString(.id)
        rating = container.looseString(.rating)
        rank = container.looseString(.rank)
    }
}

// MARK: - PlayerStats

struct PlayerStats: Decodable {
    let system: PlayerStatsSystem?
    let yearsActive: String?

    enum CodingKeys: String, CodingKey {
        case system
        case yearsActive = "years_active"
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        system = container.optionalValue(PlayerStatsSystem.self, .system)
        yearsActive = container.looseString(.yearsActive)
    }
}

/// `system` is an object keyed by ranking system name, not an array. A player
/// ranked only in the Women's system has no `open` key.
struct PlayerStatsSystem: Decodable {
    let open: PlayerStatsOpen?
    let womens: PlayerStatsOpen?

    enum CodingKeys: String, CodingKey {
        case open
        case womens
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        open = container.optionalValue(PlayerStatsOpen.self, .open)
        womens = container.optionalValue(PlayerStatsOpen.self, .womens)
    }
}

struct PlayerStatsOpen: Decodable {
    let currentRank: String?
    let lastMonthRank: String?
    let lastYearRank: String?
    let highestRank: String?
    let highestRankDate: String?
    let proRank: String?
    let currentPoints: String?
    let allTimePoints: String?
    let activePoints: String?
    let inactivePoints: String?
    let bestFinish: String?
    let bestFinishCount: String?
    let averageFinish: String?
    let averageFinishLastYear: String?
    let totalEventsAllTime: String?
    let totalActiveEvents: String?
    let totalEventsAway: String?
    let totalWinsLast3Years: String?
    let top3Last3Years: String?
    let top10Last3Years: String?
    let ratingsRank: String?
    let ratingsValue: String?
    let efficiencyRank: String?
    let efficiencyValue: String?

    enum CodingKeys: String, CodingKey {
        case currentRank = "current_rank"
        case lastMonthRank = "last_month_rank"
        case lastYearRank = "last_year_rank"
        case highestRank = "highest_rank"
        case highestRankDate = "highest_rank_date"
        case proRank = "pro_rank"
        case currentPoints = "current_points"
        case allTimePoints = "all_time_points"
        case activePoints = "active_points"
        case inactivePoints = "inactive_points"
        case bestFinish = "best_finish"
        case bestFinishCount = "best_finish_count"
        case averageFinish = "average_finish"
        case averageFinishLastYear = "average_finish_last_year"
        case totalEventsAllTime = "total_events_all_time"
        case totalActiveEvents = "total_active_events"
        case totalEventsAway = "total_events_away"
        case totalWinsLast3Years = "total_wins_last_3_years"
        case top3Last3Years = "top_3_last_3_years"
        case top10Last3Years = "top_10_last_3_years"
        case ratingsRank = "ratings_rank"
        case ratingsValue = "ratings_value"
        case efficiencyRank = "efficiency_rank"
        case efficiencyValue = "efficiency_value"
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)

        currentRank = container.looseString(.currentRank)
        lastMonthRank = container.looseString(.lastMonthRank)
        lastYearRank = container.looseString(.lastYearRank)
        highestRank = container.looseString(.highestRank)
        highestRankDate = container.looseString(.highestRankDate)
        proRank = container.looseString(.proRank)
        currentPoints = container.looseString(.currentPoints)
        allTimePoints = container.looseString(.allTimePoints)
        activePoints = container.looseString(.activePoints)
        inactivePoints = container.looseString(.inactivePoints)
        bestFinish = container.looseString(.bestFinish)
        bestFinishCount = container.looseString(.bestFinishCount)
        averageFinish = container.looseString(.averageFinish)
        averageFinishLastYear = container.looseString(.averageFinishLastYear)
        totalEventsAllTime = container.looseString(.totalEventsAllTime)
        totalActiveEvents = container.looseString(.totalActiveEvents)
        totalEventsAway = container.looseString(.totalEventsAway)
        totalWinsLast3Years = container.looseString(.totalWinsLast3Years)
        top3Last3Years = container.looseString(.top3Last3Years)
        top10Last3Years = container.looseString(.top10Last3Years)
        ratingsRank = container.looseString(.ratingsRank)
        ratingsValue = container.looseString(.ratingsValue)
        efficiencyRank = container.looseString(.efficiencyRank)
        efficiencyValue = container.looseString(.efficiencyValue)
    }
}

// MARK: - Series

struct Series: Decodable {
    let seriesCode: String?
    let regionCode: String?
    let regionName: String?
    let year: String?
    let totalPoints: String?
    let seriesRank: String?
    let system: String?

    enum CodingKeys: String, CodingKey {
        case seriesCode = "series_code"
        case regionCode = "region_code"
        case regionName = "region_name"
        case year
        case totalPoints = "total_points"
        case seriesRank = "series_rank"
        case system
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        seriesCode = container.looseString(.seriesCode)
        regionCode = container.looseString(.regionCode)
        regionName = container.looseString(.regionName)
        year = container.looseString(.year)
        totalPoints = container.looseString(.totalPoints)
        seriesRank = container.looseString(.seriesRank)
        system = container.looseString(.system)
    }
}

enum SeriesCode: String, Codable {
    case nacs = "NACS"
    case acs  = "ACS"
    case wnasco = "WNACSO"
    case wnascw = "WNASCW"
}
