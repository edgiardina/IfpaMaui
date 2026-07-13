//
//  ContentView.swift
//  NativeIFPA
//
//  Created by Ed Giardina on 6/9/23.
//

import SwiftUI
import WidgetKit

struct ContentView: View {
    @AppStorage("PlayerId", store: UserDefaults(suiteName: "group.com.edgiardina.ifpa")) private var playerId = 2
    @State var playernumber = ""
       
    var body: some View {
        VStack {
            Image(systemName: "globe")
                .imageScale(.large)
                .foregroundColor(.accentColor)
            TextField("Player Id:", text: $playernumber)
                .onSubmit {
                    playerId = Int(playernumber)!
                    WidgetCenter.shared.reloadAllTimelines()
                }
        
            Button("Randomize Player")
            {
                playerId = Int.random(in: 2..<20000)
                WidgetCenter.shared.reloadAllTimelines()
                
            }
        }
        .padding()
    }
}

struct ContentView_Previews: PreviewProvider {
    static var previews: some View {
        ContentView()
    }
}
