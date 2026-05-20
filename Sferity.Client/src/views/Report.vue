﻿<script setup lang="ts">
import {computed, onMounted, ref, watch} from 'vue'
import Button from 'primevue/button'
import 'primeicons/primeicons.css'
import {RefSymbol} from "@vue/reactivity";
import { useAuth } from '../composables/useAuth'
// ── Interface ─────────────────────────────────────────────────────────────────
interface Nazwy {
  pelna: string;
  skrocona: string;
}

interface Numery {
  duns: number;
  krs: string;
  nip: number;
  regon: string;
}

interface Stan {
  czy_dofinansowana_przez_ue: boolean;
  czy_jest_na_gwp: boolean | null;
  czy_otrzymala_pomoc_publiczna: boolean;
  czy_pozytku_publicznego: boolean;
  czy_spolka_skarbu_panstwa: boolean;
  czy_wykreslona: boolean;
  forma_prawna: string;
  pkd_przewazajace_dzial: string;
  w_likwidacji: boolean;
  w_upadlosci: boolean;
  w_zawieszeniu: boolean;
  wielkosc: string;
}

interface GlownaOsoba {
  id: number;
  imiona_i_nazwisko: string;
}

interface Adres {
  kod: string;
  miejscowosc: string;
  nr_domu: string;
  nr_mieszkania: string | null;
  panstwo: string;
  poczta: string;
  ulica: string;
}

interface Kontakt {
  emaile: string[];
  www: string;
}

interface KrsRejestry {
  rejestr_przedsiebiorcow_data_wpisu: string;
  rejestr_przedsiebiorcow_data_wykreslenia: string | null;
  rejestr_stowarzyszen_data_wpisu: string | null;
  rejestr_stowarzyszen_data_wykreslenia: string | null;
}

interface KrsWpisy {
  najnowszy_data: string;
  najnowszy_numer: number;
  najnowszy_przed_wykresleniem_data: string | null;
  najnowszy_przed_wykresleniem_numer: number | null;
  pierwszy_data: string;
  wykreslenie_uprawomocnienie_data: string | null;
}

interface KrsPowiazaniaLiczby {
  aktualne: number;
  przeszle: number;
}

interface Metadane {
  krs_odpis_synchronizacja_data_czas: string;
  krs_rozdzialy_dostepne: string[];
}

interface PodstawoweInformacje {
  id: number;
  nazwy: Nazwy;
  numery: Numery;
  stan: Stan;
  glowna_osoba: GlownaOsoba;
  adres: Adres;
  kontakt: Kontakt;
  krsRejestry: KrsRejestry;
  krs_wpisy: KrsWpisy;
  krs_powiazania_liczby: KrsPowiazaniaLiczby;
  metadane: Metadane;
  typ: string;
}

interface Plynnosc {
  PlynnoscBiezaca: number;
  PlynnoscSzybka: number;
  PlynnoscNatychmiastowa: number;
}

interface Zyski {
  ZyskOperacyjny: number;
  ZyskDzialanoscDodatkowa: number;
  ZyskFinansowy: number;
  ZyskOgolem: number;
}

interface Marze {
  MarzaBrutto: number;
  MarzaOperacyjna: number;
  MarzaNetto: number;
}

interface Rotacja {
  RotacjaZobowiazan: number;
  RotacjaNaleznosci: number;
}

interface Stany {
  StanGotowkiWKasie: number;
  SrodkiPieniezneOrazInneAktywaPieniezne: number;
  StanZapasow: number;
  StanNaleznosci: number;
  Towary: number;
  StanZobowiazan: number;
  KredytyPozyczkiKrotkoterminowe: number;
  KredytyPozyczkiDlugoterminowe: number;
  UdzielonePozyczkiKrotkoterminowe: number;
  UdzielonePozyczkiDlugoterminowe: number;
}

interface RaportFinansowySzczegoly {
  Rok: number;
  AktywaNetto: number;
  Przychody: number;
  Plynnosc: Plynnosc;
  Zyski: Zyski;
  Marze: Marze;
  Rotacja: Rotacja;
  Stany: Stany;
}

interface DokumentFinansowy {
  CzyMaJson: boolean;
  Id: number;
  Nazwa: string;
  Year: number;
}

interface FinancialReportData {
  RaportFinansowy: RaportFinansowySzczegoly[];
  ListaDokumentow: DokumentFinansowy[];
}
interface KRSReport {
  PodstawoweInformacje: PodstawoweInformacje;
  VatDane: any | null;
  PowiazaniaOrganizacji: any | null;
  RaportFinansowy: FinancialReportData | null;
  StatusOrganizacji: any | null;
  BeneficjenciRzeczywisci: any | null;
}

interface RaportAI {
  id: number;
  name: string;
  date: string;
}

interface FilterTemplate {
  id: string;
  name: string;
  selectedItems: string[];
  createdAt: string;
}

const STORAGE_KEY = 'filter_templates';
const { currentUser } = useAuth();
const selectedFile = ref<File | null>(null);
const krsReportData = ref<KRSReport | null>(null);
const isLoading = ref(false);
const error = ref<string | null>(null);

// ── Pobieranie JSON ─────────────────────────────────────────────────────────────────
const loadLocalFile = async () => {
  isLoading.value = true;
  error.value = null;
  krsReportData.value = null;

  try {
    const response = await fetch('../Files/dane.json')
    if (!response.ok) {
      throw Error(response.statusText);
    }
    const data: KRSReport = await response.json();
    console.log(data)
    krsReportData.value = data;

  } catch (error) {
    console.log(error);
  } finally {
    isLoading.value = false;
  }
};

const reports = ref<any[]>([]);
const selectedPdfUrl = ref<string | null>(null);

// ── Pobieranie raportu ─────────────────────────────────────────────────────────────────
async function downloadPdf() {
  const res = await fetch("http://localhost:5100/FinancialReport/exportPdf", {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify({
      report: krsReportDataFiltr.value,
      type: selectedFiltr.value
    })
  });

  if (!res.ok) {
    console.error(await res.text());
    return;
  }

  const blob = await res.blob();
  const url = URL.createObjectURL(blob);

  // ── Nazwa dla raportu ─────────────────────────────────────────────────────────────────
  const report = {
    id: Date.now(),
    name: "Raport "
        + filtrName.value
        + " "
        + krsReportDataFiltr.value?.PodstawoweInformacje?.nazwy?.skrocona
        + " " + Date.now(),    date: new Date().toLocaleString(),
    url
  };

  reports.value.unshift(report);
  selectedPdfUrl.value = url;
}

function downloadFile(report: any) {
  const a = document.createElement("a");
  a.href = report.url;
  a.download = report.name + ".pdf";
  a.click();
}

function deleteReport(id: number) {
  reports.value = reports.value.filter(r => r.id !== id);
}

onMounted(() => {
  loadLocalFile();

});

const krsReportAIRef = ref(null);
const raportAI = ref<string | null>(null);
const showReport = ref(false);
const wygenerowanyRaport = ref<string | null>(null);

// ── Wysylanie filtru do backendu ─────────────────────────────────────────────────────────────────
const generateAIReport = async () => {
  console.log("Dane otrzymane z backendu:", krsReportDataFiltr.value);
  console.log("Obiekt użytkownika:", currentUser.value);
  const payload = {
    report: krsReportDataFiltr.value,
    type: selectedFiltr.value,
    // DODAJ TO:
    userId: currentUser.value?.id,
    username: currentUser.value?.username,
    selectedItems: selectedItems.value
  };

  try {
    const response = await fetch("http://localhost:5100/FinancialReport/reportAnalyze", {
      method: "POST",
      headers: {"Content-Type": "application/json"},
      body: JSON.stringify(payload)
    });



    if (!response.ok) {
      console.log("Dane nie otrzymane z backendu:", krsReportDataFiltr.value);
      throw new Error(await response.text());
    }

    const data = await response.json()
    console.log("Dane otrzymane z backendu:", data);
    wygenerowanyRaport.value = data.markdown
    raportAI.value = data.markdown
    isLoading.value = false;



  } catch (err) {
    console.error(err);
    isLoading.value = false;
    error.value = "Nie udało się wygenerować raportu AI";
  }


};



// ── Szablon dla filtrow ─────────────────────────────────────────────────────────────────

const templates = ref<FilterTemplate[]>([]);
const szablonAlert = ref(false);

