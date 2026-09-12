@echo off
setlocal enabledelayedexpansion

REM =============================================================================
REM deploy-image.bat - Deploy ioschools to AWS EKS (Windows)
REM Prerequisites: aws-cli, kubectl
REM =============================================================================

set APP_NAME=ioschools
set NAMESPACE=ioschools
set SCRIPT_DIR=%~dp0
set REPO_ROOT=%SCRIPT_DIR%..
set K8S_DIR=%REPO_ROOT%kubernetes

echo ==============================================
echo   ioschools - AWS EKS Deployment Script
echo ==============================================
echo.

REM ---- Prompt for AWS / EKS configuration ----
set /p AWS_REGION="Enter AWS Region [us-east-1]: "
if "!AWS_REGION!"=="" set AWS_REGION=us-east-1

set /p CLUSTER_NAME="Enter EKS Cluster Name: "
if "!CLUSTER_NAME!"=="" (
    echo ERROR: EKS Cluster Name is required.
    exit /b 1
)

set /p IMAGE_URI="Enter full Docker Image URI (e.g. 123456789.dkr.ecr.us-east-1.amazonaws.com/ioschools:latest): "
if "!IMAGE_URI!"=="" (
    echo ERROR: Docker Image URI is required.
    exit /b 1
)

REM ---- Prompt for application-specific environment variables ----
echo.
echo --- Application Environment Variables ---
echo (Press Enter to skip any variable)
echo.

set /p CONNECTION_STRING="Enter value for CONNECTION_STRING (SQL Server connection string): "
if "!CONNECTION_STRING!"=="" set CONNECTION_STRING=

set /p CACHE_SCHEDULER_URL="Enter value for CACHE_SCHEDULER_URL [/cache/scheduler]: "
if "!CACHE_SCHEDULER_URL!"=="" set CACHE_SCHEDULER_URL=/cache/scheduler

REM ---- Configure kubectl for EKS ----
echo.
echo Configuring kubectl for EKS cluster: !CLUSTER_NAME! in !AWS_REGION!...
aws eks update-kubeconfig --region !AWS_REGION! --name !CLUSTER_NAME!
if !ERRORLEVEL! neq 0 (
    echo ERROR: Failed to configure kubectl for EKS cluster.
    exit /b 1
)

echo Verifying cluster connectivity...
kubectl cluster-info
if !ERRORLEVEL! neq 0 (
    echo ERROR: Cannot connect to EKS cluster.
    exit /b 1
)

REM ---- Copy manifests to temp and replace placeholders ----
echo.
echo Updating Kubernetes manifests with deployment values...

copy "!K8S_DIR!\deployment.yaml" "%TEMP%\ioschools-deployment.yaml" >nul
copy "!K8S_DIR!\service.yaml" "%TEMP%\ioschools-service.yaml" >nul
copy "!K8S_DIR!\ingress.yaml" "%TEMP%\ioschools-ingress.yaml" >nul
copy "!K8S_DIR!\namespace.yaml" "%TEMP%\ioschools-namespace.yaml" >nul

REM Replace placeholders using PowerShell
powershell -Command "(Get-Content '%TEMP%\ioschools-deployment.yaml') -replace '{{IMAGE_URI}}','!IMAGE_URI!' -replace '{{CONNECTION_STRING}}','!CONNECTION_STRING!' -replace '{{CACHE_SCHEDULER_URL}}','!CACHE_SCHEDULER_URL!' | Set-Content '%TEMP%\ioschools-deployment.yaml'"
if !ERRORLEVEL! neq 0 (
    echo ERROR: Failed to update deployment manifest.
    exit /b 1
)
echo Manifests updated successfully.

REM ---- Apply Kubernetes manifests in order ----
echo.
echo Applying Kubernetes manifests...

echo [1/4] Applying namespace...
kubectl apply -f "%TEMP%\ioschools-namespace.yaml"
if !ERRORLEVEL! neq 0 (
    echo ERROR: Failed to apply namespace.
    exit /b 1
)

echo [2/4] Applying deployment...
kubectl apply -f "%TEMP%\ioschools-deployment.yaml"
if !ERRORLEVEL! neq 0 (
    echo ERROR: Failed to apply deployment.
    exit /b 1
)

echo [3/4] Applying service...
kubectl apply -f "%TEMP%\ioschools-service.yaml"
if !ERRORLEVEL! neq 0 (
    echo ERROR: Failed to apply service.
    exit /b 1
)

echo [4/4] Applying ingress...
kubectl apply -f "%TEMP%\ioschools-ingress.yaml"
if !ERRORLEVEL! neq 0 (
    echo ERROR: Failed to apply ingress.
    exit /b 1
)

REM ---- Wait for rollout ----
echo.
echo Waiting for deployment rollout to complete...
kubectl rollout status deployment/!APP_NAME! -n !NAMESPACE! --timeout=300s
if !ERRORLEVEL! neq 0 (
    echo ERROR: Deployment rollout failed or timed out.
    echo To rollback, run: kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!
    exit /b 1
)

REM ---- Verify resources ----
echo.
echo Verifying deployed resources...
kubectl get pods,svc,ingress -n !NAMESPACE!

REM ---- Cleanup temp files ----
del /q "%TEMP%\ioschools-deployment.yaml" 2>nul
del /q "%TEMP%\ioschools-service.yaml" 2>nul
del /q "%TEMP%\ioschools-ingress.yaml" 2>nul
del /q "%TEMP%\ioschools-namespace.yaml" 2>nul

echo.
echo ==============================================
echo   SUCCESS: ioschools deployed to EKS!
echo   Namespace: !NAMESPACE!
echo   Image: !IMAGE_URI!
echo ==============================================
echo.
echo Useful commands:
echo   kubectl get pods -n !NAMESPACE!
echo   kubectl logs -f deployment/!APP_NAME! -n !NAMESPACE!
echo   kubectl rollout undo deployment/!APP_NAME! -n !NAMESPACE!

endlocal
