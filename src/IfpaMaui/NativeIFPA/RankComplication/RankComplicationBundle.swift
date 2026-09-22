//
//  RankComplicationBundle.swift
//  RankComplication
//

import WidgetKit
import SwiftUI

/// Watch entry point. It declares only the rank complication.
/// `RankWidgetLiveActivity` is deliberately absent: it uses ActivityKit, which
/// does not exist on watchOS.
@main
struct RankComplicationBundle: WidgetBundle {
    var body: some Widget {
        RankWidget()
    }
}
