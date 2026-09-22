# IFPA Companion (.NET MAUI)

[![continuous integration build](https://github.com/edgiardina/IfpaMaui/actions/workflows/ci.yml/badge.svg)](https://github.com/edgiardina/IfpaMaui/actions/workflows/ci.yml)

### A cross-platform mobile app for the International Flipper Pinball Association's rankings

[TiltForums Discussion Thread](http://tiltforums.com/t/ifpa-app-now-available-on-the-app-store)


[![appstore](https://github-production-user-asset-6210df.s3.amazonaws.com/3627193/262177214-a3733780-6e43-4f75-aa5f-dd48750fd375.svg)](https://apps.apple.com/us/app/ifpa-companion/id1441736303?itsct=apps_box_badge&amp;itscg=30200)
[![playstore](https://github-production-user-asset-6210df.s3.amazonaws.com/3627193/262177840-a82e9032-48b6-46e6-9472-e4dad8461d1e.svg)](https://play.google.com/store/apps/details?id=com.edgiardina.ifpa&pcampaignid=pcampaignidMKT-Other-global-all-co-prtnr-py-PartBadge-Mar2515-1)

### Features
- Overall Rankings, Championship Series Standings
  
  <img src="https://github.com/edgiardina/IfpaMaui/assets/3627193/2fa8d093-451d-493a-9c22-07b9d22f86c6" width="300" />
- "My Stats" to select your player profile and track your stats with Local Notifications

  <img src="https://github.com/edgiardina/IfpaMaui/assets/3627193/35713849-a825-41f8-b97c-a497840dc52a" width="300" />
- Calendar to view upcoming tournaments including integration to add tournaments to your device's calendar
- Notifications for Rank Change, Tournament Results posted, and new IFPA blog posts
- Widgets available for iOS and Android
    - iOS Rank Widget

      <img src="https://github.com/user-attachments/assets/2fc13c82-ca57-4f68-881d-7d7804d60c10" width="300" />

    - iOS Lock Screen Rank Widgets, in the circular, rectangular and inline sizes

    - Android Rank Widget

      <img src="https://github.com/edgiardina/IfpaMaui/assets/3627193/d20a6283-37cc-4be1-aaa6-75f607d7aaba" width="300" />
    
    - Android Upcoming Tournaments Widget
 
   

  


- Apple Watch app with your IFPA number and rank detail

  Players read their IFPA number out to staff at tournament registration, so the watch app leads with
  that number. Below it the app shows rank, points, efficiency, events played, finishes and peak rank.

    - Rank Complications for the watch face, in the circular, corner, rectangular and inline sizes

  The watch app needs the iPhone app. Choose your player in "My Stats" on the phone, and the phone
  sends it to the watch. The watch keeps its own copy, so the complication works when the phone is
  not nearby.

#### Requirements

Google Maps API Key (Provide in AndroidManifest.xml)

IFPA API Key https://www.ifpapinball.com/api/documentation/

The iOS widget, the watch app and the watch complication each read the API key from their own
`Info.plist`. CI writes the key into all three at publish time. For a local device build, put it in
`NativeIFPA/RankWidget/Info.plist`, `NativeIFPA/IFPAWatch/Info.plist` and
`NativeIFPA/RankComplication/Info.plist`.

#### Build

Github Actions available for build and publishing

CLI build supported via
`dotnet build src/IfpaMaui/IfpaMaui.csproj -c Release -f net10.0-android` (or `net10.0-ios`)

Unit tests run via `dotnet test tests/IfpaMaui.Tests/IfpaMaui.Tests.csproj`

The iOS widget, the watch app and the watch complication are Swift targets in
`src/IfpaMaui/NativeIFPA/IFPA.xcodeproj`. Build the watch app with
`xcodebuild -project src/IfpaMaui/NativeIFPA/IFPA.xcodeproj -scheme IFPAWatch`.

MAUI has no hook to embed a nested watch app, so the release workflow embeds it into the archive
after `dotnet publish` and signs the bundles from the inside out. A local `dotnet build -t:Run`
therefore gives you the phone app and the widget, but not the watch app. Run the watch app from
Xcode or a simulator instead.

`add_watch_targets.rb` creates the two watch targets through the `xcodeproj` gem. It is
find-or-create at every step, so you can run it again to repair the project file.

#### Special Thanks

Jannie Touch redrew the IFPA logo as an SVG. Thanks Jannie!
https://jannietouch.com/
