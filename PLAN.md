# Implementation plan

Living roadmap for building the guillemets engine against the `/specs` fixture
corpus. This file shrinks as milestones complete — it's *what's left*, not a
history of what's done. Agent/contributor working file, not published
documentation — see `README.md`/`docs/` for that. For *how* it's built, see
`docs/architecture.md`; for *how* to work (TDD discipline, code style), see
`CLAUDE.md`.

## Status

`dotnet test` is green: 183 passed, 0 skipped, 0 failed. Language/implementation
milestones are done. A round of external review (bug/perf/packaging audit)
surfaced 30 confirmed issues that need fixing before release; every item was
independently verified against source (exact file/line, not just reported)
before being added here. Priorities adjusted per author call: POCO reflection
caching deprioritized (production runs on JSON, not POCO), net8
multi-targeting and the Newtonsoft package split are both skipped for now.

## Remaining milestones

### P0 — silent/data-corrupting bugs (fix first)

- Property lookup is case-sensitive in all three data adapters
  (`JsonElementDataSource`, `PocoDataSource`, `JTokenDataSource`), but
  `docs/specs.md` claims case-insensitive resolution — that's only true via
  `Glossary`'s `Dehumanize()` fallback (PascalCase), so camelCase JSON (the
  ASP.NET Core default) silently resolves to nothing. Fix once in a shared
  helper used by all three adapters. While in there: drop the `Humanizer.Core`
  dependency — hand-roll `Dehumanize`/`Humanize(LowerCase)` in a small utility
  (used by `Glossary`, `VariableStore`, `FilterRegistry.NameFor`); folds in
  the per-lookup Humanizer allocation win from the P2 `Dehumanize()` caching
  item below.
- Merge the ~16 token record types under `src/Guillemets/Tokens/` (`OpenToken`,
  `CloseToken`, `CloseBlockToken`, `PipeToken`, ...) into one `Token`
  struct/class with a `TokenKind` enum and offsets into the source string.
  Removes the unconditional per-token substring allocation in `Tokenizer.cs`
  (kept even for token kinds like `OpenToken` that discard the text) and
  replaces scattered `is OpenToken`/`is CloseToken` type-pattern dispatch with
  an exhaustive switch over `TokenKind`.

### P1 — correctness bugs (parser/render)

- A block whose final `»»` has no trailing newline never closes (`Symbols`
  only registers `CloseBlock` as `»»` + `\n`) — wrong error, wrong location.
- `~` (else) isn't required to be at line start (`BodyParser.ReachedElse` has
  no `AtLineStart` guard, unlike `ValidateNotSharingCloseLine`) — a stray
  trailing `~` anywhere silently truncates the truthy body.
- Stack overflow on long guillemet runs — `SymbolTree.ExtendMatch` recurses
  once per matched char via the `repeat:true` self-loop; ~100K consecutive
  `»` crashes the process. Convert to iterative matching.
- `FilterRegistry.NameFor` does an unguarded `typeof(T).Name[..^6]` — throws
  for filter class names shorter than 6 chars, mis-slices names not ending in
  `Filter`. Also `where TFilter : IFilter, new()` blocks DI-constructed
  custom filters.
- Filtered-item-scope degrades silently to a whole-list truthy check if any
  item's flag isn't a boolean (common with sparse JSON), and only handles
  single-segment chains — multi-level chains like `«quotes: prices: active»`
  never filter despite the spec's general rule.
- POCO type mapping gaps: `DateTime`/`Guid`/enum fall through to
  `DataKind.Object` instead of being scalar/presence values; `IDictionary`
  matches `IEnumerable` first so dictionaries enumerate as `KeyValuePair`
  arrays.
- `Template.Render`'s CRLF normalization (`rendered.Replace("\n", "\r\n")`)
  is global — it rewrites `\n` embedded inside rendered *data* values too,
  not just template structure.
- `truncate` splits UTF-16 surrogate pairs (`value[..maxLength]`), and a
  bare/non-numeric `truncate` throws an unwrapped exception at render with
  no position — same unwrapped-exception asymmetry the currency/date filters
  had before their culture-round-trip fix.
- Registering a custom filter can silently change existing template output —
  a final body line glued to `»»` matching a filter name is consumed as a
  footer pipeline instead of body text. Built-in filter names are
  effectively reserved as a last body line, retroactively for new custom
  filters too.

### P2 — obvious performance issues (render-time, scale with row count)

