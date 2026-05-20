<template>
  <div class="search-bar-wrapper">
    <div class="search-bar">
      <svg class="search-icon" xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24"
           fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <circle cx="11" cy="11" r="8" /><line x1="21" y1="21" x2="16.65" y2="16.65" />
      </svg>
      <input
          :value="query"
          type="text"
          placeholder="Wpisz nazwę firmy lub osoby..."
          class="search-input"
          @keyup.enter="$emit('search')"
          @input="$emit('input', ($event.target as HTMLInputElement).value)"
      />
      <button class="search-btn" :disabled="loading" @click="$emit('search')">
        <span v-if="!loading">Szukaj</span>
        <span v-else class="spinner" />
      </button>
    </div>

    <!-- DROPDOWN WYNIKÓW -->
    <div v-if="searchResults.length > 0" class="results-dropdown">
      <div
          v-for="result in searchResults"
          :key="result.id"
          class="result-item"
          @click="$emit('select', result)"
      >
        <span class="result-badge" :class="result.entityType">
          {{ result.entityType === 'organizacja' ? 'Org' : 'Osoba' }}
        </span>
        <span class="result-label">{{ result.label }}</span>
        <span v-if="result.Krs" class="result-krs">KRS {{ result.krs }}</span>
      </div>
    </div>

    <div v-if="errorMsg" class="error-bar">{{ errorMsg }}</div>
  </div>
</template>

<script setup lang="ts">
import type { EntitySummaryDto } from '../types/graph'

defineProps<{
  query: string
  loading: boolean
  searchResults: EntitySummaryDto[]
  errorMsg: string
}>()

defineEmits<{
  search: []
  input: [value: string]
  select: [result: EntitySummaryDto]
}>()
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;600&family=DM+Mono:wght@400;500&display=swap');

.search-bar-wrapper {
  position: relative;
  z-index: 100;
  padding: 1.25rem 1.5rem 0;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.search-bar {
  display: flex;
  align-items: center;
  width: 100%;
  max-width: 640px;
  background: var(--surface-800);
  border: 1px solid #2e2e4a;
  border-radius: 12px;
  padding: 0.375rem 0.375rem 0.375rem 1rem;
  gap: 0.5rem;
  box-shadow: 0 8px 32px rgba(0,0,0,0.4);
  transition: border-color 0.2s;
}
.search-bar:focus-within { border-color: #6366f1; }
.search-icon { color: #6b7280; flex-shrink: 0; }

.search-input {
  flex: 1;
  background: transparent;
  border: none;
  outline: none;
  font-size: 0.95rem;
  color: black;
  font-family: 'DM Sans', sans-serif;
}
.search-input::placeholder { color: #4b5563; }

.search-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0.55rem 1.2rem;
  background: #6366f1;
  color: #fff;
  border: none;
  border-radius: 8px;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  font-family: 'DM Sans', sans-serif;
  transition: background 0.2s, opacity 0.2s;
  min-width: 80px;
}
.search-btn:hover:not(:disabled) { background: #4f46e5; }
.search-btn:disabled { opacity: 0.5; cursor: default; }

.spinner {
  width: 16px; height: 16px;
  border: 2px solid rgba(255,255,255,0.3);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }

.results-dropdown {
  width: 100%;
  max-width: 640px;
  background: var(--surface-800);
  border: 1px solid #2e2e4a;
  border-top: none;
  border-radius: 0 0 12px 12px;
  overflow: hidden;
  box-shadow: 0 12px 40px rgba(0,0,0,0.5);
}

.result-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1rem;
  cursor: pointer;
  transition: background 0.15s;
}
.result-item:hover { background: #a9a9a9 ; }

.result-badge {
  font-size: 0.7rem;
  font-weight: 600;
  padding: 0.15rem 0.5rem;
  border-radius: 4px;
  font-family: 'DM Mono', monospace;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}
.result-badge.organizacja { background: #312e81; color: #a5b4fc; }
.result-badge.osoba       { background: #134e4a; color: #5eead4; }

.result-label { flex: 1; font-size: 0.9rem; font-weight: 500; color: black; }
.result-krs   { font-size: 0.78rem; color: #6b7280; font-family: 'DM Mono', monospace; }

.error-bar {
  width: 100%;
  max-width: 640px;
  margin-top: 0.5rem;
  padding: 0.6rem 1rem;
  background: #2d1515;
  color: #f87171;
  border-radius: 8px;
  font-size: 0.85rem;
}
</style>
