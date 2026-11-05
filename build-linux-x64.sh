#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

echo "========================================"
echo " proSeq – Build Linux x64"
echo "========================================"
echo

dotnet publish -c Release -r linux-x64 --self-contained true -o ./publish/linux-x64 \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true

echo
echo "Fertig: publish/linux-x64/proSeq"
echo
