# System Oceniania Hackathonów - Dokumentacja

## 📋 Przegląd

System obsługuje dwa tryby oceniania submisji uczestników:
1. **Automatyczne** - porównanie z plikiem ground-truth
2. **Ręczne** - ocena przez admina/sędziego

---

## 🏗️ Architektura

### Modele Danych

#### `ChallengeAsset` (Nowy)
Przechowuje zasoby wyzwania, w tym ukryte pliki ground-truth:
```csharp
- Id: string (GUID)
- ChallengeId: string
- AssetType: string ("ground_truth", "dataset", "sample_submission")
- FileName: string
- FileUrl: string (URL w Supabase Storage)
- FileSizeMb: decimal?
- IsPublic: bool (false dla ground-truth!)
- UploadedBy: string (ID admina)
- CreatedAt: DateTime
```

#### `Submission` (Rozszerzony)
Dodane pola do śledzenia oceniania:
```csharp
- EvaluationStatus: string ("pending", "auto_evaluated", "manually_evaluated")
- EvaluatorId: string? (ID admina/sędziego dla ręcznej oceny)
- EvaluatorNotes: string? (notatki sędziego)
- EvaluatedAt: DateTime?
```

### Serwisy

#### `IScoringService` / `ScoringService`
Główny serwis odpowiedzialny za ocenianie:

**Metody:**
- `EvaluateSubmissionAsync(string submissionId)` - automatyczna ocena
- `ManuallyScoreSubmissionAsync(string submissionId, decimal score, string? notes, string evaluatorId)` - ręczna ocena
- `CalculateScoreAsync(byte[] submissionFile, byte[] groundTruthFile, string evaluationMetric, string fileExtension)` - obliczenie wyniku

**Wspierane metryki:**
- `accuracy` - dokładność klasyfikacji
- `f1` / `f1-score` - F1-score dla klasyfikacji binarnej
- `mse` / `mean-squared-error` - błąd średniokwadratowy (regresja)
- `mae` / `mean-absolute-error` - błąd średni absolutny (regresja)
- `rmse` / `root-mean-squared-error` - pierwiastek błędu średniokwadratowego

**Wspierane formaty plików:**
- `.csv` - porównanie wartości w ostatniej kolumnie
- `.json` - wymaga formatu `{ "predictions": [...] }`
- `.txt` - porównanie linijka po linijce

#### `SubmissionService` (Zaktualizowany)
Integracja z `ScoringService`:
- Po upload submission automatycznie uruchamia ocenianie w tle (asynchronicznie)
- Oblicza hash pliku (SHA256) dla wykrywania duplikatów
- Waliduje rozmiar i typ pliku zgodnie z regułami challenge

---

## 🔐 Bezpieczeństwo Ground-Truth

### Storage Structure
```
datasets/
├── challenges/              # Datasety publiczne
│   └── {challengeId}.csv
└── ground-truth/            # Pliki ground-truth (PRYWATNE!)
    └── {challengeId}.csv
```

### Polityka dostępu:
- ✅ **Admin** - pełny dostęp (upload, download, delete)
- ✅ **Serwis oceniania** - read-only dostęp przez Service Role Key
- ❌ **Uczestnicy** - BRAK DOSTĘPU (RLS policy)

### Row Level Security (RLS)
W tabeli `challenge_assets`:
```sql
-- Uczestnicy NIE widzą ground-truth
CREATE POLICY "Participants cannot see ground truth"
ON challenge_assets
FOR SELECT
USING (
  is_public = true 
  OR auth.uid() IN (
    SELECT user_id FROM profiles WHERE role = 'admin'
  )
);
```

---

## 🚀 Endpointy API

### 1. Submit Solution (Uczestnik)
```http
POST /api/submissions/challenges/{challengeId}/submit
Authorization: Bearer {JWT_TOKEN}
Content-Type: multipart/form-data

Body:
- file: IFormFile
```

**Flow:**
1. Walidacja challenge (czy aktywny, czy nie minął deadline)
2. Walidacja pliku (rozmiar, typ)
3. Obliczenie SHA256 hash (wykrywanie duplikatów)
4. Upload do Storage: `datasets/submissions/{userId}/{challengeId}/{submissionId}.csv`
5. Zapis do bazy z statusem `pending`
6. **Asynchroniczne** uruchomienie oceniania w tle

**Response:**
```json
{
  "message": "Submission accepted and will be evaluated shortly",
  "submissionId": "uuid"
}
```

### 2. Upload Ground-Truth (Admin Only)
```http
POST /api/admin/challenges/{id}/ground-truth
Authorization: Bearer {ADMIN_JWT_TOKEN}
Content-Type: multipart/form-data

Body:
- file: IFormFile
```

