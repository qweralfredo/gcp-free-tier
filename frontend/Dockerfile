# ── Stage 1: PHP-FPM 8.2 (porta alta — sem root) ─────────────────────────────
FROM php:8.2-fpm-alpine AS production

# Instalar extensões necessárias
RUN apk add --no-cache \
        libpng-dev \
        oniguruma-dev \
    && docker-php-ext-install \
        pdo \
        pdo_sqlite \
        mbstring \
        json \
    && rm -rf /var/cache/apk/*

# Configuração de segurança: rodar como usuário não-root
RUN addgroup -g 1001 briefapp \
 && adduser -D -u 1001 -G briefapp briefapp

WORKDIR /var/www/html

# Copiar código do frontend
COPY --chown=briefapp:briefapp . .

# PHP-FPM escuta na porta 9000 (alta, não privilegiada)
EXPOSE 9000

USER briefapp

CMD ["php-fpm", "--nodaemonize"]
