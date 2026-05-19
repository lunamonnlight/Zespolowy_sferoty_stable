<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import { useAuth } from '../composables/useAuth'

const { currentUser } = useAuth()
const data = ref({ balance: 0, currency: 'PLN', searchLogs: [] })
const promoCode = ref('')

async function fetchMyData() {
  if (!currentUser.value) return
  try {
    const res = await fetch(`http://localhost:5100/api/admin/user-data/${currentUser.value.id}`)
    if (res.ok) {
      data.value = await res.json()
    }
  } catch (e) {
    console.error("Błąd pobierania danych użytkownika", e)
  }
}

// Reaguj na zmianę użytkownika w testerze
watch(() => currentUser.value?.id, fetchMyData)
onMounted(fetchMyData)
</script>

<template>
  <div class="flex h-screen flex-col bg-surface-900 text-surface-0">
    <div class="flex flex-1 overflow-hidden">

      <!-- Sidebar -->
      <section class="sidebar overflow-y-auto rounded-r-rm border-y rounded-xl border-r bg-surface-800 border-surface-700 transition-all duration-300 w-60 mt-4 mb-4 p-2">
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
      </section>

      <!-- Main Content -->
      <main class="flex-1 overflow-auto p-4 mt-2 mb-2">
        <div class="h-full rounded-xl border border-surface-700 bg-surface-800 p-6 overflow-y-auto">
          <header class="flex justify-between items-end mb-8 border-b border-surface-700 pb-6">
            <div>
              <h1 class="text-3xl font-bold text-primary-400">Moje Konto</h1>
              <p class="text-surface-400 mt-1">Zalogowany jako: <span class="text-surface-0 font-semibold">{{ currentUser?.username }}</span></p>
            </div>
            <div class="bg-surface-900 p-4 rounded-xl border border-primary-500/30 text-right min-w-[180px]">
              <p class="text-xs font-bold text-surface-500 uppercase tracking-tighter">Dostępne środki</p>
              <p class="text-3xl font-mono text-green-400">{{ data.balance }} {{ data.currency }}</p>
            </div>
          </header>

          <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <!-- Kody Promocyjne -->
            <div class="bg-surface-700/30 p-6 rounded-xl border border-surface-600 h-fit">
              <h2 class="text-xl font-bold mb-2">Kod promocyjny</h2>
              <p class="text-sm text-surface-400 mb-4">Wpisz kod, aby doładować portfel.</p>
              <div class="flex flex-col gap-3">
                <InputText v-model="promoCode" placeholder="Np. START2026" class="w-full" />
                <Button label="Aktywuj" icon="pi pi-ticket" @click="promoCode = ''" class="w-full" />
              </div>
            </div>

            <!-- Historia użytkownika -->
            <div class="lg:col-span-2 bg-surface-700/30 p-6 rounded-xl border border-surface-600">
              <h2 class="text-xl font-bold mb-4 text-surface-100">Twoja historia wyszukiwań</h2>
              <DataTable :value="data.searchLogs" class="p-datatable-sm text-sm" paginator :rows="10">
                <template #empty> Brak wykonanych wyszukiwań. </template>

                <Column header="Data">
                  <template #body="s">
                    <!-- Używamy searchTimestamp (z małej litery) -->
                    {{ s.data.searchTimestamp ? new Date(s.data.searchTimestamp).toLocaleString() : '---' }}
                  </template>
                </Column>

                <Column header="Cel (NIP/KRS)">
                  <template #body="s">
                    <!-- Sprawdzamy oba pola (z małej litery) -->
                    {{ s.data.searchedNip || s.data.searchedKrs || '---' }}
                  </template>
                </Column>

                <Column header="Koszt">
                  <template #body="s">
                    <!-- cost (z małej litery) -->
                    {{ s.data.cost }} PLN
                  </template>
                </Column>

                <Column header="Status">
                  <template #body="s">
                    <!-- isSuccess (z małej litery) -->
                    <span :class="s.data.isSuccess ? 'text-green-500' : 'text-red-500'">
        {{ s.data.isSuccess ? 'Sukces' : 'Błąd' }}
      </span>
                  </template>
                </Column>
              </DataTable>
            </div>
          </div>
        </div>
      </main>

    </div>
  </div>
</template>