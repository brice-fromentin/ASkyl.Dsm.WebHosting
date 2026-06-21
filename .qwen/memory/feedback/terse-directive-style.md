---
name: Terse directive communication style
description: User gives minimal, one-line directives using arrow notation (X > Y) for structural changes
type: feedback
---

The user communicates structural preferences tersely, using arrow notation like `ClassName > Subdirectory` (e.g., `CoreUserGetParameters > Core/User`, `InformationsQueryParameters > Info`) to indicate where files should move.

**Why:** This is the user's established pattern for directing organizational changes — concise and unambiguous.

**How to apply:** When the user writes `X > Y`, interpret it as "move X into the Y subdirectory" (matching namespace to folder structure). Respond with minimal output — just confirm the move and build status. Do not add summaries or explanations unless the user asks.
