<script setup>
import { ref, computed } from 'vue'
import { useToast } from 'primevue/usetoast'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import Select from 'primevue/select'
import Slider from 'primevue/slider'
import DatePicker from 'primevue/datepicker'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'

const props = defineProps({
  promoCodes: { type: Array, required: true },
  apiUrl: { type: String, required: true }
})

const emit = defineEmits(['delete-by-label', 'disable-selected', 'delete-code', 'edit-code'])
const toast = useToast()

const selectedCodes = ref([])
const searchQuery = ref('')
const filterStatus = ref('Wszystkie')
const statusOptions = ['Wszystkie', 'Oczekujący', 'Aktywny', 'Zużyty', 'Wygasły', 'Wyłączony']
const statusEditOptions = ['Pending', 'Active', 'Used', 'Expired', 'Disabled']

// Filtr domyślnie od 0, by łapać też darmowe/bonusowe kody
const filterAmountRange = ref([0, 10000])
const filterDateRange = ref(null)

const showQrDialog = ref(false)
const currentQrCode = ref('')
const showDeleteLabelDialog = ref(false)
const labelToDelete = ref('')
const showEditDialog = ref(false)
const editingCode = ref(null)

const openQrDialog = (codeId) => {
  currentQrCode.value = codeId
  showQrDialog.value = true
}

const copyToClipboard = (text) => {
  navigator.clipboard.writeText(text)
  toast.add({ severity: 'secondary', summary: 'Skopiowano', detail: 'Kod skopiowany', life: 2000 })
}

const confirmDeleteByLabel = () => {
  if (labelToDelete.value.trim()) {
    emit('delete-by-label', labelToDelete.value.trim().toUpperCase())
    showDeleteLabelDialog.value = false
    labelToDelete.value = ''
  }
}

const handleDisableSelected = () => {
  if (selectedCodes.value.length) {
    emit('disable-selected', selectedCodes.value.map(c => c.code))
    selectedCodes.value = []
  }
}

const openEditDialog = (codeData) => {
  editingCode.value = {
    ...codeData,
    validFrom: codeData.activeFrom ? new Date(codeData.activeFrom) : null,
    expiresAt: codeData.expiresAt ? new Date(codeData.expiresAt) : null
  }
  showEditDialog.value = true
}

const saveEdit = () => {
  emit('edit-code', {
    ...editingCode.value,
    validFrom: editingCode.value.validFrom ? editingCode.value.validFrom : null,
    expiresAt: editingCode.value.expiresAt ? editingCode.value.expiresAt : null
  })
  showEditDialog.value = false
}

const getStatusDetails = (s) => {
  switch (String(s).toLowerCase()) {
    case 'pending':  return { label: 'Oczekujący', severity: 'warn' }
    case 'active':   return { label: 'Aktywny',    severity: 'success' }
    case 'used':     return { label: 'Zużyty',     severity: 'secondary' }
    case 'expired':  return { label: 'Wygasły',    severity: 'danger' }
    case 'disabled': return { label: 'Wyłączony',  severity: 'danger' }
    default:         return { label: s,            severity: 'secondary' }
  }
}

const formatDate = (dateString) => dateString ? new Date(dateString).toLocaleDateString('pl-PL') : '—'

const processedCodes = computed(() => {
  let result = props.promoCodes

  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    result = result.filter(c => c.code.toLowerCase().includes(q) || (c.label && c.label.toLowerCase().includes(q)))
  }

  if (filterStatus.value !== 'Wszystkie') {
    result = result.filter(c => getStatusDetails(c.status).label === filterStatus.value)
  }

  result = result.filter(c => c.creditAmount >= filterAmountRange.value[0] && c.creditAmount <= filterAmountRange.value[1])

  if (filterDateRange.value && filterDateRange.value[0]) {
    const filterStart = new Date(filterDateRange.value[0]).setHours(0, 0, 0, 0)
    const filterEnd = filterDateRange.value[1]
        ? new Date(filterDateRange.value[1]).setHours(23, 59, 59, 999)
        : new Date(filterDateRange.value[0]).setHours(23, 59, 59, 999)

    result = result.filter(c => {
      const codeStart = c.activeFrom ? new Date(c.activeFrom).getTime() : 0
      const codeEnd = c.expiresAt ? new Date(c.expiresAt).getTime() : Infinity
      return codeStart <= filterEnd && codeEnd >= filterStart
    })
  }

  return result
})
</script>

