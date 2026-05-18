<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Checkbox from 'primevue/checkbox'
import Dropdown from 'primevue/dropdown'
import { useAuth } from '../composables/useAuth'
import { marked } from 'marked'

const { currentUser } = useAuth()

// Zmienne wyszukiwarki NIP
const nipInput = ref('')
const isGenerating = ref(false)
const reportData = ref<any>(null)

// --- REKWIZYTY KONFIGURATORA PDF ---
const isGeneratingPdf = ref(false)
const newTemplateName = ref('')

// Bieżąca konfiguracja zaznaczeń do PDF
const pdfConfig = ref({
  includeBasicInfo: true,
  includeFinancialTables: true,
  includeAiSummary: true,
  includeConnections: false
})

// Lista zapisanych szablonów (Domyślnie ładujemy z localStorage)
const savedTemplates = ref<Array<{ name: string, config: typeof pdfConfig.value }>>([
  {
    name: 'Pełny raport (Domyślny)',
    config: { includeBasicInfo: true, includeFinancialTables: true, includeAiSummary: true, includeConnections: true }
  },
  {
    name: 'Tylko analiza finansowa',
    config: { includeBasicInfo: true, includeFinancialTables: true, includeAiSummary: false, includeConnections: false }
  }
])
const selectedTemplate = ref<any>(null)

// Wczytywanie szablonów z pamięci przeglądarki przy starcie komponentu
onMounted(() => {
  const localTemplates = localStorage.getItem('sferity_pdf_templates')
  if (localTemplates) {
    savedTemplates.value = JSON.parse(localTemplates)
  }
})

// Funkcja wczytująca wybrany szablon z listy
function applyTemplate(template: any) {
  if (!template) return
  pdfConfig.value = { ...template.config }
}

// Funkcja zapisująca nową konfigurację jako szablon
function saveCurrentConfigAsTemplate() {
  if (!newTemplateName.value.trim()) {
    alert("Podaj nazwę dla nowego szablonu!")
    return
  }

  const newTemplate = {
    name: newTemplateName.value.trim(),
    config: { ...pdfConfig.value }
  }

  savedTemplates.value.push(newTemplate)
  localStorage.setItem('sferity_pdf_templates', JSON.stringify(savedTemplates.value))
  selectedTemplate.value = newTemplate
  newTemplateName.value = ''
  alert("Szablon został zapisany pomyślnie!")
}

// --- ZAPYTANIA API ---
async function generateReport() {
  if (!nipInput.value) return
  isGenerating.value = true

  try {
    const response = await fetch(`http://localhost:5100/FinancialReport/analyze-nip/${nipInput.value}`)
    if (response.ok) {
      reportData.value = await response.json()
    } else {
      const errorText = await response.text()
      alert(`Błąd z serwera:\n${errorText}`)
    }
  } catch (e) {
    console.error(e)
    alert("Błąd sieci. Sprawdź czy backend działa na porcie 5100!")
  } finally {
    isGenerating.value = false
  }
}

// Strzał do C# po wygenerowanie pliku PDF z uwzględnieniem naszych zaznaczonych opcji
async function downloadCustomPdf() {
  isGeneratingPdf.value = true
  try {
    // Podmień w Report.vue ten fragment z fetch:
    const response = await fetch('http://localhost:5100/FinancialReport/generate-report', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        nip: reportData.value.nip,
        companyName: reportData.value.companyName, // <--- DODAJEMY NAZWĘ FIRMY!
        config: pdfConfig.value
      })
    })
   
    if (response.ok) {
      // Pobieranie pliku binarnego (Blob) i wyzwalanie zapisu w przeglądarce
      const blob = await response.blob()
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `Raport_${reportData.value.krs || reportData.value.nip}.pdf`
      document.body.appendChild(a)
      a.click()
      a.remove()
    } else {
      const prawdziwyBlad = await response.text()
      alert("Błąd z C#:\n" + prawdziwyBlad)
    }
  } catch (e) {
    alert("Błąd sieci podczas pobierania PDF.")
  } finally {
    isGeneratingPdf.value = false
  }
}

const renderedAiContent = computed(() => {
  if (!reportData.value?.aiRaport?.markdownContent) {
    return '<p class="text-surface-400 italic">Oczekiwanie na twarde dane sprawozdawcze z bazy ministerstwa...</p>'
  }
  return marked(reportData.value.aiRaport.markdownContent)
})

function resetReport() {
  reportData.value = null
  nipInput.value = ''
}
</script>

