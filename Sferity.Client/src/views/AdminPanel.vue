<script setup lang="ts">
import { ref, onMounted } from 'vue'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import { useAuth } from '../composables/useAuth'
import PromoCodeComponent from '../components/PromoCodeComponent.vue'

// Zakładki panelu admina
const activeTab = ref<'main' | 'promo'>('main')

// Stan kodów promocyjnych

// 1. Importujemy wyizolowany komponent

const { currentUser } = useAuth()

const state = ref({ users: [], funds: [], logs: [], searchLogs: [] })
const fundForm = ref({ userId: null, amount: 0 })

// ... (tutaj zostają TYLKO funkcje: loadData, updateBalance, deleteUser, getUserBalance, toggleBlock)

onMounted(() => {
  loadData()
  // loadPromoCodes() usunięte z onMounted, ponieważ komponent PromoCodeComponent
  // powinien sam pobierać swoje dane, gdy zostanie zamontowany na ekranie
})
// Formularz do zarządzania saldem
async function loadData() {
  try {
    const response = await fetch('http://localhost:5100/api/admin/data')
    const json = await response.json()
    state.value = {
      users: json.users || [],
      funds: json.funds || [],
      logs: json.logs || [],
      searchLogs: json.searchLogs || []
    }
  } catch (e) {
    console.error(e)
  }
}

// Funkcja obsługująca zarówno dodawanie jak i odejmowanie środków
async function updateBalance(action: 'add' | 'subtract') {
  if (!fundForm.value.userId || fundForm.value.amount <= 0) return

  const endpoint = action === 'add' ? 'add-fund' : 'subtract-fund'

  await fetch(`http://localhost:5100/api/admin/${endpoint}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      userId: fundForm.value.userId,
      amount: fundForm.value.amount
    })
  })

  fundForm.value.amount = 0 // Reset kwoty po operacji
  await loadData()
}

// Usuwanie użytkownika
async function deleteUser(id: number) {
  if (!confirm('Czy na pewno chcesz bezpowrotnie usunąć tego użytkownika i jego fundusze?')) return

  await fetch(`http://localhost:5100/api/admin/delete-user/${id}`, {
    method: 'DELETE'
  })
  await loadData()
}

// Pomocnicza funkcja do pobierania salda konkretnego użytkownika
const getUserBalance = (userId: number) => {
  const fund = state.value.funds.find(f => f.userId === userId)
  return fund ? fund.amount : 0
}

async function toggleBlock(id: number) {
  if (!id) {
    console.error("Błąd: Próba blokady użytkownika bez ID");
    return;
  }

  try {
    const response = await fetch(`http://localhost:5100/api/admin/toggle-user/${id}`, {
      method: 'POST'
    })

    if (response.ok) {
      await loadData() // Odświeżamy tabelę po sukcesie
    } else {
      console.error("Serwer zwrócił błąd przy próbie zmiany blokady");
    }
  } catch (e) {
    console.error("Błąd sieci przy toggleBlock:", e)
  }
}

