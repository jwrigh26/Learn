Query Understanding & Natural Language Processing Design

Overview

This document explores advanced design patterns for improving our RAG system’s ability to understand and process natural language queries, particularly for complex data filtering operations.

Current System Weaknesses

Limited Parameter Extraction
Current: only extracts names using regex patterns
Missing: date filtering, role hierarchies, complex comparisons
Example Problem: “find me all employees with a hire date before 2024” -> no parameters extracted
LLM Over-Reliance for Data Operations
Problem: we give LLMs raw data and expect precise filtering
Reality: LLMS are poor at exact date comparisons, numerical operations, and consistent data processing
Better Approach: Use LLMs for language understanding, not data processing
No Semantic Understanding
Issue: Can’t amp synonyms or domain-specific terms
Examples:
“Hire date” vs “start date” vs “joined” -> all mean `OriginalHireDate`
“Managers” vs “supervisors” vs “team leads” -> role hierarchy
“Engineering” -> could be department, team or skill
---

Key Concepts & Acronyms

NLP (Natural Language Processing)
Definition: Computer science field focused on helping computers understand, interpret, and generate human language.

Key Components:
Tokenization: Breaking text into words/phrases
Part-of-speech Tagging: Identifying nouns, verbs, adjectives
Named Entity Recognition (NER): Finding people, places, dates, organizations
Intent Classification: Understanding what the user wants to do
Sentiment Analysis: Determining emotional tone

NLU (Natural Language Understanding)
Definition: Subset of NLP focused on machine comprehension of human language meaning.

For Our Use Case:
Understanding “before 2024” means “< DateTime(2024, 1, 1)”
Recognizing “managers” include various manager titles
Mapping “engineering team” to specific department filters

Intent Classification
Definition: Categorizing user requests into predefined action types.

Out Potential Intents:
FILTER_BY_HIRE_DATE
FIND_EMPLOYEE_BY_ROLE
GET_CONTACT_INFO
SEARCH_BY_DEPARTMENT
FIND_BY_LOCATION
GET_EMPLOYEE_SUMMARY

Slot Filling
Definition: Extracting specific parameter values from user input.

Example:
Input: “employees hired before 2024 in engineering”
Slots: {
	date: “2024”,
	operator: “before”
	field: “hire_date”
	department: “engineering”
}

Entity Recognition
Definition: Identifying and classifying key information in text.

Our Entities:
PERSON: Employee names
DATE: Hire dates, time ranges
DEPARTMENT: Team/organization names
ROLE: Job titles, positions
LOCATION: Office locations, remote work

Design Patterns & Solutions

Intent Classification + Slot Filling Pattern
How It Works
Classify Intent: What type of query is this?
Extract Entities: Pull out specific values
Fill Slots: Map entities to query parameters
Build Query: Convert to structured filters
ExampleFlow
“Show me managers hired after 2020” ->
Intent: FILTER_BY_ROLE_AND_DATE
Entities: { role: “managers”, date: “2020”, operator: “after”} ->
Query: employees.where(e => e.Title.Contains(“Manager”) && e.OriginalHireDate > DateTime(2020,1,1))
Multi-Stage Processing Pipeline
Stage 1: Natural Language Understanding
Extract intents and entities
Normalize human language to structured data
Stage 2: Query Building
Convert entities to precise filters
Handle date calculations, role mappings
Stage 3: Data Filtering
Apply structured filters to employee data 
Get small relevant dataset
Stage 4: LLM Formatting
Use LLM only for presenting results
No data logic, just formatting
Hybrid Structured/Unstructured Approach
Structured Queries: Handle with code
Date comparisons
Numerical operations
Exact field matching
Boolean logic
Unstructured Queries Handle with LLM
Fuzzy name matching
Complex descriptions
Contextual understanding
Result summarization
Query Understanding Service Architecture ( flow a -> g )
User Query
Query Understanding Service
Intent Classifier
Entity Extractor
Slot Filler
Query Builder
Structured Query Parameters
Data Filtering Service
Filtered Results
LLM Formatter ( ResponseProcessor )
Clean User Response

