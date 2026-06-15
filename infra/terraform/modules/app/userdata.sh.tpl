#!/bin/bash
set -euo pipefail
exec > /var/log/gatx-userdata.log 2>&1

# ── Install Docker ─────────────────────────────────────────────────────────────
dnf update -y
dnf install -y docker
systemctl enable --now docker
usermod -aG docker ec2-user

# ── Install Docker Compose v2 ──────────────────────────────────────────────────
COMPOSE_VERSION=$(curl -s https://api.github.com/repos/docker/compose/releases/latest \
  | grep '"tag_name"' | cut -d'"' -f4)
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
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  frontend:
    image: ${frontend_image}:latest
    restart: unless-stopped
    ports:
      - "80:80"
    depends_on:
      api:
        condition: service_healthy
COMPOSE

# ── ECR login on every boot (credentials expire after 12 hours) ───────────────
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
