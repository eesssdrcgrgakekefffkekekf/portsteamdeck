#!/bin/bash
# NebulaAuth - One-Click Installer & Launcher for Steam Deck
# Just run: ./start.sh
# It will install everything automatically and launch the app.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTALL_DIR="$HOME/.local/share/NebulaAuth"
PUBLISH_DIR="$SCRIPT_DIR/publish"
DOTNET_ROOT="$HOME/.dotnet"
DESKTOP_FILE="$HOME/.local/share/applications/nebulaauth.desktop"
ICON_DIR="$HOME/.local/share/icons/hicolor/256x256/apps"

export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
export DOTNET_ROOT

# ─────────────────────────────────────────────
# 1. Check if already installed — just run
# ─────────────────────────────────────────────
if [ -f "$INSTALL_DIR/NebulaAuth" ]; then
    exec "$INSTALL_DIR/NebulaAuth" "$@"
fi

# ─────────────────────────────────────────────
# 2. Check if already built — install and run
# ─────────────────────────────────────────────
if [ -f "$PUBLISH_DIR/NebulaAuth" ]; then
    echo "[*] Installing NebulaAuth..."
    install_app
    exec "$INSTALL_DIR/NebulaAuth" "$@"
fi

echo "╔══════════════════════════════════════════════╗"
echo "║   NebulaAuth - Steam Deck Auto-Installer    ║"
echo "╚══════════════════════════════════════════════╝"
echo ""

# ─────────────────────────────────────────────
# 3. Install .NET SDK if missing
# ─────────────────────────────────────────────
install_dotnet() {
    if command -v dotnet &> /dev/null; then
        echo "[OK] .NET SDK found: $(dotnet --version)"
        return 0
    fi

    if [ -f "$DOTNET_ROOT/dotnet" ]; then
        echo "[OK] .NET SDK found: $($DOTNET_ROOT/dotnet --version)"
        return 0
    fi

    echo "[*] Installing .NET 8.0 SDK (user-local, no sudo needed)..."
    mkdir -p "$DOTNET_ROOT"
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin \
        --channel 8.0 \
        --install-dir "$DOTNET_ROOT" \
        2>&1 | grep -E "(Installing|Installed|version)"

    # Add to bashrc for future sessions
    if ! grep -q 'DOTNET_ROOT' "$HOME/.bashrc" 2>/dev/null; then
        echo '' >> "$HOME/.bashrc"
        echo '# .NET SDK (installed by NebulaAuth)' >> "$HOME/.bashrc"
        echo "export DOTNET_ROOT=\"$DOTNET_ROOT\"" >> "$HOME/.bashrc"
        echo 'export PATH="$DOTNET_ROOT:$PATH"' >> "$HOME/.bashrc"
    fi

    echo "[OK] .NET SDK installed"
}

# ─────────────────────────────────────────────
# 4. Build the project
# ─────────────────────────────────────────────
build_app() {
    echo "[*] Restoring packages..."
    dotnet restore "$SCRIPT_DIR/src/NebulaAuth.Linux/NebulaAuth.Linux.csproj" --verbosity quiet

    echo "[*] Building NebulaAuth (this may take 1-2 minutes)..."
    dotnet publish "$SCRIPT_DIR/src/NebulaAuth.Linux/NebulaAuth.Linux.csproj" \
        -c Release \
        -r linux-x64 \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=true \
        -p:TrimMode=partial \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        --verbosity quiet \
        -o "$PUBLISH_DIR"

    chmod +x "$PUBLISH_DIR/NebulaAuth"
    echo "[OK] Build complete"
}

# ─────────────────────────────────────────────
# 5. Install to user directory
# ─────────────────────────────────────────────
install_app() {
    mkdir -p "$INSTALL_DIR"
    mkdir -p "$INSTALL_DIR/maFiles"
    cp "$PUBLISH_DIR/NebulaAuth" "$INSTALL_DIR/"
    chmod +x "$INSTALL_DIR/NebulaAuth"

    # Create .desktop shortcut
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
    update-desktop-database "$HOME/.local/share/applications" 2>/dev/null || true

    echo "[OK] Installed to $INSTALL_DIR"
    echo "[OK] Desktop shortcut created"
}

# ─────────────────────────────────────────────
# Run everything
# ─────────────────────────────────────────────
install_dotnet
echo ""
build_app
echo ""
install_app
echo ""
echo "╔══════════════════════════════════════════════╗"
echo "║          Installation Complete!              ║"
echo "╚══════════════════════════════════════════════╝"
echo ""
echo "  maFiles folder: $INSTALL_DIR/maFiles/"
echo "  App location:   $INSTALL_DIR/NebulaAuth"
echo ""
echo "  Next time just run: ./start.sh"
echo "  Or find 'NebulaAuth' in your app menu."
echo ""
echo "[*] Launching NebulaAuth..."
exec "$INSTALL_DIR/NebulaAuth" "$@"
