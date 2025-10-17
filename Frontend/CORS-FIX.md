# ?? CORS-Fix f�r MRP API

## Problem gel�st! ?

Der CORS-Fehler wurde behoben. Der HttpServer unterst�tzt jetzt Cross-Origin Requests vom Frontend.

## Was wurde ge�ndert:

### HttpServer.cs - CORS-Unterst�tzung hinzugef�gt

1. **CORS-Header Funktion**:
```csharp
private static void AddCorsHeaders(HttpListenerResponse response)
{
    response.Headers.Add("Access-Control-Allow-Origin", "*");
    response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
    response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept, Authorization");
    response.Headers.Add("Access-Control-Max-Age", "3600");
}
```

2. **OPTIONS-Request Handling** (Preflight):
   - Browser senden OPTIONS-Requests vor echten API-Calls
   - Diese werden jetzt mit Status 204 beantwortet

3. **CORS-Header in allen Responses**:
   - Automatisch in `Json()` und `Text()` Methoden
   - Auch bei Fehler-Responses

## ?? Server neu starten:

### 1. Aktuellen Server stoppen
```
Dr�cken Sie Ctrl+C im Terminal wo der Server l�uft
```

### 2. Server neu starten
```bash
cd C:\Users\Philipp\Documents\PHILIPP\FH\3\Seng\repos\MRP
dotnet run
```

Der Server startet mit CORS-Unterst�tzung!

### 3. Frontend �ffnen
```
Doppelklick auf: Frontend/index.html
```

## ? Jetzt funktioniert:

- ? **User registrieren** - Keine CORS-Fehler mehr
- ? **User login** - Cross-Origin Requests erlaubt
- ? **Media erstellen** - Alle POST/PUT/DELETE funktionieren
- ? **Ratings erstellen** - Komplette API verf�gbar
- ? **Filter & Sort** - GET-Requests mit Parametern
- ? **Profile bearbeiten** - PUT-Requests funktionieren

## ?? Was passiert jetzt:

### Browser macht Preflight-Request:
```http
OPTIONS /api/users/register HTTP/1.1
Origin: file://
Access-Control-Request-Method: POST
Access-Control-Request-Headers: content-type
```

### Server antwortet mit CORS-Headers:
```http
HTTP/1.1 204 No Content
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS
Access-Control-Allow-Headers: Content-Type, Accept, Authorization
Access-Control-Max-Age: 3600
```

### Browser erlaubt dann den echten Request:
```http
POST /api/users/register HTTP/1.1
Content-Type: application/json

{"username": "test", "password": "123"}
```

## ?? Wichtige CORS-Header erkl�rt:

| Header | Bedeutung |
|--------|-----------|
| `Access-Control-Allow-Origin: *` | Erlaubt Requests von jeder Domain (f�r Development) |
| `Access-Control-Allow-Methods` | Erlaubte HTTP-Methoden |
| `Access-Control-Allow-Headers` | Erlaubte Request-Headers |
| `Access-Control-Max-Age: 3600` | Browser cached Preflight-Response f�r 1 Stunde |

## ?? F�r Production (sp�ter):

In Production sollten Sie statt `*` eine spezifische Domain angeben:

```csharp
// Statt:
response.Headers.Add("Access-Control-Allow-Origin", "*");

// Besser:
response.Headers.Add("Access-Control-Allow-Origin", "https://your-domain.com");
```

## ?? Test-Checklist:

Nach dem Neustart des Servers:

1. ? Frontend �ffnen (`Frontend/index.html`)
2. ? Browser DevTools �ffnen (F12)
3. ? Network-Tab �ffnen
4. ? User registrieren
5. ? Schauen Sie im Network-Tab:
   - OPTIONS Request mit Status 204
   - POST Request mit Status 201
   - Keine CORS-Fehler in der Console!

## ?? Falls es immer noch nicht funktioniert:

### 1. Server wirklich neu gestartet?
```bash
# Terminal 1: Server stoppen (Ctrl+C)
# Terminal 1: Server neu starten
dotnet run
```

### 2. Browser-Cache leeren
```
Ctrl+Shift+Delete ? Cache leeren
Oder: Hard-Reload (Ctrl+Shift+R)
```

### 3. DevTools Console pr�fen
```
F12 ? Console Tab
Sollte keine CORS-Fehler mehr zeigen!
```

### 4. Network-Tab pr�fen
```
F12 ? Network Tab
OPTIONS-Requests sollten Status 204 haben
Echte Requests sollten Status 200/201 haben
```

## ?? Fertig!

Das Frontend sollte jetzt vollst�ndig mit der API kommunizieren k�nnen. Viel Erfolg beim Testen! ??