**Flow:**
1. Upload pliku do `datasets/ground-truth/{challengeId}.csv`
2. Utworzenie rekordu w `challenge_assets`:
   - `asset_type = "ground_truth"`
   - `is_public = false` ⚠️
3. Aktualizacja `challenges.ground_truth_url`

**Response:**
```json
{
  "message": "Ground truth file uploaded successfully (hidden from participants)",
  "assetId": "uuid"
}
```

### 3. Manual Score (Admin/Judge Only)
```http
POST /api/admin/submissions/{submissionId}/score
Authorization: Bearer {ADMIN_JWT_TOKEN}
Content-Type: application/json

Body:
{
  "score": 95.5,
  "notes": "Bardzo kreatywne podejście, ale mały błąd w edge case."
}
```

**Flow:**
1. Aktualizacja `submissions`:
   - `score = 95.5`
   - `evaluation_status = "manually_evaluated"`
   - `evaluator_id = {admin_user_id}`
   - `evaluator_notes = "..."`
   - `evaluated_at = DateTime.UtcNow`

**Response:**
```json
{
  "message": "Submission scored successfully",
  "score": 95.5,
  "evaluatedBy": "admin_user_id"
}
```

### 4. Re-evaluate Submission (Admin Only)
```http
POST /api/admin/submissions/{submissionId}/reevaluate
Authorization: Bearer {ADMIN_JWT_TOKEN}
```

Wymusza ponowne automatyczne przeliczenie wyniku (np. po zmianie ground-truth).

---

## 🔄 Proces Automatycznego Oceniania

### 1. Trigger
```csharp
// W SubmissionService.SubmitSolutionAsync()
_ = Task.Run(async () =>
{
    await EvaluateSubmissionAsync(submissionId);
});
```

### 2. Evaluation Flow
```
┌─────────────────────────────────────┐
│ 1. Pobierz submission z bazy        │
│    Status = "processing"            │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│ 2. Pobierz challenge + ground-truth │
│    asset (WHERE asset_type =        │
│    "ground_truth")                  │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│ 3. Download plików z Storage:       │
│    - submission.file_url            │
│    - ground_truth.file_url          │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│ 4. Oblicz SHA256 hash submission    │
│    (deterministyczność)             │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│ 5. CalculateScoreAsync()            │
│    - Parsuj pliki (CSV/JSON/TXT)    │
│    - Zastosuj metrykę ewaluacji     │
│    - Zwróć score (0-100)            │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│ 6. Zapisz wynik do bazy:            │
│    - score = {calculated}           │
│    - status = "completed"           │
│    - evaluation_status =            │
│      "auto_evaluated"               │
│    - evaluated_at = Now             │
└─────────────────────────────────────┘
```

### 3. Error Handling
Jeśli ocenianie się nie powiedzie:
```csharp
submission.Status = "failed";
submission.ErrorMessage = ex.Message;
```

---

## 📊 Przykłady Użycia

### Przykład 1: Accuracy dla Klasyfikacji (CSV)

**Ground-Truth (`ground-truth/challenge-123.csv`):**
```csv
id,prediction
1,cat
2,dog
3,cat
4,bird
```

**User Submission:**
```csv
id,prediction
1,cat
2,cat
3,cat
4,bird
```

**Wynik:** 75% accuracy (3/4 poprawne)

---

### Przykład 2: MSE dla Regresji (JSON)

**Ground-Truth:**
```json
{
  "predictions": [1.0, 2.5, 3.2, 4.8]
}
```

**User Submission:**
```json
{
  "predictions": [1.1, 2.4, 3.3, 4.7]
}
```

**Obliczenie:**
```
MSE = [(1.1-1.0)² + (2.4-2.5)² + (3.3-3.2)² + (4.7-4.8)²] / 4
    = [0.01 + 0.01 + 0.01 + 0.01] / 4 = 0.01
Score = 100 * exp(-0.01) ≈ 99.00
```

---

## 🔧 Konfiguracja

### Program.cs
```csharp
builder.Services.AddScoped<IScoringService, ScoringService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
```

### appsettings.json
```json
{
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "ServiceRoleKey": "eyJhbG...", // ⚠️ Wymagane dla dostępu do ground-truth
    "AnonKey": "eyJhbG..."
  }
}
```

---

## ✅ Checklist Implementacji

