<?php
/**
 * BaseController — classe base com helpers de view e autenticação.
 */
abstract class BaseController
{
    protected function requireAuth(): void
    {
        if (empty($_SESSION['authenticated'])) {
            header('Location: /login');
            exit;
        }
    }

    protected function render(string $view, array $data = []): void
    {
        extract($data);
        $viewFile = BASEPATH . "/views/{$view}.php";
        require_once BASEPATH . '/views/layout.php';
    }

    protected function json(mixed $data, int $status = 200): void
    {
        http_response_code($status);
        header('Content-Type: application/json; charset=utf-8');
        echo json_encode($data, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
        exit;
    }

    protected function fetchApi(string $endpoint): mixed
    {
        $url = API_URL . '/api/' . ltrim($endpoint, '/');
        $ctx = stream_context_create([
            'http' => [
                'timeout' => 10,
                'ignore_errors' => true,
                'header' => 'Accept: application/json'
            ]
        ]);
        $response = @file_get_contents($url, false, $ctx);
        if ($response === false) return null;
        return json_decode($response, true);
    }
}
