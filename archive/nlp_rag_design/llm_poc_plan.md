# Query Understanding & Natural Language Processing Design

## Table of Contents
- [1. Overview](#1-overview)
- [2. Current System Weaknesses](#2-current-system-weaknesses)
- [3. Key Concepts & Acronyms](#3-key-concepts--acronyms)
  - [3.1 NLP (Natural Language Processing)](#31-nlp-natural-language-processing)
  - [3.2 NLU (Natural Language Understanding)](#32-nlu-natural-language-understanding)
  - [3.3 Intent Classification](#33-intent-classification)
  - [3.4 Slot Filling](#34-slot-filling)
  - [3.5 Entity Recognition](#35-entity-recognition)
- [4. Design Patterns & Solutions](#4-design-patterns--solutions)
  - [4.1 Intent Classification + Slot Filling Pattern](#41-intent-classification--slot-filling-pattern)
  - [4.2 Multi-Stage Processing Pipeline](#42-multi-stage-processing-pipeline)
  - [4.3 Hybrid Structured/Unstructured Approach](#43-hybrid-structuredunstructured-approach)
  - [4.4 Query Understanding Service Architecture (Flow A -> G)](#44-query-understanding-service-architecture-flow-a---g)
- [5. Libraries Under Exploration (No Cloud Services)](#5-libraries-under-exploration-no-cloud-services)
  - [5.1 Notes on .NET NLP Libraries](#51-notes-on-net-nlp-libraries)
- [6. Recommended Implementation Strategy](#6-recommended-implementation-strategy)
  - [6.1 Phase 1: Quick Wins with Microsoft.Recognizers.Text](#61-phase-1-quick-wins-with-microsoftrecognizerstext)
  - [6.2 Phase 2: Custom Intent Classification](#62-phase-2-custom-intent-classification)
  - [6.3 Phase 3: Advanced Entity Recognition](#63-phase-3-advanced-entity-recognition)
  - [6.4 Phase 4: Machine Learning Enhancement](#64-phase-4-machine-learning-enhancement)
- [7. Are We Reinventing the Wheel?](#7-are-we-reinventing-the-wheel)
- [8. Implementation Priorities](#8-implementation-priorities)
- [9. Key Takeaways](#9-key-takeaways)

---

## 1. Overview
This document explores advanced design patterns for improving our RAG system’s ability to understand and process natural language queries, particularly for complex data filtering operations.

---

## 2. Current System Weaknesses
- Limited Parameter Extraction
  - Current: only extracts names using regex patterns
  - Missing: date filtering, role hierarchies, complex comparisons
  - Example: “find me all employees with a hire date before 2024” -> no parameters extracted
- LLM Over-Reliance for Data Operations
  - Problem: we give LLMs raw data and expect precise filtering
  - Reality: LLMs are poor at exact date comparisons, numerical operations, and consistent data processing
  - Better Approach: Use LLMs for language understanding, not data processing
- No Semantic Understanding
  - Issue: Can’t map synonyms or domain-specific terms
  - Examples:
    - “Hire date” vs “start date” vs “joined” -> all mean `OriginalHireDate`
    - “Managers” vs “supervisors” vs “team leads” -> role hierarchy
    - “Engineering” -> could be department, team, or skill

---

## 3. Key Concepts & Acronyms

### 3.1 NLP (Natural Language Processing)
- Definition: Field focused on helping computers understand, interpret, and generate human language.
- Key Components:
  - Tokenization: Breaking text into words/phrases
  - Part-of-Speech Tagging: Identifying nouns, verbs, adjectives
  - Named Entity Recognition (NER): Finding people, places, dates, organizations
  - Intent Classification: Understanding what the user wants to do
  - Sentiment Analysis: Determining emotional tone

### 3.2 NLU (Natural Language Understanding)
- Definition: Subset of NLP focused on machine comprehension of human language meaning.
- For Our Use Case:
  - Understanding “before 2024” means “< DateTime(2024, 1, 1)”
  - Recognizing “managers” include various manager titles
  - Mapping “engineering team” to specific department filters

### 3.3 Intent Classification
- Definition: Categorizing user requests into predefined action types.
- Our Potential Intents:
  - FILTER_BY_HIRE_DATE
  - FIND_EMPLOYEE_BY_ROLE
  - GET_CONTACT_INFO
  - SEARCH_BY_DEPARTMENT
  - FIND_BY_LOCATION
  - GET_EMPLOYEE_SUMMARY

### 3.4 Slot Filling
- Definition: Extracting specific parameter values from user input.
- Example:
  - Input: “employees hired before 2024 in engineering”
  - Slots:
    - date: “2024”
    - operator: “before”
    - field: “hire_date”
    - department: “engineering”

### 3.5 Entity Recognition
- Definition: Identifying and classifying key information in text.
- Our Entities:
  - PERSON: Employee names
  - DATE: Hire dates, time ranges
  - DEPARTMENT: Team/organization names
  - ROLE: Job titles, positions
  - LOCATION: Office locations, remote work

---

## 4. Design Patterns & Solutions

### 4.1 Intent Classification + Slot Filling Pattern
- How It Works:
  - Classify Intent: What type of query is this?
  - Extract Entities: Pull out specific values
  - Fill Slots: Map entities to query parameters
  - Build Query: Convert to structured filters
- Example Flow:
  - “Show me managers hired after 2020” ->
  - Intent: FILTER_BY_ROLE_AND_DATE
  - Entities: { role: “managers”, date: “2020”, operator: “after” }
  - Query: employees.Where(e => e.Title.Contains("Manager") && e.OriginalHireDate > DateTime(2020,1,1))

### 4.2 Multi-Stage Processing Pipeline
- Stage 1: Natural Language Understanding
  - Extract intents and entities
  - Normalize human language to structured data
- Stage 2: Query Building
  - Convert entities to precise filters
  - Handle date calculations, role mappings
- Stage 3: Data Filtering
  - Apply structured filters to employee data
  - Get small relevant dataset
- Stage 4: LLM Formatting
  - Use LLM only for presenting results
  - No data logic, just formatting

### 4.3 Hybrid Structured/Unstructured Approach
- Structured Queries: Handle with code
  - Date comparisons, numerical operations, exact field matching, boolean logic
- Unstructured Queries: Handle with LLM
  - Fuzzy name matching, complex descriptions, contextual understanding, result summarization

### 4.4 Query Understanding Service Architecture (Flow A -> G)
- User Query -> Query Understanding Service -> Intent Classifier -> Entity Extractor -> Slot Filler -> Query Builder -> Structured Query Parameters -> Data Filtering Service -> Filtered Results -> LLM Formatter (ResponseProcessor) -> Clean User Response

---

## 5. Libraries Under Exploration (No Cloud Services)
- FuzzySharp
- Microsoft.ML
- Microsoft.Recognizers.Text
- Microsoft.Recognizers.Text.DateTime
- Microsoft.Recognizers.Text.Number
- Microsoft.SemanticKernel
- Microsoft.SemanticKernel.Plugins.Core
- ResetSharp
- System.Text.Json

### 5.1 Notes on .NET NLP Libraries
- ML.NET (Microsoft’s Machine Learning Framework)
  - Pros: Native .NET, good documentation, Microsoft support
  - Use Cases: Custom intent classification, entity recognition
  - Best For: Building domain-specific models
  - Learning Curve: Medium
- Stanford.NLP.Net
  - Pros: Mature, comprehensive NLP toolkit
  - Use Cases: POS tagging, NER, parsing
  - Best For: General NLP tasks
  - Learning Curve: High
- spaCy.NET or Microsoft.ML
  - Pros: Excellent entity recognition, fast
  - Use Cases: NER, tokenization, linguistic analysis
  - Best For: Entity extraction from text
  - Learning Curve: Medium
- Microsoft.Recognizers.Text
  - Pros: Built for recognizing dates, numbers, phone numbers
  - Use Cases: Perfect for date/time parsing needs
  - Best For: Structured data extraction
  - Learning Curve: Low
  - Recommendation: Highly recommended for our use case

---

## 6. Recommended Implementation Strategy

### 6.1 Phase 1: Quick Wins with Microsoft.Recognizers.Text
- Can parse:
  - "Before 2024" -> DateTime operation
  - "Last year" -> Calculated date range
  - "Between 2020 and 2023" -> Date range

### 6.2 Phase 2: Custom Intent Classification
- Build simple rule-based classification:
  - If query contains "email" -> Intent.GET_CONTACT_INFO
  - If query contains "hire" and "before/after" -> Intent.FILTER_BY_HIRE_DATE
  - If query contains "manager" or "director" -> Intent.FILTER_BY_ROLE

### 6.3 Phase 3: Advanced Entity Recognition
- Use ML.NET or spaCy.NET (spaCy.NET previously caused issues)
- Focus areas:
  - Department name variations
  - Role hierarchy mapping
  - Location normalization

### 6.4 Phase 4: Machine Learning Enhancement
- Train custom models on domain data:
  - Employee query patterns
  - Department/role vocabularies
  - Common user language patterns

---

## 7. Are We Reinventing the Wheel?
- Yes, if building everything from scratch:
  - Custom tokenization -> use existing libraries
  - Date parsing -> use Microsoft.Recognizers.Text
  - Basic NER -> use spaCy.NET or ML.NET
- No, for domain-specific logic:
  - Employee role hierarchies -> your business logic
  - Department mappings -> your organizational structure
  - Query patterns -> your user behavior
- Smart Approach: Hybrid
  - Use libraries for general NLP tasks (dates, entities)
  - Build custom logic for your domain (roles, departments)
  - Leverage existing patterns (intent + slot filling)

---

## 8. Implementation Priorities
- High Priority
  - Microsoft.Recognizers.Text for date parsing
  - Simple intent classification with rules
  - Domain-specific entity mapping (roles, departments)
- Medium Priority
  - ML.NET custom models for better classification
  - Fuzzy matching for names and terms
  - Query result caching for performance
- Low Priority
  - Multi-turn conversations
  - Context awareness across queries
  - Learning from user feedback

---

## 9. Key Takeaways
- Don’t reinvent NLP basics -> use proven libraries
- Customize for your domain -> your business logic is unique
- Start simple: rule-based classification can go far
- Separate concerns: language understanding != data processing
- Use LLMs for what they’re good at -> language, not logic

> Goal: Build a Query Understanding Service that converts natural language into structured database operations, letting the LLM handle only presentation and formatting.




