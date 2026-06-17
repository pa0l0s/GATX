#!/bin/bash
set -euo pipefail
exec > /var/log/gatx-userdata.log 2>&1

# ── Install Docker + cron (AL2023 ships no cron by default) ─────────────────────
dnf update -y
dnf install -y docker cronie
systemctl enable --now docker
systemctl enable --now crond
usermod -aG docker ec2-user

# ── Install Docker Compose v2 (pinned — the GitHub "latest" API rate-limits) ────
COMPOSE_VERSION=v2.32.4
curl -SL "https://github.com/docker/compose/releases/download/$${COMPOSE_VERSION}/docker-compose-linux-x86_64" \
  -o /usr/local/bin/docker-compose
chmod +x /usr/local/bin/docker-compose

# ── Authenticate to ECR ────────────────────────────────────────────────────────
aws ecr get-login-password --region ${region} \
  | docker login --username AWS --password-stdin ${ecr_registry}

# ── Write docker-compose.yml ───────────────────────────────────────────────────
mkdir -p /opt/gatx
cat > /opt/gatx/docker-compose.yml <<'COMPOSE'
services:
  api:
    image: ${api_image}:latest
    restart: unless-stopped
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__DefaultConnection: "Host=${db_host};Port=5432;Database=gatxdemo;Username=${db_username};Password=${db_password}"
      Frontend__Origin: "*"
      Jwt__Secret: "${jwt_secret}"

  frontend:
    image: ${frontend_image}:latest
    restart: unless-stopped
    ports:
      - "80:80"
    # Start order only — nginx serves the static site immediately and proxies
    # /api/ to the api container, which may still be warming up. No health gate
    # (the aspnet base image has no curl, so a curl healthcheck never passes).
    depends_on:
      - api
COMPOSE

# ── ECR login on every boot (credentials expire after 12 hours) ───────────────
mkdir -p /etc/cron.hourly
cat > /etc/cron.hourly/ecr-login <<'CRON'
#!/bin/bash
aws ecr get-login-password --region ${region} \
  | docker login --username AWS --password-stdin ${ecr_registry}
CRON
chmod +x /etc/cron.hourly/ecr-login

# ── Start the app ──────────────────────────────────────────────────────────────
cd /opt/gatx
docker-compose pull
docker-compose up -d

echo "GATX startup complete"
