# Adds (or repairs) the watchOS app and its complication extension in
# IFPA.xcodeproj.
#
# Every step is find-or-create, so running this against a project that already
# has the targets only fills in whatever is missing. That matters because the
# project file is generated rather than hand-edited: if it is ever clobbered,
# re-running this restores the exact same layout.
#
#   GEM_HOME=/opt/homebrew/Cellar/cocoapods/<version>/libexec \
#     /opt/homebrew/opt/ruby/bin/ruby add_watch_targets.rb NativeIFPA/IFPA.xcodeproj

require 'xcodeproj'

PROJECT_PATH = ARGV[0] or abort 'usage: add_watch_targets.rb <path to .xcodeproj>'
TEAM = 'KH5JKUPW2Q'
WATCH_DEPLOYMENT = '10.0'

project = Xcodeproj::Project.open(PROJECT_PATH)

def find_ref(project, basename)
  ref = project.files.find { |f| f.path && File.basename(f.path) == basename }
  abort "could not find file reference for #{basename}" if ref.nil?
  ref
end

def group_for(project, name, path)
  project.main_group.find_subpath(name, true).tap { |g| g.set_path(path) }
end

# Adds a reference to the group unless one with that path already exists.
def ensure_ref(group, name)
  group.files.find { |f| f.path == name } || group.new_reference(name)
end

def ensure_sources(target, refs)
  existing = target.source_build_phase.files.map { |bf| bf.file_ref }
  target.add_file_references(refs.reject { |r| existing.include?(r) })
end

def find_or_create_target(project, type, name, deployment)
  project.targets.find { |t| t.name == name } ||
    project.new_target(type, name, :watchos, deployment)
end

watch_app = find_or_create_target(project, :application, 'IFPAWatch', WATCH_DEPLOYMENT)
complication = find_or_create_target(project, :app_extension, 'RankComplication', WATCH_DEPLOYMENT)

# --- watch app sources and resources ----------------------------------------
watch_group = group_for(project, 'IFPAWatch', 'IFPAWatch')
watch_sources = %w[IFPAWatchApp.swift ContentView.swift PhoneSessionReceiver.swift]
watch_refs = watch_sources.map { |n| ensure_ref(watch_group, n) }
# The app fetches and formats the player itself, so it compiles the same shared
# model file as the widget and the complication.
watch_refs << find_ref(project, 'IfpaPlayer.swift')
ensure_sources(watch_app, watch_refs)
ensure_ref(watch_group, 'Info.plist')
ensure_ref(watch_group, 'IFPAWatch.entitlements')

# The app icon is the IFPA mark, rasterised from the same appicon.svg the MAUI
# app uses, on the #062C53 the csproj declares for MauiIcon.
assets = ensure_ref(watch_group, 'Assets.xcassets')
unless watch_app.resources_build_phase.files.map(&:file_ref).include?(assets)
  watch_app.resources_build_phase.add_file_reference(assets, true)
end

# --- complication sources ----------------------------------------------------
# The model and the views are the SAME file references the iOS extension uses,
# so there is one copy of that code, compiled twice.
comp_group = group_for(project, 'RankComplication', 'RankComplication')
comp_bundle = ensure_ref(comp_group, 'RankComplicationBundle.swift')
ensure_ref(comp_group, 'Info.plist')
ensure_ref(comp_group, 'RankComplication.entitlements')

ensure_sources(complication, [
  find_ref(project, 'IfpaPlayer.swift'),
  find_ref(project, 'RankWidget.swift'),
  comp_bundle
])

# --- build settings ----------------------------------------------------------
common = {
  'SDKROOT' => 'watchos',
  'TARGETED_DEVICE_FAMILY' => '4',
  'WATCHOS_DEPLOYMENT_TARGET' => WATCH_DEPLOYMENT,
  'SWIFT_VERSION' => '5.0',
  'DEVELOPMENT_TEAM' => TEAM,
  'GENERATE_INFOPLIST_FILE' => 'YES',
  'CURRENT_PROJECT_VERSION' => '1',
  'MARKETING_VERSION' => '1.0',
  'SWIFT_EMIT_LOC_STRINGS' => 'YES'
}

watch_settings = common.merge(
  'PRODUCT_BUNDLE_IDENTIFIER' => 'com.edgiardina.ifpa.watchkitapp',
  'PRODUCT_NAME' => '$(TARGET_NAME)',
  'INFOPLIST_FILE' => 'IFPAWatch/Info.plist',
  'CODE_SIGN_ENTITLEMENTS' => 'IFPAWatch/IFPAWatch.entitlements',
  'INFOPLIST_KEY_CFBundleDisplayName' => 'IFPA',
  'ASSETCATALOG_COMPILER_APPICON_NAME' => 'AppIcon',
  'ASSETCATALOG_COMPILER_GLOBAL_ACCENT_COLOR_NAME' => '',
  'SKIP_INSTALL' => 'NO'
)

comp_settings = common.merge(
  # Apple rejects 'com.edgiardina.ifpa.watchkitapp.complication' as an
  # unavailable identifier, so the suffix is rankcomplication. An app
  # extension id must still be prefixed by its containing app's id.
  'PRODUCT_BUNDLE_IDENTIFIER' => 'com.edgiardina.ifpa.watchkitapp.rankcomplication',
  'PRODUCT_NAME' => '$(TARGET_NAME)',
  'INFOPLIST_FILE' => 'RankComplication/Info.plist',
  'CODE_SIGN_ENTITLEMENTS' => 'RankComplication/RankComplication.entitlements',
  'INFOPLIST_KEY_CFBundleDisplayName' => 'IFPA Rank',
  'SKIP_INSTALL' => 'YES'
)

watch_app.build_configurations.each do |config|
  watch_settings.each { |k, v| config.build_settings[k] = v }
end

complication.build_configurations.each do |config|
  comp_settings.each { |k, v| config.build_settings[k] = v }
end

# --- embed the complication inside the watch app -----------------------------
unless watch_app.dependencies.any? { |d| d.target == complication }
  watch_app.add_dependency(complication)
end

embed = watch_app.copy_files_build_phases.find { |p| p.name == 'Embed Foundation Extensions' } ||
        watch_app.new_copy_files_build_phase('Embed Foundation Extensions')
embed.symbol_dst_subfolder_spec = :plug_ins
embed.add_file_reference(complication.product_reference, true)

project.save

puts 'targets:'
project.targets.each do |t|
  puts "  #{t.name} (#{t.product_type.to_s.split('.').last})"
end

# A shared scheme so xcodebuild -scheme and CI can both drive the watch build.
scheme = Xcodeproj::XCScheme.new
scheme.add_build_target(watch_app)
scheme.set_launch_target(watch_app)
scheme.save_as(PROJECT_PATH, 'IFPAWatch', true)
puts 'wrote shared scheme IFPAWatch'
