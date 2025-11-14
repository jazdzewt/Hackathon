# Autoryzacja w Hackathon API

## Architektura

```
┌─────────────┐      JWT Token        ┌──────────────┐
│   Frontend  │ ───────────────────>  │  Backend API │
└─────────────┘                       └──────────────┘
                                              │
                                              ├─> Middleware weryfikuje JWT
                                              ├─> Pobiera rolę z tabeli profiles
                                              └─> Dodaje claims do User
```

## Jak to działa?

### 1. Logowanie
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

**Odpowiedź:**
```json
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "v1.MQ...",
  "expiresAt": "2025-11-14T12:00:00Z"
}
```

### 2. Użycie tokena

Frontend wysyła token w nagłówku:
```http
GET /api/me
Authorization: Bearer eyJhbGc...
```

### 3. Middleware automatycznie:
1. Dekoduje JWT z nagłówka `Authorization`
2. Wyciąga `user_id` (claim `sub`)
3. Pobiera rolę z tabeli `profiles` WHERE `id = user_id`
4. Dodaje claims do `HttpContext.User`:
   - `ClaimTypes.NameIdentifier` → user_id
   - `ClaimTypes.Email` → email
   - `ClaimTypes.Role` → admin/user

### 4. Autoryzacja w kontrolerach

```csharp
[Authorize] // Wymaga tylko zalogowania
public IActionResult GetData() { }

[Authorize(Roles = "admin")] // Wymaga roli admin
public IActionResult AdminOnly() { }
```

## Struktura tabeli profiles

```sql
CREATE TABLE profiles (
  id UUID PRIMARY KEY REFERENCES auth.users(id),
  role TEXT NOT NULL DEFAULT 'user', -- 'user' lub 'admin'
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

## Endpoint do sprawdzenia zalogowanego użytkownika

```http
GET /api/me
Authorization: Bearer eyJhbGc...
```

**Odpowiedź dla zwykłego użytkownika:**
```json
{
  "id": "uuid-here",
  "email": "user@example.com",
  "role": "user",
  "isAdmin": false
}
```

**Odpowiedź dla admina:**
```json
{
  "id": "uuid-here",
  "email": "admin@example.com",
  "role": "admin",
  "isAdmin": true
}
```

## Dostęp do endpointów /api/Admin/*

Wszystkie endpointy w `AdminController` wymagają roli `admin`:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class AdminController : ControllerBase
{
    // Tylko użytkownicy z role = "admin" w tabeli profiles
}
```

## Testowanie

### 1. Zaloguj użytkownika
```bash
curl -X POST http://localhost:5043/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password123"}'
```

### 2. Skopiuj `accessToken` z odpowiedzi

### 3. Sprawdź swoją rolę
```bash
curl http://localhost:5043/api/me \
  -H "Authorization: Bearer TWOJ_TOKEN"
```

### 4. Spróbuj wywołać endpoint admina
```bash
curl http://localhost:5043/api/admin/users \
  -H "Authorization: Bearer TWOJ_TOKEN"
```

- ✅ Jeśli `role = "admin"` → zwróci listę użytkowników
- ❌ Jeśli `role = "user"` → zwróci 403 Forbidden

## Ustawianie roli admin

**Opcja 1: Przez SQL w Supabase Dashboard**
```sql
INSERT INTO profiles (id, role)
VALUES ('user-uuid-from-auth', 'admin')
ON CONFLICT (id) DO UPDATE SET role = 'admin';
```

**Opcja 2: Przez endpoint (gdy już masz jednego admina)**
```http
POST /api/admin/users/{userId}/assign-role
Authorization: Bearer ADMIN_TOKEN
Content-Type: application/json

{
  "roleName": "admin"
}
```

## Security best practices

✅ Token jest weryfikowany przez Supabase (podpis JWT)
✅ Rola jest pobierana z bazy przy każdym requeście (nie można jej sfałszować)
✅ Middleware działa przed kontrolerami
✅ `[Authorize]` sprawdza czy użytkownik jest zalogowany
✅ `[Authorize(Roles = "admin")]` sprawdza rolę
