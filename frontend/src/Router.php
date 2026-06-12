<?php
/**
 * Router simples para o frontend PHP MVC.
 * Suporta rotas GET e POST com mapeamento para Controller@action.
 */
class Router
{
    private array $routes = [];

    public function get(string $path, string $controller, string $action): void
    {
        $this->routes['GET'][$path] = [$controller, $action];
    }

    public function post(string $path, string $controller, string $action): void
    {
        $this->routes['POST'][$path] = [$controller, $action];
    }

    public function dispatch(string $uri, string $method): void
    {
        // Remover query string da URI
        $path = parse_url($uri, PHP_URL_PATH);
        $path = rtrim($path, '/') ?: '/';

        $routes = $this->routes[$method] ?? [];

        if (isset($routes[$path])) {
            [$controllerName, $action] = $routes[$path];
            $controllerFile = BASEPATH . "/src/Controllers/{$controllerName}.php";

            if (file_exists($controllerFile)) {
                require_once $controllerFile;
                $controller = new $controllerName();
                $controller->$action();
                return;
            }
        }

        // 404
        http_response_code(404);
        echo '<h1>404 — Página não encontrada</h1>';
    }
}
