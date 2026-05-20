using Sferity.Backend.Services;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
var builder = WebApplication.CreateBuilder(args);

// Rejestrujemy CORS, żeby Vue mogło pobrać dane
builder.Services.AddCors(options => {
    options.AddPolicy("Open", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
// Rejestracja serwisu AI dla kontrolerów
builder.Services.AddHttpClient<Sferity.Backend.Servises.RaportAI>();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddScoped<IConnectionsService, ConnectionsService>(); 

builder.Services.AddScoped<IPolandTimeService, PolandTimeService>(); // TEGO BRAKOWAŁO
builder.Services.AddScoped<IPromoCodeService, PromoCodeService>();
builder.Services.AddScoped<PromoCodeExpiryService>();
var app = builder.Build();

app.UseCors("Open");
app.MapControllers();

// Dodatkowy test "życia" pod adresem /ping
app.MapGet("/ping", () => "Backend żyje na porcie 5100!");

app.Run("http://localhost:5100"); // Wymuszamy port 5100 na sztywno