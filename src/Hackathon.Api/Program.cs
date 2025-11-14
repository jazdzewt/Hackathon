using Supabase;
using Hackathon.Api.Services;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient();

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

// Configure Supabase
var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseKey = builder.Configuration["Supabase:Key"];

if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
{
    throw new InvalidOperationException("Supabase URL i Key muszą być skonfigurowane w appsettings.json");
}

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Middleware autoryzacji Supabase - musi być PRZED UseAuthentication i UseAuthorization
app.UseMiddleware<Hackathon.Api.Middleware.SupabaseAuthMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();