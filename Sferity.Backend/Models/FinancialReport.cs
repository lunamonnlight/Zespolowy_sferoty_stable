using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Google.GenAI;
using Google.GenAI.Types;
using File = System.IO.File;

namespace Sferity.Backend.Models
{
    public class KRSReport
    {
        public PodstawoweInformacje PodstawoweInformacje { get; set; }
        public object VatDane { get; set; }
        public object PowiazaniaOrganizacji { get; set; }
        public RaportFinansowy RaportFinansowy { get; set; }
        public object StatusOrganizacji { get; set; }
        public object BeneficjenciRzeczywisci { get; set; }
    }

    public class PodstawoweInformacje
    {
        public int Id { get; set; }
        public Nazwy Nazwy { get; set; }
        public Numery Numery { get; set; }
        public Stan Stan { get; set; }
        public GlownaOsoba GlownaOsoba { get; set; }
        public Adres Adres { get; set; }
        public Kontakt Kontakt { get; set; }
        public KrsRejestry KrsRejestry { get; set; }
        public KrsWpisy KrsWpisy { get; set; }
        public KrsPowiazaniaLiczby KrsPowiazaniaLiczby { get; set; }
        public Metadane Metadane { get; set; }
        public string Typ { get; set; }
    }

    public class Nazwy
    {
        public string Pelna { get; set; }
        public string Skrocona { get; set; }
    }

    public class Numery
    {
        public long Duns { get; set; }
        public int Krs { get; set; }
        public long Nip { get; set; }
        public long Regon { get; set; }
    }

    public class Stan
    {
        [JsonPropertyName("czy_dofinansowana_przez_ue")]
        public bool CzyDofinansowanaPrzezUe { get; set; }
        [JsonPropertyName("czy_jest_na_gwp")]
        public bool? CzyJestNaGwp { get; set; }
        [JsonPropertyName("czy_otrzymala_pomoc_publiczna")]
        public bool CzyOtrzymalaPomocPubliczna { get; set; }
        [JsonPropertyName("czy_pozytku_publicznego")]
        public bool CzyPozytkuPublicznego { get; set; }
        [JsonPropertyName("czy_spolka_skarbu_panstwa")]
        public bool CzySpolkaSkarbuPanstwa { get; set; }
        [JsonPropertyName("czy_wykreslona")]
        public bool CzyWykreslona { get; set; }
        [JsonPropertyName("forma_prawna")]
        public string FormaPrawna { get; set; }
        [JsonPropertyName("pkd_przewazajace_dzial")]
        public string PkdPrzewazajaceDzial { get; set; }
        [JsonPropertyName("w_likwidacji")]
        public bool WLikwidacji { get; set; }
        [JsonPropertyName("w_upadlosci")]
        public bool WUpadlosci { get; set; }
        [JsonPropertyName("w_zawieszeniu")]
        public bool WZawieszeniu { get; set; }
        public string Wielkosc { get; set; }
    }

    public class GlownaOsoba
    {
        public int Id { get; set; }
        [JsonPropertyName("imiona_i_nazwisko")]
        public string ImionaINazwisko { get; set; }
    }

    public class Adres
    {
        public string Kod { get; set; }
        public string Miejscowosc { get; set; }
        [JsonPropertyName("nr_domu")]
        public string NrDomu { get; set; }
        [JsonPropertyName("nr_mieszkania")]
        public string NrMieszkania { get; set; }
        public string Panstwo { get; set; }
        public string Poczta { get; set; }
        public string Ulica { get; set; }
    }

    public class Kontakt
    {
        public List<string> Emaile { get; set; } = new List<string>();
        public string Www { get; set; }
    }

    public class KrsRejestry
    {
        [JsonPropertyName("rejestr_przedsiebiorcow_data_wpisu")]
        public DateOnly RejestrPrzedsiebiorcowDataWpisu { get; set; }
        [JsonPropertyName("rejestr_przedsiebiorcow_data_wykreslenia")]
        public DateOnly? RejestrPrzedsiebiorcowDataWykreslenia { get; set; }
        [JsonPropertyName("rejestr_stowarzyszen_data_wpisu")]
        public DateOnly? RejestrStowarzyszenDataWpisu { get; set; }
        [JsonPropertyName("rejestr_stowarzyszen_data_wykreslenia")]
        public DateOnly? RejestrStowarzyszenDataWykreslenia { get; set; }
    }

    public class KrsWpisy
    {
        [JsonPropertyName("najnowszy_data")]
        public DateOnly NajnowszyData { get; set; }
        [JsonPropertyName("najnowszy_numer")]
        public int NajnowszyNumer { get; set; }
        [JsonPropertyName("najnowszy_przed_wykresleniem_data")]
        public DateOnly? NajnowszyPrzedWykresleniemData { get; set; }
        [JsonPropertyName("najnowszy_przed_wykresleniem_numer")]
        public int? NajnowszyPrzedWykresleniemNumer { get; set; }
        [JsonPropertyName("pierwszy_data")]
        public DateOnly PierwszyData { get; set; }
        [JsonPropertyName("wykreslenie_uprawomocnienie_data")]
        public DateOnly? WykreslenieUprawomocnienieData { get; set; }
    }

