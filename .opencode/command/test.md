---
description: Run the test suite
---

Run `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --no-build --blame-hang-timeout 10s` and report the results.

Note: `--blame-hang-timeout 10s` is required — the xUnit VSTest adapter (v3.1.5) on .NET 10 does not exit after tests complete. This flag kills the hung process. Tests complete in ~4s, so 10s provides a 6s grace period before force-killing.
