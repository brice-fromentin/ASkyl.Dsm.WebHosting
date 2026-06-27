---
description: Format and build the solution
---

Run the following commands in sequence:

1. `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet`
2. `dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`

Report any errors or warnings from the build.