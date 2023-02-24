# IFPA Companion (.NET MAUI)

[![continuous integration build](https://github.com/edgiardina/IfpaMaui/actions/workflows/ci.yml/badge.svg)](https://github.com/edgiardina/IfpaMaui/actions/workflows/ci.yml)

### A cross-platform mobile app for the International Flipper Pinball Association's rankings

[TiltForums Discussion Thread](http://tiltforums.com/t/ifpa-app-now-available-on-the-app-store)

For iOS 

https://itunes.apple.com/us/app/ifpa-companion/id1441736303

For Android 

https://play.google.com/store/apps/details?id=com.edgiardina.ifpa

#### Requirements

Google Maps API Key (Provide in AndroidManifest.xml)

IFPA API Key https://www.ifpapinball.com/api/documentation/

Syncfusion License Key 
Syncfusion License is currently community (free) https://www.syncfusion.com/products/communitylicense

#### Build

Github Actions available for build and publishing

CLI build supported via
`dotnet build IfpaMaui.csproj -c Release -f net7.0-android/ios`
