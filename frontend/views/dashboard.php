<?php
// Dashboard view — dados são injetados via PHP e atualizados via AJAX
$services = $dashboard['services'] ?? [];
$summary  = $dashboard['summary'] ?? [];
$alerts   = $dashboard['recentAlerts'] ?? [];

$summaryCards = [
    ['label' => 'Serviços OK',      'value' => $summary['servicesOk'] ?? 0,        'icon' => 'mdi-check-circle',  'color' => 'success'],
    ['label' => 'Warning',          'value' => $summary['servicesWarning'] ?? 0,    'icon' => 'mdi-alert',         'color' => 'warning'],
    ['label' => 'Critical',         'value' => $summary['servicesCritical'] ?? 0,   'icon' => 'mdi-alert-circle',  'color' => 'error'],
    ['label' => 'Emergency',        'value' => $summary['servicesEmergency'] ?? 0,  'icon' => 'mdi-skull-crossbones', 'color' => 'purple'],
];
?>

<!-- Summary chips -->
<div class="d-flex ga-4 flex-wrap mb-6">
  <?php foreach ($summaryCards as $card): ?>
  <v-card class="metric-card pa-4 d-flex align-center ga-3" min-width="150">
    <v-icon color="<?= $card['color'] ?>" size="36"><?= $card['icon'] ?></v-icon>
    <div>
      <div class="text-h4 font-weight-bold"><?= $card['value'] ?></div>
      <div class="text-caption text-medium-emphasis"><?= $card['label'] ?></div>
    </div>
  </v-card>
  <?php endforeach; ?>
</div>

<!-- Gráfico de donut geral -->
<v-row class="mb-4">
  <v-col cols="12" md="5">
    <v-card class="metric-card pa-4" height="320">
      <div class="text-subtitle-1 font-weight-semibold mb-3">
        <v-icon color="blue-accent-2" size="20" class="mr-1">mdi-chart-donut</v-icon>
        Consumo por Serviço
      </div>
      <div id="donut-chart" style="height:250px"></div>
    </v-card>
  </v-col>
  <v-col cols="12" md="7">
    <v-card class="metric-card pa-4" height="320">
      <div class="text-subtitle-1 font-weight-semibold mb-3">
        <v-icon color="cyan-accent-2" size="20" class="mr-1">mdi-chart-line</v-icon>
        Projeção até Fim do Mês
      </div>
      <div id="projection-chart" style="height:250px"></div>
    </v-card>
  </v-col>
</v-row>

<!-- Cards de serviços -->
<div class="text-subtitle-1 font-weight-semibold mb-3">
  <v-icon color="blue-grey-lighten-2" size="20" class="mr-1">mdi-view-grid</v-icon>
  Serviços Monitorados
</div>
<v-row>
<?php foreach ($services as $svc): ?>
  <?php
    $statusColor = match($svc['status']) {
      'ok' => '#00e676', 'warning' => '#ffab00', 'critical' => '#ff5252', default => '#d500f9'
    };
    $pct = min(100, round($svc['usagePercent'], 1));
    $fillColor = match(true) {
      $pct >= 98 => '#d500f9', $pct >= 90 => '#ff5252', $pct >= 75 => '#ffab00', default => '#00e676'
    };
  ?>
  <v-col cols="12" sm="6" lg="4">
    <v-card class="metric-card status-<?= $svc['status'] ?> pa-4">
      <div class="d-flex justify-space-between align-start mb-2">
        <div>
          <div class="text-body-2 font-weight-semibold"><?= htmlspecialchars($svc['displayName']) ?></div>
          <div class="text-caption text-medium-emphasis"><?= htmlspecialchars($svc['serviceName']) ?> · <?= $svc['periodType'] ?></div>
        </div>
        <v-chip size="x-small" :color="'<?= $svc['status'] === 'ok' ? 'success' : ($svc['status'] === 'warning' ? 'warning' : 'error') ?>'" class="stat-chip">
          <?= strtoupper($svc['status']) ?>
        </v-chip>
      </div>

      <!-- Barra de progresso -->
      <div class="usage-bar mb-2">
        <div class="usage-fill" style="width:<?= $pct ?>%; background:<?= $fillColor ?>"></div>
      </div>

      <div class="d-flex justify-space-between">
        <span class="text-h6 font-weight-bold" style="color:<?= $fillColor ?>"><?= $pct ?>%</span>
        <span class="text-caption text-medium-emphasis">
          <?php
            $val = $svc['currentValue'];
            $lim = $svc['freeLimit'];
            echo match($svc['unit']) {
              'bytes' => sprintf('%.2f / %.0f GB', $val/1073741824, $lim/1073741824),
              'hours' => sprintf('%.1f / %dh', $val, $lim),
              default => number_format($val) . ' / ' . number_format($lim)
            };
          ?>
        </span>
      </div>

      <?php if (!empty($svc['estimatedDaysToLimit'])): ?>
      <v-alert type="warning" density="compact" variant="tonal" class="mt-2" style="font-size:11px">
        ⚡ Limite estimado em <?= $svc['estimatedDaysToLimit'] ?> dias
      </v-alert>
      <?php endif; ?>
    </v-card>
  </v-col>
