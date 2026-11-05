#!/bin/bash
set -e
cd "$(dirname "$0")"

echo "========================================"
echo " proSeq - Debug Start"
echo "========================================"
echo

echo "[1/3] Restore..."
dotnet restore

echo
echo "[2/3] Build..."
dotnet build -c Debug --no-restore

echo
echo "[3/3] Start..."
echo
dotnet run -c Debug --no-build

echo
echo "Anwendung beendet."
