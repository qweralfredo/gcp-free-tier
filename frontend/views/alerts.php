<?php
// alerts view — $alerts é um array de AlertSummaryDto
$alerts = $alerts ?? [];

$levelConfig = [
    'warning'   => ['color' => 'warning',  'icon' => 'mdi-alert',          'label' => 'WARNING'],
    'critical'  => ['color' => 'error',    'icon' => 'mdi-alert-circle',   'label' => 'CRITICAL'],
    'emergency' => ['color' => 'purple',   'icon' => 'mdi-skull-crossbones','label' => 'EMERGENCY'],
];
?>

<div class="d-flex align-center justify-space-between mb-6">
  <div>
    <h1 class="text-h5 font-weight-bold d-flex align-center ga-2">
      <v-icon color="orange" size="28">mdi-bell-alert</v-icon>
      Histórico de Alertas
    </h1>
    <p class="text-body-2 text-medium-emphasis mt-1">
      <?= count($alerts) ?> alertas registrados — guardrails ativados
    </p>
  </div>
  <v-btn href="/api/proxy/alerts?limit=200" target="_blank" variant="outlined"
         prepend-icon="mdi-download" color="blue-grey" size="small">
    Exportar JSON
  </v-btn>
</div>

<!-- Resumo por nível -->
<?php
$byLevel = ['warning' => 0, 'critical' => 0, 'emergency' => 0];
foreach ($alerts as $a) {
    $lvl = $a['level'] ?? 'warning';
    if (isset($byLevel[$lvl])) $byLevel[$lvl]++;
}
?>
<v-row class="mb-6">
  <?php foreach ($byLevel as $level => $count): ?>
    <?php $cfg = $levelConfig[$level]; ?>
  <v-col cols="12" sm="4">
    <v-card class="metric-card pa-4 d-flex align-center ga-3">
      <v-icon color="<?= $cfg['color'] ?>" size="40"><?= $cfg['icon'] ?></v-icon>
      <div>
        <div class="text-h4 font-weight-bold"><?= $count ?></div>
        <div class="text-caption text-medium-emphasis"><?= $cfg['label'] ?></div>
      </div>
    </v-card>
  </v-col>
  <?php endforeach; ?>
</v-row>

<!-- Tabela de alertas -->
<v-card class="metric-card">
  <div class="pa-4 d-flex align-center justify-space-between">
    <span class="text-subtitle-1 font-weight-semibold">
      <v-icon color="orange" size="18" class="mr-1">mdi-format-list-bulleted</v-icon>
      Todos os Alertas
    </span>
    <v-text-field
      v-model="search"
      density="compact"
      variant="outlined"
      prepend-inner-icon="mdi-magnify"
      placeholder="Filtrar..."
      hide-details
      style="max-width:220px"
      color="blue-accent-2"
    ></v-text-field>
  </div>

  <v-table density="compact" theme="dark" class="px-2 pb-2">
    <thead>
      <tr>
        <th style="width:110px">Nível</th>
        <th>Serviço / Métrica</th>
        <th style="width:90px">Consumo</th>
        <th>Ação Executada</th>
        <th style="width:130px">Data/Hora</th>
      </tr>
    </thead>
    <tbody>
      <?php if (empty($alerts)): ?>
      <tr>
        <td colspan="5" class="text-center text-medium-emphasis pa-6">
          <v-icon size="40" class="mb-2 d-block" color="success">mdi-check-circle</v-icon>
          Nenhum alerta registrado — todos os serviços dentro da quota!
        </td>
      </tr>
      <?php else: ?>
        <?php foreach ($alerts as $alert): ?>
          <?php
            $lvl = $alert['level'] ?? 'warning';
            $cfg = $levelConfig[$lvl] ?? $levelConfig['warning'];
            $pct = round($alert['triggerPercent'] ?? 0, 1);
            $barColor = match($lvl) {
              'emergency' => '#d500f9', 'critical' => '#ff5252', default => '#ffab00'
            };
          ?>
          <tr>
            <td>
              <v-chip size="x-small" color="<?= $cfg['color'] ?>" prepend-icon="<?= $cfg['icon'] ?>">
                <?= $cfg['label'] ?>
              </v-chip>
            </td>
            <td>
              <div class="text-body-2"><?= htmlspecialchars($alert['serviceName'] ?? '') ?></div>
              <div class="text-caption text-medium-emphasis"><?= htmlspecialchars($alert['metricName'] ?? '') ?></div>
            </td>
            <td>
              <div style="display:flex;align-items:center;gap:6px">
                <div class="usage-bar" style="width:50px">
                  <div class="usage-fill" style="width:<?= min(100,$pct) ?>%;background:<?= $barColor ?>"></div>
                </div>
                <span class="text-body-2 font-weight-bold" style="color:<?= $barColor ?>"><?= $pct ?>%</span>
              </div>
            </td>
            <td class="text-caption">
              <?php if (!empty($alert['actionTaken'])): ?>
                <v-chip size="x-small" variant="outlined" color="blue-grey">
                  <?= htmlspecialchars($alert['actionTaken']) ?>
                </v-chip>
              <?php else: ?>
                <span class="text-medium-emphasis">Notificação</span>
              <?php endif; ?>
            </td>
            <td class="text-caption text-medium-emphasis">
              <?= date('d/m/Y H:i:s', strtotime($alert['createdAt'] ?? 'now')) ?>
            </td>
          </tr>
        <?php endforeach; ?>
      <?php endif; ?>
    </tbody>
  </v-table>
</v-card>

<script>
// Adiciona reatividade de busca ao componente existente
document.addEventListener('DOMContentLoaded', () => {
  // search filter — injetado no setup() do App Vue principal via extension
  console.log('Alerts view loaded — <?= count($alerts) ?> registros');
});
</script>
