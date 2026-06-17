# ── ECR repositories ──────────────────────────────────────────────────────────
resource "aws_ecr_repository" "api" {
  name         = "${var.app_name}-api"
  force_delete = true
}

resource "aws_ecr_repository" "frontend" {
  name         = "${var.app_name}-frontend"
  force_delete = true
}

# ── Default VPC / subnets (free – no custom VPC needed) ───────────────────────
data "aws_vpc" "default" {
  default = true
}

data "aws_subnets" "default" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }
}

# ── EC2 security group ─────────────────────────────────────────────────────────
resource "aws_security_group" "ec2" {
  name        = "${var.app_name}-ec2-sg"
  description = "Allow HTTP and SSH"
  vpc_id      = data.aws_vpc.default.id

  ingress {
    description = "HTTP"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    description = "SSH"
    from_port   = 22
    to_port     = 22
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "${var.app_name}-ec2-sg" }
}

# ── IAM role so EC2 can pull from ECR ─────────────────────────────────────────
resource "aws_iam_role" "ec2" {
  name = "${var.app_name}-ec2-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action    = "sts:AssumeRole"
      Effect    = "Allow"
      Principal = { Service = "ec2.amazonaws.com" }
    }]
  })
}

resource "aws_iam_role_policy_attachment" "ecr_readonly" {
  role       = aws_iam_role.ec2.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonEC2ContainerRegistryReadOnly"
}

# SSM Session Manager — debug the instance without an SSH key pair
resource "aws_iam_role_policy_attachment" "ssm" {
  role       = aws_iam_role.ec2.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
}

resource "aws_iam_instance_profile" "ec2" {
  name = "${var.app_name}-ec2-profile"
  role = aws_iam_role.ec2.name
}

# ── Amazon Linux 2023 AMI (free tier eligible) ─────────────────────────────────
data "aws_ami" "al2023" {
  most_recent = true
  owners      = ["amazon"]
  filter {
    name   = "name"
    values = ["al2023-ami-*-x86_64"]
  }
  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }
}

# ── EC2 t2.micro — runs both containers via docker compose ─────────────────────
resource "aws_instance" "app" {
  ami                    = data.aws_ami.al2023.id
  instance_type          = "t3.micro"  # free-tier eligible (x86_64); t2.micro is no longer eligible on newer accounts
  iam_instance_profile   = aws_iam_instance_profile.ec2.name
  vpc_security_group_ids = [aws_security_group.ec2.id]

  # Recreate the instance when user_data changes (user_data only runs on first boot)
  user_data_replace_on_change = true

  # 30 GB gp2 — free tier includes 30 GB; AL2023 AMI snapshot requires >= 30 GB
  root_block_device {
    volume_type = "gp2"
    volume_size = 30
  }

  user_data = templatefile("${path.module}/userdata.sh.tpl", {
    region           = var.aws_region
    ecr_registry     = split("/", aws_ecr_repository.api.repository_url)[0]
    api_image        = aws_ecr_repository.api.repository_url
    frontend_image   = aws_ecr_repository.frontend.repository_url
    db_host          = var.db_host
    db_password      = var.db_password
    db_username      = var.db_username
    jwt_secret       = var.jwt_secret
  })

  tags = { Name = "${var.app_name}-ec2" }
}

# ── Outputs ───────────────────────────────────────────────────────────────────
output "api_repository_url" {
  value = aws_ecr_repository.api.repository_url
}

output "frontend_repository_url" {
  value = aws_ecr_repository.frontend.repository_url
}

output "ec2_public_ip" {
  value       = aws_instance.app.public_ip
  description = "EC2 public IP — open http://<ip> in your browser"
}

output "ec2_security_group_id" {
  value = aws_security_group.ec2.id
}