- `Dehumanize()` runs on every uncovered lookup in `Glossary` and
  `VariableStore`, uncached — chain segments are fixed at parse time, so
  resolve once per (chain, culture) and cache on the node. (Partially
  subsumed by the case-sensitivity fix's Humanizer removal above — re-check
  remaining cost once that lands.)
- Resolution happens twice: `BlockNode.ResolveBehavior` re-resolves after
  `TryResolveLoopItems` already did a full resolution on the non-array path;
  `Scope.HasProperty` does a `TryGetProperty` that `Project` immediately
  repeats. A nested-loop variable pays for this at every scope level.
- `JoinFilter` double-enumerates its lazy pipeline
  (`values.Any() ? [...] : values`) — the entire upstream resolution chain
  runs twice. Materialize once.
- String building is O(nesting) copies — `IRenderable.Render` returns
  `string` and every loop item/conditional/scope materializes its own
  `StringBuilder`, copied again into the parent. Thread one `StringBuilder`
  through the render call chain instead.
- Tokenizer scans char-by-char for non-matching text (dictionary probe per
  char, no `SearchValues`/`IndexOfAny` fast skip) and allocates a substring
  for every symbol token unconditionally, even when the token discards text.
- `BodyParser.TryParseFooter` speculatively allocates (`List<FilterNode>`,
  `StringBuilder`, result records) on every line inside every block before
  rewinding on failure, with no cheap pre-check.

### P3 — release readiness (packaging/process)

- No CI at all — no `.github/` directory. "175 tests green" is currently
  unverified by anything but the author's machine. Add a GitHub Actions
  workflow (build + test on push/PR) first — it de-risks every fix above.
- No package metadata in `Guillemets.csproj` (`PackageId`, `Description`,
  `Authors`, `PackageLicenseExpression`, `RepositoryUrl`, version).
- `Glossary.CACHE` is a static, unbounded `ConcurrentDictionary` keyed on
  `(IStringLocalizer?, culture)`. Since `IStringLocalizer<T>` is typically
  scoped/transient in ASP.NET Core, this leaks in the intended host. Key on
  something stable or use `ConditionalWeakTable`.
- `GenerateDocumentationFile`/`TreatWarningsAsErrors` are on but zero public
  APIs have XML docs (`Template`, `IFilter`, `IDataSource`, `FilterRegistry`,
  `ParseOptions`). Add docs to the public surface or turn the flag off until
  they exist.
- README doesn't document that `Template` is safe to reuse across threads
  (immutable AST, stateless filter singletons, fresh state per `Render`
  call) — real, good property, currently undocumented. Pure doc addition.
- `make init` sudo-runs an unpinned script off `main` with no checksum. Pin
  to a tag/commit + checksum.
- No plain-number-formatting filter. Surfaced while fixing the culture
  round-trip bug: making `PocoDataSource.AsDisplayString()` invariant means
  plain `«amount»` (no filter) now renders flat invariant text (`1234.5`)
  instead of locale-formatted text, and the only filter that adds
  grouping/decimals is `currency`, which also forces a currency symbol
  prefix — there's no way to get `1,234.50` without also getting
  `$1,234.50`. Add a `number` filter (e.g. `| number: N2`) to close the gap.

### Explicitly deferred (not this pass)

- POCO reflection is uncached (`PocoDataSource.TryGetProperty` calls
  `GetType().GetProperty(name)` every access). Deprioritized by author call —
  production runs on the JSON adapter, not POCO, so this isn't on the hot
  path. Revisit if POCO usage becomes real.
- `net10.0`-only target excludes net8 LTS users — multi-targeting
  `net8.0;net10.0` skipped for now (author call).
- Newtonsoft.Json as a hard core dependency / splitting into
  `Guillemets.Newtonsoft` — skipped for now (author call); package-shape
  decision to revisit later.
- The larger architectural rewrite proposed in review (line-oriented grammar
  restructuring, explicit `Bind()` step, parser/AST consolidation) — real
  value, but a redesign, not a bug/perf pass. Revisit once this backlog ships.
- Friendly parse/render diagnostics (source-context error messages) — a
  feature, not a bug fix.
- Benchmark project / fuzz target — recommended before making performance
  claims, not a blocker for fixing the perf issues already identified.
- `IDataSource`/`IFilter` contract inconsistencies across adapters (e.g.
  `EnumerateArray()` throwing vs. returning empty) — worth a pass once the
  case-sensitivity fix touches all three adapters anyway.
