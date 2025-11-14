# Instrukcje dla GitHub Copilot - Hackathon API

## Kontekst projektu
To jest ASP.NET Core 9.0 Web API dla systemu zarządzania hackathonami. Aplikacja używa Supabase jako backendu (autentykacja + baza danych PostgreSQL).

## Zasady architektury

### 1. Autentykacja i Autoryzacja

### Supabase Auth + tabela profiles
- **Używamy Supabase Auth** - wszystkie operacje JWT są zarządzane przez Supabase
- **Tabela `profiles`** - przechowuje rolę użytkownika (admin/user), UID = Supabase Auth user_id
- **Middleware `SupabaseAuthMiddleware`** - automatycznie weryfikuje JWT i pobiera rolę z tabeli
- **NIE implementuj własnego JWT** - tokeny generuje i weryfikuje Supabase
- **Logout NIE jest potrzebny** - JWT jest stateless, wylogowanie odbywa się po stronie klienta poprzez usunięcie tokenu

### Autoryzacja w kontrolerach
```csharp
[Authorize] // Wymaga zalogowania
[Authorize(Roles = "admin")] // Wymaga roli admin
```

### Pobieranie danych użytkownika w kontrolerze
```csharp
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var role = User.FindFirst(ClaimTypes.Role)?.Value;
```

### 2. Struktura kodu

```
Controllers/     - Endpointy API (cienka warstwa)
Services/        - Logika biznesowa
DTOs/           - Data Transfer Objects (Request/Response)
Models/         - Modele bazy danych (mapowane na tabele Supabase)
```

### 3. Wzorce projektowe

**Dependency Injection:**
```csharp
// W konstruktorze kontrolera
public MyController(IMyService myService)
{
    _myService = myService;
}
```

**Repository Pattern przez Supabase Client:**
```csharp
// Pobieranie danych
var result = await _supabaseClient
    .From<Model>()
    .Get();

// Dodawanie
await _supabaseClient
    .From<Model>()
    .Insert(entity);

// Aktualizacja
await _supabaseClient
    .From<Model>()
    .Update(entity);
```

### 4. DTOs i walidacja

**Zawsze używaj record types dla DTOs:**
```csharp
public record CreateSomethingDto(
    string Name,
    string Description
);
```

**Walidacja w serwisach:**
```csharp
if (string.IsNullOrWhiteSpace(dto.Name))
{
    throw new ArgumentException("Name is required");
}
```

### 5. Obsługa błędów w kontrolerach

```csharp
try
{
    var result = await _service.DoSomethingAsync(dto);
    return Ok(result);
}
catch (ArgumentException ex)
{
    return BadRequest(new { message = ex.Message });
}
catch (UnauthorizedAccessException ex)
{
    return Unauthorized(new { message = ex.Message });
}
catch (Exception ex)
{
    return StatusCode(500, new { message = "Operation failed", error = ex.Message });
}
```

### 6. Supabase Auth - jak używać

**Rejestracja:**
```csharp
var session = await _supabaseClient.Auth.SignUp(email, password);
```

**Logowanie:**
```csharp
var session = await _supabaseClient.Auth.SignIn(email, password);
```

**Odświeżanie tokenu:**
```csharp
var session = await _supabaseClient.Auth.RefreshSession();
```

**Reset hasła:**
```csharp
await _supabaseClient.Auth.ResetPasswordForEmail(email);
```

### 7. Modele bazy danych

**Zawsze używaj atrybutów Supabase:**
```csharp
[Table("table_name")]
public class MyModel : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; }
    
    [Column("email")]
    public string Email { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
```

### 8. Nazewnictwo

- **Kontrolery:** `NazwaController.cs` (liczba pojedyncza dla zasobu pojedynczego, mnoga dla kolekcji)
- **Serwisy:** `NazwaService.cs` + `INazwaService.cs`
- **DTOs:** `NazwaDto.cs` lub grupuj w folderach `DTOs/Nazwa/`
- **Endpointy:** RESTful - `/api/challenges`, `/api/submissions`

### 9. Asynchroniczność

**ZAWSZE używaj async/await:**
```csharp
public async Task<ResultDto> DoSomethingAsync()
{
    return await _supabaseClient.From<Model>().Get();
}
```

### 10. Co NIE robić

❌ Nie implementuj własnego JWT signing/validation
❌ Nie przechowuj haseł w bazie - używaj Supabase Auth
❌ Nie dodawaj endpointu logout dla JWT-only
❌ Nie używaj synchronicznych wywołań do bazy
❌ Nie mieszaj logiki biznesowej w kontrolerach
❌ Nie zwracaj surowych wyjątków do klienta

### 11. Security best practices

- Zawsze waliduj dane wejściowe
- Nigdy nie loguj haseł
- Przy reset hasła nie ujawniaj czy email istnieje
- Używaj HTTPS w production
- Tokeny JWT są readonly - nie możesz ich unieważnić server-side

## Przykład pełnego flow

**1. Dodaj model:**
```csharp
[Table("tasks")]
public class Task : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; }
    
    [Column("title")]
    public string Title { get; set; }
}
```

**2. Dodaj DTOs:**
```csharp
public record CreateTaskDto(string Title);
public record TaskDto(string Id, string Title);
```

**3. Dodaj interface:**
```csharp
public interface ITaskService
{
    Task<TaskDto> CreateAsync(CreateTaskDto dto);
}
```

**4. Implementuj serwis:**
```csharp
public class TaskService : ITaskService
{
    private readonly Supabase.Client _supabaseClient;
    
    public async Task<TaskDto> CreateAsync(CreateTaskDto dto)
    {
        var task = new Task { Title = dto.Title };
        await _supabaseClient.From<Task>().Insert(task);
        return new TaskDto(task.Id, task.Title);
    }
}
```

**5. Dodaj kontroler:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
    {
        try
        {
            var result = await _taskService.CreateAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
```

**6. Zarejestruj w DI:**
```csharp
builder.Services.AddScoped<ITaskService, TaskService>();
```

## Supabase specifics

- Connection string i klucze są w `appsettings.json`
- Używamy Supabase-csharp library
- RLS (Row Level Security) może być włączony na poziomie Supabase
- Migracje bazy wykonujemy bezpośrednio w Supabase Dashboard lub przez SQL

## Polecenia przydatne

```bash
# Build projektu
dotnet build

# Uruchom API
dotnet run

# Dodaj nowy pakiet
dotnet add package PackageName

# Restore pakietów
dotnet restore
```