function loadTemplates() {
  const data = localStorage.getItem(STORAGE_KEY);
  templates.value = data ? JSON.parse(data) : [];
}

function saveTemplatesToStorage() {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(templates.value));
}


onMounted(() => {
  loadLocalFile();
  loadTemplates();
});

function saveCurrentTemplate() {
  if (selectedItems.value.length === 0) return;

  const newTemplate: FilterTemplate = {
    id: crypto.randomUUID(),
    name: `Szablon ${templates.value.length + 1}`,
    selectedItems: [...selectedItems.value],
    createdAt: new Date().toISOString()
  };

  templates.value.unshift(newTemplate);


  if (templates.value.length > 10) {
    templates.value = templates.value.slice(0, 10);
  }

  saveTemplatesToStorage();
  console.log("Szablon zapisany!");
  szablonAlert.value = true;
  setTimeout(() => {
    szablonAlert.value = false;
  }, 3000);

}

// ── Funkcje czyszczenia ─────────────────────────────────────────────────────────────────
function clearKrsData() {
  if (!krsReportDataFiltr.value?.PodstawoweInformacje) return;

  krsReportDataFiltr.value.PodstawoweInformacje.numery = null as any;
  krsReportDataFiltr.value.PodstawoweInformacje.krs_rejestry = null as any;
  krsReportDataFiltr.value.PodstawoweInformacje.krs_wpisy = null as any;
  krsReportDataFiltr.value.PodstawoweInformacje.krs_powiazania_liczby = null as any;
  krsReportDataFiltr.value.PodstawoweInformacje.metadane = null as any;
}

function clearFinancialData() {
  if (!krsReportDataFiltr.value?.RaportFinansowy) return;

  krsReportDataFiltr.value.RaportFinansowy = null;
}

// ── Ladowanie szablonow ─────────────────────────────────────────────────────────────────
function applyTemplate(template: FilterTemplate) {
  selectedItems.value = [...template.selectedItems];
  selectedFiltr.value = "custom";

  closeTwojeSzablony();
  showFilterRaportow();

  for (const key of allItems.value) {
    itemStates.value[key] = template.selectedItems.includes(key);
  }

  toggleKrsNmbers.value = template.selectedItems.some(k =>
      ["krs", "krsRejestry", "krsWpisy", "krsPowiazania", "metadane", "nip", "regon", "duns"].includes(k)
  );

  toggleRaportFinansowy.value = template.selectedItems.some(k =>
      ["aktywa", "zyski", "marze", "plynnosc", "rotacja", "stany", "dokumenty"].includes(k)
  );

  if (!toggleKrsNmbers.value) {
    clearKrsData();
  }

  if (!toggleRaportFinansowy.value) {
    clearFinancialData();
  }

  console.log("Załadowano szablon:", template);
}
function deleteTemplate(id: string) {
  templates.value = templates.value.filter(t => t.id !== id);
  saveTemplatesToStorage();
}


const editedName = ref('');
const editingId = ref<string | null>(null);

// ── Edytowanie szablonow ─────────────────────────────────────────────────────────────────
function startEdit(template: any) {

  editingId.value = template.id;
  editedName.value = '';
}

function saveEdit(template: any) {
  if (editedName.value.trim() !== '') {
    template.name = editedName.value.trim();
    saveTemplatesToStorage();
  }

  editingId.value = null;
}

// ── Glowna funkcja tworzenia raportu ─────────────────────────────────────────────────────────────────
function analyze() {
  wyciagnijDaneZJSON()
  closeFilterRaportow()
  downloadPdf()
  generateAIReport();
  showReport.value = true;
  isLoading.value = true;

}


const filtrRaportow = ref(false);
const zaciemnienieEkranu = ref(false);
const toggleKrsNmbers = ref(false);
const toggleRaportFinansowy = ref(false);
const twojeSzablony = ref(false);
function showFilterRaportow() {
  filtrRaportow.value = true;
}

function showTwojeSzablony() {
  twojeSzablony.value = true;
}

function closeFilterRaportow() {
  if(toggleKrsNmbers.value == true || toggleRaportFinansowy.value) {
    toggleKrsNmbers.value = false;
    toggleRaportFinansowy.value = false;
    filtrRaportow.value = false;

  }
  else{
    filtrRaportow.value = false;

  }
}

function closeTwojeSzablony() {
  twojeSzablony.value = false;
}

function openKrsNumbers (){
  toggleKrsNmbers.value = !toggleKrsNmbers.value;
}

function openRaportFinansowy () {
  toggleRaportFinansowy.value = !toggleRaportFinansowy.value;
}



// ── Nazwy filtow ─────────────────────────────────────────────────────────────────

const selectedItems = ref<string[]>([]);
const allItems = ref<string[]>(["nazwyImiona", "adresy", "kontakty", "stanSpolki", "statusyPrawne",
  "nip", "duns","krs","krsRejestry", "krsWpisy", "krsPowiazania","metadane","regon",
  "raporty","aktywa", "plynnosc", "zyski","marze","rotacja","stany","dokumenty","listaSankcyjna"]);
const itemStates = ref<{ [key: string]: boolean }>({});
const allSelected = computed(() => {
  return allItems.value.every(key => itemStates.value[key])
})

// ── Dodawanie osobne i cale ─────────────────────────────────────────────────────────────────

function toggleItem(key: string, checked: boolean) {
  itemStates.value[key] = checked;
  if (checked) {
    selectedItems.value.push(key);
  } else {
    selectedItems.value = selectedItems.value.filter(i => i !== key);
    if(key == "krs"){
      selectedItems.value = selectedItems.value.filter(i => i !== "krsRejestry");
      selectedItems.value = selectedItems.value.filter(i => i !== "krsWpisy");
      selectedItems.value = selectedItems.value.filter(i => i !== "krsPowiazania");
      selectedItems.value = selectedItems.value.filter(i => i !== "metadane");
    }
    if(key == "raporty"){
      selectedItems.value = selectedItems.value.filter(i => i !== "aktywa");
      selectedItems.value = selectedItems.value.filter(i => i !== "plynnosc");
      selectedItems.value = selectedItems.value.filter(i => i !== "zyski");
      selectedItems.value = selectedItems.value.filter(i => i !== "marze");
      selectedItems.value = selectedItems.value.filter(i => i !== "rotacja");
    }
  }
  console.log('Selected Items:', selectedItems.value);
}

function toggleAll(checked: boolean) {
  selectedItems.value = [];
  if(checked) {
    toggleKrsNmbers.value = true;
    toggleRaportFinansowy.value = true;
  }
  else{
    toggleKrsNmbers.value = false;
    toggleRaportFinansowy.value = false;

  }


  for (const key of allItems.value) {
    itemStates.value[key] = checked;
    if (checked) {
      selectedItems.value.push(key);
    }
  }
  console.log('Selected Items after toggleAll:', selectedItems.value);

}

const showAlert = ref(false);

// ── Nowy interface dla wybranych filtrow ─────────────────────────────────────────────────────────────────
const krsReportDataFiltr = ref<KRSReport | null>(null);


