# 🛡️ GCP Free Tier Guardian

**Sistema de monitoramento e guardrails que mantém projetos GCP dentro dos limites gratuitos (Always Free).**

Atua como uma "trava de segurança" ativa, coletando métricas de consumo via Cloud Monitoring API e executando ações automáticas de contenção antes que as cotas gratuitas sejam ultrapassadas.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![PHP](https://img.shields.io/badge/PHP-8.2-blue)](https://www.php.net/)
[![Docker](https://img.shields.io/badge/Docker-Compose-blue)](https://docs.docker.com/compose/)

---

## 🏗️ Arquitetura

```
[ Usuário ] ──HTTPS──► [ Nginx :80/443 ]
                              │
                 ┌────────────┴────────────┐
                 │                         │
       [ PHP-FPM :9000 ]        [ .NET API :8081 ]
       Frontend MVC              Backend ASP.NET Core
       Vue.js + ECharts          DuckDB + Guardrails
                                       │
                                  [ DuckDB (local) ]
                                  briefapp_cache.db
                                       │
                               [ GCS Bucket (sync) ]
                               4x/dia via Parquet
```

### Stack

| Camada | Tecnologia |
|--------|-----------|
| Infra | VM e2-micro us-central1 (Always Free) |
| Proxy | Nginx 1.27-alpine |
| Backend | .NET 10 ASP.NET Core Minimal API |
| Banco | DuckDB 1.3 (local) + GCS (sync) |
| Frontend | PHP 8.2-FPM + Vue.js 3 + Vuetify 3 |
| Gráficos | Apache ECharts 5 |
| Alertas | Telegram Bot API |
| IaC | Terraform (GCP) |
| Orquestração | Docker Compose |

---

## 📊 Serviços Monitorados

| Serviço | Métrica | Limite Free Tier |
|---------|---------|-----------------|
| Compute Engine | Horas VM e2-micro | 730h/mês (us-central1) |
| Cloud Storage | Armazenamento total | 5 GB/mês |
| Cloud Storage | Egress (saída) | 1 GB/dia |
| Cloud Storage | Operações Classe A | 20.000/dia |
| Cloud Storage | Operações Classe B | 50.000/dia |
| Cloud Run | Requisições | 2M/mês |
| Cloud Run | CPU | 180.000 vCPU-seg/mês |
| Cloud Run | Memória | 360.000 GiB-seg/mês |
| Cloud Functions | Invocações | 2M/mês |
| Pub/Sub | Mensagens | 10 GB/mês |

### Thresholds de Alerta

| % de Quota | Nível | Ação |
|-----------|-------|------|
| 75% | ⚠️ Warning | Notificação Telegram |
| 90% | 🔴 Critical | Cloud Run → max-instances=1 |
| 98% | 🚨 Emergency | Kill-switch (Cloud Run parado) |

---

## 🚀 Setup

### Pré-requisitos

- Docker + Docker Compose v2+
- Conta GCP com projeto criado
- Service Account com roles: `Monitoring Viewer`, `Billing Account Viewer`, `Run Admin`
- Bot Telegram (opcional — para alertas)

### 1. Clonar o repositório

```bash
git clone https://github.com/qweralfredo/gcp-free-tier.git
cd gcp-free-tier
```

### 2. Configurar variáveis de ambiente

```bash
cp .env.example .env
```

Editar `.env`:

```env
GCP_PROJECT_ID=seu-projeto-gcp-123456
GCS_BUCKET_NAME=briefapp-guardian-metrics
TELEGRAM_BOT_TOKEN=seu-token-bot
TELEGRAM_CHAT_ID=-100xxxxxxxx

# Gerar hash bcrypt da sua senha:
# php -r "echo password_hash('SuaSenha123', PASSWORD_BCRYPT);"
ADMIN_PASSWORD_HASH=$2y$12$...
```

### 3. Adicionar Service Account key

Baixe a chave JSON da Service Account e salve em:
```
infra/gcp/sa-key.json
```

> ⚠️ Este arquivo está no `.gitignore` e **NUNCA deve ser commitado**.

### 4. Subir os containers

```bash
docker compose up -d
```

### 5. Verificar

```bash
docker compose ps
curl http://localhost:8081/api/health
```

Acesse `http://localhost` no navegador e faça login com a senha configurada.

---

## 📁 Estrutura do Projeto

```
gcp-free-tier/
├── backend/
│   ├── Dockerfile
│   └── src/
│       ├── BriefappGuardian.Api/     ← ASP.NET Core Minimal API
│       │   ├── Program.cs
│       │   ├── AppSettings.cs
│       │   ├── Data/DuckDbContext.cs
│       │   ├── Endpoints/
│       │   ├── Services/
│       │   └── Workers/
│       └── BriefappGuardian.Core/    ← Entidades e contratos
│           ├── Entities/
│           └── Contracts/
├── frontend/
│   ├── Dockerfile
│   ├── public/index.php              ← Front controller
│   ├── src/
│   │   ├── Router.php
│   │   └── Controllers/
│   └── views/
│       ├── layout.php                ← Layout Vuetify
│       ├── login.php
│       └── dashboard.php
├── infra/
│   ├── nginx/briefapp.conf           ← Config Nginx
│   ├── gcp/sa-key.json               ← ⚠️ NÃO commitar!
│   └── terraform/                    ← IaC GCP
│       ├── main.tf
│       ├── variables.tf
│       └── outputs.tf
├── docker-compose.yml
├── .env.example
└── .gitignore
```

---

## 🔧 Desenvolvimento Local

### Rodar apenas o backend

```bash
cd backend
dotnet run --project src/BriefappGuardian.Api
# API disponível em http://localhost:5000
# Swagger em http://localhost:5000/swagger
```

### Build Docker individual

```bash
# Backend
docker build -t briefapp-dotnet ./backend

# Frontend
docker build -t briefapp-php ./frontend
```

---

## 📖 API Endpoints

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/api/health` | Health check |
| GET | `/api/dashboard` | Estado completo de todas as quotas |
| GET | `/api/alerts?limit=50` | Histórico de alertas guardrail |
| GET | `/api/quotas` | Configurações de thresholds |

---

## 🌍 Deploy na VM GCP (Always Free)

Ver pasta `infra/terraform/` para provisionar a VM e2-micro com Terraform.

```bash
cd infra/terraform
terraform init
terraform plan -var-file="terraform.tfvars"
terraform apply
```

Após provisionar, fazer SSH e clonar o repositório:

```bash
gcloud compute ssh briefapp-guardian --zone=us-central1-a
git clone https://github.com/qweralfredo/gcp-free-tier.git
cd gcp-free-tier
# Configurar .env e sa-key.json
docker compose up -d
```

---

## 📄 Licença

MIT © 2026 — qweralfredo
