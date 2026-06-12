<?php
// quotas view — $quotas é um array de objetos de configuração
$quotas = $quotas ?? [];

$periodLabels = ['monthly' => 'Mensal', 'daily' => 'Diário'];
$serviceIcons = [
    'compute'   => ['icon' => 'mdi-server', 'color' => 'blue'],
    'storage'   => ['icon' => 'mdi-database', 'color' => 'cyan'],
    'run'       => ['icon' => 'mdi-rocket-launch', 'color' => 'green'],
    'functions' => ['icon' => 'mdi-function', 'color' => 'amber'],
    'pubsub'    => ['icon' => 'mdi-message-flash', 'color' => 'purple'],
    'monitoring'=> ['icon' => 'mdi-chart-line', 'color' => 'orange'],
];

// Agrupar por serviço
$grouped = [];
foreach ($quotas as $q) {
    $svc = $q['serviceName'] ?? 'other';
    $grouped[$svc][] = $q;
}
?>

<div class="d-flex align-center mb-6">
  <h1 class="text-h5 font-weight-bold d-flex align-center ga-2">
    <v-icon color="purple" size="28">mdi-tune</v-icon>
    Configuração de Quotas GCP
  </h1>
</div>

<v-alert type="info" variant="tonal" density="compact" class="mb-6" icon="mdi-information">
  Limites e thresholds configurados para o <strong>GCP Always Free Tier</strong>.
  Os guardrails agem automaticamente ao atingir os percentuais definidos.
  Para alterar, edite a tabela <code>quota_config</code> no DuckDB e reinicie o container.
</v-alert>

<!-- Cards por serviço -->
<?php foreach ($grouped as $serviceName => $metrics): ?>
  <?php $svcCfg = $serviceIcons[$serviceName] ?? ['icon' => 'mdi-cloud', 'color' => 'blue-grey']; ?>

  <v-card class="metric-card mb-4">
    <div class="pa-4 d-flex align-center ga-2">
      <v-icon color="<?= $svcCfg['color'] ?>" size="24"><?= $svcCfg['icon'] ?></v-icon>
      <span class="text-subtitle-1 font-weight-semibold text-capitalize"><?= htmlspecialchars($serviceName) ?></span>
      <v-chip size="x-small" color="<?= $svcCfg['color'] ?>" class="ml-1">
        <?= count($metrics) ?> métrica<?= count($metrics) > 1 ? 's' : '' ?>
      </v-chip>
    </div>

    <v-table density="comfortable" theme="dark" class="pb-2">
      <thead>
        <tr>
          <th>Métrica</th>
          <th style="width:130px">Limite Free</th>
          <th style="width:80px">Período</th>
          <th style="width:80px" class="text-center">⚠️ 75%</th>
          <th style="width:80px" class="text-center">🔴 90%</th>
          <th style="width:80px" class="text-center">🚨 98%</th>
          <th style="width:100px" class="text-center">Kill-Switch</th>
        </tr>
      </thead>
      <tbody>
        <?php foreach ($metrics as $m): ?>
          <?php
            $limit = $m['freeLimit'];
            $unit  = $m['unit'];
            $limitFormatted = match($unit) {
              'bytes'       => sprintf('%.0f GB', $limit / 1_073_741_824),
              'hours'       => sprintf('%.0fh', $limit),
              'cpu-seconds' => sprintf('%s vCPU·s', number_format($limit)),
              'gib-seconds' => sprintf('%s GiB·s', number_format($limit)),
              default       => number_format($limit) . ' ' . $unit,
            };
            $killEnabled = $m['killSwitchEnabled'] ?? true;
          ?>
          <tr>
            <td>
              <div class="text-body-2 font-weight-medium"><?= htmlspecialchars($m['displayName'] ?? $m['metricName']) ?></div>
              <div class="text-caption text-medium-emphasis"><?= htmlspecialchars($m['metricName']) ?></div>
            </td>
            <td>
              <span class="text-body-2 font-weight-semibold"><?= $limitFormatted ?></span>
            </td>
            <td>
              <v-chip size="x-small" variant="tonal" color="blue-grey">
                <?= $periodLabels[$m['periodType'] ?? 'monthly'] ?? 'Mensal' ?>
              </v-chip>
            </td>
            <td class="text-center">
              <v-chip size="x-small" color="warning"><?= $m['warningPercent'] ?? 75 ?>%</v-chip>
            </td>
            <td class="text-center">
              <v-chip size="x-small" color="error"><?= $m['criticalPercent'] ?? 90 ?>%</v-chip>
            </td>
            <td class="text-center">
              <v-chip size="x-small" color="purple"><?= $m['emergencyPercent'] ?? 98 ?>%</v-chip>
            </td>
            <td class="text-center">
              <?php if ($killEnabled): ?>
                <v-icon color="success" size="20" title="Kill-switch ativo">mdi-shield-check</v-icon>
              <?php else: ?>
                <v-icon color="error" size="20" title="Kill-switch desativado">mdi-shield-off</v-icon>
              <?php endif; ?>
            </td>
          </tr>
        <?php endforeach; ?>
      </tbody>
    </v-table>
  </v-card>
<?php endforeach; ?>

<?php if (empty($quotas)): ?>
<v-card class="metric-card pa-8 text-center">
  <v-icon size="48" color="blue-grey" class="mb-3">mdi-database-off</v-icon>
  <div class="text-h6 mb-2">Sem dados de configuração</div>
  <div class="text-body-2 text-medium-emphasis">
    A API backend não retornou configurações de quota.<br>
    Verifique se o container <code>briefapp-dotnet</code> está em execução.
  </div>
</v-card>
<?php endif; ?>

<!-- Legenda de thresholds -->
<v-card class="metric-card pa-4 mt-4">
  <div class="text-subtitle-2 font-weight-semibold mb-3">
    <v-icon size="18" class="mr-1" color="blue-grey">mdi-information-outline</v-icon>
    Legenda de Ações por Threshold
  </div>
  <v-row dense>
    <v-col cols="12" sm="4">
      <div class="d-flex align-center ga-2 mb-1">
        <v-icon color="warning" size="18">mdi-alert</v-icon>
        <span class="text-body-2"><strong>75% — Warning:</strong> Notificação Telegram enviada</span>
      </div>
    </v-col>
    <v-col cols="12" sm="4">
      <div class="d-flex align-center ga-2 mb-1">
        <v-icon color="error" size="18">mdi-alert-circle</v-icon>
        <span class="text-body-2"><strong>90% — Critical:</strong> Cloud Run limitado a 1 instância</span>
      </div>
    </v-col>
    <v-col cols="12" sm="4">
      <div class="d-flex align-center ga-2 mb-1">
        <v-icon color="purple" size="18">mdi-skull-crossbones</v-icon>
        <span class="text-body-2"><strong>98% — Emergency:</strong> Kill-switch ativado (Cloud Run parado)</span>
      </div>
    </v-col>
  </v-row>
</v-card>
