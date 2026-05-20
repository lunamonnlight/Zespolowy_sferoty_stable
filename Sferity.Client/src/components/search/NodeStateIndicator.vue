<template>
  <span v-if="expanding" class="node-expand-spinner" />
  <span v-else-if="expanded" class="node-expanded-dot" />
</template>

<script setup lang="ts">
defineProps<{
  expanding: boolean
  expanded: boolean
}>()
</script>

<style scoped>
.node-expanded-dot {
  display: block;
  width: 6px; height: 6px;
  border-radius: 50%;
  background: #4ade80;
  position: absolute;
  top: 0; right: 8px;
}

.node-expand-spinner {
  display: block;
  width: 10px; height: 10px;
  border: 2px solid rgba(99,102,241,0.3);
  border-top-color: #6366f1;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
  position: absolute;
  top: 0; right: 8px;
}

@keyframes spin { to { transform: rotate(360deg); } }
</style>
