#!/bin/bash
#
# Docker run script for SharpAI Server
# Runs the Docker container with CPU or GPU support
#

set -e

MODE="${1:-auto}"
IMAGE_NAME="${2:-sharpai:latest}"
PORT="${3:-8000}"

show_usage() {
    echo "Usage: $0 [cpu|gpu|auto] [image:tag] [port]"
    echo ""
    echo "Arguments:"
    echo "  mode       - Backend mode: cpu, gpu, or auto (default: auto)"
    echo "  image:tag  - Docker image name and tag (default: sharpai:latest)"
    echo "  port       - Host port to bind (default: 8000)"
    echo ""
    echo "Examples:"
    echo "  $0 cpu                    # Run in CPU mode"
    echo "  $0 gpu                    # Run in GPU mode (requires NVIDIA Docker)"
    echo "  $0 auto sharpai:v1.0 9000 # Auto-detect, custom image and port"
    echo ""
}

if [ "$MODE" == "--help" ] || [ "$MODE" == "-h" ]; then
    show_usage
    exit 0
fi

echo "=================================="
echo "Running SharpAI Server"
echo "=================================="
echo "Image: ${IMAGE_NAME}"
echo "Port: ${PORT}"
echo "Mode: ${MODE}"
echo ""

# Build docker run command
DOCKER_CMD="docker run --rm -it -p ${PORT}:8000"

case "$MODE" in
    cpu)
        echo "Starting in CPU-only mode..."
        # Force CPU mode via environment variable
        DOCKER_CMD="${DOCKER_CMD} -e SHARPAI_FORCE_BACKEND=cpu"
        ;;
    gpu)
        echo "Starting in GPU mode..."
        echo "Note: Requires NVIDIA Docker runtime and GPU"
        DOCKER_CMD="${DOCKER_CMD} --gpus all"
        ;;
    auto)
        echo "Starting in auto-detect mode..."
        echo "Will automatically select CPU or GPU based on availability"
        # Try to use GPU if available, otherwise fall back to CPU
        if command -v nvidia-smi &> /dev/null; then
            echo "NVIDIA GPU detected on host, enabling GPU support..."
            DOCKER_CMD="${DOCKER_CMD} --gpus all"
        else
            echo "No NVIDIA GPU detected, using CPU mode..."
        fi
        ;;
    *)
        echo "ERROR: Invalid mode '${MODE}'"
        echo ""
        show_usage
        exit 1
        ;;
esac

DOCKER_CMD="${DOCKER_CMD} ${IMAGE_NAME}"

echo "Running: ${DOCKER_CMD}"
echo ""
echo "Press Ctrl+C to stop the server"
echo ""

# Execute the docker command
eval ${DOCKER_CMD}
