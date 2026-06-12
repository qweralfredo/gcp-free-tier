# ══════════════════════════════════════════════════════════════════
# storage.tf — Bucket GCS em us-central1 (Always Free: 5GB)
# Estado Terraform também armazenado aqui (prefix: terraform/state)
# ══════════════════════════════════════════════════════════════════

resource "google_storage_bucket" "metrics" {
  project  = var.project_id
  name     = var.bucket_name
  location = "US-CENTRAL1" # Always Free — NÃO alterar

  # Segurança
  uniform_bucket_level_access = true
  public_access_prevention    = "enforced"

  # Prevenir delete acidental via terraform destroy
  force_destroy = false

  # ── Lifecycle: deletar objetos com > 90 dias ──────────────────
  # Evita ultrapassar o limite free de 5 GB de métricas Parquet
  lifecycle_rule {
    action {
      type = "Delete"
    }
    condition {
      age = 90 # dias
    }
  }

  # ── Versionamento desabilitado (economiza espaço) ─────────────
  versioning {
    enabled = false
  }

  depends_on = [google_project_service.required_apis]
}

# ── IAM: apenas a SA briefapp-guardian tem acesso ─────────────────
resource "google_storage_bucket_iam_member" "briefapp_storage" {
  bucket = google_storage_bucket.metrics.name
  role   = "roles/storage.objectAdmin"
  member = "serviceAccount:${google_service_account.briefapp.email}"
}
