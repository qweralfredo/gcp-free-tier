FROM php:8.2-fpm-alpine

# Instalar dependências PHP necessárias
RUN apk add --no-cache \
    nginx \
    curl \
    && docker-php-ext-install opcache

# Configuração do OPcache para performance
RUN { \
    echo 'opcache.enable=1'; \
    echo 'opcache.memory_consumption=32'; \
    echo 'opcache.max_accelerated_files=500'; \
    echo 'opcache.revalidate_freq=60'; \
} > /usr/local/etc/php/conf.d/opcache.ini

# Criar diretórios da aplicação
RUN mkdir -p /var/www/html /var/run/php

WORKDIR /var/www/html

# Copiar código PHP
COPY public/ /var/www/html/public/
COPY src/ /var/www/html/src/
COPY views/ /var/www/html/views/

# Permissões corretas para PHP-FPM (não-root)
RUN chown -R www-data:www-data /var/www/html

USER www-data

EXPOSE 9000

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -fs http://localhost:9000 || exit 1

CMD ["php-fpm"]