// ── Podstawa danych dla nowego interfacu  ─────────────────────────────────────────────────────────────────
function wyciagnijDaneZJSON() {
  krsReportDataFiltr.value = null;
  krsReportDataFiltr.value = {
    PodstawoweInformacje: {} as PodstawoweInformacje,
    VatDane: null,
    PowiazaniaOrganizacji: null,
    RaportFinansowy: {} as FinancialReportData,
    StatusOrganizacji: null,
    BeneficjciRzeczywisci: null,
    ListaSankcyjna: false,
  };
  if(krsReportData.value) {
    krsReportDataFiltr.value = krsReportData.value;
  }

  if (!krsReportDataFiltr.value.PodstawoweInformacje) {
    krsReportDataFiltr.value.PodstawoweInformacje = {} as PodstawoweInformacje;
  }

  if (!krsReportDataFiltr.value.RaportFinansowy) {
    krsReportDataFiltr.value.RaportFinansowy = {} as FinancialReportData;
  }
  if(!krsReportDataFiltr.value.PodstawoweInformacje.numery){
    krsReportDataFiltr.value.PodstawoweInformacje.numery = {};
  }

  if (!krsReportData.value) {
    console.log('Oryginalne dane KRS nie zostały jeszcze załadowane.');
    return;
  }



  for (let i = 0; i < selectedItems.value.length; i++) {
    if (selectedItems.value[i] === "nazwyImiona") {
      if(krsReportData.value) {
        krsReportDataFiltr.value.PodstawoweInformacje.nazwy = krsReportData.value.PodstawoweInformacje.nazwy
        krsReportDataFiltr.value.PodstawoweInformacje.glowna_osoba = krsReportData.value.PodstawoweInformacje.glowna_osoba



      }
    }
    if (selectedItems.value[i] === "adresy") {
      if(krsReportData.value) {
        krsReportDataFiltr.value.PodstawoweInformacje.adres = krsReportData.value.PodstawoweInformacje.adres
      }
    }
    if (selectedItems.value[i] === "kontakty") {
      if(krsReportData.value) {
        krsReportDataFiltr.value.PodstawoweInformacje.kontakt = krsReportData.value.PodstawoweInformacje.kontakt

      }

    }
    if (selectedItems.value[i] === "stanSpolki") {
      if(krsReportData.value) {
        krsReportDataFiltr.value.PodstawoweInformacje.stan = krsReportData.value.PodstawoweInformacje.stan
      }
    }
    if (selectedItems.value[i] === "statusyPrawne") {
      if(krsReportData.value) {
        if(krsReportData.value.StatusOrganizacji !== null) {
          krsReportDataFiltr.value.StatusOrganizacji = krsReportData.value.StatusOrganizacji
        }
        else{

          krsReportDataFiltr.value.StatusOrganizacji = null
        }

      }

    }
    if (selectedItems.value[i] === "nip") {
      if(krsReportData.value) {
        krsReportDataFiltr.value.PodstawoweInformacje.numery.nip = krsReportData.value.PodstawoweInformacje.numery.nip
      }

    }
    if (selectedItems.value[i] === "duns") {
      if(krsReportData.value) {
        krsReportDataFiltr.value.PodstawoweInformacje.numery.duns = krsReportData.value.PodstawoweInformacje.numery.duns
      }

    }
    if (selectedItems.value[i] === "krs") {
      if(krsReportData.value) {
        krsReportDataFiltr.value.PodstawoweInformacje.numery.krs = krsReportData.value.PodstawoweInformacje.numery.krs


      }
    }
    if (selectedItems.value[i] === "krsRejestry") {
      if(krsReportData.value) {
        krsReportDataFiltr.value.PodstawoweInformacje.krs_rejestry = krsReportData.value.PodstawoweInformacje.krs_rejestry

      }
    }
    if (selectedItems.value[i] === "krsWpisy") {
      if(krsReportData.value) {
        krsReportDataFiltr.value.PodstawoweInformacje.krs_wpisy = krsReportData.value.PodstawoweInformacje.krs_wpisy

      }
    }
    if (selectedItems.value[i] === "krsPowiazania") {
      if(krsReportData.value) {
        krsReportDataFiltr.value.PodstawoweInformacje.krs_powiazania_liczby = krsReportData.value.PodstawoweInformacje.krs_powiazania_liczby

      }
    }
    if (selectedItems.value[i] === "metadane") {
      if(krsReportData.value) {
        krsReportDataFiltr.value.PodstawoweInformacje.metadane = krsReportData.value.PodstawoweInformacje.metadane

      }
    }

    if (selectedItems.value[i] === "regon") {
      if(krsReportData.value) {
        krsReportDataFiltr.value.PodstawoweInformacje.numery.regon = krsReportData.value.PodstawoweInformacje.numery.regon
      }
    }
    if (selectedItems.value[i] === "dokumenty") {
      if(krsReportData.value) {
        krsReportDataFiltr.value.RaportFinansowy.ListaDokumentow = krsReportData.value.RaportFinansowy.ListaDokumentow
      }
    }
    if (selectedItems.value[i] === "listaSankcyjna") {
      if(krsReportData.value) {
        krsReportDataFiltr.value.ListaSankcyjna = true;
      }
    }

    if (krsReportData.value) {
      krsReportDataFiltr.value.RaportFinansowy.RaportFinansowy =
          krsReportData.value.RaportFinansowy.RaportFinansowy.map(el => {

            const obj: any = {};


            if (
                selectedItems.value.includes("aktywa") ||
                selectedItems.value.includes("zyski") ||
                selectedItems.value.includes("marze") ||
                selectedItems.value.includes("plynnosc") ||
                selectedItems.value.includes("rotacja") ||
                selectedItems.value.includes("stany")
            ) {
              obj.Rok = el.Rok;
            }

            if (selectedItems.value.includes("aktywa")) {
              obj.AktywaNetto = el.AktywaNetto;
              obj.Przychody = el.Przychody;
            }

            if (selectedItems.value.includes("zyski")) {
              obj.Zyski = el.Zyski;
            }

            if (selectedItems.value.includes("marze")) {
              obj.Marze = el.Marze;
            }

            if (selectedItems.value.includes("rotacja")) {
              obj.Rotacja = el.Rotacja;
            }
            if (selectedItems.value.includes("plynnosc")) {
              obj.Plynnosc = el.Plynnosc;
            }
            if (selectedItems.value.includes("stany")) {
              obj.Stany = el.Stany
            }

            return obj;
          });
    }
  }
  if(!krsReportDataFiltr.value) {
    console.log("Nic nie wybrano")
  }
  else{
    console.log(krsReportDataFiltr.value);
  }
  /*
   showAlert.value = true; 
  setTimeout(() => {
    showAlert.value = false; 
  }, 3000);
  */


}

// ── Osobne filtry raportow (prawnik, finansista, itd) ─────────────────────────────────────────────────────────────────

const isOpenedRaportList = ref(false)

function OpenRaportList(){
  isOpenedRaportList.value = !isOpenedRaportList.value
}

const filtrName = ref<string>("custom");
const selectedFiltr = ref<string>("custom");

const reportTemplates: Record<string, string[]> = {
  Finance: [
    "nazwyImiona",
    "raporty",
    "aktywa",
    "zyski",
    "marze",
    "plynnosc",
    "rotacja",
    "stany",
    "dokumenty"
  ],
  Prawnik: [
    "nazwyImiona",
    "adresy",
    "statusyPrawne",
    "krs",
    "krsRejestry",
    "krsWpisy",
    "krsPowiazania",
    "metadane",
    "nip",
    "regon",
    "listaSankcyjna"
  ],
  Sales: [
    "nazwyImiona",
    "adresy",
    "kontakty",
    "raporty",
    "aktywa",
    "zyski"
  ],
  custom: []
};

function applyFilterTemplate(type: string) {
  const template = reportTemplates[type];
  if (!template) return;

  selectedItems.value = [];

  for (const key of allItems.value) {
    const isSelected = template.includes(key);
    itemStates.value[key] = isSelected;

    if (isSelected) {
      selectedItems.value.push(key);
    }
  }

  toggleKrsNmbers.value = template.some(k =>
      ["krs", "krsRejestry", "krsWpisy", "krsPowiazania", "metadane", "nip", "regon", "duns"].includes(k)
  );

  toggleRaportFinansowy.value = template.some(k =>
      ["aktywa", "zyski", "marze", "plynnosc", "rotacja", "stany", "dokumenty"].includes(k)
  );

  if (!krsReportDataFiltr.value) {
    return;
  }

  if (!toggleKrsNmbers.value) {
    clearKrsData();
  }

  if (!toggleRaportFinansowy.value) {
    clearFinancialData();
  }

  console.log("Zastosowano szablon:", type, selectedItems.value);
}

watch(selectedFiltr, (newValue) => {
  applyFilterTemplate(newValue);
  generateReport(newValue);
});

// ── Generowanie raprtu w zaleznosci od wybranego filtry ─────────────────────────────────────────────────────────────────

function generateReport(type: string) {
  switch (type) {
    case "Finance":
      filtrName.value = "Finance";
      generateFinanceReport();
      break;

    case "Prawnik":
      filtrName.value = "Prawnik";
      generateLegalReport();
      break;

    case "Sales":
      filtrName.value = "Sales";
      generateSalesReport();
      break;

    default:
      filtrName.value = "custom";
      generateCustomReport();
  }
}

function generateFinanceReport() {

  console.log("Raport finansowy...");

}

function generateLegalReport() {
  console.log("Raport prawny...");
}

