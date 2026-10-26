# DSM Connection Diagnostic — Design

Status: implemented in 0.8.1 (PR #82), then superseded in part on 2026-10-26 in the same pull request — see
*Superseded in Part* below. The rest of this document records the design as first built.

Implements step 1 of the decided fix in `open-technical-items.md`, *The package needs a DSM hostname and a
matching certificate, and says so nowhere*: **diagnose instead of crashing**. Step 2, discovering the name
from DSM's own certificate, stays open there and is out of scope here.

## Superseded in Part (2026-10-26)

Decided with the maintainer after the loopback prototype was measured on DSM 7.4.1 and 7.2.2:

- **Production reaches DSM over loopback only**: `127.0.0.1` on the HTTPS port of `/usr/syno/etc/www/DSM.json`
  (`ssl.port`), or DSM's default 5001 when that file cannot give one. The root-listener rule of
  `DsmCertificatePolicy` makes a wrong port fail closed. The hostname mode (`external_host_ip`,
  `external_port_dsm_https`) is deleted, and step 2 of the original plan with it.
- **Local debugging names DSM in `ADWH_DSM_URL`** (an absolute `https` URL; anything else stops the start). It
  replaces the `dev-mock/synoinfo.conf` mock; a missing `synoinfo.conf` now means the default language, not a
  host that cannot start.
- **Three checks**: DSM answers on its HTTPS port, the connection is trusted (the certificate policy accepts
  the handshake), `SYNO.API.Info` answers. Name resolution, the certificate summary and the IP redaction are
  gone, along with the failures that only served them. A refused handshake is `NotTrusted`, and its
  `SslPolicyErrors` go to the log (event 3100004).

Where this document disagrees with that list, the list wins. The `open-technical-items.md` entry quoted below is
now titled *Resolved by loopback mode: the package needed a DSM hostname and a matching certificate*.

## Problem

The application reaches DSM at `https://{external_host_ip}:{external_port_dsm_https}` with full TLS
validation. When the hostname is missing the host dies at startup in `DsmSettingsService`; when it does not
resolve, does not answer, or the certificate does not cover it, the login fails with a generic toast and a
500 behind it. Neither says what to fix.

## Goal

The host always starts. In place of the login form, an anonymous visitor sees a checklist naming the first
failing check, the data behind it and the DSM setting that fixes it, with a button that re-reads
`synoinfo.conf` and runs the checks again — no restart.

Out of scope:

- A missing `synoinfo.conf` still fails at startup: it only happens in development
- No status indicator after login
- The UI language is still applied once at startup
- Discovering the hostname from the certificate (step 2)

## Decisions

| Question | Decision |
|----------|----------|
| What an anonymous visitor sees | Full details: failing check, configured hostname and port, certificate names, issuer and expiry. Never an IP address, an exception message or a stack trace. When healthy, only "healthy". |
| When checks run | On the first login-page visit, cached server-side, re-run by a *Re-check* button and automatically when a login fails |
| Architecture | The usual dual implementation: contract in `Data/Contracts/`, server implementation in `Ui/Services/`, HTTP proxy in `Ui.Client/Services/`, thin anonymous controller |
| Where the target comes from | Always `synoinfo.conf`, never the request — the endpoints cannot be pointed at another host |

## The Checks

Five checks run in order. The first failure stops the run; the remaining checks are reported `Skipped`.
Each network step is bounded by one timeout constant (about 5 s; the implementation uses 3 s so the four network
steps fit the client's 15 s).

| # | Check | How | Failure kinds |
|---|-------|-----|---------------|
| 1 | Hostname configured | Re-read `synoinfo.conf`; `external_host_ip` non-empty | missing, settings unreadable |
| 2 | Name resolves | `Dns.GetHostAddressesAsync`; an IP literal passes | not found |
| 3 | Port answers | TCP connect to host:port | refused, timed out |
| 4 | Certificate covers the name | `SslStream` with `TargetHost = host`; the validation callback records SAN names, issuer, `NotAfter` and `SslPolicyErrors`, and returns `errors == SslPolicyErrors.None` | name mismatch, untrusted, expired, handshake failed |
| 5 | DSM API answers | New `DsmApiClient.ProbeAsync`: the real `SYNO.API.Info` request through the same `HttpClient`, without touching the handshake cache | unexpected response (HTTP status) |

**Check 4 observes, it never bypasses.** The callback returns exactly what default validation would. A test
against a local self-signed TLS server must see it fail.

Classifying `SslPolicyErrors` and the chain status into a failure kind is a pure function, tested per case.

## The Report

The report carries no localized text — only enums and raw data, which the client localizes:

- Healthy flag, checked-at timestamp, configured host and port
- Per check: status (`Passed`, `Failed`, `Skipped`), failure kind, and for check 4 the certificate's names,
  issuer and expiry

Logging uses `[LoggerMessage]`: Information when healthy, Warning naming the failing check and kind. It takes
a new `Base`/`Last` pair in `LogEventIds.cs`; the next free block is 3100000.

## Server Lifecycle

### `DsmSettingsService`

- A missing `external_host_ip` no longer throws: `Server` becomes `string?` and a Warning is logged
- New `Reload()` re-reads the file and swaps one immutable settings object in a single assignment
- If `Reload()` fails, check 1 fails with *settings unreadable*
- Accepted limit: `DsmApiClient` reads `Server` and `Port` separately, so a request in flight during a reload
  can mix them. It fails once, at a moment the application is already broken.

### `DsmApiClient`

When `Server` is `null` it throws `InvalidOperationException("DSM hostname is not configured")` at once,
instead of building `https://:5001/…`. Its handshake cache fills only on success, so a fixed DSM is picked up
on the next call.

### `DsmConnectionDiagnosticService` (Singleton)

- Every run starts with `Reload()`, so the first run and a re-check read the file the same way
- `GetReportAsync()` returns the cached report, running the checks only if none exists yet
- `RecheckAsync()` runs the checks and replaces the report
- Concurrent runs are coalesced behind the existing `SemaphoreLock`: one run at a time, and callers that
  waited receive the report that finished while they waited
- A run uses `CancellationToken.None`, bounded by the per-step timeouts: a visitor closing the tab must not
  cancel a run another visitor is waiting for

### A Failed Login

`AuthenticationService.LoginAsync` catches any exception from `ConnectAsync` except caller cancellation, then
re-runs the diagnostic. **The diagnostic decides, so no exception classification is maintained:**

- Unhealthy: the result carries a new `ApiErrorCode.DsmUnreachable`
- Healthy: today's generic failure, with the exception logged instead of a 500

### Endpoints

Anonymous — no `[AuthorizeSession]`. Routes in a new `Constants/WebApi/DsmDiagnosticRoutes.cs`.

- `GET api/v1/dsm-diagnostic`: the cached report. Only the first read after startup causes network traffic
- `POST api/v1/dsm-diagnostic/recheck`: re-runs the checks. Rate-limited under **its own** policy name with
  the login policy's partition function, so a re-check never spends a login attempt

## User Interface

### `Login.razor`

- After the existing `IsAuthenticatedAsync` check, the page reads the report. Unhealthy: the same
  `FluentCard` renders `DsmDiagnosticPanel` instead of the form. If reading the report fails, the form shows
  as today — the diagnostic helps, it never blocks
- A login returning `DsmUnreachable` re-reads the report, which the server has just recomputed, and switches
  to the panel

### `DsmDiagnosticPanel`

FluentUI components only, no inline style.

- The five checks as a list with an icon each: passed, failed, skipped
- Under the failing check: its cause, the raw data, and the remedy
- **Remedies name the exact DSM 7.4 menus, in English and French, read off the DSM interface** rather than
  written from memory
- A *Re-check* button. Healthy result: the form returns. HTTP 429: a "too many attempts" toast
- Keys under `LK.Diagnostic.*` in both `.resx` files; the existing FR/EN parity test and `UiSourceAuditTests`
  cover them

## Tests

All deterministic, none needing an outside network:

1. `DsmSettingsServiceTests`: no `external_host_ip` gives `Server == null` without throwing; `Reload()`
   picks up a change; a missing file still throws
2. TLS classification: one test per failure kind
3. Real checks against local endpoints, which also cover ordering and `Skipped`:
   - hostname missing: check 1 fails, the rest are skipped
   - a `.invalid` name, which never resolves (RFC 6761): check 2 fails
   - a port reserved then released: check 3 fails
   - a local TLS server with a self-signed certificate generated in the test: check 4 **must fail**
4. `AuthenticationServiceTests`: `ConnectAsync` throws — unhealthy gives `DsmUnreachable`, healthy gives the
   generic failure, nothing propagates
5. `ApplicationStartupTests`: the host starts with a `synoinfo.conf` lacking `external_host_ip`, and the
   anonymous endpoint reports check 1 failed. **Proven by restoring the `throw` and watching it fail for that
   reason**
6. Endpoints: anonymous access without a session; the re-check rate limit, after `LoginRateLimitPolicyTests`
7. bUnit: the panel renders for an unhealthy report, the form for a healthy one

Not covered by a unit test:

- **Coalescing** rests on the existing `SemaphoreLock`; testing it would need injectable timeouts
- **Check 4 passing** would need a change to the trust store. It is accepted on the vDSM once a trusted
  certificate is installed there, which is also step 2's positive path

## Acceptance on the vDSM

The vDSM stays misconfigured on purpose until this lands.

- **Local run**, after the AGENTS.md §13 guard: checks 1–3 pass and check 4 fails on DSM's default
  self-signed certificate
- **SPK installed on the vDSM**: check 1 fails. Set the hostname in DSM, press *Re-check*: check 4 is expected to fail,
  **without a restart** — `Reload()` exercised for real
- A screenshot of each state

## Also in This Change

- Version 0.8.1 through `./src/scripts/update-version.sh 0.8.1`
- `technical-architecture.md`: the new service and the anonymous endpoints
- `open-technical-items.md`: step 1 marked done; step 2 stays open
