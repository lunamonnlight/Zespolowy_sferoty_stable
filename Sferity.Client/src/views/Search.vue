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

        <!-- 3. KONTO -->
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

        <!-- 5. POWIĄZANIA -->
        <router-link
            to="/connections"
            class="flex items-center w-full rounded-lg transition-all duration-200 cursor-pointer px-3 py-2.5 my-1 gap-3 text-surface-300 hover:bg-surface-700/40 hover:text-surface-100"
            active-class="bg-primary-500/15 text-primary-400 font-bold"
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="20" height="20" fill="currentColor" class="shrink-0"><path d="M200,144a31.84,31.84,0,0,0-19.53,6.68l-46.19-33a32.17,32.17,0,0,0,0-19.38l46.19-33A32,32,0,1,0,168,48a31.84,31.84,0,0,0,19.53,6.68L141.34,87.64a32,32,0,1,0,0,80.72l46.19,33A31.84,31.84,0,0,0,168,208a32,32,0,1,0,32-64Zm0-128a16,16,0,1,1-16,16A16,16,0,0,1,200,16ZM96,144a16,16,0,1,1,16-16A16,16,0,0,1,96,144Zm104,80a16,16,0,1,1,16-16A16,16,0,0,1,200,224Z"></path></svg>
          <span class="text-sm font-medium">Powiązania</span>
        </router-link>
      </section>

      <!-- Główna sekcja -->
      <main class="flex flex-col flex-1 overflow-hidden p-2">
        <div class="flex flex-col flex-1 min-h-0 overflow-hidden rounded-xl border border-surface-700 bg-surface-800">

          <!-- Nagłówek -->
          <div class="flex h-20 items-center border-b border-surface-700 bg-surface-900 rounded-t-xl px-6 gap-4">
            <i class="pi pi-share-alt" style="font-size: 1.7rem; color: var(--primary-400)"></i>
            <div class="flex flex-col gap-1">
              <span class="text-xl">Graf powiązań</span>
              <span class="text-xs text-surface-300">Wyszukaj firmę, aby zobaczyć jej powiązania</span>
            </div>
          </div>

          <!-- Zawartość: SearchBar + Graf -->
          <div class="flex flex-col flex-1 min-h-0 overflow-hidden">
            <SearchBar
                :query="query"
                :loading="loading"
                :search-results="searchResults"
                :error-msg="errorMsg"
                @search="handleSearch"
                @input="onQueryInput"
                @select="onResultSelect"
            />

            <div style="flex: 1; min-height: 0; height: 100%; overflow: hidden;">
              <GraphCanvas
                  :nodes="nodes"
                  :edges="edges"
                  :graph-loaded="graphLoaded"
                  :loading="loading"
                  :expanded-ids="expandedIds"
                  :expanding-id="expandingId"
                  :minimap-color="minimapColor"
                  @node-click="onNodeClick"
                  @node-context-menu="onNodeContextMenu"
              />
            </div>
          </div>

        </div>
      </main>

    </div>

    <ContextMenu
        v-if="contextMenu"
        :x="contextMenu.x"
        :y="contextMenu.y"
        :node="contextMenu.node"
        :detail="contextDetail"
        :loading="contextLoading"
        @close="closeContextMenu"
    />

  </div>
</template>

<script setup lang="ts">
import SearchBar   from '../components/search/SearchBar.vue'
import GraphCanvas from '../components/search/GraphCanvas.vue'
import ContextMenu from '../components/search/ContextMenu.vue'
import { useGraph } from '../composables/useGraph'
import { useAuth }  from '../composables/useAuth'

const { currentUser } = useAuth()

const {
  query, loading, errorMsg, searchResults, graphLoaded,
  nodes, edges, expandedIds, expandingId,
  contextMenu, contextDetail, contextLoading,
  onInput, handleSearch, onResultSelect,
  onNodeClick, onNodeContextMenu, closeContextMenu,
  minimapColor,
} = useGraph()

function onQueryInput(val: string) {
  query.value = val
  onInput()
}
</script>

<style scoped>
</style>
