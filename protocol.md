# Protokoll - Media Rating Platform (MRP)
**Projekt von:** Philipp Aigner  
**Repository:** https://github.com/PhilAigner/MRP  


---

## 1. Technische Schritte und Architekturentscheidungen

### 1.1 Gesamtarchitektur
Die Media Rating Platform folgt einem **Schichtenarchitektur-Muster** mit klarer Trennung der Verantwortlichkeiten:

```
Präsentationsschicht (HTTP Endpoints)
    
Service-Schicht (Buisnesslogic)
    
Repository-Schicht (Datenzugriff)
    
Datenbankschicht (PostgreSQL)
```

**Begründung:** Diese Architektur bietet:
- Klare Trennung zwischen HTTP-Verarbeitung, Geschäftslogik und Daten
- Einfacheres Testen durch Dependency Injection und Interface-Abstraktion
- Bessere Wartbarkeit, da jede Schicht eine einzelne Verantwortung hat

### 1.2 Hauptarchitekturkomponenten

#### HTTP Server Architektur
- **Custom HTTP Server** mit `HttpListener` aus .NET
- **Endpoint Pattern**: Jeder Funktionsbereich (Users, Media, Ratings) hat dedizierte Endpoint-Klassen
- **Interface-basiertes Routing**: `IHttpEndpoint` Interface ermöglicht dynamische Endpoint-Registrierung
- **Pfad-basiertes Routing**: Endpoints bestimmen über `CanHandle()` Methode, ob sie eine Anfrage verarbeiten können

**Begründung der Entscheidung:**
- ASP.NET Core durfte nicht verwendet werden
- Demonstriert Verständnis von HTTP-Grundlagen
- Volle Kontrolle über Request/Response-Handling

#### Repository Pattern
Jede Entität (User, Media, Rating, Profile) hat ein eigenes Repository mit Interface:
- `IUserRepository` - `UserRepository`
- `IMediaRepository` - `MediaRepository`
- `IRatingsRepository` - `RatingRepository`
- `IProfileRepository` - `ProfileRepository`

**Vorteile:**
- Abstrahiert Datenbankoperationen von Geschäftslogik
- Ermöglicht Unit-Tests mit gemockten Repositories
- Erlaubt einfachen Wechsel des Datenbank-Providers
- Validiert alle Eingaben vor Persistierung

#### Service Layer
Dedizierte Service-Klassen für komplexe Geschäftslogik:
- **UserService**: Benutzerregistrierung, Login, Profilverwaltung
- **MediaService**: Medienerstellung mit automatischer Profilstatistik des Erstellers
- **RatingService**: Rating Erstellung & Genehmigung, Like-Funktionalität
- **ProfileStatisticsService**: Automatische Berechnung von Favoriten-Genre/-Typ und Statistik-Tracking
- **TokenService**: Authentifizierungs-Token-Management
- **PasswordHasher**: Sichert Passworter gehashed mit bcrypt

**Begründung:**
- Hält Endpoints übersichtlich und fokussiert sich auf die Umsetzung der HTTP Funktionalitäten
- Kapselt komplexe Multi-Step-Operationen ( zB user erstellen )
- Stellt konsistente Anwendung von Geschäftsregeln sicher ( zB Statistik-Updates )

### 1.3 Sicherheitsarchitektur

#### Authentifizierungssystem
- **Token-basierte Authentifizierung**: Bearer Tokens werden beim Login generiert
- **User-scoped Tokens**: Ein aktiver Token pro Benutzer, wird beim Login regeneriert
- **AuthenticationHelper**: Zentralisierte Authentifizierungsvalidierung

**Passwortsicherheit:**
- BCrypt-Hashing-Algorithmus mit automatischer Salt-Generierung
- Passwörter werden niemals im Klartext gespeichert
- Hash-Verifikation beim Login

#### Autorisierung
- **User-scoped Operationen**: Benutzer können nur ihre eigenen Daten ändern
- **Creator Ownership**: Nur Media-Ersteller können ihre Medien aktualisieren/löschen
- **Media Owner Approval**: Nur Media-Besitzer können Bewertungen genehmigen
- **Explizite Berechtigungsprüfungen** in Endpoint-Handlern vor Operationen

**Begründung:**
- Token-basierte Auth ist einfach und zustandslos
- BCrypt bietet industriestandard Passwortsicherheit
- Autorisierungsprüfungen verhindern unbefugte Datenänderungen

### 1.4 Spezielle Features & Geschäftslogik

#### Rating Approval System
**Problem:** Spam/missbräuchliche Bewertungen auf Medieninhalten verhindern  
**Lösung:** Zweiphasige Bewertungssichtbarkeit:
1. Benutzer erstellen Bewertungen (initial nicht öffentlich)
2. Media-Besitzer muss über `/api/ratings/{id}/approve` Endpoint genehmigen
3. Nur genehmigte Bewertungen werden öffentlich sichtbar

