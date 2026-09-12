# ioschools - Deployment Guide

## Overview

This guide covers the complete deployment process for the **ioschools** ASP.NET MVC web application on **AWS EKS (Elastic Kubernetes Service)**.

- **Application**: ioschools (School Management System)
- **Framework**: ASP.NET MVC (.NET 8.0)
- **Application Type**: Web Application (MVC)
- **Port**: 80 (HTTP)
- **Health Endpoint**: `/health`
- **Target Platform**: AWS EKS

---

## Prerequisites

### Local Development
- [Docker Desktop](https://www.docker.com/products/docker-desktop) 20.10+
- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Git](https://git-scm.com/)

### AWS EKS Deployment
- [AWS CLI v2](https://docs.aws.amazon.com/cli/latest/userguide/install-cliv2.html) configured with appropriate IAM permissions
- [kubectl](https://kubernetes.io/docs/tasks/tools/) 1.24+
- [eksctl](https://eksctl.io/) (optional, for cluster creation)
- AWS IAM permissions: `ecr:*`, `eks:*`, `ec2:*`, `iam:PassRole`

---

## Project Structure

```
ioschools/
├── Dockerfile                  # Multi-stage Docker build
├── .dockerignore               # Docker build exclusions
├── docker-compose.yml          # Local development compose
├── kubernetes/
│   ├── namespace.yaml          # Kubernetes namespace
│   ├── deployment.yaml         # Application deployment
│   ├── service.yaml            # ClusterIP service
│   └── ingress.yaml            # AWS ALB ingress
├── scripts/
│   ├── build-push.sh           # Linux/macOS build & push
│   ├── build-push.bat          # Windows build & push
│   ├── deploy-image.sh         # Linux/macOS EKS deploy
│   └── deploy-image.bat        # Windows EKS deploy
└── docs/
    └── DEPLOYMENT.md           # This file
```

---

## Local Development with Docker Compose

### 1. Build and Run Locally

```bash
# From the repository root (parent of ioschools/)
docker-compose -f ioschools/docker-compose.yml up --build
```

### 2. Access the Application

- Application: http://localhost:80
- Health Check: http://localhost:80/health

### 3. Environment Variables

Create a `.env` file in the `ioschools/` directory:

```env
CONNECTION_STRING=Data Source=your-db-host;Database=ioschools;User ID=ioschools;Password=yourpassword;
CACHE_SCHEDULER_URL=/cache/scheduler
ASPNETCORE_ENVIRONMENT=Production
```

### 4. Stop the Application

```bash
docker-compose -f ioschools/docker-compose.yml down
```

---

## Building and Pushing the Docker Image

### Linux/macOS

```bash
chmod +x scripts/build-push.sh
./scripts/build-push.sh
```

### Windows

```cmd
scripts\build-push.bat
```

The script will prompt you to:
1. Enter an image tag (default: `latest`)
2. Select registry type: **AWS ECR** or **Docker Hub**
3. Provide registry credentials and repository details

The script automatically:
- Sanitizes the image name to be Docker-compliant (lowercase, hyphens)
- Creates the ECR repository if it does not exist (ECR only)
- Builds the image from the repository root context
- Pushes the image to the selected registry

---

## AWS EKS Deployment

### Step 1: Configure AWS CLI

```bash
aws configure
# Enter: AWS Access Key ID, Secret Access Key, Region, Output format
```

### Step 2: Create or Connect to EKS Cluster

**Create a new cluster (if needed):**
```bash
eksctl create cluster \
  --name ioschools-cluster \
  --region us-east-1 \
  --nodegroup-name standard-workers \
  --node-type t3.medium \
  --nodes 2 \
  --nodes-min 1 \
  --nodes-max 4 \
  --managed
```

**Connect to an existing cluster:**
```bash
aws eks update-kubeconfig --region us-east-1 --name your-cluster-name
kubectl cluster-info
```

### Step 3: Install AWS Load Balancer Controller (for Ingress)

```bash
# Add the EKS chart repo
helm repo add eks https://aws.github.io/eks-charts
helm repo update

# Install the controller
helm install aws-load-balancer-controller eks/aws-load-balancer-controller \
  -n kube-system \
  --set clusterName=your-cluster-name \
  --set serviceAccount.create=false \
  --set serviceAccount.name=aws-load-balancer-controller
```

### Step 4: Build and Push the Docker Image

```bash
# Linux/macOS
./scripts/build-push.sh

# Windows
scripts\build-push.bat
```

Note the full image URI output (e.g., `123456789.dkr.ecr.us-east-1.amazonaws.com/ioschools:latest`).

### Step 5: Deploy to EKS

```bash
# Linux/macOS
chmod +x scripts/deploy-image.sh
./scripts/deploy-image.sh

# Windows
scripts\deploy-image.bat
```

The script will prompt for:
- AWS Region
- EKS Cluster Name
- Docker Image URI (from Step 4)
- `CONNECTION_STRING` - SQL Server connection string
- `CACHE_SCHEDULER_URL` - Cache scheduler URL

### Step 6: Verify Deployment

```bash
# Check pods are running
kubectl get pods -n ioschools

# Check services
kubectl get svc -n ioschools

# Check ingress and get URL
kubectl get ingress -n ioschools

# View application logs
kubectl logs -f deployment/ioschools -n ioschools
```

---

## Kubernetes Manifest Descriptions

### namespace.yaml
Creates the `ioschools` Kubernetes namespace to isolate all application resources.

### deployment.yaml
Defines the application deployment with:
- **2 replicas** for high availability
- **Rolling update** strategy (zero-downtime deployments)
- **Resource limits**: CPU 500m, Memory 1Gi
- **Resource requests**: CPU 250m, Memory 512Mi
- **Liveness probe**: GET `/health` every 30s (starts after 60s)
- **Readiness probe**: GET `/health` every 15s (starts after 30s)
- **Non-root security context** (UID 1000)

### service.yaml
Creates a `ClusterIP` service exposing port 80 internally within the cluster.

### ingress.yaml
Creates an AWS ALB (Application Load Balancer) ingress with:
- Internet-facing scheme
- IP target type
- Health check on `/health`
- Host: `ioschools.example.com` (update to your domain)

---

## Configuration Management

### Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `ASPNETCORE_ENVIRONMENT` | .NET environment (Production/Development) | Yes |
| `ASPNETCORE_URLS` | Kestrel binding URL | Yes |
| `CONNECTION_STRING` | SQL Server connection string | Yes |
| `CACHE_SCHEDULER_URL` | Cache scheduler URL for container scheduling | No |
| `TZ` | Timezone (default: UTC) | No |

### Using Kubernetes Secrets for Sensitive Data

```bash
# Create a secret for the database connection string
kubectl create secret generic ioschools-secrets \
  --from-literal=connection-string="Data Source=your-db;Database=ioschools;User ID=ioschools;Password=yourpassword;" \
  -n ioschools
```

Then reference in `deployment.yaml`:
```yaml
env:
  - name: CONNECTION_STRING
    valueFrom:
      secretKeyRef:
        name: ioschools-secrets
        key: connection-string
```

---

## Scaling

### Manual Scaling

```bash
kubectl scale deployment ioschools --replicas=4 -n ioschools
```

### Horizontal Pod Autoscaler (HPA)

```bash
kubectl autoscale deployment ioschools \
  --cpu-percent=70 \
  --min=2 \
  --max=10 \
  -n ioschools
```

---

## Rolling Updates and Rollbacks

### Deploy a New Image Version

```bash
kubectl set image deployment/ioschools \
  ioschools=123456789.dkr.ecr.us-east-1.amazonaws.com/ioschools:v2.0 \
  -n ioschools

# Monitor rollout
kubectl rollout status deployment/ioschools -n ioschools
```

### Rollback to Previous Version

```bash
kubectl rollout undo deployment/ioschools -n ioschools

# Rollback to specific revision
kubectl rollout undo deployment/ioschools --to-revision=2 -n ioschools

# View rollout history
kubectl rollout history deployment/ioschools -n ioschools
```

---

## Troubleshooting

### Pod Not Starting

```bash
# Check pod status and events
kubectl describe pod -l app=ioschools -n ioschools

# Check pod logs
kubectl logs -l app=ioschools -n ioschools --previous
```

### Health Check Failures

```bash
# Test health endpoint directly
kubectl exec -it $(kubectl get pod -l app=ioschools -n ioschools -o jsonpath='{.items[0].metadata.name}') \
  -n ioschools -- wget -qO- http://localhost:80/health
```

### Ingress Not Getting an Address

```bash
# Check AWS Load Balancer Controller logs
kubectl logs -n kube-system deployment/aws-load-balancer-controller

# Check ingress events
kubectl describe ingress ioschools-ingress -n ioschools
```

### Image Pull Errors

```bash
# Verify ECR credentials
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin \
  123456789.dkr.ecr.us-east-1.amazonaws.com

# Check if image exists
aws ecr describe-images --repository-name ioschools --region us-east-1
```

### Database Connection Issues

```bash
# Check environment variables in running pod
kubectl exec -it $(kubectl get pod -l app=ioschools -n ioschools -o jsonpath='{.items[0].metadata.name}') \
  -n ioschools -- env | grep CONNECTION
```

---

## Security Considerations

1. **Non-root container**: The application runs as UID 1000 (non-root)
2. **Secrets management**: Use Kubernetes Secrets or AWS Secrets Manager for sensitive data
3. **Network policies**: Consider adding Kubernetes NetworkPolicy to restrict pod-to-pod communication
4. **Image scanning**: Enable ECR image scanning for vulnerability detection
5. **RBAC**: Apply least-privilege IAM roles for EKS node groups
6. **TLS/HTTPS**: Configure ACM certificate in the ingress annotations for HTTPS:
   ```yaml
   alb.ingress.kubernetes.io/certificate-arn: arn:aws:acm:us-east-1:123456789:certificate/xxx
   alb.ingress.kubernetes.io/listen-ports: '[{"HTTPS":443}]'
   ```

---

## .NET-Specific Notes

- **Target Framework**: .NET 8.0 (net8.0)
- **Application Type**: ASP.NET MVC with Web.config (migrated to .NET 8)
- **Build Image**: `mcr.microsoft.com/dotnet/sdk:8.0`
- **Runtime Image**: `mcr.microsoft.com/dotnet/sdk:6.0-alpine` (explicit base image)
- **Health Endpoint**: `/health` (HealthController.cs)
- **Startup Time**: Allow 60 seconds for initial liveness probe (Lucene index building)
- **Culture**: Configured for `en-NZ` locale (from Web.config globalization settings)
- **Session**: Session state is disabled (`mode="Off"` in Web.config)
- **ELMAH**: Error logging configured - requires SQL Server connection string

### .NET Performance Tuning

```yaml
# Add to deployment.yaml env section for .NET GC tuning
- name: DOTNET_GCHeapHardLimit
  value: "805306368"  # 768MB heap limit
- name: DOTNET_GCConserveMemory
  value: "5"
- name: DOTNET_ReadyToRun
  value: "1"
```

---

## Monitoring and Observability

### View Application Logs

```bash
# Stream logs from all pods
kubectl logs -f -l app=ioschools -n ioschools

# Logs from specific pod
kubectl logs -f pod/ioschools-xxxxx-yyyyy -n ioschools
```

### AWS CloudWatch Integration

Enable Container Insights for EKS:
```bash
aws eks create-addon \
  --cluster-name your-cluster-name \
  --addon-name amazon-cloudwatch-observability \
  --region us-east-1
```

---

*Generated for ioschools ASP.NET MVC application targeting AWS EKS*
