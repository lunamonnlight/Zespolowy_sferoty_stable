QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
var builder = WebApplication.CreateBuilder(args);

// Rejestrujemy tylko kontrolery
builder.Services.AddControllers();

// Rejestrujemy CORS, żeby Vue mogło pobrać dane
builder.Services.AddCors(options => {
    options.AddPolicy("Open", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
// Rejestracja serwisu AI dla kontrolerów
builder.Services.AddHttpClient<Sferity.Backend.Servises.RaportAI>();

var app = builder.Build();

app.UseCors("Open");
app.MapControllers();

// Dodatkowy test "życia" pod adresem /ping
app.MapGet("/ping", () => "Backend żyje na porcie 5100!");

app.Run("http://localhost:5100"); // Wymuszamy port 5100 na sztywno