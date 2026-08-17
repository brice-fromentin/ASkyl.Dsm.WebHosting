# DSM API Notes

Synology does not publish documentation for the `SYNO.Core.*` APIs, so every fact learned about them has
to be recorded here or it is lost and re-asked.

**Rules for this file:** one entry per fact, each with how it was established. Anything not verified is
marked as a guess. This is a reference, not an agenda — it does not get deleted.

## Transport

All calls are POST. `DsmApiClient` (`Tools/Network/`) resolves the CGI path from the `SYNO.API.Info`
handshake, except for `SYNO.API.Info` itself which is hardcoded to `query.cgi`. The SID travels as a
`Cookie: id=<sid>` header, never as a query parameter — the client is stateless and the SID is passed per
call.

Each `IApiParameters` implementation picks its own body encoding, `Form` or `Json`. The choice is not
cosmetic: DSM rejects the wrong one. Established by working code, per API below.

## APIs in use

| API | Version | Method | Body | Notes |
|---|---|---|---|---|
| `SYNO.API.Info` | 1 | `query` | Form | Handshake; always `query.cgi` |
| `SYNO.API.Auth` | 6 | `login` | Form | |
| `SYNO.API.Auth` | 6 | `logout` | Form | Returns HTTP 200; see below |
| `SYNO.Core.User` | 1 | `get` | Form | Administrator-only; see below |
| `SYNO.Core.UserSettings` | 1 | `get` | Form | |
| `SYNO.Core.ACL` | 1 | `set` | Form | |
| `SYNO.Core.AppPortal.ReverseProxy` | 1 | `list` | Form | |
| `SYNO.Core.AppPortal.ReverseProxy` | 1 | `create` | **Json** | |
| `SYNO.Core.AppPortal.ReverseProxy` | 1 | `update` | **Json** | |
| `SYNO.Core.AppPortal.ReverseProxy` | 1 | `delete` | **Json** | |
| `SYNO.FileStation.Info` | — | — | — | Requested in the handshake, not yet called |
| `SYNO.FileStation.List` | 2 | `list`, `list_share` | Form | |

`SYNO.Core.AppPortal.ReverseProxy` mixes both encodings: `list` is Form, the three mutating methods are
Json. Established by working code against the NAS.

## `SYNO.Core.User.get` is administrator-only

Calling it as a non-administrator returns error code `105` (permission denied) rather than a result.
This is the application's **entire** administrator check — there is no other one. `DsmSession` validation
therefore fails closed: any error, including a transport failure, is treated as not-an-administrator.

Consequence: do not relax that call, do not add a fallback path, and do not swap it for a cheaper API
without replacing the privilege check first.

## `SYNO.API.Auth.logout` works at version 6

Version 6 matches `login`, which is the version the application already negotiates successfully for this
API. The call returns HTTP 200 and invalidates the SID on the NAS. Established on the deployed NAS
during PR #38; the shape was previously unverified.

## Error codes

Shared codes live in `Constants/DSM/API/DsmConstants.cs`:

- `-4` — authentication failed (invalid or expired SID). Any API can return it.
- `105` — insufficient privilege. Returned by administrator-only APIs such as `SYNO.Core.User`.

## Open questions

- Whether the `Form` vs `Json` split on other `SYNO.Core.*` APIs follows a rule, or is per-method
  accident. Currently only known empirically, one method at a time.
- Version ceilings per API. The handshake response carries `MinVersion`/`MaxVersion` for every API, but
  nothing reads them — each parameters class hardcodes its own `Version`, and the reported range has
  never been recorded from a real NAS. `ApiConstants.MinVersion`/`MaxVersion` (1 and 7) are unused
  leftovers, not a negotiated range.

<!-- temporary: proving that a stacked pull request now triggers CI -->