These libraries are in my semantic-kernel-app.csproj right now.
I don't want to use Cloud services right now. I’m cheap So I’ve installed these Libraries I’m exploring:
FuzzySharp
Microsoft.ML
Microsoft.Recognizers.Text
Microsoft.Recognizers.Text.DateTime
Microsoft.Recognizers.Text.Number
Microsoft.SemanticKernel
Microsoft.SemanticKernel.Plugins.Core
ResetSharp
System.Text.Json (ofcourse)

Some more Notes about Libraries:
.Net NLP Libraries
ML.NET (Microsoft’s Machine Learning Framework)
Props: Native .Net, good documentation, Microsoft support ( don’t plan on using their support hotlines for help LOL)
UseCases: Custom intent classification, entity recognition
Best For: Building domain-specific models
Learning Curve: medium??
Stanford.NLP.Net
Pros: Mature, comprehensive NLP toolkit
Use Cases: POS tagging, NER, parsing
Best For: General NLP tasks
Learning Curve: High (yikes I’m bad at math)
spaCy.NEt or Micorsoft.ML maybe?
Pros: Excellent entity recognition, fast
Use Cases: NER, tokenization, linguistic analysis
Best For: Entity extraction from text
Learning Curve: Medium
Microsoft.Recognizers.Text
Pros: Built for recognizing dates, numbers, phone numbers
Use Cases: Perfect for our date/time parsing needs
Best For: Structured data extraction
Learning Curve: Low
Recommendation: Highly recommended for our use case

Recommended Implementation Strategy
Phase 1: Quick Wins with Microsoft.Recognizers.Text

This library can parse
Before 2024 -> DateTime operation
Last year -> Calculated date range
Between 2020 and 2023 -> date range

Phase 2: Custom Intent Classification
Build simple rule-based classification
If ( query.Contains(“email”)) -> Intent.GET_CONTACT_INFO
If (query.Contains(“hire”, “before/after”)) -> Intent.FILTER_BY_HIRE_DATE
If (query.Contains(“manager”, “director”)) -> Intent.FILTER_BY_ROLE

Phase3: Advanced Entity Recognition
Use ML.NET or spaCy.Net( tried installing spaCy.Net but it caused issues with my project )
Department name variations
Role hierarchy mapping
Location normalization

Phase 4: Machine Learning Enhancement
Train custom models on your domain data:
Employee Query patters
Department/role vocabularies
Common user language patterns

Am I Reinventing the wheel?
Yes, If you build everything from scratch
Custom tokenization -> use existing libraries
Date parsing -> use microsoft.recognizers.text
Basic NER -> Use spaCy.NET or ML.Net
No, For Domain-Specific Logic
Employee role hierarchies -> Your business logic
Department mappings -> Your organizational structure
Query Patterns -> Your user behavior

The smart approach: hybrid
Use libraries for general NLP tasks (dates, entities)
Build custom logic for your domain (roles, departments)
Leverage existing patterns (intent + slot filling)

Implementation Priorities
Microsoft.Recognizers.Text: for date parsing
Simple intent classification with rules
Domain-specific entity mapping: (roles, departments)
Medium Priority (Enhances Experience)
ML.NET custom models for better classification
Fuzzy matching for names and terms
Query result caching for performance
Low Priority (Advanced Features)
Multi-turn conversations
Context awareness across queries
Learning from user feedback

Key Takeaways
Don’t reinvent NLP basics -> use proven libraries
Do customize for your domain -> your business logic is unique
Start simple: Rule-based classification can go far
Separate concerns: Language understanding != Data processing
Use LLMs for what they’re good at -> Language, not logic

The goal is building a Query Understanding Service that converts natural language into structured database operations. Letting the LLM handle only presentation and formatting




