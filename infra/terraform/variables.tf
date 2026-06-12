# ── Obrigatórios ──────────────────────────────────────────────────
variable "project_id" {
  description = "ID do projeto GCP (ex: my-project-123456)"
  type        = string
}

variable "billing_account_id" {
  description = "ID da conta de faturamento GCP (ex: XXXXXX-XXXXXX-XXXXXX)"
  type        = string
}

# ── Região / Zona — NUNCA alterar: apenas us-central1 é Always Free ─
variable "region" {
  description = "Região GCP Always Free. NÃO alterar."
  type        = string
  default     = "us-central1"

  validation {
    condition     = contains(["us-central1", "us-west1", "us-east1"], var.region)
    error_message = "Apenas regiões Always Free são permitidas: us-central1, us-west1, us-east1."
  }
}

variable "zone" {
  description = "Zona dentro da região us-central1"
  type        = string
  default     = "us-central1-a"
}

# ── Recursos ──────────────────────────────────────────────────────
variable "vm_name" {
  description = "Nome da VM e2-micro"
  type        = string
  default     = "briefapp-guardian-vm"
}

variable "bucket_name" {
  description = "Nome do bucket GCS para métricas e estado Terraform"
  type        = string
  default     = "briefapp-guardian-metrics"
}

variable "sa_name" {
  description = "Nome da Service Account"
  type        = string
  default     = "briefapp-guardian"
}

# ── Segurança SSH ─────────────────────────────────────────────────
variable "admin_ssh_key" {
  description = "Chave pública SSH para acesso admin à VM (formato: 'usuario:ssh-rsa AAAA...')"
  type        = string
  sensitive   = true
}

variable "admin_cidrs" {
  description = "CIDRs permitidos para SSH. Use seu IP atual: ['x.x.x.x/32']"
  type        = list(string)
  default     = ["0.0.0.0/0"] # Alterar para seu IP em produção!
}
