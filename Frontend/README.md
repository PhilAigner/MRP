# MRP API Test Frontend

Ein einfaches, aber vollst�ndiges HTML/JavaScript Frontend zum Testen der MRP (Media Rating Platform) API.

## ?? Quick Start

### 1. Server starten
Stellen Sie sicher, dass Ihr MRP API-Server l�uft:
```bash
cd C:\Users\Philipp\Documents\PHILIPP\FH\3\Seng\repos\MRP
dotnet run
```

Der Server sollte auf `http://localhost:8081` laufen.

### 2. Frontend �ffnen
�ffnen Sie einfach die `index.html` Datei in Ihrem Browser:

**Option 1: Direkt �ffnen**
- Doppelklick auf `index.html`
- Oder rechtsklick ? "�ffnen mit" ? Browser ausw�hlen

**Option 2: Mit Live Server (empfohlen)**
Falls Sie VS Code mit Live Server Extension verwenden:
- Rechtsklick auf `index.html`
- "Open with Live Server"

**Option 3: Mit Python HTTP Server**
```bash
cd Frontend
python -m http.server 3000
# Dann �ffnen: http://localhost:3000
```

## ?? Features

### ? Vollst�ndige API-Abdeckung

#### ?? User Management
- **Register User** - Neuen Benutzer erstellen
- **Login User** - Benutzer einloggen
- **User Profile** - Profil anzeigen und bearbeiten

#### ?? Media Management
- **Create Media** - Neuen Media-Eintrag erstellen
- **List Media** - Alle Media-Eintr�ge anzeigen
- **Filter & Sort** - Kombinierbare Filter und Sortierung
- **Update Media** - Media-Eintrag aktualisieren
- **Delete Media** - Media-Eintrag l�schen

#### ? Ratings Management
- **Create Rating** - Neue Bewertung erstellen
- **List Ratings** - Bewertungen anzeigen (optional gefiltert)
- **Update Rating** - Bewertung aktualisieren
- **Delete Rating** - Bewertung l�schen

### ?? UI Features

#### ?? Automatisches ID-Speichern
Das Frontend speichert automatisch wichtige IDs (User, Media, Rating) im localStorage:
- Nach User-Registrierung wird die User-ID gespeichert
- Nach Media-Erstellung wird die Media-ID gespeichert
- Nach Rating-Erstellung wird die Rating-ID gespeichert

Diese IDs werden automatisch in Formulare eingetragen, wo sie ben�tigt werden!

#### ?? Smart Forms
- **Auto-Fill**: Gespeicherte IDs werden automatisch verwendet
- **Optional Fields**: Viele Felder sind optional - nur Pflichtfelder m�ssen ausgef�llt werden
- **Validation**: Clientseitige Validierung vor dem API-Call

#### ?? Response Display
- �bersichtliche Anzeige der API-Responses
- Status-Badges (Success/Error)
- JSON Syntax-Highlighting
- HTTP Status Code Anzeige

#### ?? Moderne UI
- Gradient-Design mit lila/blau Farbschema
- Responsive Layout
- Smooth Animations
- Sidebar-Navigation

## ?? Verwendung

### Typischer Workflow

#### 1. Benutzer erstellen
```
1. Navigation: User Management ? Register User
2. Username eingeben: "testuser"
3. Password eingeben: "password123"
4. "Register" klicken
5. ? User-ID wird automatisch gespeichert!
```

#### 2. Media-Eintrag erstellen
```
1. Navigation: Media ? Create Media
2. Title eingeben: "Inception"
3. Media Type w�hlen: "Movie"
4. Jahr: 2010
5. FSK: FSK12
6. Genre: "Science Fiction"
7. Creator ID wird automatisch aus gespeicherten Daten gef�llt
8. "Create Media" klicken
9. ? Media-ID wird automatisch gespeichert!
```

#### 3. Bewertung erstellen
```
1. Navigation: Ratings ? Create Rating
2. Media-ID eingeben (oder aus gespeicherten Daten verwenden)
3. User-ID wird automatisch gef�llt
4. Stars: 5
5. Comment: "Mind-blowing movie!"
6. Public Visible aktivieren
7. "Create Rating" klicken
```

#### 4. Filter & Suche verwenden
```
1. Navigation: Media ? Filter & Sort
2. Genre eingeben: "Action"
3. Media Type w�hlen: "Movie"
4. Min Year: 2020
5. Sort By: "year"
6. Sort Order: "desc"
7. "Apply Filters" klicken
8. ? Zeigt alle Action-Filme ab 2020, neueste zuerst
```

## ?? Filter & Sort Features

### Kombinierbare Filter
Alle Filter k�nnen **beliebig kombiniert** werden:

- **Title Contains** - Suche im Titel (case-insensitive)
- **Genre** - Genre-Filter (Teilstring-Suche)
- **Media Type** - Movie, Series, Documentary, Game
- **Age Restriction** - FSK0, FSK6, FSK12, FSK16, FSK18
- **Min Year** - Minimum Release Year
- **Max Year** - Maximum Release Year

### Sortierung
- **Sort By**: title, year, score
- **Sort Order**: ascending (asc) oder descending (desc)

### Beispiel-Kombinationen

**Action-Filme ab 2020, beste zuerst:**
```
Genre: Action
Media Type: Movie
Min Year: 2020
Sort By: score
Sort Order: desc
```

