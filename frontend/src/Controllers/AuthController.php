<?php
require_once BASEPATH . '/src/Controllers/BaseController.php';

class AuthController extends BaseController
{
    public function showLogin(): void
    {
        if (!empty($_SESSION['authenticated'])) {
            header('Location: /');
            exit;
        }
        $this->renderLogin(null);
    }

    public function login(): void
    {
        $password = $_POST['password'] ?? '';
        $hash = getenv('ADMIN_PASSWORD_HASH');

        if (!$hash) {
            $this->renderLogin('⚠️ ADMIN_PASSWORD_HASH não configurado no servidor.');
            return;
        }

        if (password_verify($password, $hash)) {
            session_regenerate_id(true);
            $_SESSION['authenticated'] = true;
            $_SESSION['login_at'] = time();
            header('Location: /');
            exit;
        }

        $this->renderLogin('Senha incorreta. Tente novamente.');
    }

    public function logout(): void
    {
        $_SESSION = [];
        session_destroy();
        header('Location: /login');
        exit;
    }

    private function renderLogin(?string $error): void
    {
        $pageTitle = 'Login — GCP Guardian';
        require BASEPATH . '/views/login.php';
    }
}
