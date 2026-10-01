#!/bin/zsh
# Compila e empacota como .app (sem ícone no Dock) em ~/Applications.
set -euo pipefail
cd "$(dirname "$0")"
swift build -c release --build-system native
APP=~/Applications/MenuBarStats.app
rm -rf "$APP" && mkdir -p "$APP/Contents/MacOS"
cp .build/release/MenuBarStats "$APP/Contents/MacOS/"
cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>CFBundleName</key><string>MenuBarStats</string>
  <key>CFBundleIdentifier</key><string>local.menubarstats</string>
  <key>CFBundleExecutable</key><string>MenuBarStats</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>1.0</string>
  <key>LSMinimumSystemVersion</key><string>14.0</string>
  <key>LSUIElement</key><true/>
</dict></plist>
PLIST
codesign --force --sign - "$APP"
echo "OK: $APP"

# Se o LaunchAgent (abrir no login) estiver carregado, reinicia o app com a versão nova.
launchctl kickstart -k "gui/$(id -u)/local.menubarstats" 2>/dev/null && echo "App reiniciado" || true
