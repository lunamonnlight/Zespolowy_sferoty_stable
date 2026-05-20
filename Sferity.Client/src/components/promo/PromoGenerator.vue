<script setup>
import { ref, onMounted } from 'vue'
import InputNumber from 'primevue/inputnumber'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import Checkbox from 'primevue/checkbox'
import Button from 'primevue/button'
import DatePicker from 'primevue/datepicker'

defineProps({ isLoading: Boolean })
const emit = defineEmits(['generate'])

const genAmount = ref(100)
const genLabel = ref('')
const genQuantity = ref(1)
const genAllowLabelRedemption = ref(false)

const genValidFrom = ref(new Date())
const genValidTo = ref(null)

const genDurationValue = ref(7)
const genDurationUnit = ref('days')
const durationOptions = [
  { label: 'Dni', value: 'days' },
  { label: 'Miesiące', value: 'months' }
]

// KIERUNEK 1: Zmiana czasu trwania -> Aktualizacja daty "Do"
const updateEndDate = () => {
  if (!genValidFrom.value || genDurationValue.value === null) return;
  const d = new Date(genValidFrom.value)

  if (genDurationUnit.value === 'days') {
    d.setDate(d.getDate() + genDurationValue.value)
  } else if (genDurationUnit.value === 'months') {
    d.setMonth(d.getMonth() + genDurationValue.value)
  }
  genValidTo.value = d
}

// KIERUNEK 2: Ręczny wybór daty w kalendarzu "Do" -> Aktualizacja czasu trwania
const updateDuration = () => {
  if (!genValidFrom.value || !genValidTo.value) return;

  const from = new Date(genValidFrom.value)
  const to = new Date(genValidTo.value)

  // Zerujemy czas (godziny/minuty), aby precyzyjnie policzyć pełne dni
  from.setHours(0, 0, 0, 0)
  to.setHours(0, 0, 0, 0)

  const diffTime = to.getTime() - from.getTime()
  const diffDays = Math.round(diffTime / (1000 * 60 * 60 * 24))

  if (diffDays >= 0) {
    genDurationValue.value = diffDays
    genDurationUnit.value = 'days' // Zawsze wymuszamy Dni dla precyzji po kliknięciu w kalendarz
  } else {
    // Zabezpieczenie: jeśli wybrano datę końcową wcześniejszą niż początkową
    genDurationValue.value = 0
    genValidTo.value = genValidFrom.value
  }
}

// Inicjalizacja domyślnej daty przy załadowaniu komponentu
onMounted(() => {
  updateEndDate()
})

const submit = () => {
  emit('generate', {
    creditAmount: genAmount.value,
    quantity: genQuantity.value,
    label: genLabel.value.trim().toUpperCase() || undefined,
    allowLabelRedemption: genAllowLabelRedemption.value,
    validFrom: genValidFrom.value,
    validTo: genValidTo.value
  })
  genLabel.value = ''
  genQuantity.value = 1
}
</script>

<template>
  <div class="space-y-6">
    <h2 class="text-sm font-semibold uppercase tracking-widest text-slate-400">Kreator Kodów</h2>
    <div class="grid grid-cols-1 md:grid-cols-3 gap-6">

      <div class="flex flex-col gap-2">
        <label class="text-[11px] font-medium text-slate-500 uppercase">Nominał (PLN)</label>
        <InputNumber v-model="genAmount" mode="currency" currency="PLN" locale="pl-PL" fluid class="h-11" />
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-[11px] font-medium text-slate-500 uppercase">Etykieta kampanii</label>
        <InputText v-model="genLabel" @input="(e) => genLabel = e.target.value.toUpperCase()" class="uppercase h-11 font-medium" placeholder="NP. LATO2026" fluid />
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-[11px] font-medium text-slate-500 uppercase">Nakład sztuk</label>
        <InputNumber v-model="genQuantity" showButtons :min="1" fluid class="h-11" />
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-[11px] font-medium text-slate-500 uppercase">Ważne od (D/M/Y)</label>
        <DatePicker v-model="genValidFrom" @update:modelValue="updateEndDate" dateFormat="dd.mm.yy" showIcon fluid class="h-11" />
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-[11px] font-medium text-slate-500 uppercase">Ważne do (D/M/Y)</label>
        <DatePicker v-model="genValidTo" @update:modelValue="updateDuration" dateFormat="dd.mm.yy" showIcon fluid class="h-11" />
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-[11px] font-medium text-slate-500 uppercase">Długość ważności</label>
        <div class="flex gap-2">
          <InputNumber v-model="genDurationValue" @update:modelValue="updateEndDate" fluid class="w-1/2 h-11" />
          <Select v-model="genDurationUnit" @update:modelValue="updateEndDate" :options="durationOptions" optionLabel="label" optionValue="value" class="w-1/2 h-11" />
        </div>
      </div>

    </div>

    <div class="flex items-center gap-3 pt-2">
      <Checkbox v-model="genAllowLabelRedemption" :binary="true" inputId="allowLabel" />
      <label for="allowLabel" class="text-xs text-slate-500 cursor-pointer">Zezwól na realizację samą etykietą</label>
    </div>

    <Button label="Wygeneruj kody" icon="pi pi-check" @click="submit" :loading="isLoading" class="px-8 h-12" />
  </div>
</template>