**FSK12 Medien von 2020-2023, alphabetisch:**
```
Age Restriction: FSK12
Min Year: 2020
Max Year: 2023
Sort By: title
Sort Order: asc
```

## ?? Troubleshooting

### Problem: "Failed to fetch" Fehler

**L�sung:**
1. �berpr�fen Sie, ob der Server l�uft (`http://localhost:8081`)
2. �berpr�fen Sie die Browser-Console (F12) f�r CORS-Fehler
3. Stellen Sie sicher, dass die API auf Port 8081 l�uft

### Problem: "User not found" beim Media erstellen

**L�sung:**
1. Erstellen Sie zuerst einen User (Register)
2. Die User-ID wird automatisch gespeichert
3. Beim Media erstellen wird diese ID automatisch verwendet

### Problem: Gespeicherte IDs sind weg

**L�sung:**
Die IDs werden im localStorage gespeichert. Wenn Sie:
- Browser-Cache l�schen
- Inkognito-Modus verwenden
- Anderen Browser verwenden

...gehen die gespeicherten IDs verloren. Einfach neu einen User/Media erstellen.

### Problem: Response wird nicht angezeigt

**L�sung:**
- Scrollen Sie nach unten - die Response erscheint unter dem Formular
- �berpr�fen Sie die Browser-Console (F12) f�r JavaScript-Fehler

## ?? Dateistruktur

```
Frontend/
??? index.html      # Haupt-HTML-Datei (UI)
??? app.js          # JavaScript (API-Calls & Logic)
??? README.md       # Diese Datei
```

## ?? API Endpoints

Das Frontend verwendet folgende API-Endpoints:

### User
- `POST /api/users/register` - User registrieren
- `POST /api/users/login` - User einloggen
- `GET /api/users/profile?userid={id}` - Profil abrufen
- `PUT /api/users/profile` - Profil aktualisieren

### Media
- `POST /api/media` - Media erstellen
- `GET /api/media` - Alle Media abrufen
- `GET /api/media?{filter}` - Gefilterte Media abrufen
- `PUT /api/media` - Media aktualisieren
- `DELETE /api/media?id={id}` - Media l�schen

### Ratings
- `POST /api/ratings` - Rating erstellen
- `GET /api/ratings` - Alle Ratings abrufen
- `GET /api/ratings?creator={id}` - Ratings nach Creator
- `GET /api/ratings?media={id}` - Ratings nach Media
- `PUT /api/ratings` - Rating aktualisieren
- `DELETE /api/ratings?id={id}` - Rating l�schen

## ?? Tipps & Tricks

### 1. Browser DevTools nutzen
Dr�cken Sie **F12** um die Browser DevTools zu �ffnen:
- **Console**: Zeigt API-Calls und Fehler
- **Network**: Zeigt HTTP-Requests und Responses
- **Application ? Local Storage**: Zeigt gespeicherte IDs

### 2. JSON formatieren
Responses werden automatisch formatiert und farblich hervorgehoben.

### 3. Schnelles Testen
1. User registrieren ? ID wird gespeichert
2. Mehrere Media erstellen ? Funktioniert sofort mit gespeicherter User-ID
3. Ratings hinzuf�gen ? User-ID wird automatisch verwendet

### 4. Filter zur�cksetzen
Nutzen Sie den "Clear Filters" Button, um alle Filter-Felder auf einmal zu leeren.

## ?? Anpassungen

### API URL �ndern
Falls Ihr Server auf einem anderen Port l�uft, �ndern Sie in `app.js`:

```javascript
const API_BASE = 'http://localhost:IHREN_PORT/api';
```

### Styling anpassen
Alle Styles sind in `<style>`-Tags in der `index.html`. �ndern Sie:
- Farben im `header` und `.btn-primary`
- Schriftarten in `body`
- Layout in `.main-content`

## ?? Response-Beispiele

### Erfolgreiche User-Registrierung
```json
{
  "message": "User registered successfully",
  "uuid": "123e4567-e89b-12d3-a456-426614174000"
}
```

### Media-Liste
```json
[
  {
    "uuid": "550e8400-e29b-41d4-a716-446655440000",
    "title": "Inception",
    "description": "A mind-bending thriller",
    "mediaType": 0,
    "releaseYear": 2010,
    "ageRestriction": 2,
    "genre": "Science Fiction",
    "averageScore": 4.5,
    "createdBy": {
      "uuid": "123e4567-e89b-12d3-a456-426614174000",
      "username": "testuser"
    }
  }
]
```

## ?? Erweiterte Features

### LocalStorage Inspector
�ffnen Sie die Browser DevTools (F12) und navigieren zu:
```
Application ? Local Storage ? file:// (oder Ihre Domain)
```

Dort sehen Sie den Key `mrp-saved-data` mit allen gespeicherten IDs.

### API Testing Workflow
1. **Setup**: User registrieren
2. **Content**: Mehrere Media-Eintr�ge erstellen
3. **Interaction**: Ratings hinzuf�gen
4. **Exploration**: Filter & Sort ausprobieren
5. **Management**: Updates und Deletes testen

## ?? Support

Bei Problemen oder Fragen:
1. �berpr�fen Sie die Browser-Console (F12)
2. Pr�fen Sie, ob der API-Server l�uft
3. Schauen Sie in die API-Logs

## ?? Viel Erfolg beim Testen!

Das Frontend ist vollst�ndig funktional und bereit f�r Ihre API-Tests. Alle Features sind implementiert und sollten out-of-the-box funktionieren.

**Happy Testing! ??**