function generateSalesReport() {
  console.log("Raport sprzedażowy...");
}

function generateCustomReport() {
  console.log("Raport własny...");
}








</script>

<template>
  <div class="flex h-screen flex-col bg-surface-900 text-surface-0 static z-40  ">
    <div class="flex flex-1 overflow-hidden ">

      <!-- Lewa sekcja -->
      <section class="sidebar overflow-y-auto rounded-r-rm border-y rounded-xl
    border-r bg-surface-800 border-surface-700 transition-all duration-300 w-60 mt-4 mb-4 mt-2 p-2">
        <!-- 1. HOME -->
        <router-link
            to="/"
            class="flex items-center w-full rounded-lg transition-all duration-200 px-3 py-2.5 my-1 gap-3 text-surface-300 hover:bg-surface-700/40 hover:text-surface-100"
            active-class="bg-primary-500/15 text-primary-400 font-bold"
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="20" height="20" fill="currentColor" class="shrink-0"><path d="M168,112a56,56,0,1,1-56-56A56,56,0,0,1,168,112Zm61.66,117.66a8,8,0,0,1-11.32,0l-50.06-50.07a88,88,0,1,1,11.32-11.31l50.06,50.06A8,8,0,0,1,229.66,229.66ZM112,184a72,72,0,1,0-72-72A72.08,72.08,0,0,0,112,184Z"></path></svg>
          <span class="text-sm font-medium">Home</span>
        </router-link>

        <!-- 2. PANEL ADMINA -->
        <router-link
            v-if="currentUser?.role === 'admin'"
            to="/admin"
            class="flex items-center w-full rounded-lg transition-all duration-200 px-3 py-2.5 my-1 gap-3 text-surface-300 hover:bg-surface-700/40 hover:text-surface-100"
            active-class="bg-primary-500/15 text-primary-400 font-bold"
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="20" height="20" fill="currentColor" class="shrink-0"><path d="M208,40H48A16,16,0,0,0,32,56V200a16,16,0,0,0,16,16H208a16,16,0,0,0,16-16V56A16,16,0,0,0,208,40Zm0,160H48V56H208V200Zm-32-56a8,8,0,0,1-8,8H88a8,8,0,0,1,0-16h80A8,8,0,0,1,176,144Zm0-32a8,8,0,0,1-8,8H88a8,8,0,0,1,0-16h80A8,8,0,0,1,176,112Z"></path></svg>
          <span class="text-sm font-medium">Panel Admina</span>
        </router-link>
        <!-- 3. KONTO-->
        <router-link
            v-if="currentUser"
            to="/my-account"
            class="flex items-center w-full rounded-lg transition-all duration-200 px-3 py-2.5 my-1 gap-3 text-surface-300 hover:bg-surface-700/40 hover:text-surface-100"
            active-class="bg-primary-500/15 text-primary-400 font-bold"
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="20" height="20" fill="currentColor" class="shrink-0">
            <path d="M128,24A104,104,0,1,0,232,128,104.11,104.11,0,0,0,128,24ZM74.08,197.5a64,64,0,0,1,107.84,0,87.83,87.83,0,0,1-107.84,0ZM128,120a32,32,0,1,1,32-32A32,32,0,0,1,128,120Z"></path>
          </svg>
          <span class="text-sm font-medium">Moje Konto</span>
        </router-link>
        <!-- 4. STWÓRZ RAPORT -->
        <router-link
            to="/report"
            class="flex items-center w-full rounded-lg transition-all duration-200 cursor-pointer px-3 py-2.5 my-1 gap-3 text-surface-300 hover:bg-surface-700/40 hover:text-surface-100"
            active-class="bg-primary-500/15 text-primary-400 font-bold"
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="20" height="20" fill="currentColor" class="shrink-0"><path d="M224,152a8,8,0,0,1-8,8H192v16h16a8,8,0,0,1,0,16H192v16a8,8,0,0,1-16,0V152a8,8,0,0,1,8-8h32A8,8,0,0,1,224,152ZM92,172a28,28,0,0,1-28,28H56v8a8,8,0,0,1-16,0V152a8,8,0,0,1,8-8H64A28,28,0,0,1,92,172Zm-16,0a12,12,0,0,0-12-12H56v24h8A12,12,0,0,0,76,172Zm88,8a36,36,0,0,1-36,36H112a8,8,0,0,1-8-8V152a8,8,0,0,1,8-8h16A36,36,0,0,1,164,180Zm-16,0a20,20,0,0,0-20-20h-8v40h8A20,20,0,0,0,148,180ZM40,112V40A16,16,0,0,1,56,24h96a8,8,0,0,1,5.66,2.34l56,56A8,8,0,0,1,216,88v24a8,8,0,0,1-16,0V96H152a8,8,0,0,1-8-8V40H56v72a8,8,0,0,1-16,0ZM160,80h28.69L160,51.31Z"></path></svg>
          <span class="text-sm font-medium">Stwórz raport</span>
        </router-link>
        <!-- 5. POWIĄZANIA -->
        <router-link
            to="/connections"
            class="flex items-center w-full rounded-lg transition-all duration-200 cursor-pointer px-3 py-2.5 my-1 gap-3 text-surface-300 hover:bg-surface-700/40 hover:text-surface-100"
            active-class="bg-primary-500/15 text-primary-400 font-bold"
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="20" height="20" fill="currentColor" class="shrink-0"><path d="M200,144a31.84,31.84,0,0,0-19.53,6.68l-46.19-33a32.17,32.17,0,0,0,0-19.38l46.19-33A32,32,0,1,0,168,48a31.84,31.84,0,0,0,19.53,6.68L141.34,87.64a32,32,0,1,0,0,80.72l46.19,33A31.84,31.84,0,0,0,168,208a32,32,0,1,0,32-64Zm0-128a16,16,0,1,1-16,16A16,16,0,0,1,200,16ZM96,144a16,16,0,1,1,16-16A16,16,0,0,1,96,144Zm104,80a16,16,0,1,1,16-16A16,16,0,0,1,200,224Z"></path></svg>
          <span class="text-sm font-medium">Powiązania</span>
        </router-link>
      </section>

      <!-- Glowna sekcja-->
      <main class="flex flex-col flex-1  overflow-hidden pl-2 pr-2 pb-2 mb-2 ">
        <div class="flex overflow-hidden  mb-4 items-end justify-end">

          <div class="">


            <!-- Okienko filtrow -->

            <Transition
                enter-active-class="transition duration-300"
                enter-from-class="opacity-0"
                enter-to-class="opacity-100"
                leave-active-class="transition duration-200"
                leave-from-class="opacity-100"
                leave-to-class="opacity-0"
            >
              <div
                  v-if="filtrRaportow"
                  class="fixed inset-0 bg-black/40 z-50 text-dark-filter"
                  @click="closeFilterRaportow"
              ></div>
            </Transition>


            <Transition
                enter-active-class="transition transform duration-300"
                enter-from-class="translate-x-full"
                enter-to-class="translate-x-0"
                leave-active-class="transition transform duration-300"
                leave-from-class="translate-x-0"
                leave-to-class="translate-x-full"
            >
              <div
                  v-if="filtrRaportow"
                  class="fixed right-0 top-0 w-86 h-full bg-surface-950 z-50 flex flex-col"
              >
                <div class="flex  pl-4 h-32 gap-28 items-center justify-center mb">
                  <div class="flex-1 text-4xl text-surface-300">Filtry tworzenia</div>

                  <div class="flex-1">
                    <button class="text-5xl text-surface-300"

                            @click="closeFilterRaportow"
                    >&times;

                    </button>
                  </div>
                </div>
                <div class="flex flex-col justify-between flex-1 overflow-y-auto">
                  <div class=" mx-4 mb-4">

                    <!-- Select dla filtrow -->

                    <form class="">
                      <label for="chooseFiltr" class="block mb-2.5 text-l font-medium text-heading">Wybierz typ raportu:</label>
                      <select id="chooseFiltr"
                              v-model="selectedFiltr"
                              class="block w-full h-14 px-2 py-2.5  border 
                              border-surface-600 text-heading text-l text-surface-300 rounded-base 
                              focus:ring-brand focus:border-brand shadow-xs placeholder:text-body">
                        <option selected>Wybierz typ</option>
                        <option value="Custom">Własny</option>
                        <option value="Finance">Finansista</option>
                        <option value="Prawnik">Prawnik</option>
                        <option value="Sales">Handlowiec</option>
                      </select>
                    </form>
                  </div>
                  <div class="flex gap-4 pl-4 h-14 mb-8 mr-4 ">

                    <div class="flex flex-1  p-1 justify-between rounded border text-surface-300 
                    border-surface-600 hover:bg-surface-700 transition duration-300  
                    cursor-pointer select-none items-center justify-center">

                      <label class="flex items-center">
                        <div class="w-1/4 flex-none ml-4 items-center justify-center ">
                          <span class="pi pi-check"></span>
                        </div>
                        <div class="flex-1 text-l">
                          Zaznacz wszystko
                          <input
                              type="checkbox"
                              @change="toggleAll(true)"
                              :checked="!allItems.some(key => itemStates[key]) && allItems.length > 0"
                              class="hidden peer"
                              :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                          >

                        </div>
                      </label>

                    </div>
                    <div class="flex flex-1  p-1 justify-between rounded border text-surface-300 
                    border-surface-600 hover:bg-surface-700 transition duration-300  
                    cursor-pointer select-none items-center justify-center">

                      <label class="flex items-center">
                        <div class="w-1/4 flex-none ml-4 items-center justify-center ">
                          <span class="pi pi-times"></span>
                        </div>
                        <div class="flex-1 text-l">
                          Odznacz wszystko
                          <input
                              type="checkbox"
                              @change="toggleAll(false)"
                              :checked="!allItems.some(key => itemStates[key]) && allItems.length > 0"
                              class="hidden peer"
                              :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)">
                        </div>
                      </label>
                    </div>
                  </div>

                  <!-- Sekcja filtrow -->

                  <div class="flex flex-col mb-6">
                    <div class="flex flex-col gap-4 pl-4 pb-8 border-b border-surface-600">
                      <div class="text-2xl">Podstawowe informacje</div>
                      <label class="flex items-end w-full cursor-pointer">
                        <div class="flex-1 text-l ">
                          <span >Nazwy oraz imiona </span>
                        </div>
                        <div class="w-16 flex-none r" >
                          <input
                              type="checkbox"
                              v-model="itemStates.nazwyImiona"
                              @change="toggleItem('nazwyImiona', itemStates.nazwyImiona)"
                              class="sr-only peer"
                              :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                          >
                          <div class="outline-none relative w-13 h-7 bg-surface-600
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[4px] 
                        after:start-[6px] after:bg-white after:rounded-full after:h-5 after:w-5 
                        after:transition-all peer-checked:bg-primary-500"></div>
                        </div>
                      </label>
                      <label v-if="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"  class="flex items-end w-full cursor-pointer">
                        <div class="flex-1 text-l ">
                          <span>Adresy</span>
                        </div>
                        <div class="w-16 flex-none r">
                          <input
                              type="checkbox"
                              v-model="itemStates.adresy"
                              @change="toggleItem('adresy', itemStates.adresy)"
                              class="sr-only peer"
                              checked disabled
                              :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                          >
                          <div class="outline-none relative w-13 h-7 bg-surface-600
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[4px] 
                        after:start-[6px] after:bg-white after:rounded-full after:h-5 after:w-5 
                        after:transition-all peer-checked:bg-primary-500"></div>
                        </div>
                      </label>
                      <label  v-else class="flex items-end w-full cursor-pointer">
                        <div class="flex-1 text-l ">
                          <span>Adresy</span>
                        </div>
                        <div class="w-16 flex-none r">
                          <input
                              type="checkbox"
                              v-model="itemStates.adresy"
                              @change="toggleItem('adresy', itemStates.adresy)"
                              class="sr-only peer"
                              :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                          >
                          <div class="outline-none relative w-13 h-7 bg-surface-600
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[4px] 
                        after:start-[6px] after:bg-white after:rounded-full after:h-5 after:w-5 
                        after:transition-all peer-checked:bg-primary-500"></div>
                        </div>
                      </label>
                      <label class="flex items-end w-full cursor-pointer">
                        <div class="flex-1 text-l ">
                          <span>Kontakty</span>
                        </div>
                        <div class="w-16 flex-none r">
                          <input
                              type="checkbox"
                              v-model="itemStates.kontakty"
                              @change="toggleItem('kontakty', itemStates.kontakty)"
                              class="sr-only peer"
                              :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                          >
                          <div class="outline-none relative w-13 h-7 bg-surface-600
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[4px] 
                        after:start-[6px] after:bg-white after:rounded-full after:h-5 after:w-5 
                        after:transition-all peer-checked:bg-primary-500"></div>
                        </div>
                      </label>
                      <label class="flex items-end w-full cursor-pointer">
                        <div class="flex-1 text-l ">
                          <span>Stan Spółki</span>
                        </div>
                        <div class="w-16 flex-none r">
                          <input
                              type="checkbox"
                              v-model="itemStates.stanSpolki"
                              @change="toggleItem('stanSpolki', itemStates.stanSpolki)"
                              class="sr-only peer"
                              :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                          >
                          <div class="outline-none relative w-13 h-7 bg-surface-600
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[4px] 
                        after:start-[6px] after:bg-white after:rounded-full after:h-5 after:w-5 
                        after:transition-all peer-checked:bg-primary-500"></div>
                        </div>
                      </label>
                      <label class="flex items-end w-full cursor-pointer">
                        <div class="flex-1 text-l ">
                          <span>Statusy prawne</span>
                        </div>
                        <div class="w-16 flex-none r">
                          <input
                              type="checkbox"
                              v-model="itemStates.statusyPrawne"
                              @change="toggleItem('statusyPrawne', itemStates.statusyPrawne)"
                              class="sr-only peer"
                              :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                          >
                          <div class="outline-none relative w-13 h-7 bg-surface-600
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[4px] 
                        after:start-[6px] after:bg-white after:rounded-full after:h-5 after:w-5 
                        after:transition-all peer-checked:bg-primary-500"></div>
                        </div>
                      </label>
                    </div>
                  </div>
                  <div class="flex flex-col mb-6">
                    <div class="flex flex-col gap-4 pl-4 pb-8 border-b border-surface-600">
                      <div class="text-2xl">Numery</div>
                      <label class="flex items-end w-full cursor-pointer">
                        <div class="flex-1 text-l ">
                          <span >NIP</span>
                        </div>
                        <div class="w-16 flex-none r">
                          <input
                              type="checkbox"
                              v-model="itemStates.nip"
                              @change="toggleItem('nip', itemStates.nip)"
                              class="sr-only peer"
                              :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                          >
                          <div class="outline-none relative w-13 h-7 bg-surface-600
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[4px] 
                        after:start-[6px] after:bg-white after:rounded-full after:h-5 after:w-5 
                        after:transition-all peer-checked:bg-primary-500"></div>
                        </div>
                      </label>
                      <label class="flex items-end w-full cursor-pointer">
                        <div class="flex-1 text-l ">
                          <span >DUNS</span>
                        </div>
                        <div class="w-16 flex-none r">
                          <input
                              type="checkbox"
                              v-model="itemStates.duns"
                              @change="toggleItem('duns', itemStates.duns)"
                              class="sr-only peer"
                              :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                          >
                          <div class="outline-none relative w-13 h-7 bg-surface-600
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[4px] 
                        after:start-[6px] after:bg-white after:rounded-full after:h-5 after:w-5 
                        after:transition-all peer-checked:bg-primary-500"></div>
                        </div>
                      </label>
                      <label  class="flex items-end w-full cursor-pointer">
                        <div  class="flex-1 text-l ">
                          <span >KRS</span>
                        </div>
                        <div  class="w-16 flex-none r">
                          <input
                              @click="openKrsNumbers"
                              type="checkbox"
                              v-model="itemStates.krs"
                              @change="toggleItem('krs', itemStates.krs)"
                              class="sr-only peer"
                              :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)">
                          <div class="outline-none relative w-13 h-7 bg-surface-600
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[4px] 
                        after:start-[6px] after:bg-white after:rounded-full after:h-5 after:w-5 
                        after:transition-all peer-checked:bg-primary-500"></div>
                        </div>
                      </label>
                      <div v-if="toggleKrsNmbers" class="flex flex-col border-b text-xs pb-4 gap-2" >
                        <label  class="flex items-end w-full cursor-pointer">
                          <div class="flex-1  ">
                            <span >KRS rejestry</span>
                          </div>
                          <div class="w-16 flex-none">
                            <input
                                type="checkbox"
                                v-model="itemStates.krsRejestry"
                                @change="toggleItem('krsRejestry', itemStates.krsRejestry)"
                                class="sr-only peer"
                                :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                            >
                            <div class="outline-none relative w-9.5 h-[21px] bg-surface-300
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[2.5px] 
                        after:start-[3px] after:bg-white after:rounded-full after:h-4 after:w-4 
                        after:transition-all peer-checked:bg-primary-900"></div>
                          </div>
                        </label>
                        <label  class="flex items-end w-full cursor-pointer">
                          <div class="flex-1  ">
                            <span >KRS wpisy</span>
                          </div>
                          <div class="w-16 flex-none">
                            <input
                                type="checkbox"
                                v-model="itemStates.krsWpisy"
                                @change="toggleItem('krsWpisy', itemStates.krsWpisy)"
                                class="sr-only peer"
                                :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                            >
                            <div class="outline-none relative w-9.5 h-[21px] bg-surface-300
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[2.5px] 
                        after:start-[3px] after:bg-white after:rounded-full after:h-4 after:w-4 
                        after:transition-all peer-checked:bg-primary-900"></div>
                          </div>
                        </label>
                        <label  class="flex items-end w-full cursor-pointer">
                          <div class="flex-1  ">
                            <span >KRS powiązania</span>
                          </div>
                          <div class="w-16 flex-none">
                            <input
                                type="checkbox"
                                v-model="itemStates.krsPowiazania"
                                @change="toggleItem('krsPowiazania', itemStates.krsPowiazania)"
                                class="sr-only peer"
                                :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                            >
                            <div class="outline-none relative w-9.5 h-[21px] bg-surface-300
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[2.5px] 
                        after:start-[3px] after:bg-white after:rounded-full after:h-4 after:w-4 
                        after:transition-all peer-checked:bg-primary-900"></div>
                          </div>
                        </label>
                        <label  class="flex items-end w-full cursor-pointer">
                          <div class="flex-1  ">
                            <span >Metadane</span>
                          </div>
                          <div class="w-16 flex-none">
                            <input
                                type="checkbox"
                                v-model="itemStates.metadane"
                                @change="toggleItem('metadane', itemStates.metadane)"
                                class="sr-only peer"
                                :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                            >
                            <div class="outline-none relative w-9.5 h-[21px] bg-surface-300
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[2.5px] 
                        after:start-[3px] after:bg-white after:rounded-full after:h-4 after:w-4 
                        after:transition-all peer-checked:bg-primary-900"></div>
                          </div>
                        </label>
                      </div>

                      <label class="flex items-end w-full cursor-pointer">
                        <div class="flex-1 text-l ">
                          <span >Regon</span>
                        </div>
                        <div class="w-16 flex-none r">
                          <input
                              type="checkbox"
                              v-model="itemStates.regon"
                              @change="toggleItem('regon', itemStates.regon)"
                              class="sr-only peer"
                              :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                          >
                          <div class="outline-none relative w-13 h-7 bg-surface-600
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[4px] 
                        after:start-[6px] after:bg-white after:rounded-full after:h-5 after:w-5 
                        after:transition-all peer-checked:bg-primary-500"></div>
                        </div>
                      </label>
                    </div>
                  </div>
                  <div class="flex flex-col mb-6">
                    <div class="flex flex-col gap-4 pl-4 pb-8 border-b border-surface-600">
                      <div class="text-2xl">Raporty</div>
                      <label  class="flex items-end w-full cursor-pointer">
                        <div  class="flex-1 text-l ">
                          <span >Raporty finansowe z </span>
                          <span >ostatnich lat</span>
                        </div>
                        <div  class="w-16 flex-none r">
                          <input
                              @click="openRaportFinansowy"
                              type="checkbox"
                              v-model="itemStates.raporty"
                              @change="toggleItem('raporty', itemStates.raporty)"
                              class="sr-only peer"
                              :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)">
                          <div class="outline-none relative w-13 h-7 bg-surface-600
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[4px] 
                        after:start-[6px] after:bg-white after:rounded-full after:h-5 after:w-5 
                        after:transition-all peer-checked:bg-primary-500"></div>
                        </div>
                      </label>
                      <div v-if="toggleRaportFinansowy" class="flex flex-col border-b text-xs pb-4 gap-2" >
                        <label  class="flex items-end w-full cursor-pointer">
                          <div class="flex-1  ">
                            <span >Aktywa Netto oraz Przychody</span>
                          </div>
                          <div class="w-16 flex-none">
                            <input
                                type="checkbox"
                                v-model="itemStates.aktywa"
                                @change="toggleItem('aktywa', itemStates.aktywa)"
                                class="sr-only peer"
                                :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                            >
                            <div class="outline-none relative w-9.5 h-[21px] bg-surface-300
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[2.5px] 
                        after:start-[3px] after:bg-white after:rounded-full after:h-4 after:w-4 
                        after:transition-all peer-checked:bg-primary-900"></div>
                          </div>
                        </label>
                        <label  class="flex items-end w-full cursor-pointer">
                          <div class="flex-1  ">
                            <span>Płynność</span>
                          </div>
                          <div class="w-16 flex-none">
                            <input
                                type="checkbox"
                                v-model="itemStates.plynnosc"
                                @change="toggleItem('plynnosc', itemStates.plynnosc)"
                                class="sr-only peer"
                                :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                            >
                            <div class="outline-none relative w-9.5 h-[21px] bg-surface-300
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[2.5px] 
                        after:start-[3px] after:bg-white after:rounded-full after:h-4 after:w-4 
                        after:transition-all peer-checked:bg-primary-900"></div>
                          </div>
                        </label>
                        <label  class="flex items-end w-full cursor-pointer">
                          <div class="flex-1  ">
                            <span >Zyski</span>
                          </div>
                          <div class="w-16 flex-none">
                            <input
                                type="checkbox"
                                v-model="itemStates.zyski"
                                @change="toggleItem('zyski', itemStates.zyski)"
                                class="sr-only peer"
                                :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                            >
                            <div class="outline-none relative w-9.5 h-[21px] bg-surface-300
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[2.5px] 
                        after:start-[3px] after:bg-white after:rounded-full after:h-4 after:w-4 
                        after:transition-all peer-checked:bg-primary-900"></div>
                          </div>
                        </label>
                        <label  class="flex items-end w-full cursor-pointer">
                          <div class="flex-1  ">
                            <span >Marże</span>
                          </div>
                          <div class="w-16 flex-none">
                            <input
                                type="checkbox"
                                v-model="itemStates.marze"
                                @change="toggleItem('marze', itemStates.marze)"
                                class="sr-only peer"
                                :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                            >
                            <div class="outline-none relative w-9.5 h-[21px] bg-surface-300
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[2.5px] 
                        after:start-[3px] after:bg-white after:rounded-full after:h-4 after:w-4 
                        after:transition-all peer-checked:bg-primary-900"></div>
                          </div>
                        </label>
                        <label  class="flex items-end w-full cursor-pointer">
                          <div class="flex-1  ">
                            <span >Rotacja</span>
                          </div>
                          <div class="w-16 flex-none">
                            <input
                                type="checkbox"
                                v-model="itemStates.rotacja"
                                @change="toggleItem('rotacja', itemStates.rotacja)"
                                class="sr-only peer"
                                :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                            >
                            <div class="outline-none relative w-9.5 h-[21px] bg-surface-300
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[2.5px] 
                        after:start-[3px] after:bg-white after:rounded-full after:h-4 after:w-4 
                        after:transition-all peer-checked:bg-primary-900"></div>
                          </div>
                        </label>
                        <label class="flex items-end w-full cursor-pointer">
                          <div class="flex-1 text-l ">
                            <span >Stany</span>
                          </div>
                          <div class="w-16 flex-none r">
                            <input
                                type="checkbox"
                                v-model="itemStates.stany"
                                @change="toggleItem('stany', itemStates.stany)"
                                class="sr-only peer"
                                :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                            >
                            <div class="outline-none relative w-9.5 h-[21px] bg-surface-300
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[2.5px] 
                        after:start-[3px] after:bg-white after:rounded-full after:h-4 after:w-4 
                        after:transition-all peer-checked:bg-primary-900"></div>
                          </div>
                        </label>
                      </div>



                      <label class="flex items-end w-full cursor-pointer">
                        <div class="flex-1 text-l ">
                          <span >Lista dokumentów</span>
                        </div>
                        <div class="w-16 flex-none r">
                          <input
                              type="checkbox"
                              v-model="itemStates.dokumenty"
                              @change="toggleItem('dokumenty', itemStates.dokumenty)"
                              class="sr-only peer"
                              :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                          >
                          <div class="outline-none relative w-13 h-7 bg-surface-600
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[4px] 
                        after:start-[6px] after:bg-white after:rounded-full after:h-5 after:w-5 
                        after:transition-all peer-checked:bg-primary-500"></div>
                        </div>
                      </label>
                    </div>
                  </div>
                  <div class="flex flex-col mb-6">
                    <div class="flex flex-col gap-4 pl-4 pb-8 ">
                      <div class="text-2xl">Inne</div>
                      <label class="flex items-end w-full cursor-pointer">
                        <div class="flex-1 text-l ">
                          <span >Lista sankcyjna</span>
                        </div>
                        <div class="w-16 flex-none r">
                          <input
                              type="checkbox"
                              v-model="itemStates.listaSankcyjna"
                              @change="toggleItem('listaSankcyjna', itemStates.listaSankcyjna)"
                              class="sr-only peer"
                              :disabled="['Finance', 'Prawnik', 'Sales'].includes(selectedFiltr)"
                          >
                          <div class="outline-none relative w-13 h-7 bg-surface-600
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[4px] 
                        after:start-[6px] after:bg-white after:rounded-full after:h-5 after:w-5 
                        after:transition-all peer-checked:bg-primary-500"></div>
                        </div>
                      </label>
                      <label class="flex items-end w-full cursor-pointer">
                        <div class="flex-1 text-l ">
                          <span >Inne 2</span>
                        </div>
                        <div class="w-16 flex-none r">
                          <input type="checkbox" value="" class="sr-only peer">
                          <div class="outline-none relative w-13 h-7 bg-surface-600
                        peer-focus:outline-none  peer-focus:ring-0 peer-focus:outline-none rounded-4xl peer 
                        peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full 
                        peer-focus:ring-0 peer-focus:outline-none after:content-[''] after:absolute after:top-[4px] 
                        after:start-[6px] after:bg-white after:rounded-full after:h-5 after:w-5 
                        after:transition-all peer-checked:bg-primary-500"></div>
                        </div>
                      </label>
                    </div>
                  </div>

                </div>
                <div class="h-32 flex-none border-t border-surface-600 flex flex-row items-center justify-center">

                  <div class="pt-4 flex flex-row gap-4">
                    <div v-if="szablonAlert" class="fixed top-299 right-88  z-50 
                  text-black w-48   transition ">
                      <div class="flex h-10 bg-surface-900 text-center rounded items-center justify-center">
                        <span>Szablon zapisany!</span>
                      </div>

                      <!-- Dolna sekcja filtru -->
                    </div>
                    <div>
                      <Button label="Dodaj do szablonów" @click="saveCurrentTemplate" v-ripple severity="secondary"/>

                    </div>
                    <div>
                      <Button label="Stwórz raport" @click="analyze" v-ripple class="!bg-primary-500 !border-primary-500 hover:!bg-primary-600"/>
                    </div>




                  </div>
                </div>


              </div>
            </Transition>

            <!-- Okienko szablonow -->

            <Transition
                enter-active-class="transition duration-300"
                enter-from-class="opacity-0"
                enter-to-class="opacity-100"
                leave-active-class="transition duration-200"
                leave-from-class="opacity-100"
                leave-to-class="opacity-0">
              <div
                  v-if="twojeSzablony"
                  class="fixed inset-0 bg-black/40 z-50 text-dark-filter"
                  @click="closeTwojeSzablony"
              ></div>
            </Transition>
            <Transition
                enter-active-class="transition transform duration-300"
                enter-from-class="translate-x-full"
                enter-to-class="translate-x-0"
                leave-active-class="transition transform duration-300"
                leave-from-class="translate-x-0"
                leave-to-class="translate-x-full">
              <div
                  v-if="twojeSzablony"
                  class="fixed right-0 top-0 w-86 h-full bg-surface-950 z-50 flex flex-col"
              >

                <div class="flex flex-col h-full pl-4 pr-4">
                  <div class=" flex  pl-4 h-32 gap-28 items-center justify-center mb">
                    <div class="flex-1 text-4xl text-surface-300">Twoje szablony</div>

                    <div class="flex-1">
                      <button class="text-5xl text-surface-300"

                              @click="closeTwojeSzablony"
                      >&times;

                      </button>
                    </div>
                  </div>

                  <!-- Ladowanie twoich szablonow -->

                  <div class="flex flex-col flex-1  overflow-y-auto">
                    <div v-if="twojeSzablony" class="flex flex-1 flex-col gap-4 ">
                      <div v-for="t in templates" :key="t.id" class="flex flex-col rounded 
                       p-2 border border-primary-100 bg-primary-50">
                        <div class="h-6 mb-6  text-xl text-center ">
                          <div class="flex bg-primary-500 text-white h-10 rounded ">
                            <div class="flex-1 items-center pt-1.5">
                              <p v-if="editingId !== t.id">
                                {{ t.name }}
                              </p>
                              <input
                                  v-else
                                  v-model="editedName"

                                  maxlength="15"
                                  class="text-white px-2 rounded w-full h-7 bg-primary-500 outline-none pb-2"
                                  @keyup.enter="saveEdit(t)"
                                  @blur="saveEdit(t)"
                                  autofocus
                                  placeholder="Nowa nazwa..."
                              />
                            </div>
                            <div class="w-12 pr-2  ">
                              <Button @click="startEdit(t)" icon="pi  pi-cog" class="!w-12 1h-10"/>
                            </div>
                          </div>


                        </div>
                        <div class="flex flex-1 ">
                          <div class="flex-1 ">

                            <div class="flex flex-col gap-[0.3px]">

                              <div v-for="item in t.selectedItems" class="w-50 bg-primary-200 text-primary-500 rounded" >
                                <div v-if="item ==='nazwyImiona'" class="p-2">Nazwy oraz Imiona</div>
                              </div>
                              <div v-for="item in t.selectedItems" class="w-50 bg-primary-200 text-primary-500 rounded " >
                                <div v-if="item ==='adresy'" class="p-2">Adresy</div>
                              </div>
                              <div v-for="item in t.selectedItems" class="w-50 bg-primary-200 text-primary-500 rounded pl-2" >
                                <div v-if="item ==='kontakty'" class="p-2">Kontakty</div>
                              </div>
                              <div v-for="item in t.selectedItems" class="w-50 bg-primary-200 text-primary-500 rounded pl-2" >
                                <div v-if="item ==='statusyPrawne'" class="p-2">Statusy Prawne</div>
                              </div>
                              <div v-for="item in t.selectedItems" class="w-50 bg-primary-200 text-primary-500 rounded pl-2" >
                                <div v-if="item ==='nip'" class="p-2">NIP</div>
                              </div>
                              <div v-for="item in t.selectedItems" class="w-50 bg-primary-200 text-primary-500 rounded pl-2" >
                                <div v-if="item ==='duns'" class="p-2">DUNS</div>
                              </div>
                              <div v-for="item in t.selectedItems" class="w-50 bg-primary-200 text-primary-500 rounded pl-2">
                                <div v-if="item ==='krs'" class="flex flex-col ">
                                  <div class="mr-2">
                                    KRS
                                  </div>
                                  <div v-for="item in t.selectedItems" class="text-xs ">
                                    <div v-if="item ==='krsRejestry'">- rejestry</div>
                                    <div v-if="item ==='krsWpisy'">- wpisy</div>
                                    <div v-if="item ==='krsPowiazania'">- powiązania</div>
                                    <div v-if="item ==='metadane'">- metadane</div>
                                  </div>
                                </div>
                              </div>
                              <div v-for="item in t.selectedItems" class="w-50 bg-primary-200 text-primary-500 rounded pl-2" >
                                <div v-if="item ==='regon'" class="p-2">Regon</div>
                              </div>

                              <div v-for="item in t.selectedItems" class="w-50 bg-primary-200 text-primary-500 rounded pl-2">
                                <div v-if="item ==='raporty'" class="flex flex-col ">
                                  <div class="mr-2">
                                    Raporty finansowe
                                  </div>
                                  <div v-for="item in t.selectedItems" class="text-xs ">
                                    <div v-if="item ==='aktywa'">- Aktywa Netto oraz Przychody</div>
                                    <div v-if="item ==='zyski'">- Zyski</div>
                                    <div v-if="item ==='marze'">- Marże</div>
                                    <div v-if="item ==='rotacja'">- Rotacja</div>

                                  </div>
                                </div>
                              </div>
                              <div v-for="item in t.selectedItems" class="w-50 bg-primary-200 text-primary-500 rounded pl-2" >
                                <div v-if="item ==='stany'" class="p-2">Stany spółki</div>
                              </div>
                              <div v-for="item in t.selectedItems" class="w-50 bg-primary-200 text-primary-500 rounded pl-2" >
                                <div v-if="item ==='dokumenty'" class="p-2">Lista Dokumentów</div>
                              </div>
                              <div v-for="item in t.selectedItems" class="w-50 bg-primary-200 text-primary-500 rounded pl-2" >
                                <div v-if="item ==='listaSankcyjna'" class="p-2">Lista Sankcyjna</div>
                              </div>
                            </div>

                          </div>

                          <!-- Funkcjonalnosc szablonow (uzyj/usun) -->

                          <div class="flex flex-col w-18  min-h-16 gap-4">
                            <div>
                              <Button label="Użyj" @click="applyTemplate(t)"  v-ripple class="!w-18 !h-18" />

                            </div>
                            <div>
                              <Button
                                  label="Usuń"
                                  @click="deleteTemplate(t.id)"
                                  class="!w-18 !h-18 "
                              />
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>

                  </div>

                  <!-- Dolna sekcja szanlonow -->

                  <div
                      class="border h-32 flex-none border-t border-surface-600 flex flex-row items-center justify-center">
                    dsd
                  </div>

                </div>
              </div>
            </Transition>
          </div>

        </div>

        <!-- Sekcja dla twochi raportow -->

        <div class="flex flex-1 flex-row gap-4 rounded-xl">
          <div class="flex flex-col flex-1 bg-surface-800 rounded-xl">
            <div class="flex h-20 border border-surface-700 bg-surface-900 rounded-t-xl items-center justify-start">
              <div class="flex ml-6 gap-4">

                <!-- Ladowanie i wyswietlanie -->

                <div class="flex items-center justify-start">
                  <i class="pi pi-building-columns" style="font-size: 1.7rem; color: var(--primary-400)"></i>
                </div>
                <div class="flex flex-col gap-1 ">
                  <span class="text-xl">Analiza firmy</span>
                  <span class="text-xs text-surface-300">{{krsReportData?.PodstawoweInformacje.nazwy.pelna}}</span>

                </div>
              </div>

            </div>
            <div class="flex flex-col flex-1 border border-t-0 border-surface-700 rounded-b-xl">
              <div class="flex  m-2 gap-2 ">
                <div class="flex flex-1 bg-surface-900 rounded border border-surface-700 items-center justify-start pl-2
                            text-surface-400">
                  <span>Stwórz własny raport</span>
                </div>
                <div>
                  <div class="flex flex-row justify-end gap-2 ">
                    <div class="border border-surface-700 rounded">
                      <Button
                          @click="showFilterRaportow"
                          v-tooltip.top="'Filtruj raport'"
                          icon="pi pi-align-justify"
                          severity="secondary"
                          class=""

                      />
                    </div>
                    <div >
                      <Button  label="Twoje szablony" @click="showTwojeSzablony" class="!bg-primary-500 !border-primary-500 hover:!bg-primary-600"/>
                    </div>

                  </div>
                </div>
              </div>
              <div class="flex-1 flex flex-col m-2">
                <div class="h-10 flex pl-2 justify-between pr-28 bg-surface-900 items-center text-surface-300">
                  <div class="flex gap-8 items-center">
                    <input  id="default-checkbox" type="checkbox" value="" class="w-5 h-5 border border-light 
                    rounded bg-neutral-secondary-medium focus:ring-2 focus:ring-brand-soft">

                    <span>Nazwa raportu</span>
                  </div>
                  <div class="flex gap-40">
                    <span>Data utworzenia</span>
                    <span>Akcje</span>
                  </div>
                </div>
                <div class="flex-1 ">
                  <div v-if="krsReportData" class="flex h-22 border-b border-t border-surface-700 flex-col p-2">
                    <button
                        @click="OpenRaportList()"
                        class="text-surface-400 w-7 h-7 bg-inherit hover:bg-surface-700 flex items-center justify-center rounded-full
                                  cursor-pointer   p-1 transition-all duration-200 ease-in-out">
                      <i :class="isOpenedRaportList ? 'pi pi-angle-down' : 'pi pi-angle-right'" style="font-size: 1.5rem"></i>
                    </button>
                    <div class="flex h-16 items-center justify-start gap-3 ">
                      <div class="flex text-primary-500 items-center">
                        <i class="pi pi-building" style="font-size: 1rem"></i>
                      </div>
                      <div class="flex flex-col">
                        <div class="">
                          <span class="text-l">{{krsReportData?.PodstawoweInformacje.nazwy.pelna}}</span>
                        </div>
                        <div class="flex justify-start gap-2 text-xs text-surface-300">

                          <span class="">NIP:</span>
                          <span>{{krsReportData?.PodstawoweInformacje.numery.nip}}</span>
                          <span class="">KRS:</span>
                          <span>{{krsReportData?.PodstawoweInformacje.numery.krs}}</span>
                        </div>

                      </div>
                      <div class="h-auto ml-8">
                        <div v-if="isLoading" class="flex items-center justify-center  text-blue-600">
                          <i class="pi pi-spin pi-spinner" style="font-size: 2rem"></i>
                        </div>
                        <p v-if="error" class="text-red-600 font-medium bg-red-100 p-2 rounded">{{ error }}</p>
                      </div>
                    </div>

                  </div>
                  <div v-if="isOpenedRaportList" >
                    <div v-if="wygenerowanyRaport" v-for="r in reports" :key="r.id" class="flex h-16 items-center justify-start gap-3 border-b border-surface-700">
                      <div class="mr-3 flex items-center">
                        <input  id="default-checkbox" type="checkbox" value="" class="w-5 h-5 border border-light 
                       rounded bg-neutral-secondary-medium focus:ring-2 focus:ring-brand-soft">

                      </div>
                      <div class="flex text-primary-500 items-center">
                        <i class="pi pi-file-o" style="font-size: 1rem"></i>
                      </div>
                      <div  class="flex flex-row gap-16 items-center">

                        <div class="w-110">
                          <span class="text-l">{{ r.name }}</span>
                        </div>

                        <div class="flex w-100 ml-58 gap-32 items-center">

                          <div class="text-s">
                            <span>{{ r.date }}</span>
                          </div>

                          <div class="flex gap-2">




                            <!-- POBIERZ -->
                            <Button
                                icon="pi pi-download"
                                style="color: var(--primary-500)"
                                v-tooltip.top="'Pobierz raport'"
                                class="!w-8 !h-8 !bg-surface-950 !border-1 !border-primary-500 text-black"
                                @click="downloadFile(r)"
                            />


                            <!-- USUŃ -->
                            <Button
                                icon="pi pi-trash"
                                style="color: red"
                                v-tooltip.top="'Usuń raport'"
                                class="!w-8 !h-8 !bg-surface-950 !border-1 !border-red-500 text-black"
                                @click="deleteReport(r.id)"
                            />

                          </div>

                        </div>

                      </div>
                    </div>
                  </div>
                </div>
              </div>

            </div>





          </div>

        </div>



      </main>


    </div>

    <!-- Footer -->

    <footer
        class="flex h-24 items-center justify-center border-t border-t-surface-700 bg-surface-800 pb-12 px-2 py-1 text-sm text-surface-0/60">
    <span><strong>Copyright © 2026 <a href="http://itm.com.pl/" target="_blank"
                                      class="hover:text-surface-0 transition-colors">ITM Software House</a>. </strong> Wszelkie prawa zastrzeżone. </span>
    </footer>
  </div>


</template>

<style scoped>

</style>