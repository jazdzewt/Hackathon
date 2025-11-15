using Supabase;
using Hackathon.Api.Services;
using Microsoft.AspNetCore.Authentication;
using System.Threading.RateLimiting;
using Serilog;

// Konfiguracja Serilog PRZED utworzeniem buildera
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Hackathon.Api")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/hackathon-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30) // Przechowuje logi z ostatnich 30 dni
    .WriteTo.File(
        path: "logs/errors/error-.txt",
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 90) // Błędy przechowuje 90 dni
    .CreateLogger();

try
{
    Log.Information("Starting Hackathon API...");

var builder = WebApplication.CreateBuilder(args);

// Dodaj Serilog jako logger
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Polityka domyślna - 100 requestów na minutę
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            });
    });

    // Polityka dla autentykacji - 10 prób logowania na minutę
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }));

    // Polityka dla submissions - 5 na godzinę na użytkownika
    options.AddPolicy("submissions", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromHours(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Polityka dla admina - wyższy limit
    options.AddPolicy("admin", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 20
            }));

    options.RejectionStatusCode = 429; // Too Many Requests
    
    // Customize response when rate limit is exceeded
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.ContentType = "application/json";
        
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
            ? (int?)retryAfterValue.TotalSeconds
            : null;

        var message = new
        {
            error = "Rate limit exceeded",
            message = "Too many requests. Please try again later.",
            retryAfter = retryAfter.HasValue ? $"{retryAfter} seconds" : "unknown"
        };

        await context.HttpContext.Response.WriteAsJsonAsync(message, cancellationToken);
    };
});

// Autentykacja - prosty handler dla JWT
builder.Services.AddAuthentication("SupabaseAuth")
    .AddScheme<AuthenticationSchemeOptions, Hackathon.Api.Middleware.SupabaseAuthHandler>(
        "SupabaseAuth",
        options => { }
    );

builder.Services.AddAuthorization();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.SwaggerDoc("v1", new() { Title = "Hackathon API", Version = "v1" });
});

// Register application services
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IChallengeService, ChallengeService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<IScoringService, ScoringService>();

// Configure Supabase
var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseKey = builder.Configuration["Supabase:ServiceRoleKey"]; // Używamy ServiceRoleKey aby ominąć RLS

if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
{
    throw new InvalidOperationException("Supabase URL i Key muszą być skonfigurowane w appsettings.json");
}
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


builder.Services.AddSingleton(provider => 
{
    var options = new SupabaseOptions
    {
        AutoConnectRealtime = true,
        AutoRefreshToken = true
    };
    
    var client = new Client(supabaseUrl, supabaseKey, options);
    // Inicjalizacja klienta
    client.InitializeAsync().Wait();
    return client;
});

var app = builder.Build();

// Serilog request logging
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    };
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Rate Limiting - musi być PRZED autoryzacją
app.UseRateLimiter();

// Middleware autoryzacji Supabase - musi być PRZED UseAuthentication i UseAuthorization
app.UseMiddleware<Hackathon.Api.Middleware.SupabaseAuthMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseCors("DevCors");
// Map controllers
app.MapControllers();

Log.Information("Hackathon API started successfully");
app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}