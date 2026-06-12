<?php
require_once BASEPATH . '/src/Controllers/BaseController.php';

class DashboardController extends BaseController
{
    public function index(): void
    {
        $this->requireAuth();
        $dashboard = $this->fetchApi('dashboard');
        $this->render('dashboard', ['dashboard' => $dashboard, 'pageTitle' => 'Dashboard — GCP Guardian']);
    }

    public function alerts(): void
    {
        $this->requireAuth();
        $alerts = $this->fetchApi('alerts?limit=100');
        $this->render('alerts', ['alerts' => $alerts, 'pageTitle' => 'Alertas — GCP Guardian']);
    }

    public function quotas(): void
    {
        $this->requireAuth();
        $quotas = $this->fetchApi('quotas');
        $this->render('quotas', ['quotas' => $quotas, 'pageTitle' => 'Configuração de Quotas — GCP Guardian']);
    }
}
