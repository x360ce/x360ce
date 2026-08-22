# Sub-project E — DataPorter Enhancement (design)

**Date:** 2026-05-16
**Status:** Draft — awaiting user review.
**Parent:** `docs/plans/README.md` (A → B (depends on E) → C → D + **E**)
**Code lives in:** `D:\Projects\Jocys.com\Sql\DataPorter\` (separate repo from x360ce).
**Design lives here:** keeps all planning in one place per user direction ("Write plans to docs/plans folder").

## 1. Goal

Promote `DataPorter` from a *CSV-only, schema-light* tool to the **single executable that replaces `Data/Change Scripts/Backup/BackupAndRestoreData.ps1`** in x360ce and elsewhere. Per-table backup with byte-perfect fidelity, multiple data formats, comprehensive schema scripting.

When E is complete:
- `BackupAndRestoreData.ps1` + its committed 50 MB of pinned SMO DLLs in `bin/` is deleted from x360ce.
- x360ce sub-project B uses DataPorter for both the synthetic test seed AND for `x360ce_Tests` backup/restore.
- Installer reset (future work) uses DataPorter to reseed user databases.

## 2. Capability gaps closed by E

| # | Capability | Today | After E |
|---|---|---|---|
| 1 | Data format: CSV | ✅ | ✅ (unchanged; default) |
| 2 | Data format: DAT (BCP native binary) | ❌ | ✅ via `bcp.exe` shellout — byte-perfect for `varbinary`, `geometry`, `datetime2`, etc. |
| 3 | Data format: JSON | ❌ | ✅ per-row JSON objects with type metadata; binary as base64 |
| 4 | Binary columns in CSV | ⚠️ silently mangled | ✅ base64-encoded with column-type header |
| 5 | Schema scripting: non-clustered indexes | ❌ | ✅ in per-table `.sql` |
| 6 | Schema scripting: foreign keys | ❌ | ✅ in per-table `.sql`; **import order controlled by config `Items[]`** (parent tables listed first) |
| 7 | Schema scripting: triggers | ❌ | ✅ in per-table `.sql` |
| 8 | Schema scripting: check constraints / defaults / unique | partial | ✅ in per-table `.sql` |
| 9 | Schema scripting: extended-property descriptions | ❌ | ✅ in per-table `.sql` |
| 10 | Idempotent re-import | partial | ✅ — every `CREATE` / FK guarded by `IF NOT EXISTS`. Schema import is a no-op against an already-built DB |
| 11 | Config can interleave raw SQL / SQL-file steps between table items | ❌ | ✅ `Items[]` accepts `{ "Sql": "..." }` or `{ "SqlFile": "..." }` records as well as table records |
| 12 | Single executable distribution | ✅ (net10.0 self-contained) | ✅ (unchanged) |
| 13 | CLI consistency | ✅ | ✅ (`--format csv|dat|json` added) |

### Why per-table FKs work — ordering lives in the config

Indexes, triggers, defaults, checks, FKs, and descriptions are all emitted into the same `<schema>.<table>.sql`. Cross-table dependencies (only FKs) are not a tool concern — they're a **config concern**.

The config's `Items[]` list is **authoritative for import order**. The user lists parent tables (`x360ce_Vendors`, `x360ce_UserComputers`, ...) before child tables (`x360ce_Products`, `x360ce_UserDevices`, ...). If the user lists them wrong, the import fails with SQL Server's own clear error ("references invalid table FK_xxx"). The tool doesn't second-guess.

Two consequences:
- **Fresh-DB restore:** user-ordered import of per-table `.sql` files → FKs are created as each child table is built (its parent already exists). No second pass, no consolidated FK file.
- **Data refresh into existing DB:** user runs `--type data` only; `--type schema` is not invoked; existing FKs in the target are untouched.

Where this differs from the dropped `_foreign_keys.sql` approach: instead of the tool consolidating FKs, the **config author** controls order. Simpler tool, more user responsibility. Matches the existing legacy XML config which already lists tables in `Items[]` order.

## 3. CLI changes

Existing CLI (unchanged): `dataporter <config.json> --action export|import --type schema|data [--output <path>] [--quiet] [--no-zip]`

New `--format` values for `--type data`:

```
--format csv     (default, text; binary columns base64 with header)
--format dat     (BCP native binary via bcp.exe shellout)
--format json    (one JSON document per row; binary base64; type metadata in sidecar)
```

New flags for `--type schema`:

```
--no-indexes      Skip non-clustered indexes in per-table .sql. Clustered (PK) index always kept.
--no-fks          Skip foreign keys in per-table .sql (table body only).
--no-triggers     Skip triggers in per-table .sql.
--no-descriptions Skip extended-property descriptions in per-table .sql.
```

Defaults: indexes / FKs / triggers / descriptions all **included**. The `--no-*` flags switch them off independently. (Note: `--no-fks` here just controls what goes IN the per-table file at export time. It does not split FKs into a separate file — the file is still self-contained.)

Typical recipes:

```
# Full fresh-DB restore (config Items[] must list parents before children):
dataporter cfg.json --action import --type schema           # per-table .sql in config order
dataporter cfg.json --action import --type data --format csv

