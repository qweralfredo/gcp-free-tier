#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════
# deploy.sh — Atualiza e reinicia os containers do Briefapp Guardian
# Executar na VM via SSH: ssh user@vm-ip "cd /opt/briefapp && ./infra/scripts/deploy.sh"
# ═══════════════════════════════════════════════════════════════
set -euo pipefail

DEPLOY_DIR="/opt/briefapp"
LOG_FILE="/var/log/briefapp/deploy.log"

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*" | tee -a "$LOG_FILE"
}

mkdir -p "$(dirname "$LOG_FILE")"

log "▶ Iniciando deploy..."

log "▶ [1/4] Atualizando código do repositório..."
cd "$DEPLOY_DIR"
git fetch origin
git checkout develop
git pull origin develop

log "▶ [2/4] Verificando arquivo .env..."
if [ ! -f ".env" ]; then
    log "❌ ERRO: arquivo .env não encontrado em $DEPLOY_DIR"
    log "   Copie .env.example para .env e preencha as variáveis."
    exit 1
fi

log "▶ [3/4] Build e reinício dos containers (sem downtime)..."
docker compose pull --quiet 2>/dev/null || true
docker compose up -d --build --remove-orphans

log "▶ [4/4] Verificando saúde dos containers..."
sleep 5
UNHEALTHY=$(docker compose ps --format json | python3 -c \
    "import sys,json; [print(c['Name']) for c in json.load(sys.stdin) if c.get('State') != 'running']" \
    2>/dev/null || docker compose ps | grep -v "Up" | grep -v "NAME" || true)

if [ -n "$UNHEALTHY" ]; then
    log "⚠️  Containers não saudáveis: $UNHEALTHY"
    log "   Execute 'docker compose logs' para diagnóstico."
    exit 1
fi

log "✅ Deploy concluído com sucesso!"
docker compose ps