### Backend (API)
- [x] Model `ChallengeAsset` utworzony
- [x] Model `Submission` rozszerzony (evaluation_status, evaluator_id, evaluator_notes, evaluated_at)
- [x] `IScoringService` + implementacja
- [x] `SubmissionService` zintegrowany z `ScoringService`
- [x] Endpoint: `POST /api/submissions/challenges/{id}/submit`
- [x] Endpoint: `POST /api/admin/challenges/{id}/ground-truth`
- [x] Endpoint: `POST /api/admin/submissions/{id}/score`
- [x] Endpoint: `POST /api/admin/submissions/{id}/reevaluate`
- [x] Asynchroniczne ocenianie w tle
- [x] Obliczanie SHA256 hash dla submisji
- [x] Wsparcie dla CSV, JSON, TXT
- [x] Metryki: accuracy, F1, MSE, MAE, RMSE

### Database (Supabase)
- [ ] Tabela `challenge_assets` utworzona
- [ ] Kolumny w `submissions` dodane:
  - [ ] `evaluation_status`
  - [ ] `evaluator_id`
  - [ ] `evaluator_notes`
  - [ ] `evaluated_at`
- [ ] RLS policy dla `challenge_assets` (is_public = false dla ground-truth)
- [ ] Bucket `datasets` z folderem `ground-truth/`
- [ ] Storage policy: tylko admin i service role mają dostęp do `ground-truth/`

### Frontend (TODO)
- [ ] Interfejs uploadu ground-truth dla admina
- [ ] Panel ręcznego oceniania submisji
- [ ] Podgląd statusu oceniania (pending/processing/completed)
- [ ] Wyświetlanie notatek sędziego dla uczestników

---

## 🎯 Przykładowe Scenariusze

### Scenariusz 1: Challenge z Automatycznym Ocenianiem
1. Admin tworzy challenge z metryką `accuracy`
2. Admin uploaduje `ground-truth.csv` przez endpoint
3. Uczestnik uploaduje swoją submisję
4. System automatycznie (w tle) ocenia i zapisuje wynik
5. Wynik pojawia się na leaderboardzie

### Scenariusz 2: Challenge z Ręcznym Ocenianiem
1. Admin tworzy challenge
2. Uczestnik uploaduje submisję (np. prezentację wideo)
3. Submisja ma status `pending`
4. Sędzia loguje się i ocenia przez panel admina
5. Wpisuje score (0-100) i notatkę
6. Wynik pojawia się na leaderboardzie

### Scenariusz 3: Hybrydowe Ocenianie
1. System automatycznie ocenia wszystkie submisje
2. Admin przegląda top 10 submisji
3. Admin ręcznie koryguje wyniki podejrzanych submisji
4. `evaluation_status` zmienia się na `manually_evaluated`

---

## 📝 Notatki Techniczne

### Dlaczego SHA256 hash?
- Wykrywanie duplikatów submisji
- Deterministyczna identyfikacja pliku
- Zabezpieczenie przed wielokrotnym uploadem tego samego rozwiązania

### Dlaczego asynchroniczne ocenianie?
- Nie blokuje response do użytkownika
- Można oceniać ciężkie submisje (duże pliki, skomplikowane metryki)
- Lepsze UX - użytkownik dostaje natychmiastowe potwierdzenie

### Dlaczego score 0-100?
- Standaryzacja - łatwe porównanie różnych metryk
- Przyjazne dla użytkowników (procenty)
- Dla MSE/MAE używamy `score = 100 * exp(-error)` aby przekształcić na skalę 0-100

---

## 🚨 Potencjalne Problemy i Rozwiązania

### Problem: Ground-truth jest zbyt duży
**Rozwiązanie:** Kompresja (zip) lub sampling (wzorce losowe)

### Problem: Ocenianie trwa zbyt długo
**Rozwiązanie:** 
- Queue system (RabbitMQ/Redis)
- Timeout dla oceniania
- Partial scoring dla dużych datasetów

### Problem: Użytkownik uploaduje złośliwy plik
**Rozwiązanie:**
- Skanowanie antywirusowe przed oceną
- Sandbox dla execution (jeśli potrzebne)
- Limit rozmiaru pliku

### Problem: Cheat detection
**Rozwiązanie:**
- Analiza podobieństwa submisji (plagiat detection)
- Rate limiting na submissions
- Manual review dla podejrzanych wyników

---

## 📚 Dalszy Rozwój

### Faza 2 (Przyszłość)
- [ ] Wsparcie dla własnych metryk (custom scoring functions)
- [ ] Partial scoring (wyniki per test case)
- [ ] Leaderboard freezing (zamrożenie przed końcem)
- [ ] Public/Private test splits
- [ ] Submission history i porównanie wyników
- [ ] Automated cheating detection
- [ ] Real-time leaderboard updates (WebSockets)

---

## 👨‍💻 Autorzy
System oceniania zaimplementowany zgodnie z wymaganiami platformy hackathonowej.

Data implementacji: 15.11.2025
