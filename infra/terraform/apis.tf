# ══════════════════════════════════════════════════════════════════
# apis.tf — Ativa as APIs GCP necessárias para o Briefapp Guardian
# disable_on_destroy=false: não desabilita APIs ao destruir infra
# ══════════════════════════════════════════════════════════════════

locals {
  required_apis = [
    "cloudmonitoring.googleapis.com",
    "cloudbilling.googleapis.com",
    "billingbudgets.googleapis.com",
    "recommender.googleapis.com",
    "run.googleapis.com",
    "compute.googleapis.com",
    "apikeys.googleapis.com",
    "storage.googleapis.com",
    "iam.googleapis.com",
  ]
}

resource "google_project_service" "required_apis" {
  for_each = toset(local.required_apis)

  project = var.project_id
  service = each.key

  # Não desabilitar APIs ao fazer terraform destroy
  # (evita quebrar outros recursos que possam usá-las)
  disable_on_destroy         = false
  disable_dependent_services = false
}
