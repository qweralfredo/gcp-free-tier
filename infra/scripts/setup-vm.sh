#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════
# setup-vm.sh — Provisiona a VM e2-micro Debian 12 para o
# GCP Free Tier Guardian. Executar UMA vez após criar a VM.
# ═══════════════════════════════════════════════════════════════
set -euo pipefail

echo "▶ [1/6] Atualizando pacotes do sistema..."
sudo apt-get update -qq
sudo apt-get upgrade -y -qq

echo "▶ [2/6] Instalando pré-requisitos..."
sudo apt-get install -y -qq \
    ca-certificates curl gnupg lsb-release \
    git ufw fail2ban unattended-upgrades

echo "▶ [3/6] Instalando Docker Engine + Compose Plugin..."
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/debian/gpg \
    | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

echo \
  "deb [arch=$(dpkg --print-architecture) \
  signed-by=/etc/apt/keyrings/docker.gpg] \
  https://download.docker.com/linux/debian \
  $(lsb_release -cs) stable" \
  | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt-get update -qq
sudo apt-get install -y -qq \
    docker-ce docker-ce-cli containerd.io \
    docker-buildx-plugin docker-compose-plugin

# Adicionar usuário corrente ao grupo docker (sem sudo)
sudo usermod -aG docker "$USER"

echo "▶ [4/6] Configurando UFW (firewall)..."
sudo ufw --force reset
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow 22/tcp   comment 'SSH'
sudo ufw allow 80/tcp   comment 'HTTP'
sudo ufw allow 443/tcp  comment 'HTTPS'
sudo ufw --force enable

echo "▶ [5/6] Criando diretório do projeto..."
sudo mkdir -p /opt/briefapp
sudo chown "$USER:$USER" /opt/briefapp

echo "▶ [6/6] Configurando Google Cloud SDK (gcloud)..."
if ! command -v gcloud &>/dev/null; then
    curl -sSL https://sdk.cloud.google.com | bash -s -- --disable-prompts
    source "$HOME/.bashrc"
fi

echo ""
echo "✅ Setup concluído!"
echo "   ➜ Faça logout e login novamente para ativar o grupo docker."
echo "   ➜ Execute: cd /opt/briefapp && git clone <repo> . && cp .env.example .env"
echo "   ➜ Edite o .env com suas configurações reais"
echo "   ➜ Execute: ./infra/scripts/deploy.sh"
