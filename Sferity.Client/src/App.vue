<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuth } from './composables/useAuth'

const { currentUser, setTester } = useAuth()
const usersList = ref<any[]>([])

const router = useRouter()
const route = useRoute()

async function fetchUsersForTester() {
  try {
    // Dodajemy cache-busting, aby mieć pewność, że pobieramy świeże dane z pliku JSON
    const res = await fetch('http://localhost:5100/api/admin/data?t=' + Date.now())
    if (res.ok) {
      const data = await res.json()
      usersList.value = data.users || []
    }
  } catch (err) {
    console.error("Błąd pobierania bazy testera", err)
  }
}

// ZMIENIONA FUNKCJA
async function onTesterChange(event: Event) {
  const select = event.target as HTMLSelectElement
  const userId = parseInt(select.value)

  // 1. Zawsze pobierz świeże dane z serwera przy zmianie, 
  // żeby sprawdzić czy użytkownik nie został właśnie zablokowany
  await fetchUsersForTester()

  if (!userId) {
    setTester(null)
    kickIfNotAdmin(null)
    return
  }

  const user = usersList.value.find(u => u.id === userId)
  if (user) {
    // Upewniamy się, że przesyłamy obiekt z poprawnymi nazwami pól (camelCase)
    const normalizedUser = { ...user }
    setTester(normalizedUser)
    kickIfNotAdmin(normalizedUser)
  }
}

function kickIfNotAdmin(user: any) {
  if (route.path.startsWith('/admin')) {
    // Jeśli rola to nie admin LUB użytkownik jest zablokowany - wyrzuć na home
    if (!user || user.role !== 'admin' || user.isBlocked) {
      console.warn("Dostęp zabroniony lub konto zablokowane. Wyrzucam na Home.")
      router.push('/')
    }
  }
}

onMounted(() => {
  fetchUsersForTester()
})
</script>

<template>
  <!-- PŁYWAJĄCY PASEK TESTERA -->
  <div class="fixed top-0 right-0 w-auto bg-yellow-600 text-white p-2 px-4 flex justify-between items-center z-[9999] rounded-bl-xl shadow-2xl opacity-95">
    <div class="font-bold tracking-widest mr-6 text-xs">🛠️ TESTER</div>
    <div class="flex items-center gap-2">
      <select
          class="bg-surface-800 border border-surface-600 rounded px-2 py-1 text-surface-0 text-xs outline-none cursor-pointer hover:bg-surface-700 transition-colors"
          @change="onTesterChange"
      >
        <option value="" class="bg-surface-900 text-surface-300">-- Wylogowany --</option>
        <option
            v-for="user in usersList"
            :key="user.id"
            :value="user.id"
            :selected="currentUser?.id === user.id"
            class="bg-surface-900 text-surface-0 font-medium"
        >
          <!-- Używamy małego 'isBlocked' -->
          {{ user.username }} ({{ user.role }}) {{ user.isBlocked ? '🛑 BLOKADA' : '' }}
        </option>
      </select>
      <button v-if="currentUser" @click="setTester(null)" class="bg-red-600 hover:bg-red-700 px-3 py-1 rounded text-xs font-bold transition-colors">Wyczyść</button>
    </div>
  </div>

  <!-- TWOJA NAWIGACJA SFERITY -->
  <nav>
    <div class="flex items-center justify-between border-b border-b-surface-700 bg-surface-800 px-4 py-1 text-4xl text-surface-0/80 sticky top-0 z-50">
      <ul class="font-bold">Sferity</ul>
      <ul></ul>
    </div>
  </nav>

  <main>
    <!-- EKRAN BLOKADY (Upewniamy się, że sprawdzamy 'isBlocked' z małej litery) -->
    <div v-if="currentUser?.isBlocked" class="h-[calc(100vh-60px)] flex flex-col items-center justify-center bg-surface-900 text-surface-0">
      <i class="pi pi-ban text-red-500 mb-4" style="font-size: 6rem"></i>
      <h1 class="text-4xl font-bold text-red-500 mb-2">KONTO ZABLOKOWANE</h1>
      <p class="text-surface-300">Skontaktuj się z administratorem lub zmień usera wyżej.</p>
    </div>

    <!-- WŁAŚCIWA APLIKACJA -->
    <router-view v-else />
  </main>
</template>

<style>
/* Reset bazowy */
body, html { margin: 0; padding: 0; }
</style>