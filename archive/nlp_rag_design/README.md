# Query Understanding Service – Notebooks (Updated)

This set replaces your 00–04 notebooks and adds tests + an E2E demo.

- 00-setup.ipynb — DI, logging, cache, config, in-memory Employee API, Ollama ping
- 01-intent.ipynb — Rule-based `IIntentClassifier` with quick golden tests
- 02-entities.ipynb — `ISlotFiller` using Recognizers.Text for dates + simple maps
- 03-dispatcher-and-query.ipynb — `IQueryDispatcher` that applies deterministic filters
- 04-formatter-ollama.ipynb — LLM gate; deterministic vs strict-JSON via Ollama
- 05-tests.ipynb — Golden + contract tests
- 06-e2e-demo.ipynb — End-to-end pipeline demo

> These notebooks use code cells with language set to C#. If your kernel doesn't support .NET Interactive,
> copy the code into your .NET project directly.
