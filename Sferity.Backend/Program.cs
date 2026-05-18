var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

// 1. Dodaj tę polisę
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// 2. Użyj polisy (MUSI BYĆ PRZED MapControllers)
app.UseCors("AllowAll");

app.MapControllers();
app.Run();