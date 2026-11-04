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
| `SYNO.Core.UserSettings` | 1 | `get` | Form | Refuses an account that never opened DSM; see below |
| `SYNO.Core.ACL` | 1 | `set` | Form | `dirPaths` is a share path; replaces explicit entries; see below |
| `SYNO.Core.ACL` | 1 | `get`, `status`, `stop` | Form | Read before `set`; poll and release its task; see below |
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

## `SYNO.Core.UserSettings.get` refuses an account that never opened DSM

On the Virtual DSM 7.2.2, on 2026-11-02, an administrator account just created with `synouser --add` was refused
with code `117` on each of its first two sign-ins through the application, while an established account's call
succeeded the same day. Signing in through `SYNO.API.Auth` does not create the settings; opening DSM's desktop
once as that account does, and every later call succeeded. DSM writes them about 3 s after the desktop shows: a
fresh account whose visit ended at once, or after 2 s, had none. The sign-in itself is never affected: until then the
application keeps its default language and formats, and the refusal is a Debug line (event 2000002).

## `SYNO.Core.ACL.set` takes a share path in `dirPaths`, and replaces the explicit entries

Established on the Virtual DSM 7.2.2 on 2026-11-02, by replaying the call with `curl` and reading the result
with `synoacltool -get`.

- **Parameters, as DSM's own permission editor sends them** (`webman/modules/Share/PermissionDialog.js`):
  `file_path` and `files` carry the real path (`/volume1/<share>/<folder>`), `dirPaths` the share path
  (`/<share>/<folder>`; for a file, its parent folder). `rules` lists the entries, `inherited` keeps the
  inherited ones; `acl_recur` comes from an editor checkbox (`acl_privilage_recur`) and was not exercised.
- **A real path in `dirPaths` is accepted and does nothing**: success, a `task_id`, and an unchanged ACL. The
  same call with the share path in `dirPaths` applied the entry within 5 s, explicit on the folder and
  inherited by its files.
- **A share path in `file_path`** is refused at once with code `403`.
- **`rules` is the complete explicit list, not an addition**: an explicit entry present before the call was
  gone after it; inherited entries stayed. The editor reads the ACL and sends every explicit entry back with the
  change.
- **The call is asynchronous**: it returns a `task_id` before the ACL changes.

### How DSM's own editor does it, and how the application now does it

Traced on 2026-11-04 in the browser while File Station's permission editor saved a share root, then replayed:

1. `get` with `type=all`, `file_path` the real path, `include_noname_rules=true`. The answer lists every entry with
   its `level` — 0 for one set on that path, 1 and more for one inherited from that many folders up — its
   `owner_id`, and `is_inherited` for the path itself.
2. `set` with every **level-0** entry sent back as it came, `owner_id` included and `level` left out, the new entry
   added, `inherited` equal to `get`'s `is_inherited` (`false` at a share root). The same call serves a share root
   and a folder.
3. `status` with the `task_id` until `finished` is `true` — 0.2 s for each folder on the Virtual DSM — then `stop`.

Measured details:

- Strings may go unquoted, as the Form serializer sends them; the editor quotes them (`file_path="/volume1/x"`),
  and both work for `get`, `set`, `status` and `stop`.
- **A permission object missing keys is refused with `403`.** Send all thirteen, as the C# record does.
- **The least a site needs** (checked as the package user with `sudo -u`): passage only (`exe_file`, inherit
  this folder only — `---n` in `synoacltool`) on the shared folder and on each folder in between, and Read
  (`read_data`, `exe_file`, `read_attr`, `read_ext_attr`, `read_perm`, inherited by everything) on the site's
  folder. The application then reads its dll and lists its folder, and can neither list the shared folder nor the
  folder in between, nor read a file in another folder of the share.

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
