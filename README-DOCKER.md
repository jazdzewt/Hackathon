# 🐳 Docker Setup - Hackathon Platform

## Quick Start (One Command!)

### 1. Skopiuj konfigurację
```bash
cp .env.example .env
```

### 2. Edytuj `.env` i wstaw swoje klucze Supabase
```env
SUPABASE_URL=https://twoj-projekt.supabase.co
SUPABASE_SERVICE_ROLE_KEY=eyJhbG...
SUPABASE_ANON_KEY=eyJhbG...
```

### 3. Uruchom
```bash
docker-compose up -d
```

API będzie dostępne pod: **http://localhost:5043**

---

## 📋 Komendy Docker

### Budowanie i uruchamianie
```bash
# Build i start w tle
docker-compose up -d --build

# Start bez rebuild
docker-compose up -d

# Start z logami
docker-compose up
```

### Zatrzymywanie
```bash
# Stop containers
docker-compose stop

# Stop i usuń containers
docker-compose down

# Stop, usuń containers i volumes
docker-compose down -v
```

### Logi
```bash
# Wszystkie logi
docker-compose logs

# Follow logs (live)
docker-compose logs -f

# Ostatnie 100 linii
docker-compose logs --tail=100

# Tylko dla API
docker-compose logs -f hackathon-api
```

### Restart
```bash
# Restart wszystkiego
docker-compose restart

# Restart tylko API
docker-compose restart hackathon-api
```

### Wejście do kontenera
```bash
docker exec -it hackathon-api bash
```

---

## 🏗️ Build tylko Dockerfile (bez docker-compose)

```bash
cd src/Hackathon.Api

# Build
docker build -t hackathon-api:latest .

# Run
docker run -d \
  -p 5043:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e Supabase__Url=https://twoj-projekt.supabase.co \
  -e Supabase__ServiceRoleKey=twoj-key \
  -e Supabase__AnonKey=twoj-anon-key \
  --name hackathon-api \
  hackathon-api:latest

# Logi
docker logs -f hackathon-api

# Stop
docker stop hackathon-api

# Remove
docker rm hackathon-api
```

---

## 🔧 Konfiguracja

### Zmienne środowiskowe

| Zmienna | Opis | Wymagana |
|---------|------|----------|
| `SUPABASE_URL` | URL projektu Supabase | ✅ |
| `SUPABASE_SERVICE_ROLE_KEY` | Service role key (admin access) | ✅ |
| `SUPABASE_ANON_KEY` | Anon key (public access) | ✅ |
| `ASPNETCORE_ENVIRONMENT` | Development/Production | ❌ (default: Production) |

### Porty

- **5043:8080** - API endpoint (zmień w `docker-compose.yml` jeśli potrzebne)

### Volumes

- `./logs:/app/logs` - Logi są zapisywane lokalnie w folderze `logs/`

---

## 🚀 Production Deployment

### 1. Zmień environment na Production
W `docker-compose.yml`:
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
```

### 2. Używaj secrets zamiast .env
```yaml
services:
  hackathon-api:
    environment:
      - Supabase__Url=${SUPABASE_URL}
      - Supabase__ServiceRoleKey=${SUPABASE_SERVICE_ROLE_KEY}
    secrets:
      - supabase_key

secrets:
  supabase_key:
    file: ./secrets/supabase_key.txt
```

### 3. Dodaj health check
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/api/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

### 4. Resource limits
```yaml
deploy:
  resources:
    limits:
      cpus: '2'
      memory: 2G
    reservations:
      cpus: '1'
      memory: 1G
```

---

## 🐛 Troubleshooting

### Problem: Container nie startuje
```bash
# Sprawdź logi
docker-compose logs hackathon-api

# Sprawdź status
docker-compose ps
```

### Problem: Port 5043 zajęty
Zmień w `docker-compose.yml`:
```yaml
ports:
  - "5044:8080"  # używaj 5044 zamiast 5043
```

### Problem: Brak połączenia z Supabase
1. Sprawdź czy `.env` ma poprawne klucze
2. Sprawdź czy Supabase jest dostępny: `curl https://twoj-projekt.supabase.co`
3. Zweryfikuj Service Role Key w Supabase Dashboard

### Problem: Nie widać logów
```bash
# Logi są w folderze ./logs
ls -la logs/

# Lub wewnątrz kontenera
docker exec -it hackathon-api cat /app/logs/hackathon-*.txt
```

---

## 📊 Monitoring

### Health Check
```bash
curl http://localhost:5043/api/health
```

### Sprawdź czy działa
```bash
curl http://localhost:5043/api/challenges
```

---

## 🔄 CI/CD Integration

### GitHub Actions
```yaml
- name: Build Docker image
  run: docker build -t hackathon-api:${{ github.sha }} ./src/Hackathon.Api

- name: Run tests in container
  run: |
    docker run --rm hackathon-api:${{ github.sha }} \
      dotnet test --no-build
```

### Docker Hub Push
```bash
docker tag hackathon-api:latest your-dockerhub/hackathon-api:latest
docker push your-dockerhub/hackathon-api:latest
```

---

## 📦 Multi-stage Build Benefits

Dockerfile używa multi-stage build:
1. **Build stage** - kompilacja z SDK (duży ~1GB)
2. **Publish stage** - publikacja optimized build
3. **Runtime stage** - tylko runtime (~200MB)

**Rezultat:** Mały final image (~250MB zamiast ~1GB)

---

## ✅ Verification Checklist

Po uruchomieniu sprawdź:
- [ ] API odpowiada: `curl http://localhost:5043/api/health`
- [ ] Swagger działa: `http://localhost:5043/swagger`
- [ ] Logi są zapisywane: `ls logs/`
- [ ] Supabase connection: sprawdź logi czy nie ma błędów

---

**Gotowe!** 🎉 Teraz masz w pełni działający Docker setup dla Hackathon Platform.
