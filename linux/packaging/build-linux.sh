#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# LaserGRBL Linux build script
# Produces: AppImage + .deb package
#
# Prerequisites (Ubuntu/Debian):
#   apt install dotnet-sdk-10.0 libfuse2 file desktop-file-utils
#   # linuxdeploy-x86_64.AppImage from https://github.com/linuxdeploy/linuxdeploy
#
# Usage:
#   ./build-linux.sh [--version 5.9.0] [--arch x86_64|arm64]
# ─────────────────────────────────────────────────────────────────────────────

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

# ── Configuration ─────────────────────────────────────────────────────────────
VERSION="${1:-$(date +%Y.%m.%d)}"
ARCH="${ARCH:-x86_64}"
RID="linux-$ARCH"
APP_NAME="LaserGRBL"
APP_ID="com.lasergrbl.LaserGRBL"

BUILD_DIR="$SCRIPT_DIR/build"
PUBLISH_DIR="$BUILD_DIR/publish"
APPDIR="$BUILD_DIR/$APP_NAME.AppDir"
DEB_DIR="$BUILD_DIR/deb"

echo "=== LaserGRBL Linux build v$VERSION ($RID) ==="

# ── 1. Publish (self-contained, trimmed) ─────────────────────────────────────
echo "→ Publishing..."
rm -rf "$PUBLISH_DIR"
dotnet publish \
  "$ROOT_DIR/src/LaserGRBL.App/LaserGRBL.App.csproj" \
  -c Release \
  -r "$RID" \
  --self-contained true \
  -p:PublishSingleFile=false \
  -p:PublishTrimmed=false \
  -o "$PUBLISH_DIR" \
  -p:Version="$VERSION" \
  -p:AssemblyVersion="$VERSION.0" \
  --nologo

# ── 2. AppImage ───────────────────────────────────────────────────────────────
echo "→ Building AppImage..."
rm -rf "$APPDIR"
mkdir -p "$APPDIR/usr/bin"
mkdir -p "$APPDIR/usr/share/icons/hicolor/256x256/apps"
mkdir -p "$APPDIR/usr/share/applications"
mkdir -p "$APPDIR/usr/share/lasergrbl"

# Copy published output
cp -r "$PUBLISH_DIR"/. "$APPDIR/usr/bin/"

# App icon (convert ico → png if ImageMagick is available)
if command -v convert &>/dev/null && [ -f "$ROOT_DIR/../Grafica/LaserGrbl.ico" ]; then
  convert "$ROOT_DIR/../Grafica/LaserGrbl.ico"[0] \
    -resize 256x256 \
    "$APPDIR/usr/share/icons/hicolor/256x256/apps/$APP_NAME.png"
else
  # Fallback: create a placeholder icon
  cat > /tmp/icon.svg << 'SVG'
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256">
  <rect width="256" height="256" fill="#1a1a2e"/>
  <circle cx="128" cy="128" r="80" fill="#e94560" opacity="0.8"/>
  <text x="128" y="148" font-size="60" text-anchor="middle" fill="white"
        font-family="monospace" font-weight="bold">LG</text>
</svg>
SVG
  if command -v rsvg-convert &>/dev/null; then
    rsvg-convert -w 256 -h 256 /tmp/icon.svg \
      -o "$APPDIR/usr/share/icons/hicolor/256x256/apps/$APP_NAME.png"
  else
    # Copy SVG as PNG placeholder (will break; just ensures the file exists)
    cp /tmp/icon.svg \
      "$APPDIR/usr/share/icons/hicolor/256x256/apps/$APP_NAME.png"
  fi
fi
cp "$APPDIR/usr/share/icons/hicolor/256x256/apps/$APP_NAME.png" \
   "$APPDIR/$APP_NAME.png" 2>/dev/null || true

