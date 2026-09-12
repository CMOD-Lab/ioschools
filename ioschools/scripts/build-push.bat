@echo off
setlocal enabledelayedexpansion

REM =============================================================================
REM build-push.bat - Build and Push Docker Image for ioschools (Windows)
REM Target: AWS ECR or Docker Hub
REM =============================================================================

set PROJECT_NAME=ioschools
set SCRIPT_DIR=%~dp0
set REPO_ROOT=%SCRIPT_DIR%..

echo ==============================================
echo   ioschools - Docker Build ^& Push Script
echo ==============================================
echo.

REM Sanitize image name to lowercase using PowerShell
for /f "delims=" %%i in ('powershell -Command "\"ioschools\" -replace '[^a-z0-9]','-' -replace '^-+','' -replace '-+$',''"') do set IMAGE_NAME=%%i
if "!IMAGE_NAME!"=="" set IMAGE_NAME=ioschools

REM Prompt for image tag
set /p IMAGE_TAG="Enter image tag [latest]: "
if "!IMAGE_TAG!"=="" set IMAGE_TAG=latest
for /f "delims=" %%i in ('powershell -Command "\"!IMAGE_TAG!\" -replace '[^a-z0-9._-]','-' -replace '^-+','' -replace '-+$',''"') do set IMAGE_TAG=%%i
if "!IMAGE_TAG!"=="" set IMAGE_TAG=latest

echo.
echo Select container registry:
echo   1. AWS ECR (Elastic Container Registry)
echo   2. Docker Hub
echo.
set /p REGISTRY_CHOICE="Enter choice [1 or 2]: "

if "!REGISTRY_CHOICE!"=="1" goto ECR_SETUP
if "!REGISTRY_CHOICE!"=="2" goto DOCKERHUB_SETUP
echo ERROR: Invalid choice. Please enter 1 or 2.
exit /b 1

:ECR_SETUP
echo.
echo --- AWS ECR Configuration ---
set /p AWS_REGION="Enter AWS Region [us-east-1]: "
if "!AWS_REGION!"=="" set AWS_REGION=us-east-1
set /p AWS_ACCOUNT_ID="Enter AWS Account ID: "
if "!AWS_ACCOUNT_ID!"=="" (
    echo ERROR: AWS Account ID is required.
    exit /b 1
)
set /p ECR_REPO="Enter ECR Repository name [!IMAGE_NAME!]: "
if "!ECR_REPO!"=="" set ECR_REPO=!IMAGE_NAME!

set REGISTRY_URL=!AWS_ACCOUNT_ID!.dkr.ecr.!AWS_REGION!.amazonaws.com
set FULL_IMAGE_NAME=!REGISTRY_URL!/!ECR_REPO!:!IMAGE_TAG!

echo.
echo Logging in to AWS ECR...
aws ecr get-login-password --region !AWS_REGION! | docker login --username AWS --password-stdin !REGISTRY_URL!
if !ERRORLEVEL! neq 0 (
    echo ERROR: ECR login failed.
    exit /b 1
)

echo Checking ECR repository...
aws ecr describe-repositories --repository-names !ECR_REPO! --region !AWS_REGION! >nul 2>&1
if !ERRORLEVEL! neq 0 (
    echo Creating ECR repository...
    aws ecr create-repository --repository-name !ECR_REPO! --region !AWS_REGION!
    if !ERRORLEVEL! neq 0 (
        echo ERROR: Failed to create ECR repository.
        exit /b 1
    )
)
echo ECR repository ready: !ECR_REPO!
goto BUILD_IMAGE

:DOCKERHUB_SETUP
echo.
echo --- Docker Hub Configuration ---
set /p DOCKER_USERNAME="Enter Docker Hub username: "
if "!DOCKER_USERNAME!"=="" (
    echo ERROR: Docker Hub username is required.
    exit /b 1
)
set /p DOCKER_PASSWORD="Enter Docker Hub password or access token: "
if "!DOCKER_PASSWORD!"=="" (
    echo ERROR: Docker Hub password is required.
    exit /b 1
)
set /p DH_REPO="Enter Docker Hub repository name [!IMAGE_NAME!]: "
if "!DH_REPO!"=="" set DH_REPO=!IMAGE_NAME!

set FULL_IMAGE_NAME=!DOCKER_USERNAME!/!DH_REPO!:!IMAGE_TAG!

echo.
echo Logging in to Docker Hub...
echo !DOCKER_PASSWORD! | docker login --username !DOCKER_USERNAME! --password-stdin
if !ERRORLEVEL! neq 0 (
    echo ERROR: Docker Hub login failed.
    exit /b 1
)
goto BUILD_IMAGE

:BUILD_IMAGE
echo.
echo Building Docker image...
echo   Image: !FULL_IMAGE_NAME!
echo   Context: !REPO_ROOT!
echo   Dockerfile: ioschools\Dockerfile
echo.

docker build -f "ioschools\Dockerfile" -t "!FULL_IMAGE_NAME!" "!REPO_ROOT!"
if !ERRORLEVEL! neq 0 (
    echo ERROR: Docker build failed.
    exit /b 1
)
echo Docker build succeeded.

echo.
echo Pushing image to registry...
docker push "!FULL_IMAGE_NAME!"
if !ERRORLEVEL! neq 0 (
    echo ERROR: Docker push failed.
    exit /b 1
)

echo.
echo ==============================================
echo   SUCCESS: Image pushed successfully!
echo   Image URI: !FULL_IMAGE_NAME!
echo ==============================================

endlocal