# Data refresh into existing DB (skip all schema work — FKs already in target):
dataporter cfg.json --action import --type data --format csv

# Schema-only export without FKs (e.g. capture table shapes for diff):
dataporter cfg.json --action export --type schema --no-fks
```

## 4. File layout per export

```
<output-root>/
├── SQL_Schema/                  # one file per table; everything table-related in one place
│   └── <schema>.<table>.sql     # CREATE TABLE + non-clustered indexes + FKs + triggers
│                                #   + DEFAULT / CHECK / UNIQUE constraints + descriptions
├── CSV_Data/                    # if --format csv
│   ├── <schema>.<table>.csv
│   ├── <schema>.<table>.csv.zip (if --no-zip not set)
│   └── <schema>.<table>.schema.csv  # column-name / SQL-type / nullable header — used to decode base64
├── DAT_Data/                    # if --format dat
│   ├── <schema>.<table>.dat     # bcp native, paired with .fmt
│   └── <schema>.<table>.fmt     # bcp format file (XML)
└── JSON_Data/                   # if --format json
    ├── <schema>.<table>.ndjson  # newline-delimited JSON; binary base64
    └── <schema>.<table>.types.json  # column-name / SQL-type sidecar
```

Imports auto-detect the data directory (`CSV_Data` / `DAT_Data` / `JSON_Data`) — `--format` flag selects.

Schema import applies per-table `.sql` files in the order dictated by config `Items[]` (see §6.2 / §6.4). No consolidated FK file — FKs live with their owning tables.

## 5. Binary column handling (per format)

| Format | Storage |
|---|---|
| CSV | Column metadata header (`schema.csv`) lists each column's SQL type. Cells with binary types contain base64 strings prefixed with `b64:`. Empty cells = SQL NULL. |
| DAT | Native binary via `bcp.exe -n` (or `-N` for Unicode native if char columns present). Format file (`.fmt`) generated by `bcp <table> format nul -n -f <table>.fmt` describes the binary layout. |
| JSON | One column per object key. Binary types serialized as `{ "_type": "varbinary", "value": "<base64>" }` — explicit so consumers can't mistake them for text. NULL = JSON `null`. |

### Why these three formats and not others
- **CSV** — Excel, diff tools, ad-hoc inspection. Default for editability.
- **DAT** — byte-perfect for `varbinary(MAX)`, `geometry`, `geography`, `datetime2(7)`, `datetimeoffset`. Microsoft-official format. Cannot be eyeballed.
- **JSON** — strongly typed text, programmatic, language-agnostic. Reasonable middle ground for tooling that doesn't speak BCP.

XML, Avro, Parquet rejected: XML duplicates JSON for our needs; Avro/Parquet add a NuGet dependency without unique value.

## 6. Comprehensive schema scripting

One file per table; everything table-related (including its FKs) goes in that file. Import order comes from the config's `Items[]` list.

### 6.1 Per-table `.sql` content

`Database.ScriptTable` is upgraded to use these SMO `ScriptingOptions`:

```csharp
new ScriptingOptions {
    IncludeIfNotExists       = true,
    ClusteredIndexes         = true,
    NonClusteredIndexes      = !opts.NoIndexes,
    Indexes                  = !opts.NoIndexes,
    XmlIndexes               = !opts.NoIndexes,
    DriPrimaryKey            = true,
    DriUniqueKeys            = true,
    DriChecks                = true,
    DriDefaults              = true,
    DriForeignKeys           = !opts.NoFks,            // FKs live in the same per-table file
    Triggers                 = !opts.NoTriggers,
    ExtendedProperties       = !opts.NoDescriptions,   // table + column descriptions
    Statistics               = false,                  // recomputed on import; useless on disk
    FullTextIndexes          = false,                  // out of scope unless a real consumer asks
    NoCollation              = true,                   // collation lives at DB-level
    ScriptBatchTerminator    = true,
    Permissions              = false,
}
```

Output: `<schema>.<table>.sql` containing the complete `CREATE TABLE` + indexes + DEFAULTs + CHECKs + UNIQUE + FKs + triggers + extended-property descriptions, all guarded by `IF NOT EXISTS`.

### 6.2 Import order from config

The config's `Items[]` array is **authoritative for the order in which schema and data are imported**. The author lists parent tables before children. Example for x360ce:

```json
{
  "Items": [
    { "Schema": "dbo", "Table": "x360ce_Vendors" },
    { "Schema": "dbo", "Table": "x360ce_Products" },
    { "Schema": "dbo", "Table": "x360ce_UserComputers" },
    { "Schema": "dbo", "Table": "x360ce_UserDevices" },
    { "Schema": "dbo", "Table": "x360ce_PadSettings" },
    { "Schema": "dbo", "Table": "x360ce_UserSettings" },
    { "Schema": "dbo", "Table": "x360ce_Programs" }
  ]
}
```

Wrong order → SQL Server emits a clear FK-violation error pointing at the missing parent. Tool does not topologically sort; the author owns ordering decisions.

### 6.3 Interleaved SQL / SQL-file items

`Items[]` accepts non-table records too. The tool dispatches based on which property is present:

```json
{
  "Items": [
    { "Schema": "dbo", "Table": "x360ce_Vendors" },
    { "Sql": "EXEC dbo.x360ce_Cleanup_Settings_NoPadSettings" },
    { "Schema": "dbo", "Table": "x360ce_Products" },
    { "SqlFile": "scripts/db/seed/04_programs.sql" },
    { "Schema": "dbo", "Table": "x360ce_Programs" }
  ]
}
```

Item dispatch:
- `Table` (with optional `Schema`, `Database`, `Query`) → table import or export.
- `Sql` → execute the inline T-SQL string on `TargetConnection`. Multi-batch `GO`-separated input supported via SMO `ExecuteNonQuery`.
- `SqlFile` → read the file (path relative to config file) and execute on `TargetConnection`.

Export semantics: `Sql` / `SqlFile` items are **no-ops on export** (they describe an import-only side effect). Documented; not auto-derived.

This generalises the existing `PreImportSchemaCommand` / `PostImportSchemaCommand` hooks into per-step interleaving without removing them — both stay valid.

### 6.4 Data refresh into existing DB

User runs `--action import --type data` only; `--type schema` is not invoked; existing tables/indexes/FKs/triggers in the target are untouched. Data load runs with `ClearBeforeImport=delete` (FK-safe — deletes child rows before parent rows in `Items[]` reverse order, then bulk-inserts in forward order).

## 7. Implementation notes (DataPorter repo)

Files touched (in `D:\Projects\Jocys.com\Sql\DataPorter\`):

| File | Changes |
|---|---|
| `Database.cs` | Expand `ScriptTable` options as above. Add `ScriptIndexes`, `ScriptForeignKeys`, `ScriptTriggers` helpers if the SMO `Table.Script` doesn't already produce them with the right options. |
| `Exporter.cs` | Branch on `format`: CSV (existing), new `dat` (shell `bcp` per table), new `json` (NDJSON writer). Emit schema header sidecars for CSV/JSON. |
| `Importer.cs` | Same branch. CSV: enhance to decode base64 per schema header. DAT: shell `bcp` for import. JSON: read NDJSON, decode base64 fields. |
| `Program.cs` | Accept `--format dat|json`. Validate. |
| `Config.cs` | Add `BcpPath` option (path to `bcp.exe`, auto-detected if blank). |
| `Resources/Help.txt` | Update with new formats. |
| `Resources/Default.json` | Add comments showing the new format options. |
| New: `BcpRunner.cs` | Wrapper around `bcp.exe` — discovery (PATH → SQL Server tools dir), invocation, exit-code handling. |
| Tests (if DataPorter has any) | Add per-format round-trip tests against a LocalDB. |

`bcp.exe` discovery (similar to our `Find-SqlPackage.ps1` in B):
1. `Get-Command bcp` / `where bcp`.
2. `C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\<ver>\Tools\Binn\bcp.exe`.
3. `C:\Program Files\Microsoft SQL Server\<ver>\Tools\Binn\bcp.exe`.
4. `winget install Microsoft.SQLServer.Tools` if missing.

## 8. Round-trip guarantees

| Format | Roundtrip guarantee |
|---|---|
| CSV | Byte-perfect for `int`, `bigint`, `nvarchar`, `uniqueidentifier`, `bit`. Byte-perfect for binary types via base64. Lossy only for: trailing whitespace in NVARCHAR (CSV may strip), datetime sub-second beyond CSV's text precision. |
| DAT | Byte-perfect for every SQL type. |
| JSON | Byte-perfect for binary via base64. Datetime preserved via ISO 8601 with `.fffffff` precision. Strict typing via sidecar prevents `'3' → int 3` mistakes. |

Tests in DataPorter add a "round-trip every supported SQL type" matrix per format.

## 9. Out of scope for E

- DataPorter is **not** taught about DACPAC. Schema deploy for x360ce still uses `SqlPackage /Action:Publish` from the canonical `Data/x360ce.Data.sqlproj` (per `solution-patterns` SSOT direction). DataPorter handles the *backup/restore + per-table* path; SqlPackage handles the *forward schema deploy from source-controlled DDL* path. Different jobs, different tools, no overlap.
- Cross-server transfer (Source → Target both online) — DataPorter already does this; not enhanced.
- GUI front-end. CLI only.
- Schema diff. Use `SqlPackage /Action:DeployReport` for that.
- Replication / CDC / CT support.

## 10. Acceptance criteria

E is complete when:
- `dataporter <cfg> --action export --type data --format dat` produces a per-table `.dat` + `.fmt` that round-trips byte-perfect via `--action import`.
- `dataporter <cfg> --action export --type data --format json` produces `.ndjson` + `.types.json` that round-trips byte-perfect (including binary columns via base64).
- `dataporter <cfg> --action export --type schema` produces `.sql` files that include CREATE TABLE + every index + every FK + every trigger + every constraint.
- All three formats import successfully into a fresh DB.
- DataPorter's own test suite has one round-trip test per format per common SQL type (int, bigint, nvarchar, uniqueidentifier, datetime2, datetimeoffset, varbinary, geometry).
- `BackupAndRestoreData.ps1` can be deleted from x360ce with no loss of capability.

## 11. Order with respect to B, C, D

```
E starts immediately (parallel with x360ce B). E lives in the DataPorter repo.