# .desktop file
cat > "$APPDIR/usr/share/applications/$APP_NAME.desktop" << EOF
[Desktop Entry]
Type=Application
Name=LaserGRBL
Comment=GRBL laser engraver controller
Exec=$APP_NAME %f
Icon=$APP_NAME
MimeType=application/x-gcode;
Categories=Graphics;Engineering;
StartupNotify=true
Version=1.0
EOF
cp "$APPDIR/usr/share/applications/$APP_NAME.desktop" \
   "$APPDIR/$APP_NAME.desktop" 2>/dev/null || true

# AppRun wrapper
cat > "$APPDIR/AppRun" << 'APPRUN'
#!/bin/bash
SELF="$(readlink -f "$0")"
HERE="$(dirname "$SELF")"
export LD_LIBRARY_PATH="$HERE/usr/bin:${LD_LIBRARY_PATH:-}"
exec "$HERE/usr/bin/LaserGRBL" "$@"
APPRUN
chmod +x "$APPDIR/AppRun"

# Build AppImage using linuxdeploy
LINUXDEPLOY="$BUILD_DIR/linuxdeploy-x86_64.AppImage"
if [ ! -f "$LINUXDEPLOY" ]; then
  echo "  Downloading linuxdeploy..."
  curl -L -o "$LINUXDEPLOY" \
    "https://github.com/linuxdeploy/linuxdeploy/releases/download/continuous/linuxdeploy-x86_64.AppImage"
  chmod +x "$LINUXDEPLOY"
fi

OUTPUT_APPIMAGE="$BUILD_DIR/${APP_NAME}-${VERSION}-${ARCH}.AppImage"
ARCH="$ARCH" "$LINUXDEPLOY" \
  --appdir "$APPDIR" \
  --output appimage \
  --desktop-file "$APPDIR/usr/share/applications/$APP_NAME.desktop" \
  --icon-file "$APPDIR/usr/share/icons/hicolor/256x256/apps/$APP_NAME.png"

