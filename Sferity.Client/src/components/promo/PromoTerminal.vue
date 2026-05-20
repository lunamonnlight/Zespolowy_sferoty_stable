<script setup>
import { ref } from 'vue'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import Tag from 'primevue/tag'

defineProps({ lookupData: Object })
const emit = defineEmits(['redeem', 'lookup'])
const actionInput = ref('')

const submitRedeem = () => {
  if (actionInput.value) {
    emit('redeem', actionInput.value.toUpperCase())
    actionInput.value = ''
  }
}
const submitLookup = () => {
  if (actionInput.value) emit('lookup', actionInput.value.toUpperCase())
}

const getStatusLabel = (s) => {
  const status = String(s).toLowerCase()
  if (status === 'active' || status === '0') return { label: 'Aktywny', severity: 'success' }
  if (status === 'used' || status === '1') return { label: 'Zużyty', severity: 'secondary' }
  return { label: 'Wygasły', severity: 'danger' }
}
</script>

<template>
  <div class="space-y-6 bg-slate-50 p-6 rounded-2xl border border-slate-100">
    <h2 class="text-sm font-semibold uppercase tracking-widest text-slate-400">Terminal autoryzacji</h2>
    <div class="flex flex-col gap-4">
      <InputText
          v-model="actionInput"
          @input="(e) => actionInput = e.target.value.toUpperCase()"
          placeholder="KOD LUB ETYKIETA..."
          class="w-full text-center tracking-widest text-sm h-11 uppercase"
          @keyup.enter="submitRedeem"
      />
      <div class="flex gap-2">
        <Button label="Sprawdź" severity="secondary" variant="outlined" @click="submitLookup" class="flex-1 h-12" />
        <Button label="Zrealizuj" severity="primary" @click="submitRedeem" class="flex-1 h-12" />
      </div>
    </div>

    <div v-if="lookupData" class="mt-4 p-4 bg-white border border-slate-200 rounded-xl flex flex-col gap-2 shadow-sm animate-fade-in">
      <div class="flex justify-between items-center pb-2 border-b border-slate-100">
        <span class="text-[10px] text-slate-400 uppercase tracking-widest">Informacje</span>
        <Tag :severity="getStatusLabel(lookupData.status).severity" :value="getStatusLabel(lookupData.status).label" variant="outlined" class="text-[9px]" />
      </div>
      <div class="flex justify-between items-end mt-2">
        <div>
          <div class="text-[10px] text-slate-500 uppercase">Wartość</div>
          <div class="text-xl font-medium">{{ lookupData.creditAmount }} PLN</div>
        </div>
      </div>
    </div>
  </div>
</template>