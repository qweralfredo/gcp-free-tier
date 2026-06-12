<!DOCTYPE html>
<html lang="pt-BR">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title><?= htmlspecialchars($pageTitle ?? 'GCP Guardian') ?></title>
  <meta name="description" content="BriefappGuardian — Monitore e proteja seus recursos GCP dentro do Free Tier">

  <!-- Vuetify CSS -->
  <link href="https://cdn.jsdelivr.net/npm/vuetify@3.8.0/dist/vuetify.min.css" rel="stylesheet">
  <!-- Material Design Icons -->
  <link href="https://cdn.jsdelivr.net/npm/@mdi/font@7.4.47/css/materialdesignicons.min.css" rel="stylesheet">
  <!-- Google Fonts -->
  <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">

  <style>
    * { font-family: 'Inter', sans-serif; }
    .v-application { background: #0a0e1a !important; }
    .metric-card {
      background: linear-gradient(135deg, #1a1f35 0%, #141928 100%);
      border: 1px solid rgba(255,255,255,0.07);
      border-radius: 16px !important;
      transition: transform 0.2s, box-shadow 0.2s;
    }
    .metric-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 8px 32px rgba(0,0,0,0.4) !important;
    }
    .status-ok    { border-left: 4px solid #00e676 !important; }
    .status-warning  { border-left: 4px solid #ffab00 !important; }
    .status-critical { border-left: 4px solid #ff5252 !important; }
    .status-emergency{ border-left: 4px solid #d500f9 !important; animation: pulse 1.5s infinite; }
    @keyframes pulse {
      0%,100% { box-shadow: 0 0 0 0 rgba(213,0,249,0.4); }
      50%      { box-shadow: 0 0 0 8px rgba(213,0,249,0); }
    }
    .usage-bar { border-radius: 4px; height: 8px; background: rgba(255,255,255,0.08); overflow: hidden; }
    .usage-fill { height: 100%; border-radius: 4px; transition: width 0.6s ease; }
    .nav-link { color: rgba(255,255,255,0.7) !important; }
    .nav-link.active, .nav-link:hover { color: #fff !important; }
    .sidebar { background: #0f1426 !important; border-right: 1px solid rgba(255,255,255,0.06); }
    .app-bar { background: rgba(10,14,26,0.95) !important; backdrop-filter: blur(10px); border-bottom: 1px solid rgba(255,255,255,0.06); }
    .stat-chip { font-size: 11px; font-weight: 600; letter-spacing: 0.5px; }
  </style>
</head>
<body>

<?php
$currentPath = parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);
?>

<div id="app">
  <v-app theme="dark">
    <!-- App Bar -->
    <v-app-bar class="app-bar" elevation="0">
      <v-app-bar-nav-icon @click="drawer = !drawer" color="white"></v-app-bar-nav-icon>
      <v-toolbar-title class="d-flex align-center ga-2">
        <v-icon color="blue-accent-2" size="28">mdi-shield-check</v-icon>
        <span style="font-weight:700; letter-spacing:-0.5px">GCP Guardian</span>
        <v-chip size="x-small" color="blue" class="stat-chip ml-2">FREE TIER</v-chip>
      </v-toolbar-title>
      <v-spacer></v-spacer>
      <v-chip v-if="lastUpdate" size="small" prepend-icon="mdi-clock-outline" color="surface-variant" class="mr-2">
        {{ lastUpdateFormatted }}
      </v-chip>
      <v-btn icon="mdi-refresh" variant="text" @click="refreshData" :loading="loading" color="blue-grey-lighten-2"></v-btn>
      <v-btn href="/logout" variant="text" prepend-icon="mdi-logout" color="red-lighten-2" size="small">Sair</v-btn>
    </v-app-bar>

    <!-- Navigation Drawer -->
    <v-navigation-drawer v-model="drawer" class="sidebar" :permanent="$vuetify.display.mdAndUp">
      <v-list nav density="compact" class="mt-2">
        <v-list-item
          prepend-icon="mdi-view-dashboard"
          title="Dashboard"
          href="/"
          :active="'<?= $currentPath ?>' === '/'"
          active-color="blue-accent-2"
          rounded="lg"
          class="mb-1"
        ></v-list-item>
        <v-list-item
          prepend-icon="mdi-bell-alert"
          title="Alertas"
          href="/alerts"
          :active="'<?= $currentPath ?>' === '/alerts'"
          active-color="orange"
          rounded="lg"
          class="mb-1"
        ></v-list-item>
        <v-list-item
          prepend-icon="mdi-tune"
          title="Quotas"
          href="/quotas"
          :active="'<?= $currentPath ?>' === '/quotas'"
          active-color="purple"
          rounded="lg"
          class="mb-1"
        ></v-list-item>
      </v-list>

      <template v-slot:append>
        <v-divider class="mb-2"></v-divider>
        <div class="pa-3">
          <div class="text-caption text-medium-emphasis mb-1">Projeto GCP</div>
          <div class="text-body-2 font-weight-medium text-truncate">{{ projectId || '—' }}</div>
          <div class="text-caption text-medium-emphasis mt-2">Coleta a cada 15 min</div>
        </div>
      </template>
    </v-navigation-drawer>

    <!-- Main Content -->
    <v-main>
      <v-container fluid class="pa-6">
        <?php require BASEPATH . '/views/' . $view . '.php'; ?>
      </v-container>
    </v-main>
  </v-app>
</div>

<!-- Vue 3 -->
<script src="https://cdn.jsdelivr.net/npm/vue@3.5.13/dist/vue.global.prod.js"></script>
<!-- Vuetify -->
<script src="https://cdn.jsdelivr.net/npm/vuetify@3.8.0/dist/vuetify.min.js"></script>
<!-- Apache ECharts -->
<script src="https://cdn.jsdelivr.net/npm/echarts@5.5.1/dist/echarts.min.js"></script>

<script>
const { createApp, ref, computed, onMounted, onUnmounted } = Vue;
const { createVuetify } = Vuetify;

const vuetify = createVuetify({
  theme: {
    defaultTheme: 'dark',
    themes: {
      dark: {
        colors: {
          primary: '#448AFF',
          secondary: '#00E5FF',
          background: '#0a0e1a',
          surface: '#141928',
        }
      }
    }
  }
});

// Componente principal
const app = createApp({
  setup() {
    const drawer = ref(true);
    const loading = ref(false);
    const lastUpdate = ref(null);
    const projectId = ref('');
    const dashboard = ref(<?= json_encode($dashboard ?? null, JSON_UNESCAPED_UNICODE) ?>);

    const lastUpdateFormatted = computed(() => {
      if (!lastUpdate.value) return '';
      return new Date(lastUpdate.value).toLocaleTimeString('pt-BR');
    });

    async function refreshData() {
      loading.value = true;
      try {
        const res = await fetch('/api/proxy/dashboard');
        if (res.ok) {
          dashboard.value = await res.json();
          lastUpdate.value = new Date();
          projectId.value = dashboard.value?.projectId || '';
          window.dispatchEvent(new CustomEvent('dashboard-updated', { detail: dashboard.value }));
        }
      } catch (e) {
        console.error('Erro ao atualizar dados:', e);
      } finally {
        loading.value = false;
      }
    }

    // Auto-refresh a cada 60 segundos
    let interval;
    onMounted(() => {
      if (dashboard.value) {
        lastUpdate.value = dashboard.value.generatedAt;
        projectId.value = dashboard.value.projectId || '';
      }
      interval = setInterval(refreshData, 60_000);
    });

    onUnmounted(() => clearInterval(interval));

    return { drawer, loading, lastUpdate, lastUpdateFormatted, projectId, dashboard, refreshData };
  }
});

app.use(vuetify);
app.mount('#app');
</script>

<?php if (isset($viewScript)): ?>
<script><?= $viewScript ?></script>
<?php endif; ?>

</body>
</html>
