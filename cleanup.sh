#!/bin/bash

# Cleanup Script: Remove all archived files from ModelBuilder and ModelTrainer
# This script cleans up the archive directories to free up space

set -euo pipefail
trap 'echo "Error on line $LINENO"; exit 1' ERR

#Paths
ROOT="$(cd "$(dirname "$0")" && pwd)"
MODEL_BUILDER_ARCHIVE="$ROOT/projects/ModelBuilder/Archive"
MODEL_TRAINER_ARCHIVE="$ROOT/projects/ModelTrainer/Archive"

echo "Cleaning up archives..."
echo "-----------------------------------"

# Clean ModelBuilder archive
if [ -d "$MODEL_BUILDER_ARCHIVE" ]; then
    echo "Removing ModelBuilder archive files..."
    FILE_COUNT=$(find "$MODEL_BUILDER_ARCHIVE" -type f | wc -l)
    if [ "$FILE_COUNT" -gt 0 ]; then
        rm -rf "$MODEL_BUILDER_ARCHIVE"/*
        echo "Removed $FILE_COUNT files from ModelBuilder archive."
    else
        echo "No files found in ModelBuilder archive."
    fi
else
    echo "ModelBuilder archive directory does not exist."
fi

# Clean ModelTrainer archive
if [ -d "$MODEL_TRAINER_ARCHIVE" ]; then
    echo "Removing ModelTrainer archive files..."
    FILE_COUNT=$(find "$MODEL_TRAINER_ARCHIVE" -type f | wc -l)
    if [ "$FILE_COUNT" -gt 0 ]; then
        rm -rf "$MODEL_TRAINER_ARCHIVE"/*
        echo "Removed $FILE_COUNT files from ModelTrainer archive."
    else
        echo "No files found in ModelTrainer archive."
    fi
else
    echo "ModelTrainer archive directory does not exist."
fi

echo "Cleanup completed successfully."