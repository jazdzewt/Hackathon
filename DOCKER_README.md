# 🐳 Docker - Instrukcja uruchomienia

## Wymagania
- Docker Desktop zainstalowany i uruchomiony
- Konto Supabase z projektem

## 🚀 Pierwsze uruchomienie

### 1. Skonfiguruj zmienne środowiskowe

Stwórz plik `.env` w głównym katalogu projektu (obok `docker-compose.yml`):

```env
SUPABASE_URL=https://twoj-projekt.supabase.co
SUPABASE_SERVICE_ROLE_KEY=twoj-service-role-key
SUPABASE_ANON_KEY=twoj-anon-key
```

**⚠️ WAŻNE:** Plik `.env` jest w `.gitignore` - nie commituj go!

### 2. Zbuduj i uruchom kontenery

```powershell
docker-compose up -d --build
```

⏱️ **Pierwsze budowanie trwa 15-20 minut** (pobieranie obrazów Flutter i .NET)

### 3. Sprawdź czy działa

```powershell
# Zobacz działające kontenery
docker ps

# Powinieneś zobaczyć:
# - hackathon-api (port 5043)
# - hackathon-frontend (port 8080)
```

### 4. Otwórz aplikację

- **Frontend:** http://localhost:8080
- **Backend API:** http://localhost:5043/api/health/supabase
- **Swagger:** http://localhost:5043/swagger

## 🔄 Kolejne uruchomienia

Po pierwszym zbudowaniu wystarczy:

```powershell
docker-compose up -d
```

✨ **Uruchamia się w 5-10 sekund!**

## 🛑 Zatrzymanie

```powershell
# Zatrzymaj kontenery (dane zostają)
docker-compose down

# Zatrzymaj i usuń wszystko (włącznie z wolumenami)
docker-compose down -v
```

## 🔧 Przydatne komendy

```powershell
# Zobacz logi wszystkich serwisów
docker-compose logs -f

# Zobacz logi tylko backendu
docker-compose logs -f hackathon-api

# Zobacz logi tylko frontendu
docker-compose logs -f hackathon-frontend

# Restartuj konkretny serwis
docker-compose restart hackathon-api

# Przebuduj bez cache (gdy są problemy)
docker-compose build --no-cache

# Wejdź do kontenera (debug)
docker exec -it hackathon-api sh
docker exec -it hackathon-frontend sh
```

## 🔄 Aktualizacja po zmianach w kodzie

Gdy zmienisz kod backendu lub frontendu:

```powershell
docker-compose up -d --build
```

Przebuduje tylko zmienione serwisy (reszta z cache).

## ❌ Rozwiązywanie problemów

### Kontener się nie uruchamia

```powershell
# Zobacz szczegółowe logi
docker-compose logs hackathon-api
docker-compose logs hackathon-frontend

# Uruchom bez -d żeby zobaczyć logi na żywo
docker-compose up
```

### Port już zajęty

```
Error: bind: address already in use
```

Zmień porty w `docker-compose.yml`:

```yaml
ports:
  - "5044:8080"  # zamiast 5043:8080
```

### Supabase nie działa

Sprawdź:
1. Czy plik `.env` istnieje
2. Czy credentials są prawidłowe
3. Czy projekt Supabase jest aktywny

```powershell
# Sprawdź zmienne środowiskowe w kontenerze
docker exec hackathon-api printenv | findstr SUPABASE
```

### Zresetuj wszystko

```powershell
# Usuń wszystkie kontenery i obrazy
docker-compose down -v
docker system prune -a

# Zbuduj od nowa
docker-compose up -d --build
```

## 📦 Architektura

```
┌─────────────────┐      ┌─────────────────┐
│   Frontend      │      │    Backend      │
│   (Flutter)     │─────▶│   (.NET 9.0)    │
│   Port: 8080    │      │   Port: 5043    │
│   nginx:alpine  │      │   aspnet:9.0    │
└─────────────────┘      └─────────────────┘
                                  │
                                  ▼
                         ┌─────────────────┐
                         │   Supabase      │
                         │   (PostgreSQL)  │
                         │   (Storage)     │
                         │   (Auth)        │
                         └─────────────────┘
```

## 🌐 Sieć Docker

Oba kontenery są w sieci `hackathon-network`, więc mogą się komunikować:

- Frontend → Backend: `http://hackathon-api:8080`
- Z hosta → Frontend: `http://localhost:8080`
- Z hosta → Backend: `http://localhost:5043`

## 📝 Struktura plików

```
Hackathon/
├── docker-compose.yml          # Główna konfiguracja
├── .env                        # Credentials (nie commitować!)
├── .gitignore                  # .env jest tutaj
└── src/
    ├── Hackathon.Api/
    │   └── Dockerfile          # Backend image
    └── HackathonUI/
        └── hackathon_flutter/
            └── Dockerfile      # Frontend image
```
