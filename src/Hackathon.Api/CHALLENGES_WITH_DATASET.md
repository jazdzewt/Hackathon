# 🎯 Tworzenie Challenge z Datasetem

## Przygotowanie Supabase Storage

### 1. Utwórz bucket `datasets` w Supabase Dashboard

1. Zaloguj się do Supabase Dashboard: https://app.supabase.com
2. Przejdź do **Storage** → **New bucket**
3. Nazwa: `datasets`
4. Typ: **Public** (aby użytkownicy mogli pobierać datasety)
5. Kliknij **Create bucket**

---

## Dostępne Endpointy

### 1. **POST `/api/admin/challenges`** - Bez datasetu
Tworzy challenge bez pliku datasetu (tylko metadane)

**Body (JSON):**
```json
{
  "name": "Image Classification Challenge",
  "shortDescription": "Classify images",
  "fullDescription": "Comprehensive challenge for image classification",
  "rules": "Use any ML model",
  "evaluationMetric": "accuracy",
  "startDate": "2025-11-15T00:00:00Z",
  "endDate": "2025-12-31T23:59:59Z",
  "maxFileSizeMb": 100,
  "allowedFileTypes": ["csv", "json", "zip"]
}
```

**Odpowiedź:**
```json
{
  "message": "Challenge created successfully",
  "id": "abc-123-def"
}
```

---

### 2. **POST `/api/admin/challenges/with-dataset`** ⭐ NOWY!
Tworzy challenge z plikiem datasetu

**Content-Type:** `multipart/form-data`

**Form Data:**
- `name` (string, required) - Nazwa challenge
- `shortDescription` (string, required) - Krótki opis
- `fullDescription` (string, required) - Pełny opis
- `rules` (string, required) - Zasady
- `evaluationMetric` (string, required) - Metryka oceny (np. "accuracy")
- `startDate` (DateTime, required) - Data rozpoczęcia
- `endDate` (DateTime, optional) - Data zakończenia
- `maxFileSizeMb` (int, optional) - Max rozmiar pliku submission (domyślnie: 100)
- `allowedFileTypes` (string, optional) - Dozwolone typy plików, oddzielone przecinkami (np. "csv,json,txt")
- `datasetFile` (file, optional) - Plik z datasetem

**Przykład w Postman:**

1. Ustaw metodę: **POST**
2. URL: `http://localhost:5043/api/admin/challenges/with-dataset`
3. Headers:
   - `Authorization: Bearer YOUR_JWT_TOKEN` (musisz być zalogowany jako admin)
4. Body → **form-data**:
   ```
   name: "Image Classification Challenge"
   shortDescription: "Classify images into 10 categories"
   fullDescription: "This is a comprehensive challenge..."
   rules: "Use any ML framework"
   evaluationMetric: "accuracy"
   startDate: "2025-11-15T00:00:00Z"
   endDate: "2025-12-31T23:59:59Z"
   maxFileSizeMb: 100
   allowedFileTypes: "csv,json,zip"
   datasetFile: [wybierz plik, np. dataset.zip]
   ```

**Odpowiedź:**
```json
{
  "message": "Challenge created successfully with dataset",
  "id": "e742aeff-fece-4931-8551-26023c7fe9c5",
  "datasetUrl": "https://xyz.supabase.co/storage/v1/object/public/datasets/challenges/e742aeff-fece-4931-8551-26023c7fe9c5.zip"
}
```

---

## Flow tworzenia Challenge z Datasetem

```
1. Admin loguje się (POST /api/auth/login)
   ↓
2. Admin tworzy challenge z datasetem (POST /api/admin/challenges/with-dataset)
   ↓
3. Backend:
   - Tworzy rekord w tabeli `challenges`
   - Upload pliku do Supabase Storage (bucket: `datasets`)
   - Zapisuje publiczny URL w kolumnie `dataset_url`
   ↓
4. Użytkownicy mogą:
   - Zobaczyć challenge (GET /api/challenges/{id})
   - Pobrać dataset bezpośrednio z `dataset_url`
```

---

## Struktura pliku w Storage

Dataset zostanie zapisany jako:
```
datasets/
  challenges/
    {challenge-id}.{extension}
```

Przykład:
```
datasets/challenges/e742aeff-fece-4931-8551-26023c7fe9c5.zip
```

---

## Pobieranie Datasetu przez użytkowników

### Opcja 1: Bezpośredni link (Public URL)
```
GET https://xyz.supabase.co/storage/v1/object/public/datasets/challenges/abc-123.zip
```

### Opcja 2: Przez API Storage
```
GET /api/storage/download/datasets/challenges/abc-123.zip
```

---

## Testowanie

### Przykładowy dataset do testów
Możesz użyć dowolnego pliku:
- CSV z danymi
- ZIP z obrazami
- JSON z danymi tekstowymi

### Weryfikacja
Po utworzeniu challenge, sprawdź w bazie:
```sql
SELECT id, title, dataset_url FROM challenges WHERE id = 'abc-123';
```

Powinieneś zobaczyć wypełnione pole `dataset_url` z publicznym linkiem.

---

## Bezpieczeństwo

✅ **Wymagana autoryzacja:** Endpoint wymaga roli `admin`  
✅ **Walidacja pliku:** Backend sprawdza czy plik istnieje  
✅ **Unikalne nazwy:** Pliki zapisywane są z ID challenge (brak duplikatów)  
✅ **Public bucket:** Dataset jest dostępny publicznie (zgodnie z wymaganiami)

---

## Troubleshooting

**Problem:** "Bucket 'datasets' not found"
- **Rozwiązanie:** Utwórz bucket `datasets` w Supabase Dashboard

**Problem:** "401 Unauthorized"
- **Rozwiązanie:** Zaloguj się jako admin i użyj JWT token w headerze

**Problem:** "File too large"
- **Rozwiązanie:** Sprawdź limity w Supabase (Free tier: 1GB total storage)

---

## Kolejne kroki

Po utworzeniu challenge z datasetem możesz:
1. ✅ Wyświetlić challenge (GET /api/challenges/{id})
2. ✅ Pobrać dataset (kliknij link z `dataset_url`)
3. 📝 Dodać submission (POST /api/submissions)
4. 📊 Zobaczyć ranking (GET /api/leaderboard/{challengeId})
