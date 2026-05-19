<script setup>
import { ref, computed, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import Toast from 'primevue/toast'
import Button from 'primevue/button'

import PromoDashboard from './PromoDashboard.vue'
import PromoGenerator from './PromoGenerator.vue'
import PromoTerminal from './PromoTerminal.vue'
import PromoTable from './PromoTable.vue'

const API_URL = 'https://localhost:7272/api/promocodes'
const toast = useToast()

const promoCodes = ref([])
const isLoading = ref(false)
const terminalLookupData = ref(null)

// --- TŁUMACZ DANYCH (C# -> Vue) ---
const mapFromBackend = (c) => {
  let statusStr = 'Active'
  const s = String(c.status ?? c.Status).toLowerCase()
  if (s === 'used' || s === '1' || s === '2') statusStr = 'Used'
  else if (s === 'expired' || s === 'disabled' || s === '2' || s === '3' || s === '4') statusStr = 'Expired'
  else if (s === 'pending' || s === '0') statusStr = 'Pending'

  return {
    code: c.code ?? c.Code,
    label: c.label ?? c.Label ?? '',
    creditAmount: c.creditAmount ?? c.CreditAmount ?? 0,
    status: statusStr,
    activeFrom: c.activeFrom ?? c.ActiveFrom,
    expiresAt: c.expiresAt ?? c.ExpiresAt ?? c.expiresOn ?? c.ExpiresOn,
    // Pobieramy informację o pozwoleniu na etykietę
    allowLabelRedemption: c.allowLabelRedemption ?? c.AllowLabelRedemption ?? false
  }
}

// 1. POBIERANIE WSZYSTKICH KODÓW (Do tabeli)
const fetchCodes = async () => {
  try {
    const response = await fetch(API_URL)
    if (response.ok) {
      const rawData = await response.json()
      promoCodes.value = rawData.map(mapFromBackend)
    }
    else toast.add({ severity: 'error', summary: 'Błąd', detail: 'Nie udało się pobrać kodów', life: 3000 })
  } catch {
    toast.add({ severity: 'error', summary: 'Błąd', detail: 'Brak połączenia z serwerem', life: 3000 })
  }
}

// Statystyki na górny panel
const stats = computed(() => {
  const total = promoCodes.value.length
  const active = promoCodes.value.filter(c => c.status === 'Active').length
  const used = promoCodes.value.filter(c => c.status === 'Used').length
  const expired = promoCodes.value.filter(c => c.status === 'Expired').length
  const totalValue = promoCodes.value
      .filter(c => c.status === 'Active')
      .reduce((sum, c) => sum + (c.creditAmount || 0), 0)

  return { total, active, used, expired, totalValue }
})

// Bezpieczna konwersja dat bez zmiany strefy czasowej
const getSafeLocalDate = (date) => {
  if (!date) return null;
  const d = new Date(date);
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

// 2. GENEROWANIE KODÓW
const handleGenerateCodes = async (payload) => {
  isLoading.value = true
  try {
    const body = {
      creditAmount: payload.creditAmount,
      quantity: payload.quantity,
      allowLabelRedemption: payload.allowLabelRedemption,
      activeFrom: getSafeLocalDate(payload.validFrom),
      expiresOn: getSafeLocalDate(payload.validTo)
    }
    if (payload.label) body.label = payload.label

    const response = await fetch(`${API_URL}/generate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    })

    if (response.ok) {
      toast.add({ severity: 'success', summary: 'Sukces', detail: `Wygenerowano ${payload.quantity} kodów`, life: 3000 })
      await fetchCodes()
    } else {
      const error = await response.text()
      toast.add({ severity: 'error', summary: 'Błąd', detail: error || 'Błąd podczas generowania', life: 4000 })
    }
  } catch {
    toast.add({ severity: 'error', summary: 'Błąd', detail: 'Brak połączenia z serwerem', life: 3000 })
  } finally {
    isLoading.value = false
  }
}

// 3. SPRAWDZANIE KODU W TERMINALU (Zgodne z C# GET)
const handleLookupCode = async (inputValue) => {
  try {
    const isGuid = inputValue.length > 20
    const queryParam = isGuid ? `Code=${inputValue}` : `Label=${inputValue}`

    const response = await fetch(`${API_URL}/preview?${queryParam}`, {
      method: 'GET',
      headers: { 'Content-Type': 'application/json' }
    })

    if (response.ok) {
      const rawData = await response.json()
      terminalLookupData.value = mapFromBackend(rawData)
    } else {
      terminalLookupData.value = null
      toast.add({ severity: 'warn', summary: 'Nie znaleziono', detail: 'Kod nieaktywny lub nie istnieje', life: 3000 })
    }
  } catch {
    toast.add({ severity: 'error', summary: 'Błąd', detail: 'Brak połączenia z serwerem', life: 3000 })
  }
}

// 4. REALIZACJA KODU (Idealnie dopasowany Payload)
const handleRedeemCode = async (inputValue) => {
  try {
    const isGuid = inputValue.length > 20

    const payload = {}
    if (isGuid) {
      payload.Code = inputValue
    } else {
      payload.Label = inputValue
    }

    const response = await fetch(`${API_URL}/redeem`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    })

    if (response.ok) {
      const data = await response.json()
      terminalLookupData.value = null
      toast.add({ severity: 'success', summary: 'Zrealizowano', detail: `Dodano +${data.creditAmount ?? data.CreditAmount} PLN`, life: 5000 })
      await fetchCodes()
    } else {
      toast.add({ severity: 'warn', summary: 'Odmowa', detail: 'Kod nieważny, zużyty lub jeszcze nieaktywny', life: 4000 })
    }
  } catch {
    toast.add({ severity: 'error', summary: 'Błąd', detail: 'Brak połączenia z serwerem', life: 3000 })
  }
}

// 5. DEZAKTYWACJA MASOWA (Po GUID)
const handleDisableSelected = async (codesList) => {
  try {
    const response = await fetch(`${API_URL}/disable`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ codes: codesList })
    })

    if (response.ok) {
      const result = await response.json()
      toast.add({ severity: 'success', summary: 'Wyłączono', detail: result.message, life: 3000 })
      await fetchCodes()
    }
  } catch {
    toast.add({ severity: 'error', summary: 'Błąd', detail: 'Brak połączenia z serwerem', life: 3000 })
  }
}

// 6. DEZAKTYWACJA MASOWA (Po Etykiecie)
const handleDeleteByLabel = async (label) => {
  try {
    const response = await fetch(`${API_URL}/disable`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ label })
    })

    if (response.ok) {
      const result = await response.json()
      toast.add({ severity: 'success', summary: 'Wyłączono serię', detail: result.message, life: 3000 })
      await fetchCodes()
    }
  } catch {
    toast.add({ severity: 'error', summary: 'Błąd', detail: 'Brak połączenia z serwerem', life: 3000 })
  }
}

// 7. TRWAŁE USUWANIE KODÓW
const handleDeleteCode = async (codeId) => {
  try {
    const response = await fetch(API_URL, {
      method: 'DELETE',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ codes: [codeId] })
    })

    if (response.ok) {
      toast.add({ severity: 'success', summary: 'Usunięto', detail: 'Kod został trwale usunięty', life: 3000 })
      await fetchCodes()
    }
  } catch {
    toast.add({ severity: 'error', summary: 'Błąd', detail: 'Brak połączenia z serwerem', life: 3000 })
  }
}

// 8. EDYCJA KODU
const handleEditCode = async (updatedCode) => {
  try {
    const body = {
      codes: [updatedCode.code],
      label: updatedCode.label || '',
      creditAmount: updatedCode.creditAmount,
      activeFrom: getSafeLocalDate(updatedCode.validFrom),
      expiresOn: getSafeLocalDate(updatedCode.expiresAt),
      status: updatedCode.status
    }

    const response = await fetch(API_URL, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    })

    if (response.ok) {
      const result = await response.json()
      toast.add({ severity: 'success', summary: 'Zapisano', detail: result.message, life: 3000 })
      await fetchCodes()
    } else {
      const error = await response.text()
      toast.add({ severity: 'error', summary: 'Błąd', detail: error || 'Nie udało się zapisać zmian', life: 4000 })
    }
  } catch {
    toast.add({ severity: 'error', summary: 'Błąd', detail: 'Brak połączenia z serwerem', life: 3000 })
  }
}

onMounted(() => fetchCodes())
</script>

<template>
  <div class="p-6 md:p-8 max-w-6xl mx-auto space-y-8 bg-white rounded-2xl shadow-sm border border-slate-100 w-full font-sans">

    <Toast />

    <div class="flex justify-between items-end border-b border-slate-200 pb-4">
      <div>
        <h1 class="text-xl font-light tracking-tight text-slate-800">Sferity Promo</h1>
        <p class="text-xs text-slate-400 tracking-wide mt-1 uppercase">Zarządzanie kampaniami</p>
      </div>
      <Button icon="pi pi-refresh" severity="secondary" variant="text" size="small" rounded @click="fetchCodes" />
    </div>

    <PromoDashboard :stats="stats" />

    <div class="grid grid-cols-1 lg:grid-cols-12 gap-8">
      <div class="lg:col-span-8">
        <PromoGenerator :isLoading="isLoading" @generate="handleGenerateCodes" />
      </div>
      <div class="lg:col-span-4">
        <PromoTerminal :lookupData="terminalLookupData" @redeem="handleRedeemCode" @lookup="handleLookupCode" />
      </div>
    </div>

    <div class="pt-6 border-t border-slate-200">
      <PromoTable
          :promoCodes="promoCodes"
          :apiUrl="API_URL"
          @delete-by-label="handleDeleteByLabel"
          @disable-selected="handleDisableSelected"
          @delete-code="handleDeleteCode"
          @edit-code="handleEditCode"
      />
    </div>
  </div>
</template>