#!/bin/bash

# ModelTrainer Quick Build Script
# This script builds and runs the ModelTrainer tool to generate training datasets.


echo "Building ModelTrainer..."
echo "-----------------------------------"

# Change to ModelTrainer directory
cd "$(dirname "$0")" || exit 1

# Check if we're in the right directory
if [ ! -f "ModelTrainer.csproj" ]; then
    echo "Error: ModelTrainer.csproj not found in the current directory."
    echo "Make sure you're running this script from the ModelTrainer directory."
    exit 1
fi

# Check for training data
if [ ! -d "Data" ] || [ -z "$(find Data -name 'intent_dataset_v*.json' 2>/dev/null)" ]; then
    echo "Warning: No training datset found in Data/ directory"
    echo "To get traning data, run:"
    echo "cd ../ModelBuilder && ./build.sh"
    echo "cp Output/intent_dataset_v*.json ../ModelTrainer/Data/"
    echo ""
    read -p "Continue anyway? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Cancelled."
        exit 1
    fi
fi

# Build the project
echo "Building ModelTrainer..."
dotnet build

if [ $? -ne 0 ]; then
    echo "Build failed. Please check the errors above."
    exit 1
fi

# Run the tool
echo ""
echo "Running ModelTrainer"
dotnet run

if [ $? -eq 0]; then
    echo ""
    echo "Success! Model and metrics generated in Output/ directory"
    echo ""
    echo "Quick commands to veiw results:"
    echo "View model files: ls -ls Output/"
    echo "View metrics:     cat/ Output/model_metrics_v*.json | jq ."
    echo "Test predictions: # Use the generated .zip model file"
else
    echo "ModelTrianer failed. Please check the errors above."
    exit 1
fi