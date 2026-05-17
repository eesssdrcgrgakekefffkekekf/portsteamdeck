#!/bin/bash
# NebulaAuth - Quick Start Script for Steam Deck
# Just run: ./start.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTALL_DIR="$HOME/.local/share/NebulaAuth"
PUBLISH_DIR="$SCRIPT_DIR/publish"

# Check if installed
if [ -f "$INSTALL_DIR/NebulaAuth" ]; then
    echo "Starting NebulaAuth from $INSTALL_DIR..."
    exec "$INSTALL_DIR/NebulaAuth" "$@"
fi

# Check if built locally
if [ -f "$PUBLISH_DIR/NebulaAuth" ]; then
    echo "Starting NebulaAuth from local build..."
    chmod +x "$PUBLISH_DIR/NebulaAuth"
    exec "$PUBLISH_DIR/NebulaAuth" "$@"
fi

# Not found — try to build
echo "NebulaAuth not found. Attempting to build..."
echo ""

if [ -f "$SCRIPT_DIR/scripts/build.sh" ]; then
    chmod +x "$SCRIPT_DIR/scripts/build.sh"
    "$SCRIPT_DIR/scripts/build.sh"

    if [ -f "$PUBLISH_DIR/NebulaAuth" ]; then
        echo ""
        echo "Build successful! Starting NebulaAuth..."
        chmod +x "$PUBLISH_DIR/NebulaAuth"
        exec "$PUBLISH_DIR/NebulaAuth" "$@"
    else
        echo "ERROR: Build failed."
        exit 1
    fi
else
    echo "ERROR: Cannot find build script or NebulaAuth binary."
    echo ""
    echo "Please run one of:"
    echo "  ./scripts/build.sh          - build from source"
    echo "  ./scripts/install-steamdeck.sh - install after building"
    exit 1
fi