    public class KrsPowiazaniaLiczby
    {
        public int Aktualne { get; set; }
        public int Przeszle { get; set; }
    }

    public class Metadane
    {
        [JsonPropertyName("krs_odpis_synchronizacja_data_czas")]
        public DateTime KrsOdpisSynchronizacjaDataCzas { get; set; }
        [JsonPropertyName("krs_rozdzialy_dostepne")]
        public List<string> KrsRozdzialyDostepne { get; set; } = new List<string>();
    }

    public class RaportFinansowy
    {
        public RaportFinansowy(IEnumerable raportFinansowy)
        {
            Raportfinansowy = raportFinansowy;
        }

        [JsonPropertyName("RaportFinansowy")] 
        public List<RaportFinansowySzczegoly> RaportFinansowyList { get; set; } = new List<RaportFinansowySzczegoly>();
        public List<DokumentFinansowy> ListaDokumentow { get; set; } = new List<DokumentFinansowy>();
        public IEnumerable Raportfinansowy { get; }
        public IEnumerable raportFinansowy { get; }
    }

    public class RaportFinansowySzczegoly
    {
        public int Rok { get; set; }
        public decimal AktywaNetto { get; set; }
        public decimal Przychody { get; set; }
        public Plynnosc Plynnosc { get; set; }
        public Zyski Zyski { get; set; }
        public Marze Marze { get; set; }
        public Rotacja Rotacja { get; set; }
        public Stany Stany { get; set; }
    }

    public class Plynnosc
    {
        [JsonPropertyName("PlynnoscBiezaca")]
        public decimal PlynnoscBiezaca { get; set; }
        [JsonPropertyName("PlynnoscSzybka")]
        public decimal PlynnoscSzybka { get; set; }
        [JsonPropertyName("PlynnoscNatychmiastowa")]
        public decimal PlynnoscNatychmiastowa { get; set; }
    }

    public class Zyski
    {
        [JsonPropertyName("ZyskOperacyjny")]
        public decimal ZyskOperacyjny { get; set; }
        [JsonPropertyName("ZyskDzialanoscDodatkowa")]
        public decimal ZyskDzialanoscDodatkowa { get; set; }
        [JsonPropertyName("ZyskFinansowy")]
        public decimal ZyskFinansowy { get; set; }
        [JsonPropertyName("ZyskOgolem")]
        public decimal ZyskOgolem { get; set; }
    }

    public class Marze
    {
        [JsonPropertyName("MarzaBrutto")]
        public decimal MarzaBrutto { get; set; }
        [JsonPropertyName("MarzaOperacyjna")]
        public decimal MarzaOperacyjna { get; set; }
        [JsonPropertyName("MarzaNetto")]
        public decimal MarzaNetto { get; set; }
    }

    public class Rotacja
    {
        [JsonPropertyName("RotacjaZobowiazan")]
        public decimal RotacjaZobowiazan { get; set; }
        [JsonPropertyName("RotacjaNaleznosci")]
        public decimal RotacjaNaleznosci { get; set; }
    }

    public class Stany
    {
        [JsonPropertyName("StanGotowkiWKasie")]
        public decimal StanGotowkiWKasie { get; set; }
        [JsonPropertyName("SrodkiPieniezneOrazInneAktywaPieniezne")]
        public decimal SrodkiPieniezneOrazInneAktywaPieniezne { get; set; }
        [JsonPropertyName("StanZapasow")]
        public decimal StanZapasow { get; set; }
        [JsonPropertyName("StanNaleznosci")]
        public decimal StanNaleznosci { get; set; }
        public decimal Towary { get; set; }
        [JsonPropertyName("StanZobowiazan")]
        public decimal StanZobowiazan { get; set; }
        [JsonPropertyName("KredytyPozyczkiKrotkoterminowe")]
        public decimal KredytyPozyczkiKrotkoterminowe { get; set; }
        [JsonPropertyName("KredytyPozyczkiDlugoterminowe")]
        public decimal KredytyPozyczkiDlugoterminowe { get; set; }
        [JsonPropertyName("UdzielonePozyczkiKrotkoterminowe")]
        public decimal UdzielonePozyczkiKrotkoterminowe { get; set; }
        [JsonPropertyName("UdzielonePozyczkiDlugoterminowe")]
        public decimal UdzielonePozyczkiDlugoterminowe { get; set; }
    }

    public class DokumentFinansowy
    {
        public bool CzyMaJson { get; set; }
        public int Id { get; set; }
        public string Nazwa { get; set; }
        public int Year { get; set; }
    }

    public class RaportAI
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public RaportAI(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;

        }

        public async Task<string> GenerateRaport(object jsonData)
        {
            var promptFile = await File.ReadAllTextAsync("Files/message.txt");
            var json = JsonSerializer.Serialize(jsonData, new JsonSerializerOptions { WriteIndented = true });
            var prompt = promptFile.Replace("{{JSON_DATA}}", json);

            var apiKey = _config["GeminiApiKey"];

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent"
            );

            request.Headers.Add("x-goog-api-key", apiKey);
            request.Content = JsonContent.Create(requestBody);

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Gemini error {(int)response.StatusCode}: {result}");

            return result;
        }
        
        

        
    }
    
}