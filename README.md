# IFPA Companion (.NET MAUI)

[![continuous integration build](https://github.com/edgiardina/IfpaMaui/actions/workflows/ci.yml/badge.svg)](https://github.com/edgiardina/IfpaMaui/actions/workflows/ci.yml)

### A cross-platform mobile app for the International Flipper Pinball Association's rankings

[TiltForums Discussion Thread](http://tiltforums.com/t/ifpa-app-now-available-on-the-app-store)

<a href="https://apps.apple.com/us/app/ifpa-companion/id1441736303?itsct=apps_box_badge&amp;itscg=30200" style="display:block;border-radius: 13px; width: 250px; height: 83px;"><img src="https://tools.applemediaservices.com/api/badges/download-on-the-app-store/black/en-us?size=250x83&amp;releaseDate=1541808000" alt="Download on the App Store" style="border-radius: 13px; width: 250px; height: 83px;"></a><a href='https://play.google.com/store/apps/details?id=com.edgiardina.ifpa&pcampaignid=pcampaignidMKT-Other-global-all-co-prtnr-py-PartBadge-Mar2515-1'><img width="300" alt='Get it on Google Play' src='https://play.google.com/intl/en_us/badges/static/images/badges/en_badge_web_generic.png'/></a>

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