onMounted(() => {
  loadData()

})
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

      <main class="flex-1 overflow-auto p-2 mt-2 mb-2">
        <div class="h-full rounded-xl border border-surface-700 bg-surface-800 p-6 overflow-y-auto">
          <h1 class="text-2xl font-bold mb-4 text-primary-400">Panel Administracyjny</h1>

          <!-- ZAKŁADKI -->
          <div class="flex gap-1 mb-6 border-b border-surface-600">
            <button
                @click="activeTab = 'main'"
                :class="[
                'px-4 py-2 text-sm font-semibold roundeżd-t-lg transition-all duration-200 border-b-2 -mb-px flex items-center gap-2',
                activeTab === 'main'
                  ? 'border-primary-400 text-primary-400 bg-surface-700/40'
                  : 'border-transparent text-surface-400 hover:text-surface-100 hover:bg-surface-700/20'
              ]"
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="15" height="15" fill="currentColor"><path d="M208,40H48A16,16,0,0,0,32,56V200a16,16,0,0,0,16,16H208a16,16,0,0,0,16-16V56A16,16,0,0,0,208,40Zm0,160H48V56H208V200Zm-32-56a8,8,0,0,1-8,8H88a8,8,0,0,1,0-16h80A8,8,0,0,1,176,144Zm0-32a8,8,0,0,1-8,8H88a8,8,0,0,1,0-16h80A8,8,0,0,1,176,112Z"></path></svg>
              Zarządzanie
            </button>
            <button
                @click="activeTab = 'promo'"
                :class="[
                'px-4 py-2 text-sm font-semibold rounded-t-lg transition-all duration-200 border-b-2 -mb-px flex items-center gap-2',
                activeTab === 'promo'
                  ? 'border-primary-400 text-primary-400 bg-surface-700/40'
                  : 'border-transparent text-surface-400 hover:text-surface-100 hover:bg-surface-700/20'
              ]"
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="15" height="15" fill="currentColor"><path d="M235.32,104.55,151.47,20.69a16,16,0,0,0-22.63,0L20.68,128.84a16,16,0,0,0,0,22.63l83.86,83.86A16,16,0,0,0,115.9,239h.45a16,16,0,0,0,11.31-4.68L235.32,127.18a16,16,0,0,0,0-22.63ZM96,176a16,16,0,1,1,16-16A16,16,0,0,1,96,176Zm48-48a16,16,0,1,1,16-16A16,16,0,0,1,144,128Z"></path></svg>
              Kody Promocyjne
            </button>
          </div>

          <!-- ZAKŁADKA: ZARZĄDZANIE -->
          <div v-if="activeTab === 'main'">
            <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <!-- Zarządzanie Portfelem -->
              <div class="bg-surface-700/30 p-4 rounded-lg border border-surface-600">
                <h2 class="text-lg font-semibold mb-4">Zarządzanie saldem użytkowników</h2>
                <div class="flex flex-col gap-3">
                  <select v-model="fundForm.userId" class="p-2 rounded bg-surface-900 border border-surface-600 text-sm text-surface-0">
                    <option :value="null">Wybierz użytkownika...</option>
                    <option v-for="user in state.users" :key="user.id" :value="user.id">
                      {{ user.username }} (Saldo: {{ getUserBalance(user.id) }} PLN)
                    </option>
                  </select>
                  <InputText v-model.number="fundForm.amount" type="number" placeholder="Kwota operacji (PLN)" class="w-full" />
                  <div class="flex gap-2">
                    <Button label="Dodaj środki" icon="pi pi-plus" severity="success" @click="updateBalance('add')" class="flex-1" />
                    <Button label="Odejmij środki" icon="pi pi-minus" severity="warning" @click="updateBalance('subtract')" class="flex-1" />
                  </div>
                </div>
                <div class="mt-4">
                  <p class="text-xs text-surface-400 mb-2 uppercase font-bold">Podsumowanie sald:</p>
                  <div class="max-h-32 overflow-y-auto">
                    <div v-for="user in state.users" :key="user.id" class="bg-surface-900/50 p-2 rounded flex justify-between mb-1 text-sm">
                      <span>{{ user.username }}</span>
                      <span :class="getUserBalance(user.id) < 0 ? 'text-red-400' : 'text-green-400'">
                        {{ getUserBalance(user.id) }} PLN
                      </span>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Użytkownicy, Blokady i Usuwanie -->
              <div class="bg-surface-700/30 p-4 rounded-lg border border-surface-600">
                <h2 class="text-lg font-semibold mb-4">Użytkownicy i uprawnienia</h2>
                <DataTable :value="state.users" class="p-datatable-sm text-sm">
                  <Column field="username" header="Użytkownik"></Column>
                  <Column header="Status">
                    <template #body="slotProps">
                      <span :class="slotProps.data.isBlocked ? 'text-red-400' : 'text-green-400'">
                        {{ slotProps.data.isBlocked ? 'Zablokowany' : 'Aktywny' }}
                      </span>
                    </template>
                  </Column>
                  <Column header="Akcja">
                    <template #body="slotProps">
                      <div class="flex gap-2">
                        <Button
                            :icon="slotProps.data.isBlocked ? 'pi pi-unlock' : 'pi pi-lock'"
                            :severity="slotProps.data.isBlocked ? 'success' : 'danger'"
                            @click="toggleBlock(slotProps.data.id)"
                            size="small"
                            text
                        />
                        <Button
                            v-if="slotProps.data.isBlocked"
                            icon="pi pi-trash"
                            severity="danger"
                            @click="deleteUser(slotProps.data.id)"
                            size="small"
                            text
                        />
                      </div>
                    </template>
                  </Column>
                </DataTable>
              </div>
            </div>

            <!-- Historia wyszukiwań -->
            <div class="mt-6 bg-surface-700/30 p-4 rounded-lg border border-surface-600">
              <h2 class="text-lg font-semibold mb-2 text-primary-400">Historia wyszukiwań</h2>
              <DataTable :value="state.searchLogs" class="p-datatable-sm text-xs" paginator :rows="5">
                <Column header="Data">
                  <template #body="s">
                    {{ s.data.searchTimestamp ? new Date(s.data.searchTimestamp).toLocaleString() : '---' }}
                  </template>
                </Column>
                <Column header="Użytkownik">
                  <template #body="s">
                    <span class="font-bold text-primary-400">{{ s.data.username }}</span>
                  </template>
                </Column>
                <Column header="Cel (NIP/KRS)">
                  <template #body="s">
                    {{ s.data.searchedNip || s.data.searchedKrs || '---' }}
                  </template>
                </Column>
                <Column header="Koszt">
                  <template #body="s">
                    {{ s.data.cost }} PLN
                  </template>
                </Column>
                <Column header="Status" class="text-center">
                  <template #body="s">
                    <i :class="s.data.isSuccess ? 'pi pi-check text-green-500' : 'pi pi-times text-red-500'" :title="s.data.errorMessage"></i>
                  </template>
                </Column>
              </DataTable>
            </div>

            <!-- Logi systemowe -->
            <div class="mt-6 bg-surface-900/50 p-4 rounded-lg border border-surface-700">
              <h2 class="text-lg font-semibold mb-2">Logi systemowe</h2>
              <div class="h-40 overflow-y-auto text-xs font-mono space-y-1">
                <div v-for="(log, i) in state.logs" :key="i" class="border-b border-surface-700 pb-1">
                  <span class="text-primary-500">[{{ log.timestamp ? new Date(log.timestamp).toLocaleString() : '---' }}]</span>
                  <span class="text-surface-200"> {{ log.action }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- ZAKŁADKA: KODY PROMOCYJNE -->
          <div v-if="activeTab === 'promo'">
            <PromoCodeComponent />
          </div>

        </div>
      </main>
    </div>
  </div>
</template>