#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

echo "========================================"
echo " proSeq – Build macOS"
echo "========================================"
echo

echo "[1/2] osx-arm64 ..."
dotnet publish -c Release -r osx-arm64 --self-contained true -o ./publish/osx-arm64 \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true

echo "[2/2] osx-x64 ..."
dotnet publish -c Release -r osx-x64 --self-contained true -o ./publish/osx-x64 \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true

echo
echo "Fertig:"
echo "  publish/osx-arm64/proSeq"
echo "  publish/osx-x64/proSeq"
echo
