# 2ef1fb5b — Improved password generation

- **Date:** 2022-01-28 (PR #1341, Michael Rowley)  |  **Risk (A.1):** MEDIUM (web_other)
- **Files:** 1 (+9/−13) — `x360ce.Web/Security/Controls/CreateUser.ascx.cs`

## Changes

### Fixes (security)
- Replaces the predictable password generator (`System.Random`, fixed 8 chars,
  alternating consonant/vowel pattern) with a cryptographically random one:
  12–32 characters from a 68-character set via `RandomNumberGenerator`.

## Backward compatibility for 4.17.x
- Implementation-only; no contract change. Upstream used
  `RandomNumberGenerator.GetInt32`, which does not exist on .NET Framework
  4.6.2 — ported using `RNGCryptoServiceProvider` with rejection sampling
  (same 12–32 length range, same character list).

## Decision
**Apply.** APPLIED on branch (adapted for net462), together with the doc
comments from [4b38be38](4b38be38.md). Builds under `APP_Any_v4`.
