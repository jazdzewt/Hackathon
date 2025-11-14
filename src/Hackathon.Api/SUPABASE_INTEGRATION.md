# 🔥 JAK DZIAŁA INTEGRACJA SUPABASE W HACKATHON API

## 📚 SPIS TREŚCI
1. [Po co jest Storage?](#po-co-jest-storage)
2. [Jak połączyć User z Storage?](#jak-połączyć-user-z-storage)
3. [Pełny przepływ zgłoszenia](#pełny-przepływ-zgłoszenia)
4. [Konfiguracja Supabase](#konfiguracja-supabase)
5. [Testowanie API](#testowanie-api)

---

## 🎯 PO CO JEST STORAGE?

Storage w Supabase = **miejsce do przechowywania PLIKÓW** (jak Google Drive, Dropbox, AWS S3)

### W hackathonie używamy go do:

1. **📤 Uczestnicy uploadują swoje rozwiązania**
   - Plik CSV/JSON z predykcjami modelu ML
   - Zapisywany w buckecie `submissions`

2. **📊 Admini uploadują datasety**
   - Dane treningowe dla uczestników
   - Zapisywane w buckecie `datasets`

3. **✅ Admini uploadują ground truth**
   - Prawidłowe odpowiedzi do porównania
   - Zapisywane w buckecie `evaluation` (prywatny)

---

## 🔗 JAK POŁĄCZYĆ USER Z STORAGE?

### PRZEPŁYW:
```
1. User loguje się → Dostaje JWT token
2. User uploaduje plik → Storage zapisuje plik
3. Storage zwraca URL do pliku
4. Zapisujemy URL + metadata do tabeli "submissions"
5. Tabela "submissions" ma kolumnę user_id → łączy User z jego plikami
```

### SCHEMAT BAZY:
```sql
users (id, email, name, role)
   ↓
submissions (id, user_id, file_url, challenge_id, score)
   ↓
leaderboard (id, user_id, challenge_id, best_score)
```

### KOD (SubmissionsController.cs):
```csharp
// 1. Sprawdź kto jest zalogowany
var userId = _supabase.Auth.CurrentSession?.User?.Id;

// 2. Upload pliku do Storage
var uploadPath = $"{userId}/{challengeId}/{fileName}";
await _supabase.Storage.From("submissions").Upload(fileBytes, uploadPath);

// 3. Pobierz URL do pliku
var fileUrl = _supabase.Storage.From("submissions").GetPublicUrl(uploadPath);

// 4. Zapisz do bazy z powiązaniem do user_id
var submission = new Submission {
    UserId = userId,           // ← TO ŁĄCZY USER Z PLIKIEM!
    ChallengeId = challengeId,
    FileUrl = fileUrl,         // ← URL do pliku w Storage
    Score = null               // obliczone później
};
await _supabase.From<Submission>().Insert(submission);
```

---

## 🚀 PEŁNY PRZEPŁYW ZGŁOSZENIA

### KROK PO KROKU (endpoint `POST /api/submissions`):

```
📍 ENDPOINT: POST /api/submissions
📦 Body: multipart/form-data
   - file: [plik CSV/JSON]
   - challengeId: "uuid-wyzwania"
```

### CO SIĘ DZIEJE W KODZIE:

#### 1️⃣ **AUTENTYKACJA**
```csharp
var session = _supabase.Auth.CurrentSession;
if (session?.User == null) return Unauthorized();
```

#### 2️⃣ **WALIDACJA PLIKU**
```csharp
// Sprawdź rozmiar
if (fileSizeMb > challenge.MaxFileSizeMb) return BadRequest();

// Oblicz hash (wykryj duplikaty)
var hash = MD5.HashData(fileStream);
```

#### 3️⃣ **UPLOAD DO STORAGE**
```csharp
var uploadPath = $"{userId}/{challengeId}/{fileName}";
var uploadResponse = await _supabase.Storage
    .From("submissions")
    .Upload(fileBytes, uploadPath);
```

#### 4️⃣ **POBIERZ URL**
```csharp
var fileUrl = _supabase.Storage
    .From("submissions")
    .GetPublicUrl(uploadPath);

// fileUrl = "https://xzqghxbqbqzktygymreu.supabase.co/storage/v1/object/public/submissions/user123/challenge456/file.csv"
```

#### 5️⃣ **ZAPISZ DO BAZY**
```csharp
var submission = new Submission {
    UserId = userId,
    ChallengeId = challengeId,
    FileName = file.FileName,
    FileUrl = fileUrl,        // ← LINK DO STORAGE
    FileHash = hash,
    Status = "pending",
    Score = null
};

await _supabase.From<Submission>().Insert(submission);
```

#### 6️⃣ **ZWRÓĆ ODPOWIEDŹ**
```json
{
  "message": "✅ Zgłoszenie zostało przyjęte!",
  "submission": {
    "id": "uuid",
    "userId": "user123",
    "fileName": "predictions.csv",
    "status": "pending"
  }
}
```

---

## ⚙️ KONFIGURACJA SUPABASE

### 1. UTWÓRZ BUCKETY W SUPABASE DASHBOARD

Idź do: **Supabase Dashboard → Storage → New Bucket**

Stwórz 3 buckety:

| Nazwa | Publiczny? | Opis |
|-------|-----------|------|
| `submissions` | ✅ Tak | Pliki uczestników (CSV/JSON) |
| `datasets` | ✅ Tak | Dane treningowe dla uczestników |
| `evaluation` | ❌ Nie | Ground truth (tylko admini) |

### 2. POLITYKI RLS (Row Level Security)

W Supabase włącz RLS na tabelach i dodaj polityki:

```sql
-- Użytkownicy widzą tylko aktywne wyzwania
CREATE POLICY "users_view_active_challenges" ON challenges
FOR SELECT USING (is_active = true);

-- Każdy widzi tylko swoje zgłoszenia
CREATE POLICY "users_view_own_submissions" ON submissions
FOR SELECT USING (auth.uid() = user_id);

-- Każdy może dodać zgłoszenie
CREATE POLICY "users_insert_submissions" ON submissions
FOR INSERT WITH CHECK (auth.uid() = user_id);

-- Admini widzą wszystko
CREATE POLICY "admins_full_access" ON challenges
FOR ALL USING (
    auth.uid() IN (SELECT id FROM users WHERE role = 'admin')
);
```

### 3. POLITYKI STORAGE

Dla bucketu `submissions`:

```sql
-- Każdy może uploadować do swojego folderu
CREATE POLICY "Users upload to own folder"
ON storage.objects FOR INSERT
WITH CHECK (
    bucket_id = 'submissions' AND
    (storage.foldername(name))[1] = auth.uid()::text
);

-- Każdy może czytać swoje pliki
CREATE POLICY "Users read own files"
ON storage.objects FOR SELECT
USING (
    bucket_id = 'submissions' AND
    (storage.foldername(name))[1] = auth.uid()::text
);

-- Admini widzą wszystko
CREATE POLICY "Admins full access"
ON storage.objects FOR ALL
USING (
    bucket_id = 'submissions' AND
    auth.uid() IN (SELECT id FROM users WHERE role = 'admin')
);
```

---

## 🧪 TESTOWANIE API

### 1. ZAREJESTRUJ SIĘ

```bash
POST http://localhost:5000/api/supabase/auth/signup
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Test123!"
}
```

### 2. ZALOGUJ SIĘ

```bash
POST http://localhost:5000/api/supabase/auth/signin
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Test123!"
}

# Skopiuj accessToken z odpowiedzi!
```

### 3. POBIERZ WYZWANIA

```bash
GET http://localhost:5000/api/challenges
Authorization: Bearer <accessToken>
```

### 4. PRZEŚLIJ ROZWIĄZANIE (POSTMAN!)

```bash
POST http://localhost:5000/api/submissions
Authorization: Bearer <accessToken>
Content-Type: multipart/form-data

Body (form-data):
- file: [wybierz plik predictions.csv]
- challengeId: "uuid-wyzwania"
```

**⚠️ WAŻNE:** Swagger nie obsługuje file upload! Użyj **Postman** lub **curl**

### 5. SPRAWDŹ SWOJE ZGŁOSZENIA

```bash
GET http://localhost:5000/api/submissions/my
Authorization: Bearer <accessToken>
```

### 6. SPRAWDŹ LEADERBOARD

```bash
GET http://localhost:5000/api/leaderboard/{challengeId}
Authorization: Bearer <accessToken>
```

---

## 📋 PODSUMOWANIE

### ✅ CO MASZ ZROBIONE:
- ✅ Modele: User, Challenge, Submission, Leaderboard
- ✅ Controllers: Challenges, Submissions, Leaderboard
- ✅ Autentykacja: SignUp, SignIn, Logout
- ✅ Storage: Upload plików do Supabase Storage
- ✅ Baza: Pełna integracja z tabelami

### 🔧 CO MUSISZ TERAZ ZROBIĆ:
1. **Stwórz buckety w Supabase** (`submissions`, `datasets`, `evaluation`)
2. **Włącz RLS** na tabelach i dodaj polityki
3. **Przetestuj przepływ w Postman**:
   - Zarejestruj → Zaloguj → Pobierz wyzwania → Prześlij plik
4. **Dodaj system oceniania** (porównaj plik uczestnika z ground truth)
5. **Zaktualizuj leaderboard** po obliczeniu score

---

## 🎓 ZROZUMIENIE:

**STORAGE** = miejsce na pliki  
**BAZA** = informacje o plikach (kto, kiedy, jaki wynik)  
**user_id w submissions** = to co łączy User z jego plikami!

Plik fizycznie leży w Storage, ale informacja "kto go przesłał" jest w bazie.
