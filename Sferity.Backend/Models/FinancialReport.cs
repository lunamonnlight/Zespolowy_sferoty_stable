using System;
using System.Collections.Generic;
using System.Net.Http; 
using System.Net.Http.Headers; 
using System.Net.Http.Json; 
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.IO; 

using Microsoft.Extensions.Configuration; 

// KLASA DLA DANYCH FIRMY
namespace Sferity.Backend.Models
{
    public class Nazwy
    {
        [JsonPropertyName("pelna")]
        public string Pelna { get; set; } = string.Empty;

        [JsonPropertyName("skrocona")]
        public string Skrocona { get; set; } = string.Empty;
    }

    public class Numery
    {
        [JsonPropertyName("duns")]
        public int Duns { get; set; }

        [JsonPropertyName("krs")]
        public string Krs { get; set; }

        [JsonPropertyName("nip")]
        public long Nip { get; set; }

        [JsonPropertyName("regon")]
        public int Regon { get; set; }
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
        public string FormaPrawna { get; set; } = string.Empty;

        [JsonPropertyName("pkd_przewazajace_dzial")]
        public string PkdPrzewazajaceDzial { get; set; } = string.Empty;

        [JsonPropertyName("w_likwidacji")]
        public bool WLikwidacji { get; set; }

        [JsonPropertyName("w_upadlosci")]
        public bool WUpadlosci { get; set; }

        [JsonPropertyName("w_zawieszeniu")]
        public bool WZawieszeniu { get; set; }

        [JsonPropertyName("wielkosc")]
        public string Wielkosc { get; set; } = string.Empty;
    }

