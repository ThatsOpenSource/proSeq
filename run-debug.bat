@echo off
cd /d "%~dp0"
echo proSeq – Debug build
echo.
dotnet build -c Debug
if errorlevel 1 goto :eof
echo.
echo Starte...
dotnet run -c Debug --no-build
