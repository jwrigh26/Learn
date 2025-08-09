Complete Flow Breakdown

User asks a question
Example: “What are rick, summer, and morty’s emails?”
Simple Question Analyzer
Purpose: Parse the question to extract search parameters
Input: Raw user question string
What it does:
Uses regex patterns to find names: [“rick”, “summer”, “morty”]
Detects if it’s API-related (employee questions = yes)
Extract other parameters (departments, locations, etc)
Output: Question Analysis object with parameters like
{“names”: [“rick”, “summer”, “morty”]}
RAGService (Main Orchestrator)
Purpose: Coordinates everything and calls LLM
Gets employee data from APIService (all employees from cache/API)
Filter employees using the extracted names to find Rick, Summer, and Morty
Builds context with only relevant employee data
Creates prompt with context + question + examples
Calls LLM (phi3:mini model) with the enhanced prompt
Gets raw response from LLM
ResponseProcessor (The Cleanup Crew)
Purpose: Clean up the messy LLM output to extract only what the user needs
Specifics
For Email Questions
Finds the answer section: Looks for “Your answer:” or “Answer:” in the LLM response
Extract emails: Uses regex to find all email addresses after the delimiter
Matches emails to names: Tries to associate each email with the requests names
Returns clean format: “Rick: rick@company.com\nSummer: summer@company.com”
For Other Questions:
Removes filler text: Filters out phrase like “Based on the information provided…”
Takes meaningful content: Gets the first few substantial lines
Returns clean answer: Just the facts without LLM verbosity
Example Flow with “rick, summer, and morty’s email”
SimpleQuestionAnalyzer:
Input: “rick, summer, and morty’s email”
Output: {names: [“rick”, “summer”, “morty”], isAPIRelated: true }
RAGService:
Fetches all employees
Filters to find employees matching “rick”, “summer”, “morty”
Builds prompt with their data + examples
LLM returns something and assigned to a rawResponse
ResponseProcessor:
From the rawResponse finds “Your answer:” delimiter
Extract emails:
Example: rick.sanchez@company.com, summer.smith@company.com, morty.smith@company.com,
Matches to requested names
Returns clean result
Why ResponseProcessor is Needed
The Problem: LLMs are chatty! They add explanations, context, and filter words even when you ask for just emails.
The Solution: The ResponseProcessor acts like a “date extractor” that pulls out only the essential information from the LLM’s verbose response.
Think of it as:
LLM: Gives a 5-paragraph essay with the emails buried inside
ResponseProcessor: Extracts just the emails in the exact format you want

