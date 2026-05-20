<template>
  <div class="graph-area">
    <!-- Stan pusty -->
    <div v-if="!graphLoaded && !loading" class="empty-state">
      <svg xmlns="http://www.w3.org/2000/svg" width="56" height="56" viewBox="0 0 24 24"
           fill="none" stroke="currentColor" stroke-width="1" stroke-linecap="round" stroke-linejoin="round">
        <circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/>
        <line x1="8.59" y1="13.51" x2="15.42" y2="17.49"/>
        <line x1="15.41" y1="6.51" x2="8.59" y2="10.49"/>
      </svg>
      <p>Wyszukaj firmę lub osobę, aby zobaczyć graf powiązań</p>
    </div>

    <VueFlow
        v-if="graphLoaded"
        :nodes="nodes"
        :edges="edges"
        :default-zoom="1"
        :min-zoom="0.2"
        :max-zoom="3"
        fit-view-on-init
        class="vue-flow-graph"
        @node-click="$emit('nodeClick', $event)"
        @node-context-menu="$emit('nodeContextMenu', $event)"
    >
      <Background pattern-color="#2a2a3a" :gap="24" :size="1" />
      <Controls />
      <MiniMap
          :node-color="minimapColor"
          node-stroke-color="transparent"
          mask-color="rgba(15,15,25,0.7)"
      />

      <!-- Węzeł: Centrum -->
      <template #node-organization-centrum="{ data, id }">
        <Handle type="target" :position="Position.Top" class="center-handle org-handle-target" :connectable="false" />
        <div class="custom-node" :class="{ expanding: expandingId === id }">
          <OrgIcon />
          <span class="node-label">{{ data.label }}</span>
          <NodeStateIndicator :expanding="expandingId === id" :expanded="expandedIds.has(id)" />
        </div>
        <Handle type="source" :position="Position.Bottom" class="center-handle org-handle-source" :connectable="false" />
      </template>

      <!-- Węzeł: Organizacja -->
      <template #node-organization="{ data, id }">
        <Handle type="target" :position="Position.Top" class="center-handle org-handle-target" :connectable="false" />
        <div class="custom-node" :class="{ inactive: !data.isActive, expanding: expandingId === id }">
          <OrgIcon />
          <span class="node-label">{{ data.label }}</span>
          <NodeStateIndicator :expanding="expandingId === id" :expanded="expandedIds.has(id)" />
        </div>
        <Handle type="source" :position="Position.Bottom" class="center-handle org-handle-source" :connectable="false" />
      </template>

      <!-- Węzeł: Osoba -->
      <template #node-person="{ data, id }">
        <Handle type="target" :position="Position.Top" class="center-handle person-handle-target" :connectable="false" />
        <div class="custom-node" :class="{ expanding: expandingId === id }">
          <PersonIcon />
          <span class="node-label">{{ data.label }}</span>
          <NodeStateIndicator :expanding="expandingId === id" :expanded="expandedIds.has(id)" />
        </div>
        <Handle type="source" :position="Position.Bottom" class="center-handle person-handle-source" :connectable="false" />
      </template>

      <!-- Węzeł: Osoba zagraniczna -->
      <template #node-person-foreign="{ data, id }">
        <Handle type="target" :position="Position.Top" class="center-handle person-handle-target" :connectable="false" />
        <div class="custom-node" :class="{ expanding: expandingId === id }">
          <PersonIcon />
          <span class="node-label">{{ data.label }}</span>
          <NodeStateIndicator :expanding="expandingId === id" :expanded="expandedIds.has(id)" />
        </div>
        <Handle type="source" :position="Position.Bottom" class="center-handle person-handle-source" :connectable="false" />
      </template>
    </VueFlow>
  </div>
</template>

<script setup lang="ts">
import { VueFlow, Handle, Position } from '@vue-flow/core'
import { Background } from '@vue-flow/background'
import { Controls } from '@vue-flow/controls'
import { MiniMap } from '@vue-flow/minimap'
import type { Node, Edge } from '@vue-flow/core'
import OrgIcon from './OrgIcon.vue'
import PersonIcon from './PersonIcon.vue'
import NodeStateIndicator from './NodeStateIndicator.vue'

defineProps<{
  nodes: Node[]
  edges: Edge[]
  graphLoaded: boolean
  loading: boolean
  expandedIds: Set<string>
  expandingId: string | null
  minimapColor: (node: Node) => string
}>()

defineEmits<{
  nodeClick: [payload: { node: Node }]
  nodeContextMenu: [payload: { event: MouseEvent; node: Node }]
}>()
</script>

<style>
@import '@vue-flow/core/dist/style.css';
@import '@vue-flow/core/dist/theme-default.css';
@import '@vue-flow/controls/dist/style.css';
@import '@vue-flow/minimap/dist/style.css';
</style>

<style scoped>
.graph-area {
  flex: 1;
  position: relative;
  overflow: hidden;
  width: 100%;
  height: 100%;
}

.empty-state {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 1rem;
  color: #2e2e4a;
}
.empty-state p { font-size: 0.9rem; color: #4b5563; }

.vue-flow-graph {
  width: 100%;
  height: 100%;
  background: var(--surface-800);
}

/* ── Węzły ───────────────────────────────────────────────────────────────── */

.custom-node {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.4rem;
  background: transparent;
  border: none;
  cursor: pointer;
  position: relative;
  width: 100px;
  text-align: center;
}
.custom-node.inactive  { opacity: 0.45; }
.custom-node.expanding { opacity: 0.6; }

.node-label {
  font-size: 0.72rem;
  font-weight: 500;
  color: #1e2936;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 110px;
  line-height: 1.3;
}

/* ── VueFlow overrides ───────────────────────────────────────────────────── */

:deep(.vue-flow__controls) {
  background: #1a1a2e;
  border: 1px solid #2e2e4a;
  border-radius: 8px;
  overflow: hidden;
}
:deep(.vue-flow__controls-button) {
  background: #1a1a2e;
  border-color: #2e2e4a;
  color: #94a3b8;
}
:deep(.vue-flow__controls-button:hover) {
  background: #252540;
  color: #e2e8f0;
}
:deep(.vue-flow__minimap) {
  border: 1px solid #2e2e4a;
  border-radius: 8px;
  background: #1a1a2e;
  overflow: hidden;
}

:deep(.center-handle) {
  width: 1px !important;
  height: 1px !important;
  min-width: unset !important;
  min-height: unset !important;
  background: transparent !important;
  border: none !important;
  top: 50% !important;
  left: 50% !important;
  transform: translate(-50%, -50%) !important;
  opacity: 0 !important;
  pointer-events: none !important;
}
:deep(.org-handle-target)    { top: 18px !important; }
:deep(.person-handle-source) { top: 24px !important; }
</style>
