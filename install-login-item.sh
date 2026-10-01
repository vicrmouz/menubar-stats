#!/bin/zsh
# Compila, instala em ~/Applications e registra um LaunchAgent para abrir no login.
set -euo pipefail
cd "$(dirname "$0")"
./build.sh
LABEL=local.menubarstats
PLIST=~/Library/LaunchAgents/$LABEL.plist
cat > "$PLIST" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>Label</key><string>$LABEL</string>
  <key>ProgramArguments</key>
  <array><string>$HOME/Applications/MenuBarStats.app/Contents/MacOS/MenuBarStats</string></array>
  <key>RunAtLoad</key><true/>
  <key>LimitLoadToSessionType</key><string>Aqua</string>
  <key>ProcessType</key><string>Interactive</string>
</dict>
</plist>
PLIST
launchctl bootout "gui/$(id -u)/$LABEL" 2>/dev/null || true
pkill -f MenuBarStats.app || true
launchctl bootstrap "gui/$(id -u)" "$PLIST"
echo "OK: MenuBarStats abre automaticamente no login"
