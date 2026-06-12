<!DOCTYPE html>
<html lang="pt-BR">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title><?= htmlspecialchars($pageTitle ?? 'Login — GCP Guardian') ?></title>
  <link href="https://cdn.jsdelivr.net/npm/vuetify@3.8.0/dist/vuetify.min.css" rel="stylesheet">
  <link href="https://cdn.jsdelivr.net/npm/@mdi/font@7.4.47/css/materialdesignicons.min.css" rel="stylesheet">
  <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
  <style>
    * { font-family: 'Inter', sans-serif; }
    body { background: #0a0e1a; margin: 0; }
    .login-bg {
      min-height: 100vh;
      background: radial-gradient(ellipse at 20% 50%, rgba(68,138,255,0.12) 0%, transparent 60%),
                  radial-gradient(ellipse at 80% 20%, rgba(0,229,255,0.08) 0%, transparent 50%),
                  #0a0e1a;
      display: flex; align-items: center; justify-content: center;
    }
    .login-card {
      background: linear-gradient(135deg, #141928 0%, #0f1426 100%) !important;
      border: 1px solid rgba(255,255,255,0.08);
      border-radius: 24px !important;
      backdrop-filter: blur(20px);
    }
    .shield-glow {
      filter: drop-shadow(0 0 16px rgba(68,138,255,0.6));
    }
  </style>
</head>
<body>
<div id="login-app">
  <v-app theme="dark">
    <div class="login-bg">
      <v-card class="login-card pa-8" width="420" elevation="0">
        <div class="text-center mb-6">
          <v-icon class="shield-glow mb-3" color="blue-accent-2" size="64">mdi-shield-check</v-icon>
          <h1 class="text-h5 font-weight-bold">GCP Guardian</h1>
          <p class="text-body-2 text-medium-emphasis mt-1">Painel de Monitoramento Free Tier</p>
        </div>

        <?php if ($error): ?>
        <v-alert type="error" variant="tonal" class="mb-4" density="compact" closable>
          <?= htmlspecialchars($error) ?>
        </v-alert>
        <?php endif; ?>

        <form method="POST" action="/login">
          <v-text-field
            name="password"
            label="Senha de Acesso"
            type="password"
            variant="outlined"
            prepend-inner-icon="mdi-lock"
            color="blue-accent-2"
            class="mb-4"
            autofocus
            required
          ></v-text-field>

          <v-btn
            type="submit"
            color="blue-accent-2"
            size="large"
            block
            variant="elevated"
            prepend-icon="mdi-login"
          >
            Entrar
          </v-btn>
        </form>

        <div class="text-center mt-4">
          <span class="text-caption text-medium-emphasis">
            BriefappGuardian v1.0 — Acesso restrito
          </span>
        </div>
      </v-card>
    </div>
  </v-app>
</div>

<script src="https://cdn.jsdelivr.net/npm/vue@3.5.13/dist/vue.global.prod.js"></script>
<script src="https://cdn.jsdelivr.net/npm/vuetify@3.8.0/dist/vuetify.min.js"></script>
<script>
  const { createApp } = Vue;
  const { createVuetify } = Vuetify;
  const vuetify = createVuetify({ theme: { defaultTheme: 'dark' } });
  createApp({}).use(vuetify).mount('#login-app');
</script>
</body>
</html>
