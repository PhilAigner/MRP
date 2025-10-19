# Philipp Aigner - Media Rating Platform (MRP) - Intermediate Hand in

## Git Link

><https://github.com/PhilAigner/MRP.git>



## App-Design und Struktur

### Überblick
Die Media Rating Platform (MRP) ist eine Anendung zum erstellen von Medieninhalten wie Filmen, Serien, Dokumentationen und Spielen. Die Plattform ermöglicht es Benutzern, Medieninhalte zu erstellen, zu bewerten und Bewertungen anderer Benutzer zu liken.

### Design-Entscheidungen

#### Architektur
- **HTTP-Server**: Die Anwendung verwendet einen einfachen HTTP-Server, der verschiedene Endpoints für die Verarbeitung von Benutzeranfragen bereitstellt.
- **Repository-Muster**: Für die Datenpersistenz wird das Repository-Muster verwendet, das eine abstrahierte Datenschicht bietet.
- **Service-Layer**: Für komplexere Geschäftslogik (wie die Berechnung von Statistiken oder das Verwalten von Bewertungen) werden dedizierte Services verwendet.

#### Sicherheit
- **Token-basierte Authentifizierung**: Benutzerauthentifizierung erfolgt über Token, die bei der Anmeldung generiert und bei nachfolgenden Anfragen validiert werden.
- **Berechtigungsprüfung**: Nutzer können nur ihre eigenen Daten ändern, mit Ausnahmen für bestimmte Operationen wie das Genehmigen von Bewertungen durch Medieninhaber.

#### Features
- **Accept**: Bewertungen werden erst öffentlich sichtbar, nachdem der Besitzer des Medieninhalts diese genehmigt hat.
- **Öffentliche Sichtbarkeit**: Die Eigenschaft `publicVisible` kann nur durch den api/ratings/accept Endpoint gesetzt werden, nicht direkt beim Erstellen oder Aktualisieren von Bewertungen.
- **statistiken**: Das System verfolgt automatisch Statistiken wie die Anzahl der abgegebenen Bewertungen und geschriebenen Reviews. Sowie die aktuellen lieblingsmedien type eines Benutzers

### Projektstruktur

#### Backend
- **Endpoints**: Spezialisierte Handler für verschiedene API-Endpoints (Benutzer, Medien, Bewertungen).
- **Repositories**: Datenmanagement für verschiedene Entitäten (Benutzer, Medien, Bewertungen, Profile).
- **Services**: Geschäftslogik für komplexere Operationen und statistische Berechnungen.
- **Modelle**: Datenmodelle für die verschiedenen Entitäten wie User, MediaEntry und Rating.
- **DTOs**: Data Transfer Objects für die API-Kommunikation, um die Datenübertragung zu json zu vereinfachen.

#### Frontend
- **Einfaches HTML/JS-Interface**: Ein einfaches Frontend zum Testen der API-Funktionen.
- **CORS Feature** für Testzwecke aktuell aktiviert - (damit die Seite lokal getestet werden kann)

### Testcases
- **Postman** Testcollections zum schnelle Testen aller features der API

### Besondere Features
- **Statistik-Tracking**: Automatische Aktualisierung von Benutzerstatistiken wie Likes, Number of Reviews oder der aktuelle Lieblingsmedientyp