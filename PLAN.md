# Implementation plan

Living roadmap for building the guillemets engine against the `/specs` fixture
corpus. This file shrinks as milestones complete — it's *what's left*, not a
history of what's done. Agent/contributor working file, not published
documentation — see `README.md`/`docs/` for that. For *how* it's built, see
`docs/architecture.md`; for *how* to work (TDD discipline, code style), see
`CLAUDE.md`.

## Status

`dotnet test` is green: 220 passed, 0 skipped, 0 failed. Language/implementation,
P1, and P2 milestones are done. 18 issues remain before release — see P3 and
Explicitly deferred below.

## Remaining milestones

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
- Remove any statement that claims «» is easy to type in docs. Docs should
  rather admit that it's a sacrifice for readability over writability.
  - This can be defended that no body writes by hand any more, AI writes docs
    any way, but reading is more crucial at the age of GENAI
- `make init` sudo-runs an unpinned script off `main` with no checksum. Pin
  to a tag/commit + checksum.

### Explicitly deferred (not this pass)

- `Scope.HasProperty` (inside `FindOwner`) does a `TryGetProperty` whose
  result it discards, just to answer "does this scope own it" — `Project`'s
  first-segment lookup immediately repeats the same call to get the actual
  value. Fixing this means changing `FindOwner`'s contract to hand back the
  already-resolved value, not just which scope owns it — not as cheap as it
  looks, needs a separate design pass.
- `BodyParser.TryParseFooter` speculatively allocates (`List<FilterNode>`,
  `StringBuilder`, result records) on every line inside every block before
  rewinding on failure, with no cheap pre-check — not as cheap as it looks,
  needs a separate design pass.
- String building is O(nesting) copies — `IRenderable.Render` returns
  `string` and every loop item/conditional/scope materializes its own
  `StringBuilder`, copied again into the parent. Threading one `StringBuilder`
  through the render call chain instead would fix it, but touches the
  `IRenderable`/`IBlockBehavior` interfaces and all 8 implementers — too
  large for a single reviewable pass, and `BlockNode`'s footer-filter path
  needs each loop item as a separate string anyway (`join`/`truncate` operate
  on `IEnumerable<string>`), so a naive shared-builder doesn't fully replace
  it. Needs a real design pass, not a quick fix.
- Friendly parse/render diagnostics (source-context error messages) — a
  feature, not a bug fix.
- Benchmark project / fuzz target — recommended before making performance
  claims, not a blocker for fixing the perf issues already identified.
- `net10.0`-only target excludes net8 LTS users — multi-targeting
  `net8.0;net10.0` skipped for now (author call).
- Newtonsoft.Json as a hard core dependency / splitting into
  `Guillemets.Newtonsoft` — skipped for now (author call); package-shape
  decision to revisit later.
- The larger architectural rewrite proposed in review (line-oriented grammar
  restructuring, explicit `Bind()` step, parser/AST consolidation) — real
  value, but a redesign, not a bug/perf pass. Revisit once this backlog ships.
- POCO reflection is uncached (`PocoDataSource.TryGetProperty` calls
  `GetType().GetProperty(name)` every access). Deprioritized by author call —
  production runs on the JSON adapter, not POCO, so this isn't on the hot
  path. Revisit if POCO usage becomes real.