<template>
  <div class="flex h-screen flex-col bg-surface-900 text-surface-0">
    <div class="flex flex-1 overflow-hidden">

      <section class="sidebar overflow-y-auto rounded-r-rm border-y rounded-xl border-r bg-surface-800 border-surface-700 transition-all duration-300 w-60 mt-4 mb-4 p-2">
        <router-link to="/" class="flex items-center w-full rounded-lg transition-all duration-200 px-3 py-2.5 my-1 gap-3 text-surface-300 hover:bg-surface-700/40 hover:text-surface-100" active-class="bg-primary-500/15 text-primary-400 font-bold">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="20" height="20" fill="currentColor" class="shrink-0"><path d="M168,112a56,56,0,1,1-56-56A56,56,0,0,1,168,112Zm61.66,117.66a8,8,0,0,1-11.32,0l-50.06-50.07a88,88,0,1,1,11.32-11.31l50.06,50.06A8,8,0,0,1,229.66,229.66ZM112,184a72,72,0,1,0-72-72A72.08,72.08,0,0,0,112,184Z"></path></svg>
          <span class="text-sm font-medium">Home</span>
        </router-link>

        <router-link v-if="currentUser?.role === 'admin'" to="/admin" class="flex items-center w-full rounded-lg transition-all duration-200 px-3 py-2.5 my-1 gap-3 text-surface-300 hover:bg-surface-700/40 hover:text-surface-100" active-class="bg-primary-500/15 text-primary-400 font-bold">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="20" height="20" fill="currentColor" class="shrink-0"><path d="M208,40H48A16,16,0,0,0,32,56V200a16,16,0,0,0,16,16H208a16,16,0,0,0,16-16V56A16,16,0,0,0,208,40Zm0,160H48V56H208V200Zm-32-56a8,8,0,0,1-8,8H88a8,8,0,0,1,0-16h80A8,8,0,0,1,176,144Zm0-32a8,8,0,0,1-8,8H88a8,8,0,0,1,0-16h80A8,8,0,0,1,176,112Z"></path></svg>
          <span class="text-sm font-medium">Panel Admina</span>
        </router-link>

        <router-link v-if="currentUser" to="/my-account" class="flex items-center w-full rounded-lg transition-all duration-200 px-3 py-2.5 my-1 gap-3 text-surface-300 hover:bg-surface-700/40 hover:text-surface-100" active-class="bg-primary-500/15 text-primary-400 font-bold">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="20" height="20" fill="currentColor" class="shrink-0"><path d="M128,24A104,104,0,1,0,232,128,104.11,104.11,0,0,0,128,24ZM74.08,197.5a64,64,0,0,1,107.84,0,87.83,87.83,0,0,1-107.84,0ZM128,120a32,32,0,1,1,32-32A32,32,0,0,1,128,120Z"></path></svg>
          <span class="text-sm font-medium">Moje Konto</span>
        </router-link>

        <router-link to="/report" class="flex items-center w-full rounded-lg transition-all duration-200 cursor-pointer px-3 py-2.5 my-1 gap-3 text-surface-300 hover:bg-surface-700/40 hover:text-surface-100" active-class="bg-primary-500/15 text-primary-400 font-bold">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="20" height="20" fill="currentColor" class="shrink-0"><path d="M224,152a8,8,0,0,1-8,8H192v16h16a8,8,0,0,1,0,16H192v16a8,8,0,0,1-16,0V152a8,8,0,0,1,8-8h32A8,8,0,0,1,224,152ZM92,172a28,28,0,0,1-28,28H56v8a8,8,0,0,1-16,0V152a8,8,0,0,1,8-8H64A28,28,0,0,1,92,172Zm-16,0a12,12,0,0,0-12-12H56v24h8A12,12,0,0,0,76,172Zm88,8a36,36,0,0,1-36,36H112a8,8,0,0,1-8-8V152a8,8,0,0,1,8-8h16A36,36,0,0,1,164,180Zm-16,0a20,20,0,0,0-20-20h-8v40h8A20,20,0,0,0,148,180ZM40,112V40A16,16,0,0,1,56,24h96a8,8,0,0,1,5.66,2.34l56,56A8,8,0,0,1,216,88v24a8,8,0,0,1-16,0V96H152a8,8,0,0,1-8-8V40H56v72a8,8,0,0,1-16,0ZM160,80h28.69L160,51.31Z"></path></svg>
          <span class="text-sm font-medium">Stwórz raport</span>
        </router-link>
      </section>

      <main class="flex-1 overflow-auto p-2 mt-2 mb-2">
        <div class="h-full rounded-xl border border-surface-700 bg-surface-800 p-6 flex flex-col justify-center items-center overflow-y-auto">

          <div v-if="!reportData" class="w-full max-w-xl animate-fade-in">
            <div class="bg-surface-700/30 p-8 rounded-2xl border border-surface-600 shadow-xl text-center">
              <div class="w-16 h-16 bg-primary-500/10 rounded-full flex items-center justify-center mx-auto mb-4">
                <i class="pi pi-search text-2xl text-primary-400"></i>
              </div>
              <h1 class="text-2xl font-bold text-surface-0 mb-2">System Weryfikacji Podmiotów</h1>
              <p class="text-sm text-surface-400 mb-8">Wpisz NIP firmy. System pobierze oficjalne dane rejestrowe, wygeneruje podsumowanie rynkowe oraz uruchomi konfigurator raportów PDF.</p>

              <div class="flex flex-col gap-4">
                <InputText
                    v-model="nipInput"
                    placeholder="Wprowadź NIP firmy..."
                    class="w-full bg-surface-900 p-inputtext-lg text-center tracking-widest"
                    @keyup.enter="generateReport"
                />
                <Button
                    label="Pobierz dane i buduj profil"
                    icon="pi pi-bolt"
                    size="large"
                    :loading="isGenerating"
                    @click="generateReport"
                    class="w-full"
                />
              </div>
            </div>
          </div>

          <div v-else class="w-full h-full animate-fade-in flex flex-col justify-start">

            <div class="flex justify-between items-center bg-surface-900/50 p-4 rounded-xl border border-surface-700 mb-4">
              <div>
                <h2 class="text-xl font-bold text-primary-400 uppercase tracking-wide">{{ reportData.companyName }}</h2>
                <div class="text-xs text-surface-400 mt-1 flex gap-6">
                  <span>NIP: <strong class="text-surface-100 font-mono">{{ reportData.nip }}</strong></span>
                  <span>KRS: <strong class="text-surface-100 font-mono">{{ reportData.krs }}</strong></span>
                  <span>Status VAT: <span class="px-2 py-0.5 rounded text-xs font-bold" :class="reportData.status === 'Czynny' ? 'bg-green-500/10 text-green-400' : 'bg-amber-500/10 text-amber-400'">{{ reportData.status }}</span></span>
                </div>
              </div>
              <Button label="Nowe wyszukiwanie" icon="pi pi-arrow-left" variant="outlined" severity="secondary" size="small" @click="resetReport" />
            </div>

            <div class="grid grid-cols-1 lg:grid-cols-12 gap-4 flex-1 items-start">

              <div class="lg:col-span-4 bg-surface-900/50 p-5 rounded-xl border border-surface-700 h-full flex flex-col">
                <div class="flex items-center gap-2 border-b border-surface-700 pb-3 mb-4">
                  <i class="pi pi-id-card text-primary-400"></i>
                  <h3 class="font-semibold text-surface-100 uppercase tracking-wider text-xs">Informacje na Stronie</h3>
                </div>

                <div class="space-y-4 text-sm flex-1">
                  <div class="bg-surface-800/60 p-3 rounded-lg border border-surface-700">
                    <span class="text-xs text-surface-400 block uppercase font-bold mb-1">Identyfikatory Spółki</span>
                    <p class="text-surface-200 font-medium">REGON: <span class="font-mono text-surface-100">Dostępny w PDF</span></p>
                    <p class="text-surface-200 font-medium mt-1">Forma prawna: <span class="text-primary-400">Spółka z o.o.</span></p>
                  </div>

                  <div class="bg-surface-800/60 p-3 rounded-lg border border-surface-700">
                    <span class="text-xs text-surface-400 block uppercase font-bold mb-1">Działalność (PKD)</span>
                    <p class="text-xs text-surface-200 leading-relaxed italic">System automatycznie parsuje kluczowe kody działalności biznesowej pobrane z rejestru państwowego.</p>
                  </div>

                  <div class="bg-surface-800/60 p-3 rounded-lg border border-surface-700">
                    <span class="text-xs text-surface-400 block uppercase font-bold mb-1">Status prawny</span>
                    <p class="text-green-400 font-medium flex items-center gap-1"><i class="pi pi-check text-xs"></i> Podmiot zarejestrowany i aktywny</p>
                  </div>
                </div>
              </div>

              <div class="lg:col-span-4 bg-surface-900/50 p-5 rounded-xl border border-surface-700 h-full flex flex-col">
                <div class="flex items-center gap-2 border-b border-surface-700 pb-3 mb-4">
                  <i class="pi pi-sliders-h text-amber-400"></i>
                  <h3 class="font-semibold text-surface-100 uppercase tracking-wider text-xs">Konfigurator Szablonów PDF</h3>
                </div>

                <div class="mb-4 bg-surface-800/50 p-3 rounded-lg border border-surface-700">
                  <label class="text-xs text-surface-400 block uppercase font-bold mb-2">Wybierz zapisany szablon</label>
                  <Dropdown
                      v-model="selectedTemplate"
                      :options="savedTemplates"
                      optionLabel="name"
                      placeholder="Wybierz profil układu..."
                      class="w-full bg-surface-900"
                      @change="applyTemplate(selectedTemplate)"
                  />
                </div>

                <div class="space-y-3 bg-surface-800/30 p-4 rounded-lg border border-surface-700 flex-1">
                  <span class="text-xs text-surface-400 block uppercase font-bold mb-2">Zawartość pliku wyjściowego</span>

                  <div class="flex items-center gap-2">
                    <Checkbox v-model="pdfConfig.includeBasicInfo" :binary="true" inputId="info" />
                    <label Thru="info" class="text-sm text-surface-200 cursor-pointer select-none">Podstawowe dane rejestrowe</label>
                  </div>

                  <div class="flex items-center gap-2">
                    <Checkbox v-model="pdfConfig.includeFinancialTables" :binary="true" inputId="tables" />
                    <label for="tables" class="text-sm text-surface-200 cursor-pointer select-none">Tabele i bilanse finansowe</label>
                  </div>

                  <div class="flex items-center gap-2">
                    <Checkbox v-model="pdfConfig.includeAiSummary" :binary="true" inputId="ai" />
                    <label for="ai" class="text-sm text-surface-200 cursor-pointer select-none">Analiza i ocena ryzyka AI</label>
                  </div>

                  <div class="flex items-center gap-2">
                    <Checkbox v-model="pdfConfig.includeConnections" :binary="true" inputId="conn" />
                    <label for="conn" class="text-sm text-surface-200 cursor-pointer select-none">Struktura powiązań osobowych</label>
                  </div>
                </div>

                <div class="mt-4 bg-surface-800/50 p-3 rounded-lg border border-surface-700 space-y-2">
                  <label class="text-xs text-surface-400 block uppercase font-bold">Zapisz obecną konfigurację</label>
                  <div class="flex gap-2">
                    <InputText v-model="newTemplateName" placeholder="Nazwa nowego szablonu..." class="w-full bg-surface-900 text-xs" />
                    <Button icon="pi pi-save" size="small" severity="success" @click="saveCurrentConfigAsTemplate" />
                  </div>
                </div>

                <Button
                    label="Generuj dedykowany PDF"
                    icon="pi pi-file-pdf"
                    severity="danger"
                    class="w-full mt-4 p-3 font-bold"
                    :loading="isGeneratingPdf"
                    @click="downloadCustomPdf"
                />
              </div>

              <div class="lg:col-span-4 bg-surface-900/50 p-5 rounded-xl border border-surface-700 h-full flex flex-col overflow-hidden">
                <div class="flex items-center gap-2 border-b border-surface-700 pb-3 mb-4">
                  <i class="pi pi-sparkles text-purple-400"></i>
                  <h3 class="font-semibold text-surface-100 uppercase tracking-wider text-xs">Ocena AI (Dynamiczna)</h3>
                </div>
                <div class="flex-1 overflow-y-auto pr-1">
                  <div class="prose prose-invert max-w-none text-xs text-surface-200 leading-relaxed" v-html="renderedAiContent"></div>
                </div>
              </div>

            </div> </div>

        </div>
      </main>

    </div>
  </div>
</template>

<style scoped>
.animate-fade-in {
  animation: fadeIn 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}
@keyframes fadeIn {
  from { opacity: 0; transform: scale(0.99); }
  to { opacity: 1; transform: scale(1); }
}

/* Formatowanie stylów markdown wewnątrz okienka AI */
:deep(.prose h3) {
  color: #a78bfa;
  font-weight: 700;
  margin-top: 1rem;
  margin-bottom: 0.5rem;
  font-size: 0.95rem;
}
:deep(.prose p) {
  margin-bottom: 0.75rem;
}
:deep(.prose ul) {
  list-style-type: disc;
  padding-left: 1.25rem;
  margin-bottom: 0.75rem;
}
:deep(.prose li) {
  margin-bottom: 0.25rem;
}
</style>