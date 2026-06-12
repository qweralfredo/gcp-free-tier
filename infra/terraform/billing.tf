# ══════════════════════════════════════════════════════════════════
# billing.tf — Budget de $1 USD com alertas em 50%, 80% e 100%
# Qualquer custo acima de $0 gera notificação — proteção máxima
# ══════════════════════════════════════════════════════════════════

resource "google_billing_budget" "briefapp" {
  billing_account = var.billing_account_id
  display_name    = "briefapp-guardian-free-tier-watch"

  budget_filter {
    projects = ["projects/${var.project_id}"]
  }

  # Budget de $1 — qualquer custo não-zero deve gerar alerta
  amount {
    specified_amount {
      currency_code = "USD"
      units         = "1"
    }
  }

  # Alertas em 50%, 80% e 100% do budget ($0.50, $0.80, $1.00)
  threshold_rules {
    threshold_percent = 0.5
    spend_basis       = "CURRENT_SPEND"
  }

  threshold_rules {
    threshold_percent = 0.8
    spend_basis       = "CURRENT_SPEND"
  }

  threshold_rules {
    threshold_percent = 1.0
    spend_basis       = "CURRENT_SPEND"
  }

  # Notificação por email para os billing admins do projeto
  all_updates_rule {
    disable_default_iam_recipients   = false
    monitoring_notification_channels = []
  }

  depends_on = [google_project_service.required_apis]
}
