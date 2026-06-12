terraform {
  required_version = ">= 1.6.0"

  required_providers {
    google = {
      source  = "hashicorp/google"
      version = ">= 5.0, < 6.0"
    }
  }

  # ── Estado remoto no próprio bucket GCS (Always Free) ────────────
  # BOOTSTRAP: criar o bucket primeiro com -backend=false, depois
  # executar 'terraform init' normalmente para migrar o estado.
  backend "gcs" {
    bucket = "briefapp-guardian-metrics" # sobrescrito por -backend-config
    prefix = "terraform/state"
  }
}

provider "google" {
  project = var.project_id
  region  = var.region
}
