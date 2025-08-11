# Complete Flow Breakdown

## Table of Contents
- [1. High-Level Flow](#1-high-level-flow)
- [2. Simple Question Analyzer](#2-simple-question-analyzer)
- [3. RAGService (Main Orchestrator)](#3-ragservice-main-orchestrator)
  - [3.1 Steps](#31-steps)
- [4. ResponseProcessor (The Cleanup Crew)](#4-responseprocessor-the-cleanup-crew)
  - [4.1 For Email Questions](#41-for-email-questions)
  - [4.2 For Other Questions](#42-for-other-questions)
- [5. Example Flow: "Rick, Summer, and Morty’s Email"](#5-example-flow-rick-summer-and-mortys-email)
  - [5.1 Simple Question Analyzer](#51-simple-question-analyzer)
  - [5.2 RAGService](#52-ragservice)
  - [5.3 ResponseProcessor](#53-responseprocessor)
- [6. Why ResponseProcessor Is Needed](#6-why-responseprocessor-is-needed)

---

## 1. High-Level Flow
1. User asks a question
   - Example: "What are Rick, Summer, and Morty’s emails?"
2. Simple Question Analyzer parses the question to extract search parameters
3. RAGService gathers relevant employee data and calls the LLM
4. ResponseProcessor cleans and formats the LLM output

---

## 2. Simple Question Analyzer
- Purpose: Parse the question to extract search parameters
- Input: Raw user question string
- What it does:
  - Uses regex patterns to find names (e.g., ["rick", "summer", "morty"]) 
  - Detects if it’s API-related (employee questions = yes)
  - Extracts other parameters (departments, locations, etc.)
- Output (example):
  - `{ "names": ["rick", "summer", "morty"] }`

---

## 3. RAGService (Main Orchestrator)

### 3.1 Steps
- Get employee data from APIService (all employees from cache/API)
- Filter employees using extracted names (e.g., Rick, Summer, Morty)
- Build context with only relevant employee data
- Create prompt with context + question + examples
- Call LLM (phi3:mini model) with the enhanced prompt
- Capture raw response from LLM

---

## 4. ResponseProcessor (The Cleanup Crew)
- Purpose: Clean up the LLM output to extract only what the user needs

### 4.1 For Email Questions
- Find the answer section: look for markers like "Your answer:" or "Answer:"
- Extract emails: use regex to find all email addresses after the delimiter
- Match emails to names: associate each email with the requested names
- Return clean format, e.g.:
  - Rick: rick@company.com
  - Summer: summer@company.com
  - Morty: morty@company.com

### 4.2 For Other Questions
- Remove filler text (e.g., "Based on the information provided…")
- Keep the first few substantial lines with facts
- Return a concise, clean answer

---

## 5. Example Flow: "Rick, Summer, and Morty’s Email"

### 5.1 Simple Question Analyzer
- Input: `"rick, summer, and morty’s email"`
- Output: `{ names: ["rick", "summer", "morty"], isAPIRelated: true }`

### 5.2 RAGService
- Fetch all employees
- Filter to employees matching "rick", "summer", "morty"
- Build prompt with their data + examples
- Call LLM and capture `rawResponse`

### 5.3 ResponseProcessor
- Locate the "Your answer:" delimiter in `rawResponse`
- Extract emails:
  - Example: rick.sanchez@company.com, summer.smith@company.com, morty.smith@company.com
- Match emails to requested names
- Return clean result

---

## 6. Why ResponseProcessor Is Needed
- Problem: LLMs are chatty; they add explanations and filler
- Solution: ResponseProcessor acts like a data extractor, pulling only essential info
- Analogy:
  - LLM: writes a 5-paragraph essay with emails buried inside
  - ResponseProcessor: extracts just the emails in the exact format you want