**Implementierung:**
- `publicVisible` Feld im Rating Model (standardmäßig false)
- Kann NUR über Approval-Endpoint gesetzt werden, nicht bei Erstellung/Update
- Kann NUR vom Mediabesitzer auf true gesetzt werden
- Stellt sicher, dass Content-Besitzer kontrollieren, welche Bewertungen erscheinen

#### Automatische Profilstatistiken
Das System trackt automatisch Benutzeraktivitäten:
- `numberOfLogins`: Bei jedem erfolgreichen Login erhöht
- `numberOfRatingsGiven`: Bei Bewertungserstellung/-löschung getrackt
- `numberOfReviewsWritten`: Zählt Bewertungen mit Textkommentaren
- `numberOfMediaAdded`: Bei Medienerstellung/-löschung aktualisiert
- `favoriteGenre` & `favoriteMediaType`: Aus Benutzerbewertungen berechnet

**Implementierung:**
- Statistiken werden transaktional mit Hauptoperationen aktualisiert
- ProfileStatisticsService berechnet Favoriten basierend auf Bewertungshäufigkeit
- Stellt Datenkonsistenz über Operationen hinweg sicher


**Datenbankstruktur:**
- `users`: Benutzeranmeldedaten und Metadaten
- `profiles`: Benutzerstatistiken und Präferenzen (1:1 mit users)
- `media_entries`: Von Benutzern erstellte Medieninhalte
- `ratings`: Benutzerbewertungen zu Medien
- `rating_likes`: Many-to-Many Beziehung für Rating Likes


### 1.5 CORS Konfiguration
- Aktiviert für lokale Frontend-Tests
- Erlaubt Cross-Origin Requests während der Entwicklung
- **Hinweis:** Sollte in Produktionsumgebung eingeschränkt werden

---

## 2. Unit Test Coverage und Teststrategie

### 2.1 Test-Ansatz
Alle Tests verwenden **NUnit** Framework mit **Moq** für Mocking von Dependencies. Tests folgen dem **AAA-Pattern** (Arrange-Act-Assert).

### 2.2 Test Coverage nach Komponente

#### TokenService Tests (`TokenServiceTests.cs`)
**Getestete Logik:**
- Token-Generierung gibt gültige, benutzernamenhaltige Tokens zurück
- Gleicher Benutzer erhält immer gleichen Token (Idempotenz)
- Token-Validierung identifiziert korrekt gültige Tokens und gibt User-ID zurück
- Bearer Token Extraktion aus Authorization Header
- Token-Widerruf entfernt Tokens aus aktivem Pool

**Warum diese Tests:**
- Authentifizierung ist sicherheitskritisch
- Token-Eindeutigkeit verhindert Session-Hijacking
- Korrektes Bearer Token Parsing stellt API-Konformität sicher

#### UserService Tests (`UserServiceTests.cs`)
**Getestete Logik:**
- Neue Benutzerregistrierung erstellt Benutzer mit gehashtem Passwort
- Doppelte Benutzernamen-Registrierung schlägt fehl (gibt leere GUID zurück)
- Gültige Anmeldedaten geben Authentifizierungs-Token zurück
- Ungültiger Benutzername/Passwort gibt null zurück
- Login erhöht Profil-Login-Zähler

**Warum diese Tests:**
- Registrierungsvalidierung verhindert doppelte Benutzer
- Passwort-Hash-Verifikation stellt Sicherheit sicher
- Login-Flow ist Einstiegspunkt für alle authentifizierten Operationen

#### MediaService Tests (`MediaServiceTests.cs`)
**Getestete Logik:**
- Medienerstellung erhöht Profil-Media-Zähler
- Media-Updates ändern bestehende Einträge
- Media-Löschung verringert Profil-Zähler
- Nicht-existierende Media-Operationen schlagen elegant fehl

**Warum diese Tests:**
- Stellt sicher, dass Profilstatistiken synchron bleiben
- Validiert, dass Media CRUD-Operationen korrekt funktionieren
- Testet kaskadierende Effekte auf verknüpfte Daten

#### RatingService Tests (`RatingServiceTests.cs`)
**Getestete Logik:**
- Neue Bewertung erhöht Profil-Bewertungszähler
- Aktualisierung bestehender Bewertung ändert Sterne ohne Duplikat zu erstellen
- Bewertungslöschung verringert Profilzähler (sowohl Rating als auch Review falls zutreffend)
- Bewertungsgenehmigung erfordert Media-Besitzer-Autorisierung
- Nicht-Besitzer können Bewertungen nicht genehmigen
- Like/Unlike Funktionalität funktioniert korrekt

