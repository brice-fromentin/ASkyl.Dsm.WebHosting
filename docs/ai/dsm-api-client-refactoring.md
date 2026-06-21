# DsmApiClient Refactoring Backlog

## Status: Deferred

---

## Issue #1: Thread Safety on `DefaultRequestHeaders`

**FILE:** `src/Askyl.Dsm.WebHosting.Tools/Network/DsmApiClient.cs`
**SEVERITY:** High

**Problem:** `DsmApiClient` is registered as `Singleton`. `SetSid()`
(lines 73-80) calls `Remove()` then `Add()` on `DefaultRequestHeaders`,
which is a `Dictionary<string, List<string>>` internally — not thread-safe
for concurrent writes. If two logins/disconnects happen concurrently,
the headers dictionary can be corrupted. The Remove/Add sequence is
also not atomic.

**Impact:** Concurrent requests from multiple users/circuits could cause race conditions, leading to lost or corrupted session headers.

**Suggested Fix:** Add a `lock` around header modifications:

```csharp
private readonly object _headerLock = new();

public void SetSid(string sid)
{
    lock (_headerLock)
    {
        Sid = sid;
        _httpClient.DefaultRequestHeaders.Remove(NetworkConstants.CookieHeader);
        if (sid.Length > 0)
        {
            _httpClient.DefaultRequestHeaders.Add(NetworkConstants.CookieHeader, $"{NetworkConstants.SessionCookieName}={sid}");
        }
    }
}
```

Apply the same pattern to `DisconnectAsync()`.

---

## Issue #2: `ReadSettings()` Throws Instead of Graceful Fallback

**FILE:** `src/Askyl.Dsm.WebHosting.Tools/Network/DsmApiClient.cs`
**SEVERITY:** High

**Problem:** `ReadSettings()` throws `FileNotFoundException` (line 151)
and `MandatorySettingMissingException` (line 163). Since `SystemPreferences`
is a property initialized at construction time (line 43), these exceptions
crash the application at startup with no recovery path. The previous
behavior (returning `false` from `ConnectAsync`) allowed the app to start
and fail gracefully at login time.

**Impact:** Missing `/etc/synoinfo.conf` or missing required keys will prevent the application from starting entirely, rather than allowing degraded operation.

**Suggested Fix:** Either:

1. Wrap `ReadSettings()` in try-catch and log warnings, returning defaults
2. Defer settings read to first use (lazy initialization)
3. Move settings read to `ConnectAsync()` with proper error handling

---

## Issue #9: Singleton `DsmApiClient` with Mutable Session State

**FILE:** `src/Askyl.Dsm.WebHosting.Tools/Network/DsmApiClient.cs`
**SEVERITY:** Low (related to #1)

**Problem:** `DsmApiClient` is a singleton with mutable per-session state (`Sid`, `_sessionValid`, `_lastSessionValidation`). This design is inherently unsafe for concurrent access from multiple users/circuits.

**Impact:** Session state from one user could leak into another user's requests.

**Suggested Fix:** Consider:

1. Per-request/per-scope `DsmApiClient` instances
2. Thread-local session state
3. Session-scoped client wrapper that holds session state separately
   from the singleton HTTP client

---

## Notes

- These issues were identified during the globalization branch code review (2026-06-14)
- All three are architectural concerns related to the singleton pattern and session state management
- Recommended to address together as a cohesive refactoring effort
