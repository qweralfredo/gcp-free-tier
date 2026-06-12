<?php
require_once BASEPATH . '/src/Controllers/BaseController.php';

/**
 * Proxy transparente para a API .NET.
 * Permite que o JavaScript do frontend faça chamadas AJAX sem CORS direto ao .NET.
 */
class ApiProxyController extends BaseController
{
    public function dashboard(): void
    {
        $this->requireAuth();
        $data = $this->fetchApi('dashboard');
        $this->json($data ?? ['error' => 'API indisponível']);
    }

    public function alerts(): void
    {
        $this->requireAuth();
        $limit = (int)($_GET['limit'] ?? 50);
        $data = $this->fetchApi("alerts?limit={$limit}");
        $this->json($data ?? []);
    }

    public function quotas(): void
    {
        $this->requireAuth();
        $data = $this->fetchApi('quotas');
        $this->json($data ?? []);
    }
}
