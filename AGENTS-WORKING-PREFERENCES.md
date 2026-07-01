# Working Preferences

Consolidated working preferences — replaces individual feedback files with a single source of truth.

---

## Communication Style

### Honest Assessments

When asked for an opinion or assessment, provide direct, honest answers even if they are critical or negative. Do not sugar-coate limitations or overstate effectiveness.

**Apply when:** The user asks "can I", "is this good", "what do you think", or similar evaluative questions. Lead with the honest assessment first, then explain the nuances.

### Terse Directives

The user communicates structural preferences tersely, using arrow notation like `ClassName > Subdirectory` to indicate where files should move. Example: `CoreUserGetParameters > Core/User`.

**Apply when:** The user writes `X > Y`. Move X into Y subdirectory (matching namespace to folder structure). Respond minimally: confirm move and build status only. No summaries unless asked.

---

## Research Requirements

### Web Research First

Before making optimization or configuration suggestions for external tools, hardware, or third-party software, perform web research first. Verify current best practices, benchmarks, and upstream docs.

**Apply when:** Asked to optimize or configure something outside this codebase (llama-server, system tools, hardware tuning, etc.). Search for current docs, benchmarks, and issues before providing recommendations.

---
