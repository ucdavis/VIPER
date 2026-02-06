# Copilot Instructions

## C# Conventions

- **DateTime**: Use `DateTime.Now` and `DateTimeKind.Local`, never `DateTime.UtcNow`. The application operates in a single-timezone campus environment and all database timestamps are local.

## Database

- **SQL Server 2016** compatibility required — do not suggest `STRING_AGG`, `TRIM`, `CONCAT_WS`, `GREATEST`, or `LEAST`.

## Security

- **Log injection**: User-supplied values must be sanitized before logging via `LogSanitizer` (`SanitizeId()`, `SanitizeString()`, `SanitizeYear()`). Hard-coded strings, enums, and DB-sourced values do not need sanitization.