<template>
  <div class="space-y-6">
    <div class="flex justify-between items-center">
      <h2 class="text-sm font-semibold uppercase tracking-widest text-slate-400">Rejestr kodów</h2>
      <div class="flex gap-2">
        <Button v-if="selectedCodes.length > 0" label="Wyłącz zaznaczone" icon="pi pi-ban" severity="warning" variant="outlined" size="small" @click="handleDisableSelected" />
        <Button label="Wyłącz serię" icon="pi pi-filter-slash" severity="danger" variant="outlined" size="small" @click="showDeleteLabelDialog = true" />
      </div>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-4 gap-6 bg-white pb-6">
      <div class="flex flex-col gap-2">
        <label class="text-[10px] uppercase text-slate-400 font-bold tracking-wider">Szukaj</label>
        <InputText v-model="searchQuery" placeholder="Kod lub etykieta..." class="h-11" />
      </div>
      <div class="flex flex-col gap-2">
        <label class="text-[10px] uppercase text-slate-400 font-bold tracking-wider">Status</label>
        <Select v-model="filterStatus" :options="statusOptions" class="h-11" />
      </div>
      <div class="flex flex-col gap-2">
        <label class="text-[10px] uppercase text-slate-400 font-bold tracking-wider flex justify-between">
          <span>Wartość</span><span class="text-blue-600 font-bold">{{ filterAmountRange[0] }}-{{ filterAmountRange[1] }} zł</span>
        </label>
        <div class="pt-3 px-2">
          <Slider v-model="filterAmountRange" :min="0" :max="10000" :step="50" range class="w-full" />
        </div>
      </div>
      <div class="flex flex-col gap-2">
        <label class="text-[10px] uppercase text-slate-400 font-bold tracking-wider">Okres ważności</label>
        <DatePicker v-model="filterDateRange" selectionMode="range" dateFormat="dd.mm.yy" class="h-11" placeholder="Wybierz zakres" />
      </div>
    </div>

    <DataTable v-model:selection="selectedCodes" :value="processedCodes" dataKey="code" paginator :rows="10" size="small" class="border border-slate-100 rounded-xl overflow-hidden">
      <Column selectionMode="multiple" headerStyle="width: 3rem"></Column>

      <Column field="label" header="Etykieta">
        <template #body="{ data }">
          <span v-if="data.label" class="font-bold text-slate-700">{{ data.label }}</span>
          <span v-else class="text-slate-300 italic text-xs">brak</span>
        </template>
      </Column>

      <Column field="allowLabelRedemption" header="Tylko Etykieta" headerStyle="width: 8rem" class="text-center">
        <template #body="{ data }">
          <div class="flex justify-center items-center">
            <i v-if="data.allowLabelRedemption" class="pi pi-check text-emerald-500 font-bold" title="Można użyć samej etykiety w terminalu"></i>
            <i v-else class="pi pi-minus text-slate-300" title="Wymagany pełny kod GUID"></i>
          </div>
        </template>
      </Column>

      <Column field="code" header="GUID">
        <template #body="{ data }">
          <div class="flex items-center gap-2 py-1">
            <span class="text-[12px] font-mono text-slate-600 truncate max-w-[120px]">{{ data.code }}</span>
            <Button icon="pi pi-copy" variant="text" severity="secondary" size="small" class="flex-shrink-0" @click="copyToClipboard(data.code)" />
          </div>
        </template>
      </Column>

      <Column field="creditAmount" header="Nominał">
        <template #body="{ data }"><span class="font-medium">{{ data.creditAmount }} PLN</span></template>
      </Column>
      <Column field="activeFrom" header="Ważne od">
        <template #body="{ data }"><span class="text-xs text-slate-500">{{ formatDate(data.activeFrom) }}</span></template>
      </Column>
      <Column field="expiresAt" header="Ważne do">
        <template #body="{ data }"><span class="text-xs text-slate-500">{{ formatDate(data.expiresAt) }}</span></template>
      </Column>
      <Column field="status" header="Status">
        <template #body="{ data }">
          <Tag :severity="getStatusDetails(data.status).severity" :value="getStatusDetails(data.status).label" variant="outlined" class="text-[10px] uppercase font-bold" />
        </template>
      </Column>
      <Column headerStyle="width: 10rem" header="Akcje">
        <template #body="{ data }">
          <div class="flex gap-1">
            <Button icon="pi pi-pencil" variant="text" severity="secondary" rounded tooltip="Edytuj" @click="openEditDialog(data)" />
            <Button icon="pi pi-qrcode" variant="text" severity="secondary" rounded tooltip="Kod QR" @click="openQrDialog(data.code)" />
            <Button icon="pi pi-trash" variant="text" severity="danger" rounded tooltip="Usuń trwale" @click="$emit('delete-code', data.code)" />
          </div>
        </template>
      </Column>
    </DataTable>

    <Dialog v-model:visible="showQrDialog" modal :showHeader="false" :style="{ width: '280px' }">
      <div class="flex flex-col items-center p-8 bg-white relative rounded-2xl">
        <Button icon="pi pi-times" variant="text" rounded severity="secondary" class="absolute top-2 right-2" @click="showQrDialog = false" />
        <img :src="`${props.apiUrl}/qr/${currentQrCode}`" class="w-48 h-48 mb-4" alt="QR" />
        <span class="text-[10px] font-mono text-center text-slate-400 break-all leading-tight">{{ currentQrCode }}</span>
      </div>
    </Dialog>

    <Dialog v-model:visible="showDeleteLabelDialog" modal header="Dezaktywacja serii" :style="{ width: '400px' }">
      <div class="flex flex-col gap-4">
        <p class="text-sm text-slate-500">Wpisz nazwę etykiety. Kody zostaną oznaczone jako wyłączone.</p>
        <InputText v-model="labelToDelete" @input="(e) => labelToDelete = e.target.value.toUpperCase()" placeholder="NP. LATO2026" class="uppercase font-bold text-center h-12" />
        <div class="flex justify-end gap-2 pt-2">
          <Button label="Anuluj" severity="secondary" variant="text" @click="showDeleteLabelDialog = false" />
          <Button label="Wyłącz kody" severity="danger" @click="confirmDeleteByLabel" :disabled="!labelToDelete" />
        </div>
      </div>
    </Dialog>

    <Dialog v-model:visible="showEditDialog" modal header="Edycja Kodu" :style="{ width: '500px' }">
      <div class="flex flex-col gap-4 pt-2" v-if="editingCode">
        <div class="flex flex-col gap-1">
          <label class="text-xs font-bold text-slate-500 uppercase">Etykieta</label>
          <InputText v-model="editingCode.label" @input="(e) => editingCode.label = e.target.value.toUpperCase()" class="uppercase" />
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-xs font-bold text-slate-500 uppercase">Kwota (PLN)</label>
          <InputNumber v-model="editingCode.creditAmount" />
        </div>
        <div class="grid grid-cols-2 gap-4">
          <div class="flex flex-col gap-1">
            <label class="text-xs font-bold text-slate-500 uppercase">Ważne od</label>
            <DatePicker v-model="editingCode.validFrom" dateFormat="dd.mm.yy" showIcon fluid />
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-xs font-bold text-slate-500 uppercase">Ważne do</label>
            <DatePicker v-model="editingCode.expiresAt" dateFormat="dd.mm.yy" showIcon fluid />
          </div>
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-xs font-bold text-slate-500 uppercase">Status</label>
          <Select v-model="editingCode.status" :options="statusEditOptions" />
        </div>
        <div class="flex justify-end gap-2 pt-4 border-t border-slate-100">
          <Button label="Anuluj" severity="secondary" variant="text" @click="showEditDialog = false" />
          <Button label="Zapisz zmiany" severity="success" @click="saveEdit" />
        </div>
      </div>
    </Dialog>
  </div>
</template>