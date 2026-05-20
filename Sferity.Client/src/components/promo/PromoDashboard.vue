<script setup>
defineProps({
  stats: {
    type: Object,
    required: true
  }
})
</script>

<template>
  <div class="grid grid-cols-2 md:grid-cols-5 gap-4">
    <div class="border-l border-surface-200 pl-4">
      <div class="text-[9px] font-semibold text-surface-400 uppercase tracking-[0.2em] mb-1">Baza kodów</div>
      <div class="text-2xl font-light">{{ stats.total }}</div>
    </div>
    <div class="border-l border-surface-200 pl-4">
      <div class="text-[9px] font-semibold text-emerald-500 uppercase tracking-[0.2em] mb-1">Aktywne</div>
      <div class="text-2xl font-light">{{ stats.active }}</div>
    </div>
    <div class="border-l border-surface-200 pl-4">
      <div class="text-[9px] font-semibold text-surface-400 uppercase tracking-[0.2em] mb-1">Zużyte</div>
      <div class="text-2xl font-light">{{ stats.used }}</div>
    </div>
    <div class="border-l border-surface-200 pl-4">
      <div class="text-[9px] font-semibold text-red-400 uppercase tracking-[0.2em] mb-1">Wyłączone</div>
      <div class="text-2xl font-light">{{ stats.expired }}</div>
    </div>
    <div class="border-l border-primary-200 pl-4">
      <div class="text-[9px] font-semibold text-primary-500 uppercase tracking-[0.2em] mb-1">Wartość aktywnych</div>
      <div class="text-2xl font-light">{{ stats.totalValue.toLocaleString() }} zł</div>
    </div>
  </div>
</template>