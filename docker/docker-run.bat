@echo off
REM
REM Docker run script for SharpAI Server (Windows)
REM Runs the Docker container with CPU or GPU support
REM

setlocal enabledelayedexpansion

set MODE=%1
set IMAGE_NAME=%2
set PORT=%3

if "%MODE%"=="" set MODE=auto
if "%IMAGE_NAME%"=="" set IMAGE_NAME=sharpai:latest
if "%PORT%"=="" set PORT=8000

if "%MODE%"=="--help" goto :show_usage
if "%MODE%"=="-h" goto :show_usage
if "%MODE%"=="/?" goto :show_usage

echo ==================================
echo Running SharpAI Server
echo ==================================
echo Image: %IMAGE_NAME%
echo Port: %PORT%
echo Mode: %MODE%
echo.

REM Build docker run command with volume mounts
set DOCKER_CMD=docker run --rm -it -p %PORT%:8000 -v "%~dp0sharpai.json:/app/sharpai.json" -v "%~dp0sharpai.db:/app/sharpai.db" -v "%~dp0logs:/app/logs" -v "%~dp0models:/app/models"

if /i "%MODE%"=="cpu" (
    echo Starting in CPU-only mode...
    set DOCKER_CMD=!DOCKER_CMD! -e SHARPAI_FORCE_BACKEND=cpu
) else if /i "%MODE%"=="gpu" (
    echo Starting in GPU mode...
    echo Note: Requires NVIDIA Docker runtime and GPU
    set DOCKER_CMD=!DOCKER_CMD! --gpus all
) else if /i "%MODE%"=="auto" (
    echo Starting in auto-detect mode...
    echo Will automatically select CPU or GPU based on availability
    REM Try to detect NVIDIA GPU
    nvidia-smi >nul 2>&1
    if !errorlevel! equ 0 (
        echo NVIDIA GPU detected on host, enabling GPU support...
        set DOCKER_CMD=!DOCKER_CMD! --gpus all
    ) else (
        echo No NVIDIA GPU detected, using CPU mode...
    )
) else (
    echo ERROR: Invalid mode '%MODE%'
    echo.
    goto :show_usage
)

set DOCKER_CMD=!DOCKER_CMD! %IMAGE_NAME%

echo Running: !DOCKER_CMD!
echo.
echo Press Ctrl+C to stop the server
echo.

REM Execute the docker command
!DOCKER_CMD!

goto :eof

:show_usage
echo Usage: %~nx0 [cpu^|gpu^|auto] [image:tag] [port]
echo.
echo Arguments:
echo   mode       - Backend mode: cpu, gpu, or auto (default: auto)
echo   image:tag  - Docker image name and tag (default: sharpai:latest)
echo   port       - Host port to bind (default: 8000)
echo.
echo Examples:
echo   %~nx0 cpu                    # Run in CPU mode
echo   %~nx0 gpu                    # Run in GPU mode (requires NVIDIA Docker)
echo   %~nx0 auto sharpai:v1.0 9000 # Auto-detect, custom image and port
echo.
exit /b 0

endlocal
