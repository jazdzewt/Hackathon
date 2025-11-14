# 🎯 GOTOWE! TWÓJ BACKEND JEST SKONFIGUROWANY

## ✅ CO ZOSTAŁO ZROBIONE:

### 1. MODELE (Models/)
- ✅ `User.cs` - użytkownicy z rolami (participant/admin/judge)
- ✅ `Challenge.cs` - wyzwania hackathonowe
- ✅ `Submission.cs` - zgłoszenia uczestników
- ✅ `Leaderboard.cs` - tablica wyników

### 2. CONTROLLERS (Controllers/)
- ✅ `ChallengesController.cs` - zarządzanie wyzwaniami
  - GET /api/challenges - lista wyzwań
  - GET /api/challenges/{id} - szczegóły wyzwania
  - POST /api/challenges - tworzenie (admin)
  - PUT /api/challenges/{id} - edycja (admin)

- ✅ `SubmissionsController.cs` - **TO NAJWAŻNIEJSZE!**
  - POST /api/submissions - upload pliku + zapis do bazy
  - GET /api/submissions/my - moje zgłoszenia
  - GET /api/submissions/challenge/{id} - zgłoszenia dla wyzwania

- ✅ `LeaderboardController.cs` - ranking
  - GET /api/leaderboard/{challengeId} - tablica wyników

### 3. DTOs (DTOs/)
- ✅ Request/Response modele dla API

### 4. DOKUMENTACJA
- ✅ `SUPABASE_INTEGRATION.md` - **PRZECZYTAJ TO!**
  - Wyjaśnia jak działa Storage
  - Pokazuje pełny przepływ zgłoszenia
  - Instrukcje konfiguracji Supabase

---

## 🔥 JAK TO DZIAŁA - PRZYKŁAD:

### SCENARIUSZ: Uczestnik przesyła rozwiązanie

```
1. Uczestnik loguje się → dostaje JWT token
   POST /api/supabase/auth/signin

2. Pobiera listę wyzwań → wybiera jedno
   GET /api/challenges

3. Przesyła plik CSV z predykcjami
   POST /api/submissions
   Body: file=predictions.csv, challengeId=abc-123

4. CO SIĘ DZIEJE W KODZIE:
   a) Sprawdź czy zalogowany ✅
   b) Sprawdź czy wyzwanie istnieje ✅
   c) Sprawdź rozmiar pliku ✅
   d) Oblicz hash (wykryj duplikaty) ✅
   e) UPLOAD do Supabase Storage → plik zapisany
   f) Pobierz URL do pliku
   g) ZAPIS do tabeli submissions:
      {
        user_id: "user123",        ← KTO przesłał
        challenge_id: "abc-123",   ← DO JAKIEGO wyzwania
        file_url: "https://...",   ← GDZIE leży plik
        file_hash: "md5...",       ← Unikalne ID
        score: null,               ← Będzie obliczone
        status: "pending"
      }

5. System później:
   - Pobiera plik z file_url
   - Porównuje z ground truth
   - Oblicza score (np. ROC-AUC, RMSE)
   - Aktualizuje tabele submissions i leaderboard
```

---

## 🗄️ GDZIE SĄ DANE?

### W SUPABASE:

**TABELE (PostgreSQL):**
```
users          → konta użytkowników
challenges     → lista hackathonów
submissions    → informacje o zgłoszeniach (metadata)
leaderboard    → najlepsze wyniki
```

**STORAGE (pliki):**
```
submissions/   → pliki CSV/JSON uczestników
  user123/
    challenge456/
      predictions_uuid.csv  ← tu jest fizyczny plik
datasets/      → dane treningowe
evaluation/    → ground truth (prywatne)
```

---

## 🔗 POŁĄCZENIE USER ↔ STORAGE

```
USER            STORAGE               BAZA
(id: user123)   (plik: file.csv)     (submissions)

   |                 |                      |
   |--- upload --->  |                      |
   |                 |                      |
   |                 |--- URL ---> zapisz URL + user_id
   |                 |                      |
   |<--- query --------------------------------|
   |           "Twoje pliki to:"            |
   |           - file1.csv (score: 0.95)    |
   |           - file2.csv (score: 0.92)    |
```

**KLUCZ:** `submissions.user_id` = `users.id`

To łączy użytkownika z jego plikami!

---

## 📋 CO MUSISZ TERAZ ZROBIĆ:

### 1. STWÓRZ BUCKETY W SUPABASE (5 min)

Idź do: https://supabase.com/dashboard → **Storage**

Kliknij **"New Bucket"** i stwórz:
- ✅ `submissions` (publiczny)
- ✅ `datasets` (publiczny)
- ✅ `evaluation` (prywatny)

### 2. WŁĄCZ RLS (Row Level Security) (10 min)

Idź do: **Database → Tables** → wybierz tabelę → **"RLS"**

Skopiuj polityki z `SUPABASE_INTEGRATION.md`

### 3. TESTUJ W POSTMAN (15 min)

**⚠️ SWAGGER NIE DZIAŁA Z FILE UPLOAD! Użyj Postman!**

Przepływ testowy:
```
1. POST /api/supabase/auth/signup → zarejestruj
2. POST /api/supabase/auth/signin → zaloguj (skopiuj token!)
3. GET /api/challenges → pobierz wyzwania
4. POST /api/challenges → stwórz wyzwanie (jako admin)
5. POST /api/submissions → prześlij plik (POSTMAN!)
   - Authorization: Bearer <token>
   - Body → form-data → file + challengeId
6. GET /api/submissions/my → sprawdź swoje zgłoszenia
7. GET /api/leaderboard/{id} → zobacz ranking
```

### 4. DODAJ SYSTEM OCENIANIA (później)

To jest bardziej skomplikowane - trzeba:
- Pobrać plik uczestnika z Storage
- Porównać z ground truth
- Obliczyć metrykę (ROC-AUC, RMSE, Accuracy)
- Zaktualizować score w submissions
- Zaktualizować leaderboard

---

## 🎓 WYJAŚNIENIE DLA CIEBIE:

### "PO CO STORAGE?"

Storage to **MAGAZYN NA PLIKI**. W hackathonie:
- Uczestnicy przesyłają pliki CSV z predykcjami (może być 100 MB!)
- Nie możesz trzymać tego w bazie (za duże)
- Zapisujesz plik w Storage, a w bazie tylko URL

### "JAK USER JEST POŁĄCZONY Z STORAGE?"

Nie jest bezpośrednio! Połączenie jest przez **tabelę submissions**:

```
USER (id=user123)
  ↓
SUBMISSIONS (user_id=user123, file_url="https://...")
  ↓
STORAGE (plik pod tym URL)
```

Gdy chcesz "pliki użytkownika user123":
1. SELECT * FROM submissions WHERE user_id='user123'
2. Dostajesz listę URL
3. Pobierasz pliki z Storage używając tych URL

---

## 🚀 JESTEŚ GOTOWY!

Masz:
- ✅ Modele połączone z Supabase
- ✅ Kontrolery z pełną logiką
- ✅ Storage + Baza zintegrowane
- ✅ Dokumentację jak to działa

**NASTĘPNY KROK:** Stwórz buckety w Supabase i przetestuj w Postman!

Powodzenia! 🎉
