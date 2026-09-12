#!/bin/bash
# =============================================================================
# build-push.sh - Build and Push Docker Image for ioschools
# Target: AWS ECR or Docker Hub
# =============================================================================
set -e
set -o pipefail

PROJECT_NAME="ioschools"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "=============================================="
echo "  ioschools - Docker Build & Push Script"
echo "=============================================="
echo ""

# Sanitize image name: lowercase, replace non-alphanumeric with hyphens, trim hyphens
IMAGE_NAME=$(echo "$PROJECT_NAME" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9' '-' | sed 's/^-*//;s/-*$//')

# Prompt for image tag
read -p "Enter image tag [latest]: " IMAGE_TAG
IMAGE_TAG="${IMAGE_TAG:-latest}"
# Sanitize tag
IMAGE_TAG=$(echo "$IMAGE_TAG" | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9._-' '-' | sed 's/^-*//;s/-*$//')
IMAGE_TAG="${IMAGE_TAG:-latest}"

echo ""
echo "Select container registry:"
echo "  1. AWS ECR (Elastic Container Registry)"
echo "  2. Docker Hub"
echo ""
read -p "Enter choice [1 or 2]: " REGISTRY_CHOICE

if [ "$REGISTRY_CHOICE" = "1" ]; then
    # ---- AWS ECR ----
    echo ""
    echo "--- AWS ECR Configuration ---"
    read -p "Enter AWS Region [us-east-1]: " AWS_REGION
    AWS_REGION="${AWS_REGION:-us-east-1}"
    read -p "Enter AWS Account ID: " AWS_ACCOUNT_ID
    if [ -z "$AWS_ACCOUNT_ID" ]; then
        echo "ERROR: AWS Account ID is required."
        exit 1
    fi
    read -p "Enter ECR Repository name [$IMAGE_NAME]: " ECR_REPO
    ECR_REPO="${ECR_REPO:-$IMAGE_NAME}"

    REGISTRY_URL="${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com"
    FULL_IMAGE_NAME="${REGISTRY_URL}/${ECR_REPO}:${IMAGE_TAG}"

    echo ""
    echo "Logging in to AWS ECR..."
    aws ecr get-login-password --region "$AWS_REGION" | docker login --username AWS --password-stdin "$REGISTRY_URL"
    if [ $? -ne 0 ]; then
        echo "ERROR: ECR login failed."
        exit 1
    fi

    # Auto-create ECR repository if it does not exist
    echo "Checking ECR repository..."
    aws ecr describe-repositories --repository-names "$ECR_REPO" --region "$AWS_REGION" >/dev/null 2>&1 || \
        aws ecr create-repository --repository-name "$ECR_REPO" --region "$AWS_REGION"
    echo "ECR repository ready: $ECR_REPO"

elif [ "$REGISTRY_CHOICE" = "2" ]; then
    # ---- Docker Hub ----
    echo ""
    echo "--- Docker Hub Configuration ---"
    read -p "Enter Docker Hub username: " DOCKER_USERNAME
    if [ -z "$DOCKER_USERNAME" ]; then
        echo "ERROR: Docker Hub username is required."
        exit 1
    fi
    read -s -p "Enter Docker Hub password or access token: " DOCKER_PASSWORD
    echo ""
    if [ -z "$DOCKER_PASSWORD" ]; then
        echo "ERROR: Docker Hub password is required."
        exit 1
    fi
    read -p "Enter Docker Hub repository name [$IMAGE_NAME]: " DH_REPO
    DH_REPO="${DH_REPO:-$IMAGE_NAME}"

    FULL_IMAGE_NAME="${DOCKER_USERNAME}/${DH_REPO}:${IMAGE_TAG}"

    echo ""
    echo "Logging in to Docker Hub..."
    echo "$DOCKER_PASSWORD" | docker login --username "$DOCKER_USERNAME" --password-stdin
    if [ $? -ne 0 ]; then
        echo "ERROR: Docker Hub login failed."
        exit 1
    fi

else
    echo "ERROR: Invalid choice. Please enter 1 or 2."
    exit 1
fi

echo ""
echo "Building Docker image..."
echo "  Image: $FULL_IMAGE_NAME"
echo "  Context: $REPO_ROOT"
echo "  Dockerfile: ioschools/Dockerfile"
echo ""

docker build -f "ioschools/Dockerfile" -t "$FULL_IMAGE_NAME" "$REPO_ROOT"
if [ $? -ne 0 ]; then
    echo "ERROR: Docker build failed."
    exit 1
fi
echo "Docker build succeeded."

echo ""
echo "Pushing image to registry..."
docker push "$FULL_IMAGE_NAME"
if [ $? -ne 0 ]; then
    echo "ERROR: Docker push failed."
    exit 1
fi

echo ""
echo "=============================================="
echo "  SUCCESS: Image pushed successfully!"
echo "  Image URI: $FULL_IMAGE_NAME"
echo "=============================================="
