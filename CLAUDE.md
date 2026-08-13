# CLAUDE.md

## Project

Guillemets — a logicless, markdown-aware template engine for non-technical
authors. `«»` (U+00AB/U+00BB) are the sole delimiters, chosen because they never
collide with markdown and are easy to type via AltGr (e.g. Turkish keyboards).

**`docs/specs.md` is the source of truth for behavior**, authoritative over this
file — resolve spec ambiguities there alongside the code change, don't just
patch around them. It's runtime-agnostic: it defines the template language
itself, including the filter *mechanism* and the `join`/`join last` filters
it guarantees, but deliberately not other filters (what exists, how each
formats output), since those wrap whatever the host runtime provides.
`docs/implementations/dotnet.md` is the source of truth for this .NET
implementation's own behavior on top of that (currently its `date`/
`currency`/`truncate` filters, plus any .NET-specific notes on `join`/
`join last`) — same rule applies, resolve ambiguities there. A port to
another runtime gets its own file under `docs/implementations/`, not edits
to `specs.md` or this one.

**Cold start?** Read `PLAN.md` first for implementation status and remaining
milestones, then `docs/architecture.md` for how the engine is actually built.
This file is the durable *how to work here*; `PLAN.md` is the living *what's
left* (it shrinks as milestones complete); `docs/architecture.md` is the durable
*how it's built*.

This file and `PLAN.md` are agent/contributor working files, not published
documentation — neither should be linked from `README.md` or anything under
`/docs`. The published docs are `README.md` (basic) and `/docs` (`specs.md`,
`architecture.md`, `implementations/dotnet.md` and any future per-runtime
sibling — lowercase, no reference back to this file or `PLAN.md`).

**`docs/architecture.md` is written for humans, not as a terse internal note.**
Short sentences. A mermaid diagram for any structure that's easier to see than
to read (a pipeline, an interface with several implementations, a dependency
direction). When updating it, rewrite the affected section in that style rather
than appending a dense paragraph — don't let it drift back into long compound
sentences full of parentheticals.

**Every `.md` file in this repo is hard-wrapped at 80 columns**, prose filled
greedily (a short line only when the next word genuinely wouldn't fit, or the
line is inside a fenced code block/table/heading, which stay untouched). When
editing a paragraph or list item, reflow the whole thing rather than patching
in place — don't leave a ragged line just because only its own text changed.
Real markdown links (`[text](path)`) for cross-references between published
docs (`README.md`, `/docs/*`); plain backtick-quoted filenames (no link
syntax) inside `CLAUDE.md`/`PLAN.md` themselves.

## Stack

C#/.NET, targeting `net10.0`. Layout:
- `/src/Guillemets` — the class library. `/test/Guillemets.Tests` — NUnit test
  project.
- `/specs` — the fixture corpus, the acceptance contract. Don't edit fixtures to
  make a test pass; if one looks wrong, fix it deliberately and say why. If
  satisfying a fixture demands disproportionate parser/engine complexity, check
  whether the fixture's *template* (not just its data/expected output) is shaped
  awkwardly before adding permanent special-casing — the same capability can
  often be exercised with a more natural template, and that's usually the better
  fix. Each case is a flat file pair or triple sharing a basename in a numbered
  group folder: `.guil.md`/`.md` (template/expected output) for success, or
  `.guil.md`/`.error` (expected exception message) for cases that must throw
  `TemplateParseException`, plus an optional `.json` data file — omit it and the
  case renders against `{}`, which is the common case for anything that doesn't
  touch data (parse-error cases, plain literal text). Several cases can share
  one template by giving the template just the group number and suffixing each
  case's `.md`/`.error` (and `.json`, if present) with a letter
  (`005-nested-blocks.guil.md` + `005a-...`/`005b-...`); `SpecTests.cs`
  matches a case to its template by leading digits. Group folders are numbered
  on disk for sort order only — refer to fixtures by name in prose, not number.
