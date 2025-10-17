# Postman Tests - Media Endpoint Sorting & Filtering

## �bersicht

Diese Postman Collection testet alle Sorting- und Filtering-Funktionen des Media-Endpoints.

## Wichtige Hinweise zu den Response-Strukturen

### MediaEntry Response
Die API gibt `MediaEntry`-Objekte mit folgender Struktur zur�ck:

```json
{
  "uuid": "guid",
  "title": "string",
  "description": "string",
  "mediaType": 0,  // 0=Movie, 1=Series, 2=Documentary, 3=Game
  "releaseYear": 2023,
  "ageRestriction": 2,  // 0=FSK0, 1=FSK6, 2=FSK12, 3=FSK16, 4=FSK18
  "genre": "string",
  "createdAt": "datetime",
  "averageScore": 0.0,
  "createdBy": {
    "uuid": "guid",
    "username": "string",
    "created": "datetime",
    "profileUuid": "guid"
  },
  "ratings": []
}
```

### Wichtige Enumerationen

#### EMediaType (mediaType)
- `0` = Movie
- `1` = Series
- `2` = Documentary
- `3` = Game

#### EFSK (ageRestriction)
- `0` = FSK0
- `1` = FSK6
- `2` = FSK12
- `3` = FSK16
- `4` = FSK18

## Test-Kategorien

### 1. Setup - Create Test Data (5 Tests)
Erstellt Testdaten f�r alle nachfolgenden Tests:
- 1 Test-User
- 4 verschiedene Media-Eintr�ge mit unterschiedlichen Eigenschaften

**WICHTIG:** Diese Tests m�ssen als erstes ausgef�hrt werden!

### 2. Sorting Tests (5 Tests)
Testet alle Sortierungsm�glichkeiten:
- ? Nach Titel (aufsteigend/absteigend)
- ? Nach Jahr (aufsteigend/absteigend)
- ? Nach Score/Rating (absteigend)

### 3. Filter Tests (8 Tests)
Testet alle Filter-Optionen:
- ? Genre-Filter (Teilstring-Suche)
- ? Media-Type-Filter (Movie, Series, Game)
- ? Jahr-Filter (exakt, min, max)
- ? Altersfreigabe-Filter (FSK)
- ? Titel-Filter (Teilstring)

### 4. Combined Tests (4 Tests)
Testet Kombinationen aus Filtern und Sortierung:
- ? Genre + Sort
- ? MediaType + Sort
- ? Multiple Filter + Sort
- ? Creator + Sort

### 5. Edge Cases (5 Tests)
Testet Grenzf�lle und Fehlerbehandlung:
- ? Ung�ltige Sortier-Felder
- ? Case-Insensitive Parameter
- ? Leere Ergebnisse
- ? Sortier-Aliase

## Ausf�hrung der Tests

### Einzelne Tests
1. �ffnen Sie die Collection in Postman
2. F�hren Sie zuerst **alle Tests in "Setup - Create Test Data"** aus
3. F�hren Sie dann beliebige andere Tests aus

### Collection Runner (Empfohlen)
1. Klicken Sie auf die Collection
2. Klicken Sie auf "Run"
3. Stellen Sie sicher, dass alle Tests ausgew�hlt sind
4. Klicken Sie auf "Run Media Endpoint..."

Die Tests werden in der richtigen Reihenfolge ausgef�hrt.

## Bekannte Besonderheiten

### createdBy ist ein Objekt
Der Test pr�ft `item.createdBy.uuid`, nicht direkt `item.createdBy`, da es sich um ein verschachteltes User-Objekt handelt.

### Enum-Werte sind Zahlen
Die API gibt Enumerationen als Integer-Werte zur�ck:
- `mediaType`: 0-3
- `ageRestriction`: 0-4

### Leere Ergebnisse sind OK
Wenn ein Filter keine Ergebnisse liefert, gibt die API ein leeres Array `[]` zur�ck (nicht `null` oder einen Fehler).

### averageScore ist Read-Only
Der `averageScore` wird automatisch aus den Ratings berechnet und kann nicht direkt gesetzt werden.

## Fehlerbehebung

### "Cannot read property 'uuid' of undefined"
?? F�hren Sie zuerst die Setup-Tests aus, um die Collection Variables zu setzen.

### "Expected 0 but got 1"
?? Die Datenbank enth�lt bereits Daten aus vorherigen Tests. Entweder:
   - Starten Sie die Anwendung neu (l�scht In-Memory-Daten)
   - Passen Sie die Tests an die vorhandenen Daten an

