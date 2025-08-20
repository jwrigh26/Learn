#!/usr/bin/env bash

# Build and Train orchestrator
# - Archives previous outputs
# - Runs ModelBuilder then ModelTrainer build scripts (if present)

set -euo pipefail
trap 'echo "Error on line $LINENO"; exit 1' ERR

# Paths
ROOT="$(cd "$(dirname "$0")" && pwd)"
BUILDER="$ROOT/projects/ModelBuilder"
TRAINER="$ROOT/projects/ModelTrainer"
MODEL_BUILDER_ARCHIVE="$ROOT/projects/ModelBuilder/Archive"
MODEL_TRAINER_ARCHIVE="$ROOT/projects/ModelTrainer/Archive"

# Ensure archive directories exist
mkdir -p "$MODEL_BUILDER_ARCHIVE" "$MODEL_TRAINER_ARCHIVE"

# Archive any files from Builder Output (if present)
if [ -d "$BUILDER/Output" ]; then
    echo "Archiving ModelBuilder output files..."
    find "$BUILDER/Output" -type f \( -name "*.json" -o -name "*.zip" \) -exec mv {} "$MODEL_BUILDER_ARCHIVE/" \;
else
    echo "No ModelBuilder Output directory to archive."
fi

# Archive any files from Trainer Output (if present)
if [ -d "$TRAINER/Output" ]; then
    echo "Archiving ModelTrainer output files..."
    find "$TRAINER/Output" -type f \( -name "*.zip" -o -name "*.json" \) -exec mv {} "$MODEL_TRAINER_ARCHIVE/" \;
else
    echo "No ModelTrainer Output directory to archive."
fi

# Run ModelBuilder build script
cd "$BUILDER" || { echo "Cannot change directory to $BUILDER"; exit 1; }
if [ -x "./build.sh" ]; then
    echo "Running ModelBuilder build script..."
    ./build.sh
elif [ -f "./build.sh" ]; then
    echo "Running ModelBuilder build script with sh..."
    sh ./build.sh
else
    echo "Error: ModelBuilder build script not found."
    exit 1
fi

# Run ModelTrainer build script (if present)
cd "$TRAINER" || { echo "Cannot change directory to $TRAINER"; exit 1; }
if [ -x "./build.sh" ]; then
    echo "Running ModelTrainer build script..."
    ./build.sh
elif [ -f "./build.sh" ]; then
    echo "Running ModelTrainer build script with sh..."
    sh ./build.sh
else
    echo "No ModelTrainer build script found; skipping trainer build."
fi

echo "Build and archive steps completed successfully."
