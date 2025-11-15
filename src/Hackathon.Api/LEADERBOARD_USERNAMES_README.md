# Instrukcja - Leaderboard z nazwami użytkowników

## Co zostało zmienione?

Backend został zmodyfikowany tak, aby w leaderboardzie wyświetlały się nazwy użytkowników (display_name lub email) zamiast UUID użytkownika.

## Zmiany w backendzie

### 1. LeaderboardService.cs
- Dodano metodę `GetUserDisplayNameAsync()` która pobiera display_name lub email użytkownika z Supabase Auth
- Zmodyfikowano `GetLeaderboardAsync()` aby pobierała nazwy dla wszystkich użytkowników przed zwróceniem wyników
- Leaderboard teraz zwraca już gotowe nazwy w polu `Username` w DTO

### 2. Funkcja SQL w Supabase
Utworzono funkcję SQL `get_user_email()` która pobiera dane użytkownika z tabeli `auth.users`.

## Wymagane kroki konfiguracji

### Krok 1: Wykonaj funkcję SQL w Supabase

1. Wejdź na: **Supabase Dashboard → SQL Editor**
2. Otwórz plik: `SQL/get_user_email_function.sql`
3. Skopiuj cały kod SQL i wykonaj go w Supabase SQL Editor
4. Kliknij **RUN** aby stworzyć funkcję

### Krok 2: Przetestuj backend

```powershell
cd c:\Users\piotr\Desktop\Hackathon\Hackathon\src\Hackathon.Api
dotnet run
```

Następnie wywołaj endpoint leaderboard:
```bash
GET http://localhost:5043/api/Leaderboard/{challengeId}
Authorization: Bearer YOUR_TOKEN
```

Odpowiedź powinna wyglądać tak:
```json
[
  {
    "rank": 1,
    "username": "Jan Kowalski",  // ← Teraz wyświetla nazwę zamiast UUID!
    "bestScore": 95.5,
    "totalSubmissions": 1,
    "lastSubmissionDate": "2025-11-15T10:30:00Z",
    "status": "completed"
  },
  {
    "rank": 2,
    "username": "user@example.com",  // ← Lub email jeśli brak display_name
    "bestScore": 92.3,
    "totalSubmissions": 1,
    "lastSubmissionDate": "2025-11-15T09:15:00Z",
    "status": "completed"
  }
]
```

### Krok 3: Flutter - brak zmian!

**Dobra wiadomość:** Nie musisz zmieniać nic we Flutterze! 

Frontend już używa pola `username` z odpowiedzi API. Teraz backend zwraca tam już gotowe nazwy użytkowników, więc:

- ❌ Flutter już **NIE MUSI** wywoływać `/api/Me` dla każdego użytkownika
- ✅ Leaderboard **od razu wyświetla** prawidłowe nazwy
- ✅ Wszystko działa **szybciej** (mniej requestów HTTP)

## Jak to działa?

### Backend (LeaderboardService.cs)

```csharp
// 1. Pobiera submissions z bazy
var allSubmissions = await _supabaseClient
    .From<Submission>()
    .Where(s => s.ChallengeId == challengeId)
    .Get();

// 2. Sortuje po wyniku
var sorted = allSubmissions.Models
    .OrderByDescending(s => s.Score)
    .Take(topN);

// 3. Pobiera unikalne userId
var uniqueUserIds = sorted.Select(s => s.UserId).Distinct();

// 4. Dla każdego userId wywołuje funkcję SQL get_user_email()
foreach (var userId in uniqueUserIds)
{
    string displayName = await GetUserDisplayNameAsync(userId);
    userDisplayNames[userId] = displayName;
}

// 5. Tworzy LeaderboardEntryDto z gotowymi nazwami
leaderboard.Add(new LeaderboardEntryDto(
    Rank: rank++,
    Username: displayName,  // ← Już zmapowane!
    BestScore: submission.Score,
    ...
));
```

### Funkcja SQL (get_user_email)

```sql
CREATE OR REPLACE FUNCTION get_user_email(user_uid UUID)
RETURNS TEXT AS $$
  SELECT COALESCE(
    raw_user_meta_data->>'display_name',  -- Najpierw sprawdź display_name
    email,                                  -- Potem email
    user_uid::TEXT                          -- W ostateczności UUID
  ) FROM auth.users WHERE id = user_uid;
$$ LANGUAGE SQL SECURITY DEFINER;
```

### Frontend (Flutter)

```dart
// challenge_user_page.dart - NIE TRZEBA JUŻ TEGO!
// final userName = await fetchUserName(userId);  ← USUNIĘTE

// Zamiast tego używamy od razu:
final String displayName = entry['username'];  // ← Już gotowe!
```

## Troubleshooting

### Problem: Leaderboard nadal pokazuje UUID
**Rozwiązanie:**
1. Sprawdź czy wykonałeś funkcję SQL w Supabase
2. Zrestartuj backend: `dotnet run`
3. Sprawdź logi backendu czy są błędy

### Problem: Błąd "function get_user_email does not exist"
**Rozwiązanie:**
1. Wykonaj SQL z pliku `SQL/get_user_email_function.sql`
2. Upewnij się że funkcja została stworzona w odpowiedniej bazie danych
3. Sprawdź czy użytkownik ma uprawnienia do wykonania funkcji

### Problem: Wyświetla się email zamiast display_name
**Wyjaśnienie:** To normalne! Jeśli użytkownik nie ustawił `display_name` podczas rejestracji, funkcja zwraca email jako fallback.

Aby dodać display_name do istniejących użytkowników:
```sql
-- W Supabase SQL Editor
UPDATE auth.users 
SET raw_user_meta_data = jsonb_set(
  COALESCE(raw_user_meta_data, '{}'::jsonb),
  '{display_name}',
  '"Jan Kowalski"'
)
WHERE id = 'user-uuid-here';
```

## Podsumowanie

✅ **Backend**: Leaderboard zwraca już gotowe nazwy w polu `username`  
✅ **SQL**: Funkcja `get_user_email()` pobiera display_name lub email z auth.users  
✅ **Flutter**: Działa bez zmian! Wyświetla od razu prawidłowe nazwy  
✅ **Performance**: Mniej requestów HTTP, szybsze ładowanie leaderboardu
