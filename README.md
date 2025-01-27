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
- Rank Widget available for Android

  <img src="https://github.com/edgiardina/IfpaMaui/assets/3627193/d20a6283-37cc-4be1-aaa6-75f607d7aaba" width="300" />


#### Requirements

Google Maps API Key (Provide in AndroidManifest.xml)

IFPA API Key https://www.ifpapinball.com/api/documentation/

Syncfusion License Key 
Syncfusion License is currently community (free) https://www.syncfusion.com/products/communitylicense

#### Build

Github Actions available for build and publishing

CLI build supported via
`dotnet build IfpaMaui.csproj -c Release -f net7.0-android/ios`

#### Special Thanks

Jannie Touch redrew the IFPA logo as an SVG. Thanks Jannie!
https://jannietouch.com/
