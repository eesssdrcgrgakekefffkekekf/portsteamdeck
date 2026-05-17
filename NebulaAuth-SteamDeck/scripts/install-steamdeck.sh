#!/bin/bash
# NebulaAuth Steam Deck Installation Script
# Installs NebulaAuth to the Steam Deck and creates a desktop/Game Mode shortcut

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/.."
PUBLISH_DIR="$PROJECT_DIR/publish"
INSTALL_DIR="$HOME/.local/share/NebulaAuth"
DESKTOP_FILE="$HOME/.local/share/applications/nebulaauth.desktop"
ICON_DIR="$HOME/.local/share/icons/hicolor/256x256/apps"

echo "=== NebulaAuth Steam Deck Installer ==="
echo ""

# Check if build exists
if [ ! -f "$PUBLISH_DIR/NebulaAuth" ]; then
    echo "ERROR: Build not found. Run ./scripts/build.sh first"
    exit 1
fi

# Create install directory
echo "Installing to: $INSTALL_DIR"
mkdir -p "$INSTALL_DIR"
mkdir -p "$INSTALL_DIR/maFiles"

# Copy files
cp "$PUBLISH_DIR/NebulaAuth" "$INSTALL_DIR/"
chmod +x "$INSTALL_DIR/NebulaAuth"

# Copy icon if exists
mkdir -p "$ICON_DIR"
if [ -f "$PROJECT_DIR/src/NebulaAuth.Linux/Assets/nebulaauth.png" ]; then
    cp "$PROJECT_DIR/src/NebulaAuth.Linux/Assets/nebulaauth.png" "$ICON_DIR/nebulaauth.png"
fi

# Create .desktop file for KDE/Desktop Mode
echo "Creating desktop shortcut..."
mkdir -p "$(dirname "$DESKTOP_FILE")"
cat > "$DESKTOP_FILE" << EOF
[Desktop Entry]
Name=NebulaAuth
Comment=Steam Guard Authenticator for Steam Deck
Exec=$INSTALL_DIR/NebulaAuth
Icon=nebulaauth
Terminal=false
Type=Application
Categories=Game;Utility;
Keywords=Steam;Guard;2FA;Authenticator;
StartupWMClass=NebulaAuth
EOF

# Update desktop database
if command -v update-desktop-database &> /dev/null; then
    update-desktop-database "$HOME/.local/share/applications" 2>/dev/null || true
fi

echo ""
echo "=== Installation Complete ==="
echo ""
echo "NebulaAuth installed to: $INSTALL_DIR"
echo ""
echo "To run:"
echo "  $INSTALL_DIR/NebulaAuth"
echo ""
echo "The app appears in your application menu (Desktop Mode)."
echo ""
echo "=== Adding to Steam (Game Mode) ==="
echo "To use in Game Mode:"
echo "  1. Open Steam in Desktop Mode"
echo "  2. Games -> Add a Non-Steam Game"
echo "  3. Browse to: $INSTALL_DIR/NebulaAuth"
echo "  4. Add Selected Programs"
echo "  5. The app will now appear in your Steam library"
echo ""
echo "=== maFiles ==="
echo "Place your .maFile files in: $INSTALL_DIR/maFiles/"
echo "You can also import them through the app interface."