### "Status code is 404"
?? �berpr�fen Sie:
   - L�uft der Server auf `http://localhost:8081`?
   - Sind die Routes korrekt implementiert?
   - Wurde der User/Media-Eintrag korrekt erstellt?

### Tests schlagen bei Sortierung fehl
?? �berpr�fen Sie, dass die `ApplySorting`-Methode in der C#-Implementierung korrekt ist.

## API-Dokumentation

### Verf�gbare Query-Parameter

#### Sortierung
- `sortBy`: `title`, `year`, `releaseYear`, `score`, `rating`, `averageScore`
- `sortOrder`: `asc`, `desc` (Standard: `asc`)

#### Filter (KOMBINIERBAR!)
Alle Filter k�nnen beliebig kombiniert werden. Die Ergebnisse werden durch ALLE angegebenen Filter gefiltert (UND-Verkn�pfung).

- `id`: GUID (exakt) - **Gibt einzelnes Objekt zur�ck, keine Kombination m�glich**
- `creator`: GUID (exakt)
- `title`: String (exakt, case-insensitive)
- `titleContains`: String (Teilstring, case-insensitive)
- `genre`: String (Teilstring, case-insensitive)
- `mediaType`: `Movie`, `Series`, `Documentary`, `Game`
- `releaseYear`: Integer (exakt)
- `minYear`: Integer (>=)
- `maxYear`: Integer (<=)
- `ageRestriction`: `FSK0`, `FSK6`, `FSK12`, `FSK16`, `FSK18`
- `minRating`: Float (>=)
- `maxRating`: Float (<=)

**Hinweis:** Der `id`-Parameter ist speziell und gibt nur ein einzelnes Objekt zur�ck. Alle anderen Filter sind frei kombinierbar.

### Beispiel-Requests

**Alle Action-Filme, sortiert nach Jahr (neueste zuerst):**
```
GET /api/media?genre=Action&mediaType=Movie&sortBy=year&sortOrder=desc
```

**Alle Medien von 2020-2023 mit FSK12:**
```
GET /api/media?minYear=2020&maxYear=2023&ageRestriction=FSK12
```

**Suche nach "Star" im Titel, beste Bewertungen zuerst:**
```
GET /api/media?titleContains=Star&sortBy=score&sortOrder=desc
```

**Action-Spiele ab 2020 mit FSK16 oder h�her, sortiert nach Bewertung:**
```
GET /api/media?genre=Action&mediaType=Game&minYear=2020&ageRestriction=FSK16&sortBy=score&sortOrder=desc
```

**Alle Filme eines bestimmten Creators mit minestens 4 Sternen:**
```
GET /api/media?creator=USER-GUID&mediaType=Movie&minRating=4&sortBy=year&sortOrder=desc
```

## Erwartete Test-Ergebnisse

Bei korrekter Implementierung sollten **alle 27 Tests erfolgreich** durchlaufen:
- ? 5 Setup-Tests (Status 201)
- ? 5 Sorting-Tests
- ? 8 Filter-Tests
- ? 4 Combined-Tests
- ? 5 Edge-Case-Tests

## Collection Variables

Die Collection verwendet folgende Variablen (werden automatisch gesetzt):

| Variable | Beschreibung | Beispiel |
|----------|--------------|----------|
| `base_url` | API Base URL | `http://localhost:8081` |
| `user1_id` | UUID des Test-Users | Auto-generiert |
| `media1_id` | UUID: Action Movie 2023 | Auto-generiert |
| `media2_id` | UUID: Comedy Series 2021 | Auto-generiert |
| `media3_id` | UUID: Drama Movie 2020 | Auto-generiert |
| `media4_id` | UUID: Action Game 2022 | Auto-generiert |

Diese Variablen k�nnen Sie im Collection-Tab unter "Variables" einsehen.

## Version & Kompatibilit�t

- **Postman Collection Version:** 2.1.0
- **Getestet mit:** Postman 10.x
- **API-Version:** .NET 8
- **Letzte Aktualisierung:** 2024

## Support

Bei Fragen oder Problemen:
1. �berpr�fen Sie die Console in Postman (View ? Show Postman Console)
2. Pr�fen Sie die Server-Logs
3. Stellen Sie sicher, dass der Server l�uft und erreichbar ist
