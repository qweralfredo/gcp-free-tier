# ══════════════════════════════════════════════════════════════════
# compute.tf — VM e2-micro us-central1 (Always Free: 744h/mês)
# ══════════════════════════════════════════════════════════════════

# ── VM e2-micro ────────────────────────────────────────────────────
resource "google_compute_instance" "briefapp_vm" {
  project      = var.project_id
  name         = var.vm_name
  machine_type = "e2-micro"  # Always Free — NÃO alterar
  zone         = var.zone

  tags = ["briefapp-guardian", "http-server", "https-server"]

  boot_disk {
    initialize_params {
      image = "debian-cloud/debian-12"
      size  = 30   # GB — máximo Always Free
      type  = "pd-standard"
    }
  }

  network_interface {
    network = "default"

    # IP externo efêmero (free em us-central1)
    access_config {}
  }

  # ── Credenciais SA montadas via metadata ──────────────────────
  service_account {
    email  = google_service_account.briefapp.email
    scopes = ["cloud-platform"]
  }

  # ── SSH Key ───────────────────────────────────────────────────
  metadata = {
    ssh-keys = var.admin_ssh_key

    # Startup: instala Docker e clona o repositório
    startup-script = <<-EOT
      #!/bin/bash
      set -euo pipefail
      # Instalar git para bootstrap inicial
      apt-get update -qq && apt-get install -y -qq git
      # Clonar repositório se não existir
      if [ ! -d "/opt/briefapp/.git" ]; then
        git clone https://github.com/qweralfredo/gcp-free-tier.git /opt/briefapp
      fi
      # Executar script de setup
      bash /opt/briefapp/infra/scripts/setup-vm.sh
    EOT
  }

  # Evitar recrear a VM ao mudar apenas metadata não crítica
  lifecycle {
    ignore_changes = [metadata["startup-script"]]
  }

  depends_on = [
    google_project_service.required_apis,
    google_service_account.briefapp,
  ]
}

# ── Firewall: HTTP ─────────────────────────────────────────────────
resource "google_compute_firewall" "allow_http" {
  project = var.project_id
  name    = "${var.vm_name}-allow-http"
  network = "default"

  allow {
    protocol = "tcp"
    ports    = ["80"]
  }

  target_tags   = ["http-server"]
  source_ranges = ["0.0.0.0/0"]
}

# ── Firewall: HTTPS ────────────────────────────────────────────────
resource "google_compute_firewall" "allow_https" {
  project = var.project_id
  name    = "${var.vm_name}-allow-https"
  network = "default"

  allow {
    protocol = "tcp"
    ports    = ["443"]
  }

  target_tags   = ["https-server"]
  source_ranges = ["0.0.0.0/0"]
}

# ── Firewall: SSH (restrito ao IP do admin) ────────────────────────
resource "google_compute_firewall" "allow_ssh" {
  project = var.project_id
  name    = "${var.vm_name}-allow-ssh"
  network = "default"

  allow {
    protocol = "tcp"
    ports    = ["22"]
  }

  target_tags   = ["briefapp-guardian"]
  source_ranges = var.admin_cidrs
}
