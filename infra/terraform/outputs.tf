output "vm_external_ip" {
  description = "IP externo da VM e2-micro (usar para configurar DNS)"
  value       = google_compute_instance.briefapp_vm.network_interface[0].access_config[0].nat_ip
}

output "vm_name" {
  description = "Nome da VM no GCP"
  value       = google_compute_instance.briefapp_vm.name
}

output "bucket_url" {
  description = "URL do bucket GCS de métricas"
  value       = "gs://${google_storage_bucket.metrics.name}"
}

output "sa_email" {
  description = "Email da Service Account briefapp-guardian"
  value       = google_service_account.briefapp.email
}

output "sa_key_base64" {
  description = "Chave JSON da SA em base64. Decodifique e salve em infra/gcp/sa-key.json"
  value       = google_service_account_key.briefapp_key.private_key
  sensitive   = true
}
