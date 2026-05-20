<template>
  <div
      class="context-menu"
      :style="{ top: y + 'px', left: x + 'px' }"
      @click.stop
  >
    <!-- Nagłówek -->
    <div class="cm-header">
      <span class="cm-badge" :class="node.type">
        {{ node.type === 'person' || node.type === 'person-foreign' ? 'Osoba' : 'Org' }}
      </span>
      <span class="cm-title">{{ node.data.Label }}</span>
      <button class="cm-close" @click="$emit('close')">✕</button>
    </div>

    <!-- Ładowanie -->
    <div v-if="loading" class="cm-loading">
      <span class="spinner" />
    </div>

    <!-- Dane organizacji -->
    <template v-else-if="detail && detail.EntityType === 'organizacja'">
      <div class="cm-section">
        <div v-if="detail.FormaPrawna" class="cm-row">
          <span class="cm-key">Forma prawna</span>
          <span class="cm-val">{{ detail.FormaPrawna }}</span>
        </div>
        <div v-if="detail.Krs" class="cm-row">
          <span class="cm-key">KRS</span>
          <span class="cm-val mono">{{ detail.Krs }}</span>
        </div>
        <div v-if="detail.Nip" class="cm-row">
          <span class="cm-key">NIP</span>
          <span class="cm-val mono">{{ detail.Nip }}</span>
        </div>
        <div v-if="detail.Regon" class="cm-row">
          <span class="cm-key">REGON</span>
          <span class="cm-val mono">{{ detail.Regon }}</span>
        </div>
        <div v-if="detail.PkdDzial" class="cm-row">
          <span class="cm-key">PKD</span>
          <span class="cm-val">{{ detail.PkdDzial }}</span>
        </div>
        <div v-if="detail.Adres" class="cm-row">
          <span class="cm-key">Adres</span>
          <span class="cm-val">{{ detail.Adres }}</span>
        </div>
        <div v-if="detail.WLikwidacji || detail.WUpadlosci" class="cm-row">
          <span class="cm-key">Status</span>
          <span class="cm-val warn">
            {{ detail.WLikwidacji ? 'W likwidacji' : '' }}
            {{ detail.WUpadlosci  ? 'W upadłości'  : '' }}
          </span>
        </div>
      </div>
      <div v-if="detail.connections && detail.connections.length" class="cm-section cm-connections">
        <div class="cm-section-title">Powiązane osoby</div>
        <div v-for="c in detail.connections" :key="c.targetId" class="cm-conn-row">
          <span class="cm-conn-label">{{ c.targetLabel }}</span>
          <span class="cm-conn-rel" :class="{ active: c.isActive }">
            {{ c.Opis || c.RelationType }}
          </span>
        </div>
      </div>
    </template>

    <!-- Dane osoby -->
    <template v-else-if="detail">
      <div v-if="detail.connections && detail.connections.length" class="cm-section cm-connections">
        <div class="cm-section-title">Powiązane organizacje</div>
        <div v-for="c in detail.connections" :key="c.targetId" class="cm-conn-row">
          <span class="cm-conn-label">{{ c.targetLabel }}</span>
          <span class="cm-conn-rel" :class="{ active: c.isActive }">
            {{ c.opis || c.relationType }}
          </span>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import type { Node } from '@vue-flow/core'
import type { EntityDetailDto } from '../types/graph'
import { onMounted } from 'vue'

const props = defineProps<{
  x: number
  y: number
  node: Node
  detail: EntityDetailDto | null
  loading: boolean
}>()

onMounted(() => {
  console.log(props)
})

defineEmits<{
  close: []
}>()
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;600&family=DM+Mono:wght@400;500&display=swap');

.spinner {
  width: 16px; height: 16px;
  border: 2px solid rgba(255,255,255,0.3);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }

.context-menu {
  position: fixed;
  z-index: 999;
  background: #1a1a2e;
  border: 1px solid #2e2e4a;
  border-radius: 10px;
  min-width: 240px;
  max-width: 320px;
  box-shadow: 0 12px 40px rgba(0,0,0,0.6);
  overflow: hidden;
  font-size: 0.82rem;
  font-family: 'DM Sans', sans-serif;
}

.cm-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.6rem 0.75rem;
  background: #121220;
  border-bottom: 1px solid #2e2e4a;
}

.cm-badge {
  font-size: 0.65rem;
  font-weight: 700;
  padding: 0.1rem 0.4rem;
  border-radius: 4px;
  text-transform: uppercase;
  font-family: 'DM Mono', monospace;
  flex-shrink: 0;
}
.cm-badge.person,
.cm-badge.person-foreign { background: #134e4a; color: #5eead4; }
.cm-badge.organization,
.cm-badge.organization-centrum { background: #312e81; color: #a5b4fc; }

.cm-title {
  flex: 1;
  font-weight: 600;
  color: #e2e8f0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.cm-close {
  background: none;
  border: none;
  color: #6b7280;
  cursor: pointer;
  font-size: 0.8rem;
  padding: 0;
  line-height: 1;
  flex-shrink: 0;
}
.cm-close:hover { color: #e2e8f0; }

.cm-loading {
  display: flex;
  justify-content: center;
  padding: 1rem;
}

.cm-section {
  padding: 0.5rem 0.75rem;
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}
.cm-section + .cm-section { border-top: 1px solid #2e2e4a; }

.cm-section-title {
  font-size: 0.7rem;
  font-weight: 600;
  color: #6b7280;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: 0.2rem;
}

.cm-row {
  display: flex;
  gap: 0.5rem;
  align-items: baseline;
}

.cm-key { color: #6b7280; flex-shrink: 0; width: 80px; }
.cm-val { color: #cbd5e1; word-break: break-word; }
.cm-val.mono { font-family: 'DM Mono', monospace; font-size: 0.78rem; }
.cm-val.warn { color: #f87171; font-weight: 600; }

.cm-connections { max-height: 180px; overflow-y: auto; }

.cm-conn-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.5rem;
  padding: 0.2rem 0;
}

.cm-conn-label {
  color: #cbd5e1;
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.cm-conn-rel {
  font-size: 0.72rem;
  color: #6b7280;
  flex-shrink: 0;
  font-family: 'DM Mono', monospace;
}
.cm-conn-rel.active { color: #4ade80; }
</style>