B-current-plan: ships with custom SQL seed (`scripts/db/seed/*.sql`) AND uses
   DataPorter-current (CSV) where applicable. BackupAndRestoreData stays.
   → Acceptable interim if you need to ship B before E lands.

B-after-E: replace custom SQL seed with DataPorter `--format csv` (or DAT for
   binary columns if/when added). Add `Backup-TestDb.ps1` / `Restore-TestDb.ps1`
   that wrap DataPorter. Delete `Data/Change Scripts/Backup/`.

C: depends on B.

D: depends on C.
```

User choice (gated): does B-current ship now (with the legacy in place), or does B wait for E? Default if no choice given: B ships now; B is revised after E lands.

## 12. Spec self-review

- [x] Goal stated in one sentence (§1).
- [x] No TBDs.
- [x] Capability gap table is exhaustive vs current DataPorter source (read 2026-05-16: Program.cs, Config.cs, Exporter.cs, Importer.cs).
- [x] All three formats specified with concrete file layouts and binary-column handling.
- [x] Out-of-scope items called out so future enhancements don't sneak in.
- [x] Acceptance criteria are testable.

## 13. Open question

- **Does DataPorter already have a test suite?** I haven't read the DataPorter test files (if any). E should add round-trip tests; if a Pester/MSTest harness exists, extend it. If not, scaffold one. Will resolve on implementation.

## 14. Next steps

1. User reviews this design (only after agreeing this is the right direction — see §11 about B's order).
2. Invoke `writing-plans` for E's plan in the DataPorter repo (path: `D:\Projects\Jocys.com\Sql\DataPorter\docs\plans\enhancement-2026-05.md` or wherever DataPorter prefers).
3. Implement E (DataPorter changes).
4. Revise x360ce B plan to use enhanced DataPorter; delete `Data/Change Scripts/Backup/`.
5. Continue with C, D.
