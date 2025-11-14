# 📦 Supabase Storage - Dokumentacja

## 🎯 Co to jest?

**Supabase Storage** pozwala przechowywać i zarządzać plikami (obrazy, dokumenty, filmy, itp.) w chmurze.

---

## 🔧 Konfiguracja w Supabase Dashboard

### Krok 1: Utwórz Bucket (pojemnik na pliki)

1. Zaloguj się do **Supabase Dashboard**: https://app.supabase.com
2. Wybierz swój projekt
3. Przejdź do **Storage** (ikona 📦 w menu)
4. Kliknij **New bucket**
5. Wpisz nazwę np. `uploads`
6. Wybierz:
   - **Public bucket** - jeśli pliki mają być publicznie dostępne
   - **Private bucket** - jeśli pliki wymagają autoryzacji
7. Kliknij **Create bucket**

---

## 📡 Dostępne Endpointy

### 1. **GET `/api/storage/test`** - Test połączenia
Sprawdza czy Storage działa

**Odpowiedź:**
```json
{
  "message": "✅ Połączenie z Supabase Storage działa!",
  "bucketsCount": 2,
  "timestamp": "2025-11-14T..."
}
```

---

### 2. **GET `/api/storage/buckets`** - Lista bucket'ów
Pobiera wszystkie bucket'y

**Odpowiedź:**
```json
{
  "message": "✅ Lista bucket'ów",
  "count": 2,
  "buckets": [
    {
      "id": "uploads",
      "name": "uploads",
      "isPublic": true,
      "createdAt": "2025-11-14T..."
    }
  ]
}
```

---

### 3. **POST `/api/storage/upload`** - Upload pliku
Przesyła plik do bucket'a

**Form Data:**
- `file` - plik do przesłania (wymagane)
- `bucketName` - nazwa bucket'a (domyślnie: "uploads")
- `folder` - opcjonalny folder wewnątrz bucket'a

**Przykład (Postman/Swagger):**
```
POST http://localhost:5043/api/storage/upload
Content-Type: multipart/form-data

file: [wybierz plik]
bucketName: uploads
folder: images
```

**Odpowiedź:**
```json
{
  "message": "✅ Plik przesłany!",
  "fileName": "zdjecie.jpg",
  "originalSize": 150000,
  "storedPath": "images/abc-123-def.jpg",
  "publicUrl": "https://...supabase.co/storage/v1/object/public/uploads/images/abc-123-def.jpg",
  "bucket": "uploads"
}
```

---

### 4. **GET `/api/storage/files/{bucketName}?folder=xxx`** - Lista plików
Pobiera pliki z bucket'a

**Przykład:**
```
GET http://localhost:5043/api/storage/files/uploads?folder=images
```

**Odpowiedź:**
```json
{
  "message": "✅ Lista plików",
  "bucket": "uploads",
  "folder": "images",
  "count": 5,
  "files": [
    {
      "name": "abc-123-def.jpg",
      "id": "abc-123-def.jpg",
      "createdAt": "2025-11-14T...",
      "updatedAt": "2025-11-14T..."
    }
  ]
}
```

---

### 5. **GET `/api/storage/url/{bucketName}/{filePath}`** - Publiczny URL
Generuje publiczny link do pliku

**Przykład:**
```
GET http://localhost:5043/api/storage/url/uploads/images/abc-123-def.jpg
```

**Odpowiedź:**
```json
{
  "message": "✅ Publiczny URL",
  "filePath": "images/abc-123-def.jpg",
  "publicUrl": "https://...supabase.co/storage/v1/object/public/uploads/images/abc-123-def.jpg"
}
```

---

### 6. **GET `/api/storage/download/{bucketName}/{filePath}`** - Pobierz plik
Pobiera plik (download)

**Przykład:**
```
GET http://localhost:5043/api/storage/download/uploads/images/abc-123-def.jpg
```

**Odpowiedź:** Plik do pobrania

---

### 7. **DELETE `/api/storage/delete/{bucketName}/{filePath}`** - Usuń plik
Usuwa plik z bucket'a

**Przykład:**
```
DELETE http://localhost:5043/api/storage/delete/uploads/images/abc-123-def.jpg
```

**Odpowiedź:**
```json
{
  "message": "✅ Plik usunięty!",
  "filePath": "images/abc-123-def.jpg",
  "bucket": "uploads"
}
```

---

### 8. **POST `/api/storage/move`** - Przenieś/zmień nazwę pliku
Przenosi lub zmienia nazwę pliku

**Body:**
```json
{
  "bucketName": "uploads",
  "fromPath": "images/old-name.jpg",
  "toPath": "photos/new-name.jpg"
}
```

**Odpowiedź:**
```json
{
  "message": "✅ Plik przeniesiony!",
  "from": "images/old-name.jpg",
  "to": "photos/new-name.jpg",
  "bucket": "uploads"
}
```

---

### 9. **GET `/api/health/storage`** - Health Check
Sprawdza status Storage

---

## 🚀 Przykładowy Flow

### Upload i wyświetlenie obrazu:

1. **Upload pliku:**
   ```bash
   POST /api/storage/upload
   Form: file=zdjecie.jpg, bucketName=uploads, folder=images
   
   → Otrzymujesz publicUrl
   ```

2. **Wyświetl w przeglądarce:**
   ```html
   <img src="https://...supabase.co/storage/v1/object/public/uploads/images/abc-123.jpg" />
   ```

3. **Pobierz przez API:**
   ```bash
   GET /api/storage/download/uploads/images/abc-123.jpg
   ```

4. **Usuń:**
   ```bash
   DELETE /api/storage/delete/uploads/images/abc-123.jpg
   ```

---

## 📝 Typy plików

Storage obsługuje wszystkie typy:
- **Obrazy**: .jpg, .png, .gif, .webp
- **Dokumenty**: .pdf, .doc, .xlsx
- **Wideo**: .mp4, .mov
- **Audio**: .mp3, .wav
- **Inne**: dowolne pliki

---

## 🔐 Bezpieczeństwo

### Public Bucket
- Każdy może pobierać pliki przez URL
- Upload wymaga autoryzacji przez API

### Private Bucket
- Dostęp tylko przez API z tokenem autoryzacji
- Secure, ale wymaga więcej konfiguracji

---

## 💡 Wskazówki

1. **Nazwy plików:** API generuje unikalne UUID, więc nie musisz się martwić o duplikaty
2. **Foldery:** Używaj folderów do organizacji (np. `images/`, `documents/`)
3. **Limity:** Supabase Free Tier: 1GB storage
4. **Optymalizacja:** Kompresuj obrazy przed uploadem

---

## 🧪 Testowanie w Swagger

1. Uruchom aplikację: `dotnet run`
2. Otwórz: http://localhost:5043/swagger
3. Znajdź sekcję **Storage**
4. Wypróbuj endpointy!

---

## 🛠️ Troubleshooting

**Problem:** "Bucket not found"
- **Rozwiązanie:** Utwórz bucket w Supabase Dashboard

**Problem:** "Permission denied"
- **Rozwiązanie:** Sprawdź RLS policies w Storage Settings

**Problem:** "File too large"
- **Rozwiązanie:** Zmniejsz rozmiar pliku lub zwiększ limit w Supabase
