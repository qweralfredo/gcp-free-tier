# ══════════════════════════════════════════════════════════════════
# iam.tf — Service Account com roles mínimas (Principle of Least Privilege)
# ══════════════════════════════════════════════════════════════════

# ── Service Account ────────────────────────────────────────────────
resource "google_service_account" "briefapp" {
  project      = var.project_id
  account_id   = var.sa_name
  display_name = "Briefapp Guardian SA"
  description  = "Service Account do Briefapp Guardian. Permissões mínimas para monitoramento e guardrails GCP Free Tier."

  depends_on = [google_project_service.required_apis]
}

# ── Roles IAM mínimas ─────────────────────────────────────────────
locals {
  sa_roles = [
    "roles/monitoring.viewer",          # Leitura de métricas Cloud Monitoring
    "roles/billing.viewer",             # Leitura de custos e faturamento
    "roles/recommender.viewer",         # Leitura de sugestões de otimização
    "roles/run.admin",                  # Gerenciar Cloud Run (kill-switch)
    "roles/compute.instanceAdmin.v1",   # Parar VMs acessórias
    "roles/iam.serviceAccountTokenCreator", # Impersonation se necessário
    "roles/apikeys.admin",              # Desativar API Keys em emergência
  ]
}

resource "google_project_iam_member" "briefapp_roles" {
  for_each = toset(local.sa_roles)

  project = var.project_id
  role    = each.key
  member  = "serviceAccount:${google_service_account.briefapp.email}"
}

# ── Chave JSON da SA (armazenada no estado Terraform — sensitive) ──
resource "google_service_account_key" "briefapp_key" {
  service_account_id = google_service_account.briefapp.name
  key_algorithm      = "KEY_ALG_RSA_2048"
}
