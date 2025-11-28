#!/bin/bash
# Docker-based Windows x64 build script for VibeProxy
# Builds the application from macOS/Linux using Docker

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
CONFIGURATION="Release"
IMAGE_NAME="vibeproxy-windows-builder"
CONTAINER_NAME="vibeproxy-build-$$"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --debug)
            CONFIGURATION="Debug"
            shift
            ;;
        --release)
            CONFIGURATION="Release"
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  -c, --configuration <Debug|Release>  Build configuration (default: Release)"
            echo "  --debug                              Shorthand for -c Debug"
            echo "  --release                            Shorthand for -c Release"
            echo "  -h, --help                           Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo "========================================"
echo "VibeProxy Windows Docker Build"
echo "Configuration: $CONFIGURATION"
echo "========================================"

cd "$PROJECT_ROOT"

# Create output directory
mkdir -p out

# Build the Docker image
echo ""
echo "Building Docker image..."
docker build \
    --build-arg CONFIGURATION="$CONFIGURATION" \
    -f Dockerfile.windows-build \
    -t "$IMAGE_NAME:latest" \
    --target build \
    .

# Extract artifacts from the image
echo ""
echo "Extracting build artifacts..."

# Create a temporary container to copy files
docker create --name "$CONTAINER_NAME" "$IMAGE_NAME:latest" /bin/true
docker cp "$CONTAINER_NAME:/app/publish" out/
docker rm "$CONTAINER_NAME"

# Create ZIP archive
ZIP_NAME="VibeProxy-Windows-$CONFIGURATION.zip"
echo ""
echo "Creating ZIP archive: $ZIP_NAME"

cd out
rm -f "$ZIP_NAME"
zip -r "$ZIP_NAME" publish/
cd "$PROJECT_ROOT"

# Calculate checksum
echo ""
echo "Calculating checksum..."
if command -v sha256sum &> /dev/null; then
    sha256sum "out/$ZIP_NAME" | awk '{print $1 "  " FILENAME}' FILENAME="$ZIP_NAME" > "out/$ZIP_NAME.sha256"
elif command -v shasum &> /dev/null; then
    shasum -a 256 "out/$ZIP_NAME" | awk '{print $1 "  " FILENAME}' FILENAME="$ZIP_NAME" > "out/$ZIP_NAME.sha256"
fi

echo ""
echo "========================================"
echo "Build Complete!"
echo "========================================"
echo ""
echo "Artifacts:"
echo "  - out/publish/           (unpacked files)"
echo "  - out/$ZIP_NAME          (distributable archive)"
if [ -f "out/$ZIP_NAME.sha256" ]; then
    echo "  - out/$ZIP_NAME.sha256   (checksum)"
    echo ""
    echo "Checksum:"
    cat "out/$ZIP_NAME.sha256"
fi
echo ""
