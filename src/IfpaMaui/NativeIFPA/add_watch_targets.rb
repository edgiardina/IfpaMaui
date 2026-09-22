# Adds the watchOS app and its complication extension to IFPA.xcodeproj.
# Idempotent: re-running is a no-op once the targets exist.

require 'xcodeproj'

PROJECT_PATH = ARGV[0] or abort 'usage: add_watch_targets.rb <path to .xcodeproj>'
TEAM = 'KH5JKUPW2Q'
WATCH_DEPLOYMENT = '10.0'

project = Xcodeproj::Project.open(PROJECT_PATH)

if project.targets.any? { |t| t.name == 'IFPAWatch' }
  puts 'IFPAWatch already present, nothing to do'
  exit 0
end

def find_ref(project, basename)
  ref = project.files.find { |f| f.path && File.basename(f.path) == basename }
  abort "could not find file reference for #{basename}" if ref.nil?
  ref
end

main_group = project.main_group

watch_app = project.new_target(:application, 'IFPAWatch', :watchos, WATCH_DEPLOYMENT)
complication = project.new_target(:app_extension, 'RankComplication', :watchos, WATCH_DEPLOYMENT)

# --- watch app sources -------------------------------------------------------
watch_group = main_group.new_group('IFPAWatch', 'IFPAWatch')
%w[IFPAWatchApp.swift ContentView.swift PhoneSessionReceiver.swift].each do |name|
  watch_app.add_file_references([watch_group.new_reference(name)])
end
watch_group.new_reference('Info.plist')
watch_group.new_reference('IFPAWatch.entitlements')

# --- complication sources ----------------------------------------------------
# The model and the views are the SAME file references the iOS extension uses,
# so there is one copy of that code, compiled twice.
comp_group = main_group.new_group('RankComplication', 'RankComplication')
comp_bundle = comp_group.new_reference('RankComplicationBundle.swift')
comp_group.new_reference('Info.plist')
comp_group.new_reference('RankComplication.entitlements')

complication.add_file_references([
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
  'SKIP_INSTALL' => 'NO'
)

comp_settings = common.merge(
  'PRODUCT_BUNDLE_IDENTIFIER' => 'com.edgiardina.ifpa.watchkitapp.complication',
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
watch_app.add_dependency(complication)
embed = watch_app.new_copy_files_build_phase('Embed Foundation Extensions')
embed.symbol_dst_subfolder_spec = :plug_ins
embed.add_file_reference(complication.product_reference, true)

project.save

puts 'created targets:'
project.targets.each do |t|
  puts "  #{t.name} (#{t.product_type.to_s.split('.').last})"
end

# A shared scheme so xcodebuild -scheme and CI can both drive the watch build.
scheme = Xcodeproj::XCScheme.new
scheme.add_build_target(watch_app)
scheme.set_launch_target(watch_app)
scheme.save_as(PROJECT_PATH, 'IFPAWatch', true)
puts 'wrote shared scheme IFPAWatch'
