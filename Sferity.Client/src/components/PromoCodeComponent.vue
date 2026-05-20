<script setup>
import { ref, computed, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import Toast from 'primevue/toast'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import Select from 'primevue/select'
import Dialog from 'primevue/dialog'
import Checkbox from 'primevue/checkbox'
import Slider from 'primevue/slider'
import DatePicker from 'primevue/datepicker'

// POPRAWKA: Ustawiono Twój port serwera (5100)
// Upewnij się, że ścieżka /api/PromoCodes zgadza się z Twoim kontrolerem C#
const API_URL = 'http://localhost:5100/api/promocodes'
const toast = useToast()

// --- STAN ---
const promoCodes = ref([])
const selectedCodes = ref([])
const isLoading = ref(false)

// Formularz generowania
const genAmount = ref(100)
const genDays = ref(7)
const genQuantity = ref(1)
const genLabel = ref('')
const genAllowLabelRedemption = ref(false)

// Akcje (Realizacja)
const actionInput = ref('')
const actionResult = ref(null)

// Podgląd QR
const showQrDialog = ref(false)
const currentQrUrl = ref('')
const currentQrCode = ref('')

// Filtrowanie
const searchQuery = ref('')
const filterStatus = ref('Wszystkie')
const statusOptions = ['Wszystkie', 'Aktywne', 'Zużyte', 'Wygasłe']
const filterAmountRange = ref([0, 2000])
const filterDateRange = ref(null)

// --- STATYSTYKI (Dashboard) ---
const stats = computed(() => {
  const total = promoCodes.value.length
  const active = promoCodes.value.filter(c => getStatusDetails(c.status).label === 'Aktywny').length
  const used = promoCodes.value.filter(c => getStatusDetails(c.status).label === 'Zużyty').length
  const totalValue = promoCodes.value
      .filter(c => getStatusDetails(c.status).label === 'Aktywny')
      .reduce((sum, c) => sum + (c.creditAmount || 0), 0)

  return { total, active, used, totalValue }
})

// --- FUNKCJE API ---
const fetchCodes = async () => {
  try {
    const response = await fetch(API_URL)
    if (response.ok) promoCodes.value = await response.json()
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Błąd', detail: 'Nie udało się pobrać kodów', life: 3000 })
  }
}

const generateCodes = async () => {
  isLoading.value = true
  try {
    const payload = {
      creditAmount: genAmount.value,
      expirationDays: genDays.value,
      quantity: genQuantity.value,
      allowLabelRedemption: genAllowLabelRedemption.value
    }
    if (genLabel.value.trim()) payload.label = genLabel.value.trim()

    const response = await fetch(`${API_URL}/generate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    })

    if (response.ok) {
      toast.add({ severity: 'success', summary: 'Sukces', detail: `Wygenerowano ${genQuantity.value} kodów`, life: 3000 })
      genLabel.value = ''
      genQuantity.value = 1
      await fetchCodes()
    }
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Błąd', detail: 'Błąd podczas generowania', life: 3000 })
  } finally {
    isLoading.value = false
  }
}

const redeemCode = async () => {
  if (!actionInput.value) return
  const isGuid = actionInput.value.length > 20
  const payload = isGuid ? { code: actionInput.value } : { label: actionInput.value }

  try {
    const response = await fetch(`${API_URL}/redeem`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    })

    if (response.ok) {
      const data = await response.json()
      toast.add({ severity: 'success', summary: 'Zrealizowano', detail: `Dodano +${data.creditAmount} PLN`, life: 5000 })
      actionInput.value = ''
      await fetchCodes()
    } else {
      toast.add({ severity: 'warn', summary: 'Odmowa', detail: 'Kod jest nieważny lub zużyty', life: 4000 })
    }
  } catch (error) {
    toast.add({ severity: 'error', summary: 'Błąd', detail: 'Błąd serwera', life: 3000 })
  }
}

const copyToClipboard = (text) => {
  navigator.clipboard.writeText(text)
  toast.add({ severity: 'secondary', summary: 'Skopiowano', detail: 'Kod został skopiowany', life: 2000 })
}

const openQrDialog = (codeId) => {
  currentQrCode.value = codeId
  currentQrUrl.value = `${API_URL}/qr/${codeId}`
  showQrDialog.value = true
}

const getStatusDetails = (s) => {
  const status = String(s).toLowerCase()
  if (status === 'active' || status === '0') return { label: 'Aktywny', severity: 'success' }
  if (status === 'used' || status === '1') return { label: 'Zużyty', severity: 'secondary' }
  return { label: 'Wygasły', severity: 'danger' }
}

const processedCodes = computed(() => {
  let result = promoCodes.value
  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    result = result.filter(c => c.code.toLowerCase().includes(q) || (c.label && c.label.toLowerCase().includes(q)))
  }
  if (filterStatus.value !== 'Wszystkie') {
    result = result.filter(c => getStatusDetails(c.status).label === filterStatus.value)
  }
  result = result.filter(c => c.creditAmount >= filterAmountRange.value[0] && c.creditAmount <= filterAmountRange.value[1])
  if (filterDateRange.value && filterDateRange.value[0] && filterDateRange.value[1]) {
    const startDate = new Date(filterDateRange.value[0]).getTime()
    const endDate = new Date(filterDateRange.value[1]).setHours(23, 59, 59, 999)
    result = result.filter(c => {
      if (!c.expiresAt) return true
      const expDate = new Date(c.expiresAt).getTime()
      return expDate >= startDate && expDate <= endDate
    })
  }
  return result
})

const formatDate = (dateString) => dateString ? new Date(dateString).toLocaleDateString('pl-PL') : 'Brak'

onMounted(() => fetchCodes())
</script><template>
  <div class="space-y-12 font-sans text-surface-0">
    <Toast />

    <div class="flex justify-between items-end border-b border-surface-600 pb-6 mt-4">
      <div>
        <h1 class="text-2xl font-bold text-primary-400 tracking-tight">Sferity Promo</h1>
        <p class="text-sm text-surface-400 tracking-wide mt-1">Panel zarządzania kodami</p>
      </div>
      <Button icon="pi pi-refresh" severity="secondary" variant="text" rounded @click="fetchCodes" />
    </div>

    <div class="grid grid-cols-2 md:grid-cols-4 gap-8">
      <div class="border-l border-surface-600 pl-5">
        <div class="text-[10px] font-semibold text-surface-400 uppercase tracking-[0.2em] mb-1">Baza kodów</div>
        <div class="text-3xl font-light">{{ stats.total }}</div>
      </div>
      <div class="border-l border-surface-600 pl-5">
        <div class="text-[10px] font-semibold text-green-400 uppercase tracking-[0.2em] mb-1">Aktywne</div>
        <div class="text-3xl font-light">{{ stats.active }}</div>
      </div>
      <div class="border-l border-surface-600 pl-5">
        <div class="text-[10px] font-semibold text-surface-400 uppercase tracking-[0.2em] mb-1">Zużyte</div>
        <div class="text-3xl font-light">{{ stats.used }}</div>
      </div>
      <div class="border-l border-primary-500 pl-5">
        <div class="text-[10px] font-semibold text-primary-400 uppercase tracking-[0.2em] mb-1">Wartość rynkowa</div>
        <div class="text-3xl font-light">{{ stats.totalValue.toLocaleString() }} zł</div>
      </div>
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-12 gap-6">
      <div class="lg:col-span-8 bg-surface-700/30 p-6 rounded-lg border border-surface-600 space-y-6">
        <h2 class="text-sm font-semibold uppercase tracking-widest text-surface-400">Kreator Partii</h2>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-x-8 gap-y-6">
          <div class="flex flex-col gap-2">
            <label class="text-[11px] font-medium text-surface-400">Nominał (PLN)</label>
            <InputNumber v-model="genAmount" mode="currency" currency="PLN" locale="pl-PL" fluid class="p-inputtext-sm" />
          </div>
          <div class="flex flex-col gap-2">
            <label class="text-[11px] font-medium text-surface-400">Ważność (dni)</label>
            <InputNumber v-model="genDays" fluid class="p-inputtext-sm" />
          </div>
          <div class="flex flex-col gap-2">
            <label class="text-[11px] font-medium text-surface-400">Etykieta identyfikacyjna</label>
            <InputText v-model="genLabel" class="uppercase font-medium p-inputtext-sm bg-surface-900" fluid placeholder="np. LATO24" />
          </div>
          <div class="flex flex-col gap-2">
            <label class="text-[11px] font-medium text-surface-400">Nakład sztuk</label>
            <InputNumber v-model="genQuantity" :min="1" :max="500" showButtons fluid class="p-inputtext-sm" />
          </div>
        </div>
        <div class="flex items-center gap-3 pt-2">
          <Checkbox v-model="genAllowLabelRedemption" :binary="true" inputId="allowLabel" />
          <label for="allowLabel" class="text-xs text-surface-300 cursor-pointer hover:text-surface-100 transition-colors">Zezwól na realizację posługując się samą etykietą</label>
        </div>
        <div class="pt-4">
          <Button label="Wygeneruj kody" icon="pi pi-check" @click="generateCodes" :loading="isLoading" severity="success" class="w-full md:w-auto px-8" />
        </div>
      </div>

      <div class="lg:col-span-4 bg-surface-700/30 p-6 rounded-lg border border-surface-600 space-y-6">
        <h2 class="text-sm font-semibold uppercase tracking-widest text-surface-400">Terminal autoryzacji</h2>
        <div class="flex flex-col gap-4 mt-4">
          <InputText v-model="actionInput" placeholder="Wprowadź kod..." class="w-full text-center tracking-widest p-inputtext-lg bg-surface-900" />
          <Button label="Zrealizuj" severity="primary" @click="redeemCode" class="w-full h-12" />
        </div>
      </div>
    </div>

    <div class="bg-surface-700/30 p-6 rounded-lg border border-surface-600 space-y-6 mt-6">
      <div class="flex justify-between items-center">
        <h2 class="text-sm font-semibold uppercase tracking-widest text-surface-400">Rejestr kodów</h2>
        <Button v-if="selectedCodes.length" label="Dezaktywuj wybrane" severity="danger" variant="outlined" size="small" icon="pi pi-ban" />
      </div>

      <div class="grid grid-cols-1 md:grid-cols-4 gap-6 pb-2">
        <div class="flex flex-col gap-2">
          <label class="text-[10px] text-surface-400 uppercase tracking-widest">Wyszukiwanie</label>
          <InputText v-model="searchQuery" placeholder="Szukaj..." class="w-full p-inputtext-sm bg-surface-900" />
        </div>
        <div class="flex flex-col gap-2">
          <label class="text-[10px] text-surface-400 uppercase tracking-widest">Status</label>
          <Select v-model="filterStatus" :options="statusOptions" class="w-full p-inputtext-sm bg-surface-900" />
        </div>
        <div class="flex flex-col gap-2">
          <label class="text-[10px] text-surface-400 uppercase tracking-widest flex justify-between">
            <span>Wartość</span>
            <span class="text-primary-400 font-bold">{{ filterAmountRange[0] }}-{{ filterAmountRange[1] }} zł</span>
          </label>
          <div class="pt-3 px-2">
            <Slider v-model="filterAmountRange" :min="0" :max="2000" :step="50" range class="w-full" />
          </div>
        </div>
        <div class="flex flex-col gap-2">
          <label class="text-[10px] text-surface-400 uppercase tracking-widest">Ważność</label>
          <DatePicker v-model="filterDateRange" selectionMode="range" :manualInput="false" class="w-full p-inputtext-sm bg-surface-900" />
        </div>
      </div>

      <DataTable
          v-model:selection="selectedCodes" :value="processedCodes" dataKey="code"
          paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]"
          currentPageReportTemplate="{first}-{last} / {totalRecords}"
          paginatorTemplate="PrevPageLink PageLinks NextPageLink CurrentPageReport RowsPerPageDropdown"
          size="small"
          class="border border-surface-700 rounded-xl overflow-hidden"
          :pt="{ headerRow: { class: 'bg-surface-800' } }"
      >
        <Column selectionMode="multiple" headerStyle="width: 3rem"></Column>
        <Column field="label" header="Etykieta">
          <template #body="{ data }">
            <span v-if="data.label" class="text-surface-100 font-medium tracking-wide">{{ data.label }}</span>
            <span v-else class="text-surface-500 italic text-xs">—</span>
          </template>
        </Column>
        <Column field="code" header="Identyfikator (GUID)">
          <template #body="{ data }">
            <div class="flex items-center gap-2 group">
              <span class="text-[10px] font-mono text-surface-400">{{ data.code }}</span>
              <Button icon="pi pi-copy" variant="text" severity="secondary" size="small" class="p-0 h-6 w-6 opacity-0 group-hover:opacity-100 transition-opacity" @click="copyToClipboard(data.code)" />
            </div>
          </template>
        </Column>
        <Column field="creditAmount" header="Nominał">
          <template #body="{ data }"><span class="font-medium text-primary-400">{{ data.creditAmount }} PLN</span></template>
        </Column>
        <Column field="expiresAt" header="Ważność">
          <template #body="{ data }">
            <span class="text-xs text-surface-400">{{ formatDate(data.expiresAt) }}</span>
          </template>
        </Column>
        <Column field="status" header="Status">
          <template #body="{ data }">
            <Tag :severity="getStatusDetails(data.status).severity" :value="getStatusDetails(data.status).label" variant="outlined" class="text-[10px] uppercase tracking-wider" />
          </template>
        </Column>
        <Column header="" headerStyle="width: 4rem">
          <template #body="{ data }">
            <Button icon="pi pi-qrcode" variant="text" severity="secondary" rounded @click="openQrDialog(data.code)" />
          </template>
        </Column>
      </DataTable>
    </div>

    <Dialog v-model:visible="showQrDialog" modal :showHeader="false" :style="{ width: '280px' }" :pt="{ content: { class: 'p-0 rounded-2xl overflow-hidden' } }">
      <div class="flex flex-col items-center p-8 bg-surface-800 border border-surface-600 relative">
        <Button icon="pi pi-times" variant="text" rounded severity="secondary" class="absolute top-2 right-2" @click="showQrDialog = false" />
        <div class="bg-white p-2 rounded-lg mb-4">
          <img :src="currentQrUrl" class="w-40 h-40" />
        </div>
        <span class="text-[9px] text-center font-mono text-surface-300 break-all leading-tight">{{ currentQrCode }}</span>
      </div>
    </Dialog>
  </div>
</template>

<style scoped>
/* Dopasowanie tabeli do ciemnego motywu */
:deep(.p-datatable .p-datatable-thead > tr > th) {
  background: transparent;
  border-bottom: 1px solid var(--p-surface-700);
  font-weight: 500;
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--p-surface-300);
  padding-top: 1rem;
  padding-bottom: 1rem;
}
:deep(.p-datatable .p-datatable-tbody > tr > td) {
  border-bottom: 1px solid var(--p-surface-700);
  background-color: transparent;
  padding-top: 0.75rem;
  padding-bottom: 0.75rem;
}
:deep(.p-datatable .p-datatable-tbody > tr) {
  background-color: transparent;
  transition: background-color 0.2s;
}
:deep(.p-datatable .p-datatable-tbody > tr:hover) {
  background-color: rgba(255, 255, 255, 0.03);
}
:deep(.p-paginator) {
  background: transparent;
  border-top: none;
}
</style>