- `Guillemets.slnx` at repo root (.NET 10's default `dotnet new sln` format).
- Central package management: `Directory.Packages.props` (versions) +
  `Directory.Build.props` (shared `TargetFramework`/`LangVersion`/
  `Nullable`/etc.), both at repo root.
- Assertions use **Shouldly**, not NUnit's `Assert.That`.
  PascalCase-of-space-words resolution uses **Humanizer.Core**'s
  `.Dehumanize()`, not hand-rolled splitting.

## Core concepts

(Full detail in `docs/specs.md` — this is a map, not a replacement.)

- **Delimiters**: `«»`. A single `«»` is an inline variable/token; a run of two
  or more (`««`, `«««`, ...) opens a block, closed by the exact same run length
  or `TemplateParseException` is thrown — depth beyond 2 is cosmetic (fixtures
  go one guillemet deeper per nesting level, for readability, not because the
  parser requires it), validated via
  `OpenBlockToken.Depth`/`CloseBlockToken.Depth` in `Parser.ParseBlock`.
- **Property access**: `:` drills into objects and projects over lists
  (`.Select()`); chained across lists it flattens (`.SelectMany()`).
- **Blocks**: `««name` ... `»»`. Behavior is inferred from the resolved type of
  `name` — boolean → if, list → loop, object → scope. No keywords, same syntax
  for all three. Variable lookup falls back to enclosing scopes.
- **Else**: `~` on its own line splits truthy/falsy (or non-null/null) branches
  inside a block.
- **Magic loop variables**: `«first»`, `«last»`; `!` negates any boolean.
- **Variable definitions**: `««name = expr` ... `»»` captures a block's rendered
  output (or resolved value) into `name` for reuse below, under the same
  type-inferred if/loop/scope rules.
- **Tables**: a block may open/close with a leading/trailing `|` so it stays
  valid inside a markdown table row.
- **Inline lists**: scalar lists auto-join with `, `; override via the
  `join`/`join last` filters, usable inline or as the last line of a loop
  block.
- **Filters**: `name: value` chained with ` | ` after a property chain or
  another filter, no parens — `«expr | filter: value»`. `: ` (colon+space)
  is a fixed token, same as property access; nothing after it is trimmed.
  `\` escapes a reserved character. Built-ins: `date`, `currency`, `truncate`,
  `join`, `join last`.

## Localization / naming

Templates are authored with natural, space-separated words — the author's
business vocabulary. Models are defined by developers in PascalCase/camelCase —
the developer's code vocabulary. Where the two don't match (e.g. "quote no" vs.
`OfferNo`), a schema mapping bridges them:
`Localized Term = template token = PropertyName`, resolved case-insensitively
against the default language. See "Schema & Localization" in `docs/specs.md`.

## C# code style

- `using` directives sorted alphabetically (no special-casing `System.*`);
  `using static` directives form their own group below, separated by a blank
  line.
- A boolean expression that doesn't fit on one line breaks with `&&`/`||` at
  the *end* of each line, not the start; a closing `)` that ends up alone gets
  its own line, the same way a closing `}` would
  (`if (!FilterParser.TryParse(expectLeadingPipe: false, out var pipeline) ||\n
  _tokens.AtEnd ||\n    _tokens.Current is not CloseBlockToken\n)` in
  `BodyParser.TryParseFooter`).
- Never write `private` explicitly — it's the default.
- Keep whitespace between statements minimal — no blank-line padding between
  unrelated statements.
- One type per file.
- C# namespace lookup already sees a type's *ancestor* namespaces without an
  explicit `using` (code in `Guillemets.Data.Json` sees `Guillemets.Data` and
  `Guillemets` for free). Exploit this deliberately for a public extension
  method meant to be broadly discoverable: `Template`'s `Render`/`RenderObject`
  extensions (`JsonElementExtensions.cs`/`PocoExtensions.cs`/
  `JTokenExtensions.cs`, one per adapter folder under `/src/Guillemets/Data`)
  live in the bare root `Guillemets` namespace, *not* nested under their
  adapter's own namespace (`Guillemets.Data.Json`/`Guillemets.Data.Poco`/
  `Guillemets.Data.Newtonsoft`, where the adapter types
  `JsonElementDataSource`/`PocoDataSource`/`JTokenDataSource` themselves stay) —
  so any consumer who already has `using Guillemets;` for `Template` gets the
  extension methods for free, no extra `using` needed. `Render(JsonElement)` and
  `Render(JToken)` are plain overloads of one name — their parameter types are
  concrete and unrelated, so there's no ambiguity. `RenderObject(object)` keeps
  its own name instead of also being called `Render`: `object` is broad enough
  that folding it into the same overload set would blur which one a call
  actually hits. A future adapter should follow the same split: adapter type in
  its own `Guillemets.Data.X` namespace, extension method named `Render` in the
  bare root `Guillemets` namespace if its parameter is a concrete type. All
  adapters — including Newtonsoft's `JTokenDataSource` — live in the one core
  `Guillemets` package; there's no per-adapter sibling project (decided when
  `JTokenDataSource` was added: one package is simpler while there's only a
  handful of adapters, and `Newtonsoft.Json` isn't a heavy dependency to carry).
- Prefer polymorphic dispatch (base type + virtual/abstract method, or a
  strategy class per type) over a `switch`/pattern-match implementing per-type
  behavior inline — the behavior must live in its own class, not in the switch
  arms. A `switch` that only *selects* between already-implemented strategies is
  fine. Doesn't apply to a genuinely stateful, sequential parser walking a token
  stream — that's normal parser-writing.
- Don't call `new SomeType(...)` inside a constructor body unless `SomeType` is
  a DTO or `record`. Real dependencies are constructor-injected and wired up at
  the composition root (`Template.Render`, `Tokenizer.Tokenize()`,
  `Parsing/Parser.cs`). When two collaborators need each other (e.g.
  `BodyParser` dispatches to `BlockParser`, `BlockParser` recurses back into
  `BodyParser`), don't resolve the cycle with a mutable/settable field assigned
  after construction — use a `Lazy<T>`-backed field resolved through
  `ParserRegistry.GetLazy<T>()` (a private property of the same name exposes
  `.Value`), applied uniformly to *every* registry-sourced collaborator, not
  just the ones that are actually circular, so registration order in
  `Parser.cs` never becomes a hazard. (`TokenCursor.Rewind`-based speculative
  parsing — try an interpretation, rewind and fall back if it doesn't pan
  out — is a legitimate alternative for this general class of problem too;
  don't treat its absence from the current code as a decision against it.)
- A type whose only externally-relevant contract is a single interface (nothing
  about the concrete type should be called directly from outside it) implements
  that interface explicitly rather than with a `public` method of the same name
  (`string ISomeInterface.Method(...)`, not `public string Method(...)`). If
  the type's own internals still need to call that logic directly (without
  going through an interface-typed reference), keep a plain private method next
  to the explicit implementation and have the explicit member forward to it.
  Don't keep an interface around once nothing actually needs it
  polymorphically — a single-implementation interface that exists only to
  hand a not-yet-fully-constructed `this` to a collaborator is unnecessary,
  since `this` is already a valid, fully-typed reference at that point.
- Inheritance clause (`: Base`/`: IInterface`) on a type with a primary
  constructor: put it on its own indented line when the constructor's
  parameter list fits on one line
  (`internal class LoopBehavior(Scope _scope, IReadOnlyList<IDataSource> _items)\n    : IBlockBehavior`);
  let it trail the closing `)` on the same line when the parameter list
  already spans multiple lines
  (`internal record BlockNode(PropertyChainNode Properties, ...\n) : IRenderable`)
  — the parameter list dictates whether the type name and the base type can
  already be told apart at a glance without the extra line. This applies no
  matter how short the parameter list is — even a single parameter still
  forces the inheritance clause onto its own line, and `record` types follow
  it exactly like `class` types
  (`public record JsonElementDataSource(JsonElement Element)\n    : IDataSource`,
  not `public record JsonElementDataSource(JsonElement Element) : IDataSource`).
  A type with *no* primary constructor keeps `: Base` on the declaration line
  regardless of length (`internal class UndefinedDataSource : IDataSource`) —
  this rule only ever triggers once there's a parameter list to compete with
  the base type for attention.
- A constructor or record-creation call with more than 2 optional/named
  parameters is never written on one line, even when it would fit — break to
  one parameter per line, the same shape a primary constructor's own parameter
  list uses once it goes multi-line (see above):
  `new Scope(Items[i],\n    Parent: Scope,\n    IsFirst: i == 0,\n    IsLast: i == Items.Count - 1\n);`
  in `LoopBehavior`, not all four arguments crammed onto one line.
- Expression-bodied **methods** (including constructors) put the `=>` at the end
  of the signature line and the expression on its own indented line below, even
  when it would fit on one line (`public bool AsBoolean() =>\n    Value;`, not
  `public bool AsBoolean() => Value;`) — consistent regardless of expression
  length. Expression-bodied **properties** are the opposite: keep `=>` and the
  expression inline on the same line as the property
  (`public DataKind Kind => DataKind.Boolean;`), including when the expression
  itself spans multiple lines via `switch`
  (`public DataKind Kind => Value switch\n {\n    ...\n};` — the `switch`
  keyword stays on the `Kind =>` line). The distinguishing signal is the
  parameter list: has `()` → method formatting; no `()` → property formatting.
- Never write `sealed` — explicit house style; types stay open for inheritance
  even with no current subtypes.
- Naming (see `.editorconfig`): private instance fields are `_camelCase`; any
  `static` field, regardless of accessibility, is `SCREAMING_CASE` (a custom
  rule, since standard "static fields start uppercase" conventions would
  otherwise conflict with the private-field rule).
- A character/string literal that carries meaning beyond its own face value —
  a delimiter, a sentinel, a syntax marker — gets a named `SCREAMING_CASE`
  constant instead of being inlined at each use site, e.g. `Position.NEWLINE`
  for `'\n'`, `LoopBehavior.TABLE_ROW_DELIMITER` for the `'|'` that marks a
  loop body as a markdown table. A literal used only for its own sake (an
  error message, arbitrary test data) doesn't need this.
- Write small, single-purpose methods from the start, not as a later cleanup
  pass — factor out a repeated multi-line sequence immediately. Prefer a plain
  private method over a local function closing over another method's locals.
- Give a stateful, sequential scanner (a cursor, a parser) private instance
  fields only for state that must persist *across* separate method calls (e.g.
  `TokenCursor._position`, since `Parser` drives it call by call). When one
  method owns an entire scan start to finish, plain locals are the better fit
  (e.g. `Tokenizer.Tokenize()`).
- Avoid tuples/small one-off DTOs used purely to shuttle two or three values
  between methods. A success/failure method returns `bool` (mutating instance
  state as a side effect); a method that needs to hand back one meaningful value
  returns that value directly, typed explicitly. A method with one primary
  return value plus an optional secondary one uses an `out` parameter for the
  secondary value instead of wrapping both in a record
  (`PropertyChainParser.Parse(Position openPosition, bool stopAtNewline, out
  string? variableName)` returns the `PropertyChainNode` directly and hands
  the captured variable name back via `out`, rather than a
  `BlockHeader(string?, PropertyChainNode)` DTO) — matches the pre-existing
  `SymbolTree.TryMatchSymbol`/`Scope.TryGetMagic` idiom already in this
  codebase.
- No comments in source. If code needs one to be understood, that's a signal to
  restructure — extract a well-named method, turn an encoded string/boolean
  convention into a properly-named type or property — not to narrate it in
  prose. Applies to WHY-comments too, not just WHAT-comments.
- Fix a bug in the component that actually owns the relevant knowledge, not by
  compensating with a heuristic wherever the symptom happened to surface — if a
  fix requires guessing at another layer's shape or invariants, the guess
  belongs in that layer instead. Relatedly, a method shouldn't reach into a
  caller's shared/mutable state (e.g. casting a cursor's `Current`) to get what
  it needs — take it as an explicit parameter, even if the caller has to compute
  it first. Same principle in reverse: don't get an object back from a call and
  then poke its fields/methods yourself to finish the job — pass along what the
  callee needs so it mutates its own state itself.
- Never use the `!` null-forgiving operator — it silences the compiler instead
  of resolving the issue, defeating the point of `Nullable`
  (`Directory.Build.props`). Follow the house nullable guide:
  <https://github.com/mouseless/learn-dotnet/blob/main/nullable-usage/README.md>.
  When a value is nullable by type but a real invariant guarantees it isn't null
  at some point, use `?? throw new InvalidOperationException(...)` instead — it
  fails loudly at the point of use if the invariant is ever broken, rather than
  risking a `NullReferenceException` downstream.

## Working on this repo

- Run `dotnet test` from the repo root for the full fixture suite — each fixture
  becomes one NUnit test case, named by its relative path under `/specs`.
- Engine work proceeds fixture-group by fixture-group, simplest → most complex —
  implement one group's mechanic, confirm `dotnet test` flips exactly that group
  green with no regressions, then move on.
- **TDD, one fixture at a time.** Pick the smallest next fixture, write only the
  minimal code to pass it, run the full suite to confirm no regressions — then
  actually refactor (correct layering, remove duplication, apply the style rules
  above) rather than leaving cleanup for later. Report and let the fixture's
  author/reviewer weigh in before moving to the next one.
- **When the user says "reviewed" (with no further detail), grep the touched
  files for `TODO` before doing anything else.** The user's review workflow is
  to read the diff and leave inline `// TODO ...` comments marking what they
  want changed, rather than typing it all out in chat. If any are found,
  address each one (the TODO comment itself gets removed once resolved — it's
  a review note, not documentation) and rerun `dotnet test`; if none are
  found, say so and move straight to wrapping up/parking or continuing, per
  what the user asks next.
