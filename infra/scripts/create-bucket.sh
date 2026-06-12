#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════
# create-bucket.sh — Cria o bucket GCS em us-central1 (Always Free)
# Executar UMA vez com: bash infra/scripts/create-bucket.sh
# ═══════════════════════════════════════════════════════════════
set -euo pipefail

PROJECT_ID="${GCP_PROJECT_ID:-}"
BUCKET_NAME="${GCS_BUCKET_NAME:-briefapp-guardian-metrics}"
SA_EMAIL="briefapp-guardian@${PROJECT_ID}.iam.gserviceaccount.com"

if [ -z "$PROJECT_ID" ]; then
    echo "❌ GCP_PROJECT_ID não definido. Export a variável ou use .env"
    exit 1
fi

echo "▶ [1/4] Criando bucket GCS em us-central1..."
gcloud storage buckets create "gs://${BUCKET_NAME}" \
    --project="$PROJECT_ID" \
    --location=us-central1 \
    --uniform-bucket-level-access \
    --public-access-prevention

echo "▶ [2/4] Configurando lifecycle (deletar objetos > 90 dias)..."
cat > /tmp/lifecycle.json <<EOF
{
  "lifecycle": {
    "rule": [{
      "action": {"type": "Delete"},
      "condition": {"age": 90}
    }]
  }
}
EOF
gcloud storage buckets update "gs://${BUCKET_NAME}" \
    --lifecycle-file=/tmp/lifecycle.json

echo "▶ [3/4] Configurando IAM: apenas SA briefapp-guardian tem acesso..."
gcloud storage buckets add-iam-policy-binding "gs://${BUCKET_NAME}" \
    --member="serviceAccount:${SA_EMAIL}" \
    --role=roles/storage.objectAdmin

echo "▶ [4/4] Verificando bucket criado..."
gcloud storage buckets describe "gs://${BUCKET_NAME}" \
    --format="value(location,storageClass,lifecycle)"

echo ""
echo "✅ Bucket gs://${BUCKET_NAME} criado com sucesso!"
echo "   Região: us-central1 (Always Free)"
echo "   Lifecycle: delete após 90 dias"
echo "   Acesso: apenas SA ${SA_EMAIL}"
