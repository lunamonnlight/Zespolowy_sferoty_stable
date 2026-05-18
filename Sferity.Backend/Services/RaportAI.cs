using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Sferity.Backend.Servises;

public class RaportAI
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    // Wstrzykujemy HttpClient (który naprawiliśmy w Program.cs) oraz dostęp do appsettings.json
    public RaportAI(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<string> GenerateRaport(JsonElement jsonData)
    {
        // 1. Pobranie klucza z konfiguracji
        string? apiKey = _config["AIConfig:ApiKey"];

        // Zabezpieczenie przed brakiem klucza
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "TUTAJ_WKLEJ_PRAWIDLOWY_KLUCZ")
        {
            return "### ⚠️ Błąd Konfiguracji AI\n\n**Brak prawidłowego klucza API.** Skonfiguruj plik `appsettings.json`, aby umożliwić prawdziwą analizę danych.";
        }

        try
        {
            // 2. Przygotowanie danych (np. zamiana JSON na tekst dla promptu)
            string daneFinansowe = jsonData.GetRawText();
            string prompt = $"Jesteś analitykiem finansowym. Przeanalizuj poniższe dane i wypisz główne ryzyka w formacie Markdown:\n{daneFinansowe}";

            // ==========================================
            // 3. MIEJSCE NA DOCELOWE API
            // Tutaj w przyszłości zbudujesz request do prawdziwego AI (OpenAI / Gemini).
            // Przykład: var response = await _httpClient.PostAsync("https://api.openai.com/v1/...", requestData);
            // ==========================================

            // Symulacja czasu myślenia modelu (i przy okazji pozbywamy się ostrzeżenia CS1998 o braku await!)
            await Task.Delay(2000); 

            // Zwracamy przykładowy sformatowany wynik Markdown
            return $@"### 📊 Raport Analityczny AI
Przeanalizowano dostarczony dokument finansowy. Model sztucznej inteligencji wykrył następujące wskaźniki:

* **Płynność finansowa:** W normie.
* **Zadłużenie:** Wymaga weryfikacji w kolejnym kwartale.
* **Rekomendacja:** Zdolność kredytowa na poziomie akceptowalnym.

*(Pamiętaj, że jest to raport wygenerowany ze szkieletu testowego, ponieważ nie podłączono jeszcze docelowego klucza API).*";

        }
        catch (Exception ex)
        {
            return $"### ❌ Błąd Krytyczny Modułu AI\n\nWystąpił problem podczas przetwarzania raportu: `{ex.Message}`";
        }
    }
}