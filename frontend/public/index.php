<?php
/**
 * BriefappGuardian — Front Controller
 * Ponto de entrada único do frontend PHP.
 * Roteia as requisições para os controllers MVC.
 */

define('BASEPATH', dirname(__DIR__));
define('API_URL', getenv('DOTNET_API_URL') ?: 'http://briefapp-dotnet:8081');

require_once BASEPATH . '/src/Router.php';

session_name(getenv('PHP_SESSION_NAME') ?: 'briefapp_session');
session_start();

$router = new Router();

$router->get('/', 'DashboardController', 'index');
$router->get('/dashboard', 'DashboardController', 'index');
$router->get('/alerts', 'DashboardController', 'alerts');
$router->get('/quotas', 'DashboardController', 'quotas');
$router->get('/login', 'AuthController', 'showLogin');
$router->post('/login', 'AuthController', 'login');
$router->get('/logout', 'AuthController', 'logout');
$router->get('/api/proxy/dashboard', 'ApiProxyController', 'dashboard');
$router->get('/api/proxy/alerts', 'ApiProxyController', 'alerts');
$router->get('/api/proxy/quotas', 'ApiProxyController', 'quotas');

$router->dispatch($_SERVER['REQUEST_URI'], $_SERVER['REQUEST_METHOD']);