    public class GlownaOsoba
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("imiona_i_nazwisko")]
        public string ImionaINazwisko { get; set; } = string.Empty;
    }

    public class Adres
    {
        [JsonPropertyName("kod")]
        public string Kod { get; set; } = string.Empty;

        [JsonPropertyName("miejscowosc")]
        public string Miejscowosc { get; set; } = string.Empty;

        [JsonPropertyName("nr_domu")]
        public string NumerDomu { get; set; } = string.Empty;

        [JsonPropertyName("nr_mieszkania")]
        public string? NumerMieszkania { get; set; }

        [JsonPropertyName("panstwo")]
        public string Panstwo { get; set; } = string.Empty;

        [JsonPropertyName("poczta")]
        public string Poczta { get; set; } = string.Empty;

        [JsonPropertyName("ulica")]
        public string Ulica { get; set; } = string.Empty;
    }

    public class Kontakt
    {
        [JsonPropertyName("emaile")]
        public List<string> Emaile { get; set; } = new List<string>();

        [JsonPropertyName("www")]
        public string Www { get; set; } = string.Empty;
    }

    public class KrsRejestry
    {
        [JsonPropertyName("rejestr_przedsiebiorcow_data_wpisu")]
        public string RejestrPrzedsiebiorcowDataWpisu { get; set; } = string.Empty;

        [JsonPropertyName("rejestr_przedsiebiorcow_data_wykreslenia")]
        public string? RejestrPrzedsiebiorcowDataWykreslenia { get; set; }

        [JsonPropertyName("rejestr_stowarzyszen_data_wpisu")]
        public string? RejestrStowarzyszenDataWpisu { get; set; }

        [JsonPropertyName("rejestr_stowarzyszen_data_wykreslenia")]
        public string? RejestrStowarzyszenDataWykreslenia { get; set; }
    }

    public class KrsWpisy
    {
        [JsonPropertyName("najnowszy_data")]
        public string NajnowszyData { get; set; } = string.Empty;

        [JsonPropertyName("najnowszy_numer")]
        public int NajnowszyNumer { get; set; }

        [JsonPropertyName("najnowszy_przed_wykresleniem_data")]
        public string? NajnowszyPrzedWykresleniemData { get; set; }

        [JsonPropertyName("najnowszy_przed_wykresleniem_numer")]
        public int? NajnowszyPrzedWykresleniemNumer { get; set; }

        [JsonPropertyName("pierwszy_data")]
        public string PierwszyData { get; set; } = string.Empty;

        [JsonPropertyName("wykreslenie_uprawomocnienie_data")]
        public string? WykreslenieUprawomocnienieData { get; set; }
    }

    public class KrsPowiazaniaLiczby
    {
        [JsonPropertyName("aktualne")]
        public int Aktualne { get; set; }

        [JsonPropertyName("przeszle")]
        public int Przeszle { get; set; }
    }

    public class Metadane
    {
        [JsonPropertyName("krs_odpis_synchronizacja_data_czas")]
        public string KrsOdpisSynchronizacjaDataCzas { get; set; } = string.Empty;

        [JsonPropertyName("krs_rozdzialy_dostepne")]
        public List<string> KrsRozdzialyDostepne { get; set; } = new List<string>();
    }

    public class PodstawoweInformacje
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("nazwy")]
        public Nazwy Nazwy { get; set; } = new Nazwy();

        [JsonPropertyName("numery")]
        public Numery Numery { get; set; } = new Numery();

        [JsonPropertyName("stan")]
        public Stan Stan { get; set; } = new Stan();

        [JsonPropertyName("glowna_osoba")]
        public GlownaOsoba GlownaOsoba { get; set; } = new GlownaOsoba();

        [JsonPropertyName("adres")]
        public Adres Adres { get; set; } = new Adres();

        [JsonPropertyName("kontakt")]
        public Kontakt Kontakt { get; set; } = new Kontakt();

        [JsonPropertyName("krs_rejestry")]
        public KrsRejestry KrsRejestry { get; set; } = new KrsRejestry();

        [JsonPropertyName("krs_wpisy")]
        public KrsWpisy KrsWpisy { get; set; } = new KrsWpisy();

        [JsonPropertyName("krs_powiazania_liczby")]
        public KrsPowiazaniaLiczby KrsPowiazaniaLiczby { get; set; } = new KrsPowiazaniaLiczby();

        [JsonPropertyName("metadane")]
        public Metadane Metadane { get; set; } = new Metadane();

        [JsonPropertyName("typ")]
        public string Typ { get; set; } = string.Empty;
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

        [JsonPropertyName("Towary")]
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

    public class RaportFinansowySzczegoly
    {
        [JsonPropertyName("Rok")]
        public int Rok { get; set; }

        [JsonPropertyName("AktywaNetto")]
        public decimal AktywaNetto { get; set; }

        [JsonPropertyName("Przychody")]
        public decimal Przychody { get; set; }

        [JsonPropertyName("Plynnosc")]
        public Plynnosc Plynnosc { get; set; } = new Plynnosc();

        [JsonPropertyName("Zyski")]
        public Zyski Zyski { get; set; } = new Zyski();

        [JsonPropertyName("Marze")]
        public Marze Marze { get; set; } = new Marze();

        [JsonPropertyName("Rotacja")]
        public Rotacja Rotacja { get; set; } = new Rotacja();

        [JsonPropertyName("Stany")]
        public Stany Stany { get; set; } = new Stany();
    }

    public class DokumentFinansowy
    {
        [JsonPropertyName("CzyMaJson")]
        public bool CzyMaJson { get; set; }

        [JsonPropertyName("Id")]
        public int Id { get; set; }

        [JsonPropertyName("Nazwa")]
        public string Nazwa { get; set; } = string.Empty;

        [JsonPropertyName("Year")]
        public int Year { get; set; }
    }

    public class FinancialReportData
    {
        [JsonPropertyName("RaportFinansowy")]
        public List<RaportFinansowySzczegoly> RaportFinansowy { get; set; } = new List<RaportFinansowySzczegoly>();

        [JsonPropertyName("ListaDokumentow")]
        public List<DokumentFinansowy> ListaDokumentow { get; set; } = new List<DokumentFinansowy>();
    }
    
    
    

    public class KRSReport
    {
        [JsonPropertyName("PodstawoweInformacje")]
        public PodstawoweInformacje PodstawoweInformacje { get; set; } = new PodstawoweInformacje();

        [JsonPropertyName("VatDane")]
        public object? VatDane { get; set; }

        [JsonPropertyName("PowiazaniaOrganizacji")]
        public object? PowiazaniaOrganizacji { get; set; }

        [JsonPropertyName("RaportFinansowy")]
        public FinancialReportData? RaportFinansowy { get; set; }

        [JsonPropertyName("StatusOrganizacji")]
        public object? StatusOrganizacji { get; set; }

        [JsonPropertyName("BeneficjenciRzeczywisci")]
        public object? BeneficjenciRzeczywisci { get; set; }
        
        [JsonPropertyName("ListaSankcyjna")]
        public bool ListaSankcyjna { get; set; }
    }

    
}