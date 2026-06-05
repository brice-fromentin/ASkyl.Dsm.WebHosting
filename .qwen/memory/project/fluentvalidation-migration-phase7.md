---
name: FluentValidation migration planned as Phase 7
description: FluentValidation replaces DataAnnotations for localized validation messages, scheduled as Phase 7 after Culture Manager (Phase 6)
type: project
---

FluentValidation migration is planned as **Phase 7** of the localization effort, replacing DataAnnotations for `WebSiteConfiguration` and other models with user-facing validation strings.

**Why Phase 7 (not Phase 6):** Phase 6 (Culture Manager & Resolution) must land first so that `IStringLocalizer` respects the user's culture at runtime — otherwise FluentValidation validators would resolve to English-only strings. Phase 7 depends on Phase 6 working correctly.

**Why FluentValidation over custom attributes:** Industry-standard approach, no fragile DI coupling in validation attributes, cleaner separation of concerns. Custom attributes require boilerplate per attribute type and couple validation logic to `IServiceProvider`.

**What changes:**
- Add `FluentValidation` + `FluentValidation.AspNetCore` NuGet to Ui project
- Create validator classes (e.g., `WebSiteConfigurationValidator`) that inject `IStringLocalizer<SharedResource>`
- Replace `<DataAnnotationsValidator />` with `<FluentValidationValidator />` in Razor components
- Remove user-facing strings from `WebSiteConstants.cs`
- `RuntimeConstants` error strings in `DownloaderService` (Tools) remain — they're internal exceptions caught and converted to `L.Error.OperationFailed`

**How to apply:** When scoping localization work, treat FluentValidation as a discrete phase after culture resolution is proven working.