- **A redesign spanning multiple fixtures gets its own checkpoint before any
  code changes.** When a change is bigger than one fixture (a grammar
  redesign, a rename, dropping a restriction — anything touching several
  fixtures/docs at once), do the *entire* spec/fixture/doc rewrite first,
  confirm it's red against the still-old engine, then move every touched
  fixture into `IGNORED_FIXTURES` (grouped under one comment pointing at the
  `PLAN.md` milestone, not one comment per fixture) so `dotnet test` is green
  again — and stop there for review. Only after that's confirmed does
  implementation start, back to the normal one-fixture-at-a-time loop above.
- **No failing tests at commit time.** Unimplemented fixtures are listed in
  `SpecTests.cs`'s `IGNORED_FIXTURES` set (`Ignored`, never `Failed`) —
  remove a fixture's name once its case goes green. When a fixture is
  deliberately left unimplemented, leave a comment above its entry explaining
  what's undecided.
- **The build enforces style, not just `dotnet format`.**
  `Directory.Build.props` sets `EnforceCodeStyleInBuild`/
  `TreatWarningsAsErrors`, so `dotnet build`/`dotnet test` fail on any
  `.editorconfig` violation or compiler warning — including `IDE0060` (unused
  parameter), which `.editorconfig` escalates to `error`. That's a non-issue for
  a parameter required by an interface signature (Roslyn exempts
  interface-implementation methods from `IDE0060` automatically, implicit or
  explicit) — it only bites a parameter that's unused and has no such
  contractual reason to exist.
- Known flaky build issue: `MSB3374` (can't set last-write-time on an
  `obj/**/*.Up2Date` file) — not a real problem, just retry once.

## Parking (ending a session)

When the user says they're "parking" (wrapping up for the day):

1. Run `dotnet test`, confirm all-green — flag clearly if not; don't park on
   red.
2. Update `PLAN.md`: refresh status/fixture count, remove completed work from
   "Remaining milestones" (don't just annotate it done — delete it, this file
   shrinks), add new "Known v1 scope decisions."
3. Update `docs/architecture.md` with any structural change from this session
   (new types, moved namespaces, a resolved design decision) — keep it
   describing current shape only, not a changelog.
4. Update `CLAUDE.md` with any durable convention/rule/decision from this
   session — these three files are what survive to a cold start elsewhere;
   nothing load-bearing should live only in chat history.
5. Give a short summary: what's done, what's next, anything to double-check.

## Git

Read-only `git` commands (`log`, `diff`, `show`, `status`, `blame`, etc.) are
fine to run directly. Never run a `git` command that writes (`add`, `commit`,
`push`, `checkout`, `reset`, etc.) — this process has no write permission on
`.git` anyway, so it would fail. The user handles all of git themselves; don't
prepare commands for them or remind them about pending git tasks.
