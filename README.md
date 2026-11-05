# proSeq

Native C# Desktop-Anwendung (.NET 10 / Avalonia) zur Generierung von **Buchungssequenzen** für die Zeitwirtschaft.

## Features

- **Deutsche Oberfläche**, minimalistisch, schwarz-weiß
- Dropdown-basierte Entscheidungsmatrix für alle 12 Beispiel-Buchungsvorgänge
- Dynamisch ein- und ausblendende Felder je nach Auswahl
- Alle Pausen über den Pausen-Dialog (keine feste Mittagspause)
- Pausen innerhalb von Dienstgang-Segmenten werden kontextgerecht gebucht
- Nur Pausen ≥ 15 Minuten zählen zur gesetzlich erforderlichen Mindestpause (30/45 Min)
- Fehlende Mindestpause wird in der Zeitübersicht als Abzug ausgewiesen (Sequenz bleibt unverändert)
- **Konfigurierbar** über `Config/procedures.json` (Building-Block-System)

## Build

```bash
dotnet restore
dotnet build
dotnet run
```

Voraussetzung: .NET 10 SDK.

### Publish

```bash
# Windows
./build-win-x64.bat          # → publish/win-x64/proSeq.exe

# Linux / macOS
chmod +x build-*.sh
./build-linux-x64.sh         # → publish/linux-x64/proSeq
./build-osx.sh               # → publish/osx-arm64|osx-x64/proSeq
./build-all.sh
```

## Konfiguration erweitern

Die Datei `Config/procedures.json` enthält:

- `AvailableActions` – erlaubte Buchungsaktionen
- `TimeFieldLabels` – Labels für Zeitfelder
- `Procedures` – Liste der Prozeduren mit:
  - `Conditions` – Matching-Schlüssel (OrtBeginn, Dienstgang, DienstgangOrt, …)
  - `RequiredTimeFields` – welche Zeiten abgefragt werden
  - `Steps` – die eigentliche Buchungssequenz (Action + TimeKey + optional Comment)

Neue Szenarien können als weitere Prozedur-Objekte hinzugefügt werden, ohne Code zu ändern.

## Architektur

- `Models/` – Procedure, BookingStep, Config
- `Services/ConfigService` – JSON laden/speichern
- `Services/MatrixEngine` – Matching & Generierung der Buchungen
- `ViewModels/MainViewModel` – gesamte UI-Logik (MVVM)
- Avalonia UI, Fluent Theme, B&W Custom Styles
