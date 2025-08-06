// This file was generated from JSON Schema using quicktype, do not modify it directly.
// To parse the JSON, add this file to your project and do:
//
//   let ifpaPlayer = try? JSONDecoder().decode(IfpaPlayer.self, from: jsonData)

import Foundation

// MARK: - IfpaPlayer
struct IfpaPlayer: Codable {
    let player: [Player]
    
    static func getPlayerById(from playerId:Int) async throws -> IfpaPlayer {
        guard let apiKey = Bundle.main.object(forInfoDictionaryKey: "IFPAApiKey") as? String else {
            throw NSError(domain: "IfpaPlayer", code: 1, userInfo: [NSLocalizedDescriptionKey: "API key not found in Info.plist"])
        }
        let url = URL(string: "https://api.ifpapinball.com/player/\(playerId)?api_key=\(apiKey)")!
        let (data, _) = try await URLSession.shared.data(from: url)
        return try! JSONDecoder().decode(IfpaPlayer.self, from: data)
    }
}

// MARK: - Player
struct Player: Codable {
    let playerID, firstName, lastName, initials: String
    let excludedFlag, age, city, stateprov: String
    let countryName, countryCode, ifpaRegistered, womensFlag: String
    let profilePhoto: String
    let matchplayEvents: MatchplayEvents
    let twitchUsername: String?
    let pinsideUsername: String?
    let playerStats: PlayerStats
    let series: [Series]?

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
}

// MARK: - MatchplayEvents
struct MatchplayEvents: Codable {
    let id, rating, rank: String?
}

// MARK: - PlayerStats
struct PlayerStats: Codable {
    let system: PlayerStatsSystem
    let yearsActive: String

    enum CodingKeys: String, CodingKey {
        case system
        case yearsActive = "years_active"
    }
}

struct PlayerStatsSystem: Codable {
    let open: PlayerStatsOpen
}

struct PlayerStatsOpen: Codable {
    let currentRank, lastMonthRank, lastYearRank, highestRank: String
    let highestRankDate, proRank, currentPoints, allTimePoints: String
    let activePoints, inactivePoints, bestFinish, bestFinishCount: String
    let averageFinish, averageFinishLastYear, totalEventsAllTime, totalActiveEvents: String
    let totalEventsAway, totalWinsLast3Years, top3Last3Years, top10Last3Years: String
    let ratingsRank, ratingsValue, efficiencyRank, efficiencyValue: String

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
}

struct Series: Codable {
    let seriesCode, regionCode, regionName, year, totalPoints, seriesRank: String?

    enum CodingKeys: String, CodingKey {
        case seriesCode = "series_code"
        case regionCode = "region_code"
        case regionName = "region_name"
        case year
        case totalPoints = "total_points"
        case seriesRank = "series_rank"
    }
}

enum SeriesCode: String, Codable {
    case nacs = "NACS"
    case acs  = "ACS"
    case wnasco = "WNACSO"
    case wnascw = "WNASCW"
}