# linuxdeploy names the output based on the desktop file; rename to our convention
GENERATED_APPIMAGE=$(ls "$BUILD_DIR"/*.AppImage 2>/dev/null | grep -v linuxdeploy | head -1)
if [ -n "$GENERATED_APPIMAGE" ] && [ "$GENERATED_APPIMAGE" != "$OUTPUT_APPIMAGE" ]; then
  mv "$GENERATED_APPIMAGE" "$OUTPUT_APPIMAGE"
fi

echo "✓ AppImage: $OUTPUT_APPIMAGE"

# ── 3. .deb package ───────────────────────────────────────────────────────────
echo "→ Building .deb..."
DEB_ARCH="${ARCH}"
[ "$ARCH" = "x86_64" ] && DEB_ARCH="amd64"
[ "$ARCH" = "arm64"  ] && DEB_ARCH="arm64"

DEB_PKG_DIR="$DEB_DIR/lasergrbl_${VERSION}_${DEB_ARCH}"
INSTALL_DIR="$DEB_PKG_DIR/usr"
rm -rf "$DEB_PKG_DIR"
mkdir -p "$DEB_PKG_DIR/DEBIAN"
mkdir -p "$INSTALL_DIR/bin"
mkdir -p "$INSTALL_DIR/share/applications"
mkdir -p "$INSTALL_DIR/share/icons/hicolor/256x256/apps"
mkdir -p "$INSTALL_DIR/share/lasergrbl"

# Install files
cp -r "$PUBLISH_DIR"/. "$INSTALL_DIR/share/lasergrbl/"
chmod +x "$INSTALL_DIR/share/lasergrbl/LaserGRBL"

# Launcher wrapper
cat > "$INSTALL_DIR/bin/lasergrbl" << 'WRAPPER'
#!/bin/bash
exec /usr/share/lasergrbl/LaserGRBL "$@"
WRAPPER
chmod +x "$INSTALL_DIR/bin/lasergrbl"

# Icon
cp "$APPDIR/usr/share/icons/hicolor/256x256/apps/$APP_NAME.png" \
   "$INSTALL_DIR/share/icons/hicolor/256x256/apps/lasergrbl.png" 2>/dev/null || true

# Desktop file
cat > "$INSTALL_DIR/share/applications/lasergrbl.desktop" << EOF
[Desktop Entry]
Type=Application
Name=LaserGRBL
Comment=GRBL laser engraver controller
Exec=/usr/bin/lasergrbl %f
Icon=lasergrbl
MimeType=application/x-gcode;
Categories=Graphics;Engineering;
StartupNotify=true
Version=1.0
EOF

# DEBIAN control
cat > "$DEB_PKG_DIR/DEBIAN/control" << EOF
Package: lasergrbl
Version: $VERSION
Architecture: $DEB_ARCH
Maintainer: LaserGRBL Linux Port <linux@lasergrbl.com>
Description: GRBL laser engraver controller (Linux port)
 LaserGRBL is a Windows GUI for GRBL laser cutters and engravers,
 ported to Linux using Avalonia UI and .NET 10.
 .
 Features: G-code loading/preview, raster + vector image import,
 jogging, feed/speed/power overrides, custom buttons, Grbl emulator,
 WiFi/ESP8266 support.
Depends: libicu-dev
Recommends: autotrace, avrdude
Homepage: http://lasergrbl.com
EOF

# postinst: add udev rule for USB-serial devices + register file association
cat > "$DEB_PKG_DIR/DEBIAN/postinst" << 'POSTINST'
#!/bin/bash
set -e
# udev rule so non-root users can access USB serial devices
if [ ! -f /etc/udev/rules.d/99-lasergrbl.rules ]; then
  cat > /etc/udev/rules.d/99-lasergrbl.rules << 'UDEV'
# LaserGRBL: allow users in the 'dialout' group to access USB-serial devices
SUBSYSTEM=="tty", ATTRS{idVendor}=="1a86", MODE="0660", GROUP="dialout"  # CH340/CH341
SUBSYSTEM=="tty", ATTRS{idVendor}=="10c4", MODE="0660", GROUP="dialout"  # CP2102
SUBSYSTEM=="tty", ATTRS{idVendor}=="0403", MODE="0660", GROUP="dialout"  # FTDI
UDEV
  udevadm control --reload-rules 2>/dev/null || true
fi
# MIME type
if command -v update-mime-database &>/dev/null; then
  mkdir -p /usr/share/mime/packages
  cat > /usr/share/mime/packages/lasergrbl-gcode.xml << 'MIME'
<?xml version="1.0" encoding="UTF-8"?>
<mime-info xmlns='http://www.freedesktop.org/standards/shared-mime-info'>
  <mime-type type="application/x-gcode">
    <comment>G-Code file</comment>
    <glob pattern="*.nc"/>
    <glob pattern="*.gcode"/>
    <glob pattern="*.ngc"/>
    <glob pattern="*.cnc"/>
    <glob pattern="*.tap"/>
  </mime-type>
</mime-info>
MIME
  update-mime-database /usr/share/mime 2>/dev/null || true
fi
# Desktop integration
if command -v update-desktop-database &>/dev/null; then
  update-desktop-database /usr/share/applications 2>/dev/null || true
fi
echo "LaserGRBL installed. Add your user to the 'dialout' group:"
echo "  sudo usermod -aG dialout \$USER  (log out and back in to apply)"
POSTINST
chmod +x "$DEB_PKG_DIR/DEBIAN/postinst"

# Build .deb
DEB_FILE="$BUILD_DIR/${APP_NAME,,}_${VERSION}_${DEB_ARCH}.deb"
dpkg-deb --build "$DEB_PKG_DIR" "$DEB_FILE" 2>/dev/null || \
  fakeroot dpkg-deb --build "$DEB_PKG_DIR" "$DEB_FILE"
echo "✓ .deb: $DEB_FILE"

echo ""
echo "=== Build complete ==="
echo "  AppImage: $OUTPUT_APPIMAGE"
echo "  .deb:     $DEB_FILE"
