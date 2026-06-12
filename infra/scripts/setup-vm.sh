#!/bin/bash
# ══════════════════════════════════════════════════════════════════
# setup-vm.sh — Script de inicialização da VM e2-micro (Debian 12)
# Executado pelo startup-script do Terraform na primeira inicialização
# e também manualmente para atualizações: bash /opt/briefapp/infra/scripts/setup-vm.sh
# ══════════════════════════════════════════════════════════════════
set -euo pipefail

BRIEFAPP_DIR="/opt/briefapp"
LOG_FILE="/var/log/briefapp-setup.log"
GITHUB_REPO="https://github.com/qweralfredo/gcp-free-tier.git"

log() { echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*" | tee -a "$LOG_FILE"; }

# ── 0. Verificar root ──────────────────────────────────────────────
if [[ $EUID -ne 0 ]]; then
  echo "Execute como root: sudo bash setup-vm.sh" >&2
  exit 1
fi

log "=== BriefappGuardian Setup Iniciado ==="
mkdir -p /var/log/briefapp

# ── 1. Atualizar sistema ───────────────────────────────────────────
log "Atualizando pacotes do sistema..."
apt-get update -qq
apt-get upgrade -y -qq --no-install-recommends

# ── 2. Instalar Docker Engine ──────────────────────────────────────
if ! command -v docker &>/dev/null; then
  log "Instalando Docker Engine..."
  apt-get install -y -qq ca-certificates curl gnupg lsb-release

  install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/debian/gpg | \
    gpg --dearmor -o /etc/apt/keyrings/docker.gpg
  chmod a+r /etc/apt/keyrings/docker.gpg

  echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
    https://download.docker.com/linux/debian $(lsb_release -cs) stable" \
    > /etc/apt/sources.list.d/docker.list

  apt-get update -qq
  apt-get install -y -qq docker-ce docker-ce-cli containerd.io docker-compose-plugin

  systemctl enable docker
  systemctl start docker
  log "Docker instalado: $(docker --version)"
else
  log "Docker já instalado: $(docker --version)"
fi

# ── 3. Instalar Certbot (standalone para Let's Encrypt) ────────────
if ! command -v certbot &>/dev/null; then
  log "Instalando Certbot..."
  apt-get install -y -qq certbot
  log "Certbot instalado: $(certbot --version)"
fi

# ── 4. Clonar ou atualizar repositório ────────────────────────────
if [ ! -d "$BRIEFAPP_DIR/.git" ]; then
  log "Clonando repositório $GITHUB_REPO..."
  git clone --depth=1 "$GITHUB_REPO" "$BRIEFAPP_DIR"
else
  log "Atualizando repositório..."
  git -C "$BRIEFAPP_DIR" pull --ff-only origin develop
fi

# ── 5. Verificar .env ─────────────────────────────────────────────
if [ ! -f "$BRIEFAPP_DIR/.env" ]; then
  log "ATENÇÃO: .env não encontrado. Copiando .env.example..."
  cp "$BRIEFAPP_DIR/.env.example" "$BRIEFAPP_DIR/.env"
  log "⚠️  Edite $BRIEFAPP_DIR/.env antes de prosseguir!"
  log "    Necessário: GCP_PROJECT_ID, ADMIN_PASSWORD_HASH, TELEGRAM_BOT_TOKEN"
fi

# ── 6. Verificar sa-key.json ──────────────────────────────────────
if [ ! -f "$BRIEFAPP_DIR/infra/gcp/sa-key.json" ]; then
  log "⚠️  infra/gcp/sa-key.json não encontrado!"
  log "    Faça o download da chave da Service Account e coloque neste caminho."
fi

# ── 7. Criar diretórios de dados ──────────────────────────────────
log "Criando diretórios de runtime..."
mkdir -p /opt/briefapp-data/duckdb
mkdir -p /var/log/briefapp
chmod 755 /opt/briefapp-data/duckdb
chmod 755 /var/log/briefapp

# ── 8. Configurar renovação automática do certificado SSL ─────────
if ! crontab -l 2>/dev/null | grep -q "certbot renew"; then
  log "Configurando renovação automática do SSL (cron)..."
  (crontab -l 2>/dev/null; echo "0 3 * * * certbot renew --quiet --deploy-hook 'docker compose -f $BRIEFAPP_DIR/docker-compose.yml restart nginx' >> /var/log/briefapp/certbot.log 2>&1") | crontab -
fi

# ── 9. Configurar swap (vital para VM com 1GB RAM) ────────────────
if [ ! -f /swapfile ]; then
  log "Criando arquivo de swap (512 MB)..."
  fallocate -l 512M /swapfile
  chmod 600 /swapfile
  mkswap /swapfile
  swapon /swapfile
  echo '/swapfile none swap sw 0 0' >> /etc/fstab
  log "Swap de 512MB ativado."
fi

# ── 10. Docker Compose Up ─────────────────────────────────────────
if [ -f "$BRIEFAPP_DIR/.env" ] && [ -f "$BRIEFAPP_DIR/infra/gcp/sa-key.json" ]; then
  log "Subindo containers com Docker Compose..."
  docker compose -f "$BRIEFAPP_DIR/docker-compose.yml" pull
  docker compose -f "$BRIEFAPP_DIR/docker-compose.yml" up -d --build
  log "Containers iniciados. Verificando health..."
  sleep 10
  docker compose -f "$BRIEFAPP_DIR/docker-compose.yml" ps
else
  log "⚠️  Setup incompleto. Configure .env e sa-key.json, depois execute:"
  log "    docker compose -f $BRIEFAPP_DIR/docker-compose.yml up -d --build"
fi

log "=== Setup concluído! ==="
log "Acesse: https://$(curl -s ifconfig.me 2>/dev/null || echo 'SEU-IP')"
