---
description: Format and build the solution
---

Run the following commands in sequence:

1. `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet`
2. `dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
3. Verify the build output — **ZERO** errors or warnings allowed (including warnings from previous sessions)

**NEVER skip the format step.** Any warnings in the build output must be fixed, even if they existed before your changes.