---
description: Run the test suite
---

Run `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --no-build` and report the results.

Note: a healthy run completes in ~5s and exits 0. A hang or abort means a deadlock in the code — diagnose it with `--blame-hang-timeout 10s`, do not adopt the flag as standard.
