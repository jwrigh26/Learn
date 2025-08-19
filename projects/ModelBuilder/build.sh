#!/bin/bash

# ModelBuilder Quick Build Script
# This script builds and runs the ModelBuilder tool to generate training datasets.


echo "Building ModelBuilder..."
echo "-----------------------------------"

# Change to ModelBuilder directory
cd "$(dirname "$0")" || exit 1

# Check if we're in the right directory
if [ ! -f "ModelBuilder.csproj" ]; then
    echo "Error: ModelBuilder.csproj not found in the current directory."
    echo "Make sure you're running this script from the ModelBuilder directory."
    exit 1
fi

# Build the project
echo "Building ModelBuilder..."
dotnet build

if [ $? -ne 0 ]; then
    echo "Build failed. Please check the errors above."
    exit 1
fi

# Run the ModelBuilder tool
echo ""
echo "Running ModelBuilder..."
dotnet run

if [ $? -eq 0]; then
    echo ""
    echo "ModelBuilder completed successfully."
    echo ""
    echo "Quick commands to view results:"
    echo " - View dataset: head -20 Output/intent_dataset_v*.json"
    echo " - View metadata: head -20 Output/metadata_v*.json"
    echo " - List outputs: ls -la Output/"
else
    echo "ModelBuilder encountered an error. Please check the output above."
    exit 1
fi