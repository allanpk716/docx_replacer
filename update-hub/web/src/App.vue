<template>
  <div class="app">
    <router-view />
  </div>
</template>

<script setup lang="ts">
</script>

<style>
:root {
  --color-bg: #f5f5f7;
  --color-surface: #ffffff;
  --color-border: #e0e0e0;
  --color-text: #1d1d1f;
  --color-text-secondary: #6e6e73;
  --color-primary: #0066cc;
  --color-primary-hover: #0055b3;
  --color-danger: #cc0033;
  --color-danger-hover: #aa0029;
  --color-success: #00875a;
  --radius: 8px;
  --shadow: 0 1px 3px rgba(0, 0, 0, 0.08), 0 1px 2px rgba(0, 0, 0, 0.06);
}

* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
  background: var(--color-bg);
  color: var(--color-text);
  line-height: 1.5;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

.app {
  min-height: 100vh;
}
</style>
