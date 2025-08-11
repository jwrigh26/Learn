# Query Understanding Service — Technical Review & Starter Playbook
**Date:** 2025-08-09

This is the same review I summarized earlier, regenerated so you have a stable copy to download.
(If a previous link failed, it was likely a generation hiccup—my bad. This one will stick.)

**TL;DR**
- Keep the pipeline: Intent → Entities/Slots → Query Builder → Data Filter → LLM **formatting only**.
- Replace regex ResponseProcessor with **strict JSON** from the model and parse with `System.Text.Json`.
- Use Recognizers.Text for date/ranges; FuzzySharp (thresholded) for names; a small **domain lexicon** for dept/role.
- Start with three intents: `GET_CONTACT_INFO`, `FILTER_BY_HIRE_DATE`, `FILTER_BY_ROLE`.
- Work notebook-first; codify wins into this console app.
