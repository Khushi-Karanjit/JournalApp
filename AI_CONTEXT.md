# Project Context (MAUI Blazor Hybrid)

Tech:
- .NET: (put your version here, e.g., net8.0)
- App type: .NET MAUI Blazor Hybrid
- Storage: SQLite (local)
- Security: SecureStorage for PIN, Preferences for username
- Pattern: Services + Models (no heavy MVVM)

Folders:
- Models/
- Services/
- Data/ (SQLite DB + repositories)
- Pages/ (Razor pages)
- Components/

Rules:
- Do not change MauiProgram.cs unless explicitly asked.
- Do not rename routes without updating navigation.
- Keep code consistent with existing services (async methods, DI-friendly).
- All DB writes go through JournalDatabase / DbContext-like service.
