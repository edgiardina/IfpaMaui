//
//  RankWidgetBundle.swift
//  RankWidget
//
//  Created by Ed Giardina on 6/9/23.
//

import WidgetKit
import SwiftUI

@main
struct RankWidgetBundle: WidgetBundle {
    var body: some Widget {
        RankWidget()
        RankWidgetLiveActivity()
    }
}
