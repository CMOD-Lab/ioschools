#!/bin/bash
# =============================================================================
# deploy-image.sh - Deploy ioschools to AWS EKS
# Prerequisites: aws-cli, kubectl
# =============================================================================
set -e
set -o pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
K8S_DIR="$REPO_ROOT/kubernetes"
APP_NAME="ioschools"
NAMESPACE="ioschools"

echo "=============================================="
echo "  ioschools - AWS EKS Deployment Script"
echo "=============================================="
echo ""

# ---- Prompt for AWS / EKS configuration ----
read -p "Enter AWS Region [us-east-1]: " AWS_REGION
AWS_REGION="${AWS_REGION:-us-east-1}"

read -p "Enter EKS Cluster Name: " CLUSTER_NAME
if [ -z "$CLUSTER_NAME" ]; then
    echo "ERROR: EKS Cluster Name is required."
    exit 1
fi

read -p "Enter full Docker Image URI (e.g. 123456789.dkr.ecr.us-east-1.amazonaws.com/ioschools:latest): " IMAGE_URI
if [ -z "$IMAGE_URI" ]; then
    echo "ERROR: Docker Image URI is required."
    exit 1
fi

# ---- Prompt for application-specific environment variables ----
echo ""
echo "--- Application Environment Variables ---"
echo "(Press Enter to skip any variable)"
echo ""

read -p "Enter value for CONNECTION_STRING (SQL Server connection string): " CONNECTION_STRING
CONNECTION_STRING="${CONNECTION_STRING:-}"

read -p "Enter value for CACHE_SCHEDULER_URL (cache scheduler URL, e.g. /cache/scheduler): " CACHE_SCHEDULER_URL
CACHE_SCHEDULER_URL="${CACHE_SCHEDULER_URL:-/cache/scheduler}"

# ---- Configure kubectl for EKS ----
echo ""
echo "Configuring kubectl for EKS cluster: $CLUSTER_NAME in $AWS_REGION..."
aws eks update-kubeconfig --region "$AWS_REGION" --name "$CLUSTER_NAME"
if [ $? -ne 0 ]; then
    echo "ERROR: Failed to configure kubectl for EKS cluster."
    exit 1
fi

echo "Verifying cluster connectivity..."
kubectl cluster-info || { echo "ERROR: Cannot connect to EKS cluster."; exit 1; }

# ---- Update Kubernetes manifests with actual values ----
echo ""
echo "Updating Kubernetes manifests with deployment values..."

# Work on copies to avoid modifying originals
cp "$K8S_DIR/deployment.yaml" /tmp/ioschools-deployment.yaml
cp "$K8S_DIR/service.yaml" /tmp/ioschools-service.yaml
cp "$K8S_DIR/ingress.yaml" /tmp/ioschools-ingress.yaml
cp "$K8S_DIR/namespace.yaml" /tmp/ioschools-namespace.yaml

# Replace placeholders using pipe delimiter
sed -i 's|{{IMAGE_URI}}|'"$IMAGE_URI"'|g' /tmp/ioschools-deployment.yaml
sed -i 's|{{CONNECTION_STRING}}|'"$CONNECTION_STRING"'|g' /tmp/ioschools-deployment.yaml
sed -i 's|{{CACHE_SCHEDULER_URL}}|'"$CACHE_SCHEDULER_URL"'|g' /tmp/ioschools-deployment.yaml

echo "Manifests updated successfully."

# ---- Apply Kubernetes manifests in order ----
echo ""
echo "Applying Kubernetes manifests..."

echo "[1/4] Applying namespace..."
kubectl apply -f /tmp/ioschools-namespace.yaml

echo "[2/4] Applying deployment..."
kubectl apply -f /tmp/ioschools-deployment.yaml

echo "[3/4] Applying service..."
kubectl apply -f /tmp/ioschools-service.yaml

echo "[4/4] Applying ingress..."
kubectl apply -f /tmp/ioschools-ingress.yaml

# ---- Wait for rollout ----
echo ""
echo "Waiting for deployment rollout to complete..."
kubectl rollout status deployment/$APP_NAME -n $NAMESPACE --timeout=300s
if [ $? -ne 0 ]; then
    echo "ERROR: Deployment rollout failed or timed out."
    echo "To rollback, run: kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE"
    exit 1
fi

# ---- Verify resources ----
echo ""
echo "Verifying deployed resources..."
kubectl get pods,svc,ingress -n $NAMESPACE

# ---- Display application URL ----
echo ""
echo "Fetching application ingress URL..."
INGRESS_HOST=$(kubectl get ingress ioschools-ingress -n $NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || echo "pending")
if [ "$INGRESS_HOST" != "pending" ] && [ -n "$INGRESS_HOST" ]; then
    echo "Application URL: http://$INGRESS_HOST"
else
    echo "Ingress hostname is still provisioning. Run the following to check:"
    echo "  kubectl get ingress ioschools-ingress -n $NAMESPACE"
fi

# ---- Cleanup temp files ----
rm -f /tmp/ioschools-deployment.yaml /tmp/ioschools-service.yaml /tmp/ioschools-ingress.yaml /tmp/ioschools-namespace.yaml

echo ""
echo "=============================================="
echo "  SUCCESS: ioschools deployed to EKS!"
echo "  Namespace: $NAMESPACE"
echo "  Image: $IMAGE_URI"
echo "=============================================="
echo ""
echo "Useful commands:"
echo "  kubectl get pods -n $NAMESPACE"
echo "  kubectl logs -f deployment/$APP_NAME -n $NAMESPACE"
echo "  kubectl rollout undo deployment/$APP_NAME -n $NAMESPACE  # rollback"