<?php endforeach; ?>
</v-row>

<!-- Alertas recentes -->
<?php if (!empty($alerts)): ?>
<div class="text-subtitle-1 font-weight-semibold mt-6 mb-3">
  <v-icon color="orange" size="20" class="mr-1">mdi-bell</v-icon>
  Alertas Recentes
</div>
<v-card class="metric-card">
  <v-table density="compact" theme="dark">
    <thead>
      <tr>
        <th>Nível</th><th>Serviço</th><th>Consumo</th><th>Ação</th><th>Horário</th>
      </tr>
    </thead>
    <tbody>
      <?php foreach (array_slice($alerts, 0, 8) as $alert): ?>
      <tr>
        <td>
          <v-chip size="x-small" color="<?= $alert['level'] === 'emergency' ? 'purple' : ($alert['level'] === 'critical' ? 'error' : 'warning') ?>">
            <?= strtoupper($alert['level']) ?>
          </v-chip>
        </td>
        <td class="text-caption"><?= htmlspecialchars($alert['serviceName']) ?>/<?= htmlspecialchars($alert['metricName']) ?></td>
        <td class="font-weight-bold"><?= round($alert['triggerPercent'], 1) ?>%</td>
        <td class="text-caption text-medium-emphasis"><?= htmlspecialchars($alert['actionTaken'] ?? '—') ?></td>
        <td class="text-caption"><?= date('d/m H:i', strtotime($alert['createdAt'])) ?></td>
      </tr>
      <?php endforeach; ?>
    </tbody>
  </v-table>
</v-card>
<?php endif; ?>

<?php
// Prepara dados para ECharts
$chartData = array_map(fn($s) => [
  'name'  => $s['displayName'],
  'value' => round($s['usagePercent'], 2)
], $services);

$projectionData = array_filter($services, fn($s) => !empty($s['projectedEndOfPeriod']));
$projNames = json_encode(array_values(array_map(fn($s) => $s['serviceName'], $projectionData)));
$projValues = json_encode(array_values(array_map(fn($s) => round($s['projectedEndOfPeriod'], 0), $projectionData)));
$freeValues = json_encode(array_values(array_map(fn($s) => $s['freeLimit'], $projectionData)));
?>

<script>
// Donut chart — consumo %
const donutEl = document.getElementById('donut-chart');
if (donutEl) {
  const donut = echarts.init(donutEl, 'dark');
  donut.setOption({
    backgroundColor: 'transparent',
    tooltip: { trigger: 'item', formatter: '{b}: {c}%' },
    series: [{
      type: 'pie', radius: ['55%', '80%'],
      data: <?= json_encode($chartData, JSON_UNESCAPED_UNICODE) ?>,
      itemStyle: { borderRadius: 6, borderWidth: 2, borderColor: '#141928' },
      label: { show: false },
      emphasis: { label: { show: true, fontSize: 14, fontWeight: 'bold' } }
    }],
    color: ['#448AFF','#00E5FF','#69F0AE','#FFD740','#FF5252','#EA80FC']
  });
}

// Projeção de fim de mês
const projEl = document.getElementById('projection-chart');
if (projEl) {
  const proj = echarts.init(projEl, 'dark');
  proj.setOption({
    backgroundColor: 'transparent',
    tooltip: { trigger: 'axis' },
    legend: { data: ['Projeção', 'Limite Free'], textStyle: { color: '#aaa', fontSize: 11 } },
    xAxis: { type: 'category', data: <?= $projNames ?>, axisLabel: { color: '#aaa', fontSize: 10 } },
    yAxis: { type: 'value', axisLabel: { color: '#aaa', fontSize: 10 } },
    series: [
      { name: 'Projeção', type: 'bar', data: <?= $projValues ?>, itemStyle: { color: '#448AFF' }, barMaxWidth: 40 },
      { name: 'Limite Free', type: 'line', data: <?= $freeValues ?>, itemStyle: { color: '#FF5252' }, lineStyle: { type: 'dashed' }, symbol: 'none' }
    ]
  });
}

// Escuta atualizações do AJAX
window.addEventListener('dashboard-updated', (e) => {
  // Re-render na próxima versão com Vue reactive charts
  console.log('Dashboard atualizado:', new Date().toLocaleTimeString());
});
</script>
