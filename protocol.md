# Protokoll - Media Rating Platform (MRP)
**Projekt von:** Philipp Aigner  
**Repository:** https://github.com/PhilAigner/MRP  


---

## 1. Technische Schritte und Architekturentscheidungen

### 1.1 Gesamtarchitektur
Die Media Rating Platform folgt einem **Schichtenarchitektur-Muster** mit klarer Trennung der Verantwortlichkeiten:

```
Präsentationsschicht (HTTP Endpoints)
    ?
Service-Schicht (Buisnesslogic)
    ?
Repository-Schicht (Datenzugriff)
    ?
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
- Vermeidung von Frameworks (ASP.NET Core) zu Lernzwecken
- Demonstriert Verständnis von HTTP-Grundlagen
- Volle Kontrolle über Request/Response-Handling

#### Repository Pattern
Jede Entität (User, Media, Rating, Profile) hat ein eigenes Repository mit Interface:
- `IUserRepository` ? `UserRepository`
- `IMediaRepository` ? `MediaRepository`
- `IRatingsRepository` ? `RatingRepository`
- `IProfileRepository` ? `ProfileRepository`

**Vorteile:**
- Abstrahiert Datenbankoperationen von Geschäftslogik
- Ermöglicht Unit-Tests mit gemockten Repositories
- Erlaubt einfachen Wechsel des Datenbank-Providers
- Validiert alle Eingaben vor Persistierung

#### Service Layer
Dedizierte Service-Klassen für komplexe Geschäftslogik:
- **UserService**: Benutzerregistrierung, Login, Profilverwaltung
- **MediaService**: Medienerstellung mit automatischer Profilstatistik
- **RatingService**: Bewertungserstellung/-genehmigung, Like-Funktionalität
- **ProfileStatisticsService**: Automatische Berechnung von Favoriten-Genre/-Typ
- **TokenService**: Authentifizierungs-Token-Management
- **PasswordHasher**: Sichere Passwort-Hashierung mit bcrypt

**Begründung:**
- Hält Endpoints dünn und fokussiert auf HTTP-Belange
- Kapselt komplexe Multi-Step-Operationen
- Stellt konsistente Anwendung von Geschäftsregeln sicher

### 1.3 Sicherheitsarchitektur

#### Authentifizierungssystem
- **Token-basierte Authentifizierung**: Bearer Tokens werden beim Login generiert
- **In-Memory Token Storage**: `ConcurrentDictionary` für thread-sichere Token-Verwaltung
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

#### Batch Loading Optimierung
**Problem:** N+1 Query Problem beim Laden von Rating Likes  
**Lösung:** Batch Loading in `LoadLikedByBatch()`
- Eine Query holt alle Likes für mehrere Ratings mit SQL IN Klausel
- Dictionary-basiertes Lookup weist Likes zu Ratings zu (O(1) Komplexität)
- Reduziert Datenbank-Roundtrips signifikant

### 1.5 Datenbankdesign
- **PostgreSQL** als RDBMS
- **Foreign Key Constraints** erzwingen referentielle Integrität
- **Separate Junction Table** (`rating_likes`) für Many-to-Many Beziehung
- **Umgebungsbasierte Konfiguration** via .env Datei

**Tabellen:**
- `users`: Benutzeranmeldedaten und Metadaten
- `profiles`: Benutzerstatistiken und Präferenzen (1:1 mit users)
- `media_entries`: Von Benutzern erstellte Medieninhalte
- `ratings`: Benutzerbewertungen zu Medien
- `rating_likes`: Many-to-Many Beziehung für Rating Likes

### 1.6 CORS Konfiguration
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

### 3.4 Token-Persistenz über Sessions hinweg
**Problem:** In-Memory Token Storage verliert Tokens bei Server-Neustart  
**Lösung:** Als Limitierung für aktuellen Scope akzeptiert (für zukünftige Verbesserung notiert)  
**Zukünftige Verbesserung:** Migration zu Datenbank-gestütztem Token Storage mit Ablauf

### 3.5 Rating Approval Autorisierung
**Problem:** Initiale Implementierung erlaubte jedem Benutzer das Setzen des `publicVisible` Flags  
**Lösung:** `publicVisible` als read-only im DTO gemacht, erzwungen über dedizierten Approval-Endpoint  
**Lernerfolg:** Autorisierungslogik muss serverseitig erzwungen werden, nicht vom Client vertraubar

### 3.6 Profilstatistik-Synchronisation
**Problem:** Statistiken gerieten außer Synchronisation, wenn Operationen mitten drin fehlschlugen  
**Lösung:** Statistiken in gleicher Transaktion wie Hauptoperation aktualisiert, Validierung hinzugefügt  
**Zukünftige Verbesserung:** Datenbank-Triggers für automatische Statistik-Updates in Betracht ziehen

### 3.7 CORS Issues mit Frontend
**Problem:** Browser blockierte API-Requests von lokaler HTML-Datei  
**Lösung:** CORS-Header zu HTTP-Responses hinzugefügt  
**Hinweis:** CORS sollte in Produktion auf spezifische Origins beschränkt werden

### 3.8 Case-Sensitive Path Matching
**Problem:** API-Routen schlugen fehl wegen Case-Sensitivity  
**Lösung:** Alle Pfade in `CanHandle()` Methoden auf lowercase normalisiert  
**Lernerfolg:** HTTP-Pfade sollten case-insensitive sein für bessere User Experience

---

## 4. Zeitschätzung und Projektaufschlüsselung

### 4.1 Zeittracking nach Phase

| Phase | Aufgabe | Geschätzte Zeit | Notizen |
|-------|---------|----------------|---------|
| **Setup & Infrastruktur** | | **~4 Stunden** | |
| | Projektstruktur & .NET Setup | 1h | Solution, Projekte, NuGet Packages |
| | Datenbank-Schema Design | 1h | Tabellen, Beziehungen, Constraints |
| | PostgreSQL Integration & Testing | 1.5h | Connection, Environment Config |
| | HTTP Server Implementierung | 0.5h | Basis HttpListener Setup |
| **Data Layer** | | **~6 Stunden** | |
| | User & Profile Repositories | 1.5h | CRUD-Operationen, Validierung |
| | Media Repository | 1.5h | CRUD mit Filterung |
| | Rating Repository | 2h | Komplexe Queries, Likes Junction Table |
| | Database Exception Handling | 1h | Custom Exceptions, Validierung |
| **Geschäftslogik** | | **~8 Stunden** | |
| | User Service & Authentifizierung | 2h | Registrierung, Login, Token-Generierung |
| | Password Hashing Integration | 1h | BCrypt Implementierung |
| | Media Service | 1.5h | CRUD mit Statistik-Updates |
| | Rating Service | 2.5h | Rating, Approval, Like Logik |
| | Profile Statistics Service | 1h | Favoriten-Berechnungs-Algorithmus |
| **API Endpoints** | | **~7 Stunden** | |
| | User Endpoints (register/login) | 1.5h | Request Parsing, Response Formatting |
| | Profile Endpoint | 1h | Authentifizierung, Autorisierung |
| | Media Endpoints | 2h | Full CRUD, Filtering, Sorting |
| | Rating Endpoints | 2h | Rating CRUD, Approve, Like |
| | Authentication Helper | 0.5h | Zentralisierte Auth-Logik |
| **Testing** | | **~8 Stunden** | |
| | Test Infrastructure Setup | 1h | NUnit, Moq Konfiguration |
| | Service Layer Tests | 4h | Alle Service-Tests mit Mocks |
| | Model Tests | 1h | Basis-Validierungstests |
| | Password Hasher Tests | 0.5h | Sicherheits-Validierung |
| | Bug Fixing aus Tests | 1.5h | Issues gefunden während Testing |
| **Frontend & Dokumentation** | | **~5 Stunden** | |
| | HTML/CSS Frontend | 2h | Basis-UI zum Testen |
| | JavaScript API Client | 2h | Fetch Calls, Form Handling |
| | Readme Dokumentation | 0.5h | Projekt-Übersicht |
| | CORS Konfiguration | 0.5h | Cross-Origin Requests aktivieren |
| **Debugging & Verfeinerung** | | **~6 Stunden** | |
| | Autorisierungs-Bugfixes | 1.5h | Permission Checks, Edge Cases |
| | Statistik-Synchronisation | 1.5h | Out-of-Sync Zähler fixen |
| | Batch Loading Optimierung | 1h | N+1 Query Auflösung |
| | Allgemeines Debugging | 2h | Diverse kleine Fixes |

**Geschätzte Gesamtzeit: ~44 Stunden**

### 4.2 Entwicklungsphasen

1. **Foundation** (Tage 1-2): Datenbank-Schema, Repositories, basis HTTP Server
2. **Core Features** (Tage 3-4): Services, Authentifizierung, Geschäftslogik
3. **API Implementierung** (Tage 5-6): Alle Endpoints, Autorisierung
4. **Testing & Quality** (Tage 7-8): Unit Tests, Bugfixes
5. **Polish & Dokumentation** (Tag 9): Frontend, Dokumentation, finales Testing

---

## 5. Zukünftige Verbesserungen & Bekannte Limitierungen

### 5.1 Aktuelle Limitierungen
- **In-Memory Token Storage**: Verloren bei Server-Neustart
- **Keine Token-Ablaufzeit**: Tokens bleiben unbegrenzt gültig
- **Keine Paginierung**: Alle Ergebnisse werden auf einmal zurückgegeben (Skalierungsproblem)
- **In-Memory Filterung**: Sollte in SQL für Performance gemacht werden
- **Kein Rate Limiting**: Anfällig für Missbrauch
- **CORS weit offen**: Sollte in Produktion auf spezifische Origins beschränkt werden

### 5.2 Potentielle Verbesserungen
- **Persistenter Token Storage**: Migration zur Datenbank mit Ablaufzeit
- **JWT Tokens**: Standard Token-Format mit Claims
- **Paginierung**: Offset/Limit zu List-Endpoints hinzufügen
- **SQL Filtering**: Filterung zu Datenbank-Queries pushen
- **Caching**: Response-Caching für häufig abgerufene Daten hinzufügen
- **API Versionierung**: Unterstützung mehrerer API-Versionen
- **Logging**: Umfassendes Logging für Debugging und Monitoring
- **Docker Deployment**: Application containerisieren
- **API Dokumentation**: OpenAPI/Swagger Integration

---

## 6. Lessons Learned

### 6.1 Technische Erkenntnisse
- **Schichtenarchitektur** verbessert Code-Organisation und Testbarkeit signifikant
- **Repository Pattern** macht Wechsel von Datenquellen trivial
- **Dependency Injection** via Interfaces ermöglicht effektives Unit Testing
- **Autorisierung muss serverseitig** sein und kann nicht vom Client vertraut werden
- **Statistik-Tracking** erfordert sorgfältiges Transaktionsmanagement
- **Performance-Optimierung** erfordert oft Batch-Operationen und richtiges Indexing

### 6.2 Entwicklungspraktiken
- **Test-driven Development** fängt Bugs früh ab und dokumentiert erwartetes Verhalten
- **Git History** dient als natürliche Dokumentation des Entscheidungsprozesses
- **Umgebungskonfiguration** (`.env` Dateien) verhindert hardcodierte Credentials
- **Klare Separation of Concerns** macht Debugging und Wartung einfacher
- **Input-Validierung** auf Repository-Ebene verhindert Persistierung ungültiger Daten

### 6.3 Sicherheitsbewusstsein
- **Passwort-Hashing** ist nicht verhandelbar für Benutzerdatenschutz
- **Autorisierungsprüfungen** müssen explizit und umfassend sein
- **Token-Management** erfordert sorgfältige Überlegung zu Storage und Lifecycle
- **CORS-Konfiguration** muss in Produktion restriktiv sein
- **SQL Injection Prevention** durch parametrisierte Queries

---

## 7. Fazit

Die Media Rating Platform demonstriert erfolgreich eine Full-Stack .NET Anwendung mit:
- Sauberer Schichtenarchitektur
- Umfassender Sicherheit (Authentifizierung & Autorisierung)
- Automatischem Statistik-Tracking
- Extensiver Unit Test Coverage
- Custom HTTP Server Implementierung

Das Projekt priorisiert Code-Qualität, Testbarkeit und Sicherheit bei gleichzeitiger klarer Trennung der Verantwortlichkeiten. Die Git-History bietet detaillierte Dokumentation des Entwicklungsprozesses und der Entscheidungsbegründungen.

---

## Hinweise zur Dokumentation

- Die Git-History ist Teil der Dokumentation und enthält den detaillierten Entwicklungsverlauf
- Commit-Messages dokumentieren die Entscheidungen und Änderungen im Projektverlauf
- Dieses Protokoll fasst die wichtigsten architektonischen Entscheidungen, Probleme und deren Lösungen zusammen
