---
name: Run tests after code changes
description: User expects tests to be run after code changes, not just format and build
type: feedback
---

After making code changes (renames, refactors, etc.), always run the full test suite in addition to format and build.

**Why:** The user explicitly called out "did you ran tests ?" when I completed a rename without running tests, indicating this is an expected step I should not skip.

**How to apply:** Run `dotnet test /nr:false ./src/Askyl.Dsm.WebHosting.slnx` after format and build whenever code changes are made.
