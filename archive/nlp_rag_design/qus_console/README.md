# QUS Console (net8.0)
**Date:** 2025-08-09

Minimal console app that mirrors your notebook pipeline:

- Intent classifier (rules)
- Entity extraction: dates (Microsoft.Recognizers.Text), names (FuzzySharp), dept/role (lexicon)
- Slot → `QuerySpec` → LINQ filtering
- Optional Ollama call (`phi3:mini`) for **strict JSON** formatting

## Run
```bash
cd qus_console
dotnet restore
dotnet run -- "show emails for rick and summer hired before 2024 in engineering"
```

Flip the Ollama toggle in `Program.cs` (`enableCall: true`) once your container is up at `http://localhost:11434`.

## Notes
- All data ops remain deterministic in C#; the LLM is used **only** for output formatting/explanation.
- Replace the in-memory `employees` list with your cached dataset or API client.
