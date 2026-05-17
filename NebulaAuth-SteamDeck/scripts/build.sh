#!/bin/bash
# NebulaAuth Linux Build Script
# Builds the application for Steam Deck (linux-x64)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/.."
SRC_DIR="$PROJECT_DIR/src/NebulaAuth.Linux"
BUILD_DIR="$PROJECT_DIR/build"
PUBLISH_DIR="$PROJECT_DIR/publish"

echo "=== NebulaAuth Linux Build ==="
echo "Building for linux-x64 (Steam Deck / Arch Linux)"
echo ""

# Check .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK not found!"
    echo "Install with: sudo pacman -S dotnet-sdk-8.0"
    echo "Or on Steam Deck: flatpak install flathub org.freedesktop.Sdk.Extension.dotnet8"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo "Using .NET SDK: $DOTNET_VERSION"

# Clean
echo ""
echo "Cleaning previous build..."
rm -rf "$BUILD_DIR" "$PUBLISH_DIR"

# Restore
echo "Restoring packages..."
dotnet restore "$SRC_DIR/NebulaAuth.Linux.csproj"

# Build
echo ""
echo "Building..."
dotnet build "$SRC_DIR/NebulaAuth.Linux.csproj" -c Release --no-restore

# Publish (self-contained for Steam Deck compatibility)
echo ""
echo "Publishing self-contained for linux-x64..."
dotnet publish "$SRC_DIR/NebulaAuth.Linux.csproj" \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    -p:TrimMode=partial \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$PUBLISH_DIR"

# Make executable
chmod +x "$PUBLISH_DIR/NebulaAuth"

echo ""
echo "=== Build Complete ==="
echo "Output: $PUBLISH_DIR/NebulaAuth"
echo ""
echo "To run: $PUBLISH_DIR/NebulaAuth"
echo "To install on Steam Deck, run: ./scripts/install-steamdeck.sh"
