# Terraform — GCP Free Tier Guardian

## Pré-requisitos

- [Terraform >= 1.6](https://developer.hashicorp.com/terraform/install)
- [Google Cloud SDK](https://cloud.google.com/sdk/docs/install) autenticado: `gcloud auth application-default login`
- Projeto GCP com billing account vinculada
- Permissão de `Owner` ou `Editor` no projeto GCP

## Estrutura dos arquivos

| Arquivo | Descrição |
|---------|-----------|
| `versions.tf` | Provider Google e backend GCS (estado remoto) |
| `variables.tf` | Todas as variáveis configuráveis |
| `apis.tf` | Ativa as 9 APIs GCP necessárias |
| `iam.tf` | Service Account `briefapp-guardian` com roles mínimas |
| `storage.tf` | Bucket GCS `us-central1` com lifecycle 90 dias |
| `compute.tf` | VM `e2-micro` + firewall rules |
| `billing.tf` | Budget alert $1 USD (50%/80%/100%) |
| `outputs.tf` | IP da VM, URL do bucket, email SA, chave JSON |

## Bootstrap (primeira vez)

O estado Terraform é armazenado no próprio bucket GCS. Por isso, o bucket precisa existir antes de `terraform init` com backend remoto.

```bash
# 1. Configurar variáveis
cp terraform.tfvars.example terraform.tfvars
# Editar terraform.tfvars com seus valores reais

# 2. Init sem backend (local state)
terraform init -backend=false

# 3. Criar APENAS o bucket primeiro
terraform apply -target=google_storage_bucket.metrics \
                -target=google_project_service.required_apis

# 4. Reinicializar com backend remoto (migra estado para o bucket)
terraform init -migrate-state

# 5. Aplicar o restante da infra
terraform apply
```

## Comandos do dia a dia

```bash
# Ver o que será criado/alterado
terraform plan

# Aplicar mudanças
terraform apply

# Ver IPs e outputs
terraform output

# Extrair chave SA (salvar em infra/gcp/sa-key.json)
terraform output -raw sa_key_base64 | base64 -d > ../../infra/gcp/sa-key.json

# Destruir tudo (apenas em test!)
terraform destroy
```

## Recursos provisionados e custo estimado

| Recurso | Tipo | Custo mensal |
|---------|------|-------------|
| VM `briefapp-guardian-vm` | e2-micro / us-central1 | **$0,00** (Always Free) |
| Disco boot 30GB | pd-standard | **$0,00** (incluso na VM) |
| Bucket GCS `us-central1` | Standard Storage < 5GB | **$0,00** (Always Free) |
| Egress GCS | < 1GB/dia | **$0,00** (Always Free) |
| Cloud Monitoring API | < 1M chamadas | **$0,00** (Free tier) |
| Cloud Billing API | Ilimitado | **$0,00** (Gratuita) |
| **TOTAL** | | **$0,00/mês** |

> ⚠️ O billing budget de $1 garante que qualquer desvio seja detectado antes de gerar custo significativo.

## Segurança

- `terraform.tfvars` está no `.gitignore` — nunca commite
- A chave SA gerada via `terraform output sa_key_base64` é **sensitive** e não aparece em `terraform show`
- SSH restrito ao IP do admin via `admin_cidrs`
- Bucket com `public_access_prevention = enforced`
