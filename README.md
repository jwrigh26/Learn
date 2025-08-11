# Learning Repository Structure

## Overview
This repository is organized to separate learning/exploration (notebooks) from production code (projects).

## Structure

### `/notebooks/`
Interactive Jupyter notebooks for learning, experimentation, and prototyping.

- **`/notebooks/NLP/`** - Natural Language Processing notebooks
  - `ml_multiclass_model.ipynb` - ML.NET intent classification training
  - `ml_multiclass_model_demo.ipynb` - Detailed ML.NET learning notebook
  - `ml_multiclass_slots.ipynb` - Intent + slot extraction implementation
  
- **`/notebooks/CSharp/`** - C# learning notebooks
  - `hello_csharp.ipynb` - Basic C# in Jupyter

### `/projects/`
Production-ready applications and services.

- **`/projects/QueryUnderstanding/`** - Console application implementing the ML.NET intent classification and slot extraction logic

### `/NLP/`
Preserved NLP resources and data.

- **`/NLP/Data/`** - Training data and datasets
  - `intent_seed_v6.json` - Intent classification training data
  - `intent_model.zip` - Trained ML.NET model

### `/archive/`
Archived files that may be referenced but aren't actively used.

- **`/archive/nlp_rag_design/`** - Design documents and planning files
  - `llm_poc_current.md` - Current implementation flow
  - `llm_poc_plan.md` - NLP strategy and planning

## Getting Started

1. **For Learning/Experimentation**: Start with notebooks in `/notebooks/`
2. **For Building Applications**: Work in `/projects/`
3. **For Data/Models**: Use resources in `/NLP/`

## Next Steps

1. Enhance the console application in `/projects/QueryUnderstanding/`
2. Add API endpoints for testing with Postman
3. Implement the trained ML.NET model in the console project

## Watch

dotnet watch run --project /Users/maneki-neko/learning/projects/MLIntentClassifierAPI/MLIntentClassifierAPI.csproj