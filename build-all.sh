#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

echo "Building all targets (proSeq)..."
dotnet publish -c Release -r win-x64   --self-contained true -o ./publish/win-x64   -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c Release -r linux-x64 --self-contained true -o ./publish/linux-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c Release -r osx-arm64 --self-contained true -o ./publish/osx-arm64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c Release -r osx-x64   --self-contained true -o ./publish/osx-x64   -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
echo "All builds done under ./publish/"
