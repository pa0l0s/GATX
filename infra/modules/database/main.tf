# ── Default VPC ───────────────────────────────────────────────────────────────
data "aws_vpc" "default" {
  default = true
}

data "aws_subnets" "default" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }
}

# ── DB subnet group (required even in default VPC) ────────────────────────────
resource "aws_db_subnet_group" "postgres" {
  name       = "${var.app_name}-db-subnet-group"
  subnet_ids = data.aws_subnets.default.ids

  tags = { Name = "${var.app_name}-db-subnet-group" }
}

# ── Security group — allows PostgreSQL inside the default VPC only ────────────
resource "aws_security_group" "postgres" {
  name        = "${var.app_name}-db-sg"
  description = "Allow PostgreSQL inside the default VPC"
  vpc_id      = data.aws_vpc.default.id

  ingress {
    description = "PostgreSQL from default VPC CIDR"
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = [data.aws_vpc.default.cidr_block]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "${var.app_name}-db-sg" }
}

# ── RDS db.t3.micro — FREE TIER eligible (750 hrs/month for 12 months) ────────
resource "aws_db_instance" "postgres" {
  allocated_storage       = 20 # free tier: up to 20 GB
  db_name                 = replace(var.app_name, "-", "")
  engine                  = "postgres"
  engine_version          = "16"
  instance_class          = "db.t3.micro" # fixed: t4g.micro is NOT free tier
  username                = var.db_username
  password                = var.db_password
  publicly_accessible     = false
  skip_final_snapshot     = true
  deletion_protection     = false
  backup_retention_period = 0
  db_subnet_group_name    = aws_db_subnet_group.postgres.name
  vpc_security_group_ids  = [aws_security_group.postgres.id]

  tags = { Name = "${var.app_name}-postgres" }
}

output "endpoint" {
  description = "Full RDS endpoint (host:port)"
  value       = aws_db_instance.postgres.endpoint
}

output "host" {
  description = "RDS hostname only (without port)"
  value       = aws_db_instance.postgres.address
}

output "security_group_id" {
  value = aws_security_group.postgres.id
}
