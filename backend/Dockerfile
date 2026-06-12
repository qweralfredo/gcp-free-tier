# ─────────────────────────────────────────────────────────
# Stage 1 — Build
# ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copiar apenas os csproj primeiro (aproveita o cache de layers)
COPY src/BriefappGuardian.Core/BriefappGuardian.Core.csproj   src/BriefappGuardian.Core/
COPY src/BriefappGuardian.Api/BriefappGuardian.Api.csproj     src/BriefappGuardian.Api/

# Restaurar dependências (camada cacheada enquanto .csproj não mudar)
RUN dotnet restore src/BriefappGuardian.Api/BriefappGuardian.Api.csproj

# Copiar o restante do código-fonte
COPY src/ src/

# Publicar em Release sem runtime standalone (usa o runtime da imagem base)
RUN dotnet publish src/BriefappGuardian.Api/BriefappGuardian.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ─────────────────────────────────────────────────────────
# Stage 2 — Runtime
# Runtime mínimo ASP.NET Core 10 (~80 MB vs ~200 MB do SDK)
# ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Criar diretórios necessários
RUN mkdir -p /app/data /var/log/briefapp \
    && chown -R app:app /app/data /var/log/briefapp

# Copiar o artefato publicado do stage de build
COPY --from=build /app/publish .

# Usuário não-root para segurança (evita execução como root no container)
USER app

# Porta exposta (alta, sem root necessário)
EXPOSE 8081

# Health check para o Docker Compose
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8081/api/health || exit 1

ENV ASPNETCORE_URLS=http://+:8081
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "BriefappGuardian.Api.dll"]