**Warum diese Tests:**
- Bewertungsgenehmigungssystem ist ein zentrales Sicherheitsfeature
- Statistik-Tracking ist komplex mit mehreren Seiteneffekten
- Like-Funktionalität erfordert korrekte Autorisierung

#### ProfileStatisticsService Tests (`ProfileStatisticsServiceTests.cs`)
**Getestete Logik:**
- Favoriten-Genre-Berechnung wählt am häufigsten bewertetes Genre
- Favoriten-Media-Typ-Berechnung wählt am häufigsten bewerteten Typ
- Vollständige Statistik-Neuberechnung zählt Ratings, Reviews und Media
- Keine Ratings löst keine unnötigen Updates aus

**Warum diese Tests:**
- Favoritenberechnung ist algorithmisch komplex
- Statistiken müssen Benutzeraktivität genau widerspiegeln
- Verhindert unnötige Datenbank-Schreibvorgänge

#### Model Tests (`ModelTests.cs`)
**Getestete Logik:**
- Objektinstanziierung mit korrekten Standardwerten
- GUID-Generierung ist eindeutig
- Read-only Properties können nach Konstruktion nicht geändert werden
- DateTime Properties werden korrekt gesetzt

**Warum diese Tests:**
- Stellt sicher, dass Domänenmodelle sich korrekt verhalten
- Validiert Unveränderlichkeit kritischer Felder
- Testet Entitätserstellung mit erforderlichen Dependencies

#### Password Hasher Tests (`PasswordHasherTests.cs`)
**Getestete Logik:**
- Gehashtes Passwort unterscheidet sich von Klartext
- Gleiches Passwort erzeugt unterschiedliche Hashes (durch Salt)
- Korrekte Passwort-Verifikation gelingt
- Falsche Passwort-Verifikation schlägt fehl

**Warum diese Tests:**
- Passwortsicherheit ist kritisch für Benutzerschutz
- Verifiziert bcrypt Salt-Zufälligkeit
- Stellt sicher, dass Hash-Verifikation korrekt funktioniert

### 2.3 Teststrategie-Begründung

**Warum Mock Repositories:**
- Testet Geschäftslogik isoliert von Datenbank
- Schnellere Testausführung (kein Datenbank-Setup/Teardown)
- Konsistente Testdaten ohne Datenbank-Zustandsprobleme
- Einfacher Edge Cases und Fehler zu simulieren

**Was nicht getestet wurde:**
- HTTP Endpoint Request/Response Handling (benötigt Integrationstests)
- Tatsächliche Datenbankoperationen (benötigt Datenbank-Integrationstests)
- Frontend JavaScript Code (würde separate Frontend-Tests benötigen)

**Test Coverage Fokus:**
- **Services** (Geschäftslogik) - höchste Priorität
- **Sicherheitskritische Komponenten** (Authentifizierung, Autorisierung)
- **Komplexe Berechnungen** (Statistiken, Favoriten)
- **Models** (grundlegende Validierung und Konstruktion)

---

## 3. Aufgetretene Probleme und Lösungen

### 3.1 Foreign Key Constraint Violations
**Problem:** Profilerstellung schlug fehl, weil User noch nicht in Datenbank existierte  
**Lösung:** Reihenfolge der Erstellung geändert - User zuerst speichern (für Foreign Key), dann Profile erstellen  
**Lernerfolg:** Datenbank-Constraints erzwingen Datenintegrität, erfordern aber sorgfältige Operationsreihenfolge

### 3.2 N+1 Query Problem mit Rating Likes
**Problem:** Laden von Rating Likes benötigte eine Query pro Rating (Performance-Problem)  
**Lösung:** Implementierung von `LoadLikedByBatch()` mit SQL IN Klausel und Dictionary-basierter Zuweisung  
**Auswirkung:** Reduzierung der Datenbank-Queries von N+1 auf 2 (eine für Ratings, eine für alle Likes)

### 3.3 Passwortsicherheit
**Problem:** Initiale Implementierung speicherte Passwörter im Klartext  
**Lösung:** Integration von BCrypt via `PasswordHasher` Service mit Salt und Hashing  
**Lernerfolg:** Niemals Klartext-Passwörter speichern; bewährte Hashing-Algorithmen verwenden

---

## 4 Zeitplan

1. **Foundation** ( ~ 10 h ): Datenbank-Schema, Repositories, basis HTTP Server
2. **Core Features** ( ~ 20 h ): Services, Authentifizierung, Geschäftslogik
3. **API Implementierung** ( ~ 15 h ): Alle Endpoints, Autorisierung
4. **Testing & Quality** ( ~25 h ): Unit Tests, Bugfixes, tiefgehende Funktionstests
5. **Polish & Dokumentation** ( ~ 8 h ): Frontend, Postmantests ausfühlicher, Protokoll, finales Testing

---