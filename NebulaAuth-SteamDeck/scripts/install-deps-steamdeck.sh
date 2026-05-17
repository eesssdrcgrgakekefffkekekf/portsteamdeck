#!/bin/bash
# Install dependencies for NebulaAuth on Steam Deck
# Run this ONCE before building or running NebulaAuth

set -e

echo "=== NebulaAuth - Steam Deck Dependencies ==="
echo ""
echo "Steam Deck runs SteamOS (Arch Linux based)."
echo "The filesystem is read-only by default."
echo ""

# Check if we're on Steam Deck
if [ -f /etc/os-release ]; then
    . /etc/os-release
    echo "Detected OS: $PRETTY_NAME"
fi

echo ""
echo "Choose installation method:"
echo "  1) Pre-built binary (no dependencies needed - RECOMMENDED)"
echo "  2) Build from source (requires .NET SDK)"
echo ""
read -p "Enter choice [1/2]: " choice

case $choice in
    1)
        echo ""
        echo "For pre-built binary, no additional dependencies are needed!"
        echo "The self-contained build includes the .NET runtime."
        echo ""
        echo "Just run: ./scripts/install-steamdeck.sh"
        echo "(after downloading the release archive)"
        ;;
    2)
        echo ""
        echo "Installing .NET SDK for building from source..."
        echo ""
        
        # Method 1: Disable read-only filesystem (if needed)
        if command -v steamos-readonly &> /dev/null; then
            echo "Disabling read-only filesystem temporarily..."
            sudo steamos-readonly disable
            READONLY_DISABLED=true
        fi

        # Install via pacman if available
        if command -v pacman &> /dev/null; then
            echo "Installing via pacman..."
            sudo pacman -Sy --noconfirm dotnet-sdk-8.0 || {
                echo ""
                echo "Pacman install failed. Using manual install method..."
                
                # Manual .NET install
                export DOTNET_ROOT="$HOME/.dotnet"
                mkdir -p "$DOTNET_ROOT"
                
                echo "Downloading .NET 8.0 SDK..."
                curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin \
                    --channel 8.0 \
                    --install-dir "$DOTNET_ROOT"
                
                # Add to PATH
                echo 'export DOTNET_ROOT="$HOME/.dotnet"' >> "$HOME/.bashrc"
                echo 'export PATH="$DOTNET_ROOT:$PATH"' >> "$HOME/.bashrc"
                
                export PATH="$DOTNET_ROOT:$PATH"
                echo ".NET installed to $DOTNET_ROOT"
            }
        else
            # Fallback: manual install
            export DOTNET_ROOT="$HOME/.dotnet"
            mkdir -p "$DOTNET_ROOT"
            
            echo "Downloading .NET 8.0 SDK..."
            curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin \
                --channel 8.0 \
                --install-dir "$DOTNET_ROOT"
            
            echo 'export DOTNET_ROOT="$HOME/.dotnet"' >> "$HOME/.bashrc"
            echo 'export PATH="$DOTNET_ROOT:$PATH"' >> "$HOME/.bashrc"
            
            export PATH="$DOTNET_ROOT:$PATH"
        fi

        # Re-enable read-only if we disabled it
        if [ "$READONLY_DISABLED" = true ]; then
            echo "Re-enabling read-only filesystem..."
            sudo steamos-readonly enable
        fi

        echo ""
        echo "Verifying installation..."
        dotnet --version && echo "Success!" || echo "Failed. Please restart terminal and try again."
        echo ""
        echo "Now you can build: ./scripts/build.sh"
        ;;
    *)
        echo "Invalid choice"
        exit 1
        ;;
esac

echo ""
echo "=== Additional notes for Steam Deck ==="
echo ""
echo "Clipboard support requires one of:"
echo "  - xclip (X11): sudo pacman -S xclip"
echo "  - wl-copy (Wayland): sudo pacman -S wl-clipboard"
echo ""
echo "Both should be pre-installed on SteamOS."
