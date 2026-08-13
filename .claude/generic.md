# Generic (.NET)

Reusable across any .NET repo as-is — this file is meant to stay
identical everywhere it's used, kept in sync via `cihandeniz/config-files`.
Anything specific to *this* project lives in `.claude/specific.md`
instead; nothing project-specific belongs here.

## Project scaffolding

A repo following this convention has three durable-vs-living docs, each
with one job: `CLAUDE.md` (plus `.claude/generic.md`/`.claude/specific.md`)
is the durable *how to work here*; a `PLAN.md` is the living *what's left*
— it shrinks as milestones complete, and only ever tracks actionable
remaining work, never a changelog; an architecture doc is the durable
*how it's built*, written for humans (short sentences, a diagram for any
structure that's easier to see than to read), rewritten section-by-section
as it changes rather than accumulating dense appended paragraphs. These
are agent/contributor working files, not published documentation — don't
link them from published docs (a README, a docs site).

**Every `.md` file meant as an internal working doc (this file, a `PLAN.md`,
an architecture doc) is hard-wrapped at 80 columns**, prose filled greedily
(a short line only when the next word genuinely wouldn't fit, or the line
is inside a fenced code block/table/heading, which stay untouched). When
editing a paragraph or list item, reflow the whole thing rather than
patching in place — don't leave a ragged line just because only its own
text changed. Real markdown links (`[text](path)`) for cross-references
between *published* docs; plain backtick-quoted filenames (no link syntax)
inside internal working docs themselves.

## Working habits

- **TDD, one test case at a time.** Pick the smallest next case, write
  only the minimal code to pass it, run the full suite to confirm no
  regressions — then actually refactor (correct layering, remove
  duplication, apply the style rules below) rather than leaving cleanup
  for later. Report and let the case's author/reviewer weigh in before
  moving to the next one. This ordering holds even when a task feels
  time-boxed or urgent — perceived pressure (including urgency carried
  over from an earlier, unrelated request) is never license to write the
  code fix before a failing test exists to prove it's needed.
- **After a broad rewrite/rename/migration touching many files,
  proactively audit for leftovers before reporting it done** — grep the
  whole affected tree for the old pattern/name being replaced, and
  cross-check that everything the change touches is reflected in whatever
  tracking mechanism exists (an ignore/skip list, `PLAN.md`), rather than
  waiting to be asked "did you get all of them?" and only auditing then.
- **When the user says "reviewed" (with no further detail), grep the
  touched files for `TODO` before doing anything else.** Their review
  workflow is to read the diff and leave inline `// TODO ...` comments
  marking what they want changed, rather than typing it all out in chat.
  If any are found, address each one (the TODO comment itself gets
  removed once resolved — it's a review note, not documentation) and
  rerun the test suite; if none are found, say so and move straight to
  wrapping up/parking or continuing, per what the user asks next.
  Exception: before implementing a TODO (or any terse instruction), check
  whether it would silently undo something deliberate and already
  decided/documented earlier in the *same* conversation — TODOs are terse
  and don't carry that context back. If it would, lay out the specific
  tension plainly (what breaks, why) and ask how to resolve it rather
  than silently complying or silently ignoring it.
- **A redesign spanning multiple test cases gets its own checkpoint
  before any code changes.** When a change is bigger than one case (a
  grammar redesign, a rename, dropping a restriction — anything touching
  several cases/docs at once), do the *entire* spec/test/doc rewrite
  first, confirm it's red against the still-old code, then move every
  touched case into whatever ignore/skip mechanism the test framework
  offers so the suite is green again — and stop there for review. Track
  the milestone and its remaining cases in `PLAN.md` rather than a
  comment in the test source. Only after that's confirmed does
  implementation start, back to the normal one-case-at-a-time loop above.
- **No failing tests at commit time.** Unimplemented cases are marked
  Ignored/Skipped, never Failed — remove a case's ignore entry once it
  goes green. When a case is deliberately left unimplemented because
  something about it is genuinely undecided, note what's undecided in
  `PLAN.md` (under the relevant milestone) rather than a comment above
  its entry in the test source.
- **When a new feature raises a "what if X doesn't exist / isn't there"
  question, default to consistent absence-propagation over introducing a
  new hard restriction.** Check first whether the system already has a
  graceful, consistent answer for missing/absent state (e.g. it already
  degrades gracefully somewhere similar) before adding a new failure
  mode. Reserve a real hard failure for a genuine *structural/syntax*
  problem, not for *data* that might simply be absent.
- If the project enables build-time style enforcement (e.g. .NET's
  `EnforceCodeStyleInBuild`/`TreatWarningsAsErrors`), expect `dotnet
  build`/`dotnet test` to fail on any `.editorconfig` violation or
  compiler warning — including `IDE0060` (unused parameter) when
  escalated to `error`. That's a non-issue for a parameter required by an
  interface signature (Roslyn exempts interface-implementation methods
  from `IDE0060` automatically, implicit or explicit) — it only bites a
  parameter that's unused and has no such contractual reason to exist.
- Known flaky MSBuild issue in this sandbox: `MSB3374` (can't set
  last-write-time on an `obj/**/*.Up2Date` file) — not a real problem,
  just retry the build once.

## C# code style

- `using` directives sorted alphabetically (no special-casing `System.*`);
  `using static` directives form their own group below, separated by a blank
  line.
- A boolean expression that doesn't fit on one line breaks with `&&`/`||` at
  the *end* of each line, not the start; a closing `)` that ends up alone gets
  its own line, the same way a closing `}` would.
- Never write `private` explicitly — it's the default.
- Keep whitespace between statements minimal — no blank-line padding between
  unrelated statements.
- One type per file (a tightly-coupled nested helper type, like a builder
  or a method object, can share its owner's file).
- C# namespace lookup already sees a type's *ancestor* namespaces without
  an explicit `using`. A public extension method meant to be broadly
  discoverable can exploit this deliberately by living in a shared root
  namespace rather than nested under its own feature namespace, so any
  consumer who already has a `using` for the root type gets the
  extension for free — see `.claude/specific.md` for how this project
  applies it to its own adapter types.
- Prefer polymorphic dispatch (base type + virtual/abstract method, or a
  strategy class per type) over a `switch`/pattern-match implementing per-type
  behavior inline — the behavior must live in its own class, not in the switch
  arms. A `switch` that only *selects* between already-implemented strategies is
  fine. Doesn't apply to a genuinely stateful, sequential parser walking a token
  stream — that's normal parser-writing.
- Don't call `new SomeType(...)` inside a constructor body unless `SomeType` is
  a DTO or `record`. Real dependencies are constructor-injected and wired up at
  the composition root. When two collaborators need each other, don't
  resolve the cycle with a mutable/settable field assigned after
  construction — use a `Lazy<T>`-backed field resolved through a shared
  registry, applied uniformly to *every* registry-sourced collaborator,
  not just the ones that are actually circular, so registration order
  never becomes a hazard. (`TokenCursor`/cursor-`Rewind`-based speculative
  parsing — try an interpretation, rewind and fall back if it doesn't pan
  out — is a legitimate alternative for this general class of problem too;
  don't treat its absence from the current code as a decision against it.)
- A type that should be built at most once per some key (not per call site)
  gets a plain (no-modifier, so implicitly private per the rule above)
  constructor plus a `public static GetOrCreate(...)` factory backed by a
  `static readonly` cache field — global and thread-safe, not
  per-instance of whatever owns the call site, so unrelated callers
  sharing the same key reuse the same built value. C# has no `private`
  primary-constructor modifier (`class Foo private(...)` doesn't parse)
  — dropping down to a regular constructor is the only way to get this
  shape, and it's the right call whenever a primary constructor's
  brevity would otherwise let external code bypass the cache with `new
  Foo(...)` directly.
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
  parameter list fits on one line; let it trail the closing `)` on the
  same line when the parameter list already spans multiple lines — the
  parameter list dictates whether the type name and the base type can
  already be told apart at a glance without the extra line. This applies
  no matter how short the parameter list is — even a single parameter
  still forces the inheritance clause onto its own line, and `record`
  types follow it exactly like `class` types. A type with *no* primary
  constructor keeps `: Base` on the declaration line regardless of length
  — this rule only ever triggers once there's a parameter list to
  compete with the base type for attention.
- A constructor or record-creation call with more than 2 optional/named
  parameters is never written on one line, even when it would fit — break to
  one parameter per line, the same shape a primary constructor's own parameter
  list uses once it goes multi-line.
- Expression-bodied **methods** (including constructors) put the `=>` at the end
  of the signature line and the expression on its own indented line below, even
  when it would fit on one line (`public bool AsBoolean() =>\n    Value;`, not
  `public bool AsBoolean() => Value;`) — consistent regardless of expression
  length. Expression-bodied **properties** are the opposite: keep `=>` and the
  expression inline on the same line as the property, including when the
  expression itself spans multiple lines via `switch` (the `switch` keyword
  stays on the property's own line). The distinguishing signal is the
  parameter list: has `()` → method formatting; no `()` → property formatting.
- Never write `sealed` — explicit house style; types stay open for inheritance
  even with no current subtypes.
- Naming: private instance fields are `_camelCase`; any `static` field,
  regardless of accessibility, is `SCREAMING_CASE` (a custom rule, since
  standard "static fields start uppercase" conventions would otherwise
  conflict with the private-field rule).
- `[Test]`-attributed method names are `Snake_case` — a plain sentence
  describing the case, only its first letter capitalized, with an
  underscore anywhere the sentence would have a space, comma, or semicolon
  (`Date_filter_formats_with_given_pattern`, not
  `DateFilter_FormatsWithGivenPattern` or
  `date_filter_formats_with_given_pattern`).
- A character/string literal that carries meaning beyond its own face value —
  a delimiter, a sentinel, a syntax marker — gets a named `SCREAMING_CASE`
  constant instead of being inlined at each use site. A literal used only
  for its own sake (an error message, arbitrary test data) doesn't need this.
- Write small, single-purpose methods from the start, not as a later cleanup
  pass — factor out a repeated multi-line sequence immediately. Prefer a plain
  private method over a local function closing over another method's locals.
- Give a stateful, sequential scanner (a cursor, a parser) private instance
  fields only for state that must persist *across* separate method calls.
  When one method owns an entire scan start to finish, plain locals are
  the better fit.
- Avoid tuples/small one-off DTOs used purely to shuttle two or three values
  between methods. A success/failure method returns `bool` (mutating instance
  state as a side effect); a method that needs to hand back one meaningful value
  returns that value directly, typed explicitly. A method with one primary
  return value plus an optional secondary one uses an `out` parameter for the
  secondary value instead of wrapping both in a record. That changes once
  there are genuinely three or more values to hand back, not just one
  secondary alongside the main result: consolidate them into a nested
  result record instead of piling up more `out` parameters, but keep the
  method itself in the same `bool TryXxx(..., out result)` shape — return
  the record via a single `out`, don't switch the method to returning the
  record directly.
- When a code-review comment names a specific refactoring technique by its
  actual term (e.g. "Method Object," "Inappropriate Intimacy" — both from
  Fowler's *Refactoring*), apply that exact technique, not a smaller
  substitute that happens to touch the same lines. Before implementing the
  fix, restate in one sentence what the *named* technique actually does and
  check the planned fix genuinely matches it — not just "touches the same
  symptom." If the comment offers a bracketed/hedged suggestion for a
  detail ("[name can be better]", "or something"), treat that as one
  possible detail, not a substitute for the named technique itself.
- Use target-typed `new(...)` (dropping the repeated type name) wherever
  the compiler can actually infer it — an assignment/return/`out` whose
  declared type exactly matches what's being constructed. Don't use it
  where the declared type is a base/interface and the constructed type is
  a concrete implementer (`new(...)` there would mean "construct the base
  type," which isn't valid), where the target is `var` (no declared type
  to infer from), or where the `new(...)` is the receiver of a chained
  call rather than the value actually being assigned.
- No comments in source, including test code. If code needs one to be
  understood, that's a signal to restructure — extract a well-named
  method, turn an encoded string/boolean convention into a properly-named
  type or property — not to narrate it in prose. Applies to WHY-comments
  too, not just WHAT-comments. A fact worth keeping doesn't become a
  source comment just because it lives in a test file; it goes in
  `PLAN.md`, `.claude/specific.md`, or the architecture doc instead,
  whichever already owns that kind of fact.
- Fix a bug in the component that actually owns the relevant knowledge, not by
  compensating with a heuristic wherever the symptom happened to surface — if a
  fix requires guessing at another layer's shape or invariants, the guess
  belongs in that layer instead. Relatedly, a method shouldn't reach into a
  caller's shared/mutable state to get what it needs — take it as an
  explicit parameter, even if the caller has to compute it first. Same
  principle in reverse: don't get an object back from a call and then poke
  its fields/methods yourself to finish the job — pass along what the
  callee needs so it mutates its own state itself.
- Never use the `!` null-forgiving operator — it silences the compiler instead
  of resolving the issue, defeating the point of `Nullable`. Follow the house
  nullable guide:
  <https://github.com/mouseless/learn-dotnet/blob/main/nullable-usage/README.md>.
  When a value is nullable by type but a real invariant guarantees it isn't null
  at some point, use `?? throw new InvalidOperationException(...)` instead — it
  fails loudly at the point of use if the invariant is ever broken, rather than
  risking a `NullReferenceException` downstream.

## Parking (ending a session)

When the user says they're "parking" (wrapping up for the day):

1. Run the test suite, confirm all-green — flag clearly if not; don't
   park on red.
2. Update `PLAN.md`: refresh status/counts, remove completed work from
   "Remaining milestones" (don't just annotate it done — delete it, this
   file shrinks to empty once nothing's left). `PLAN.md` only ever
   tracks actionable remaining work — an accepted tradeoff or known
   limitation with no follow-up action isn't a todo, so it doesn't
   belong here; fold it into the relevant behavior/architecture doc
   instead, as a plain fact about current behavior, the same as anything
   else there.
3. Update the architecture doc with any structural change from this
   session (new types, moved namespaces, a resolved design decision) —
   keep it describing current shape only, not a changelog.
4. Update `.claude/specific.md` (or `.claude/generic.md`, if the
   learning isn't actually project-specific) with any durable
   convention/rule/decision from this session — these files plus
   `PLAN.md` are what survive to a cold start elsewhere; nothing
   load-bearing should live only in chat history.
5. Give a short summary: what's done, what's next, anything to
   double-check.

## Git

Read-only `git` commands (`log`, `diff`, `show`, `status`, `blame`, etc.) are
fine to run directly. Never run a `git` command that writes (`add`, `commit`,
`push`, `checkout`, `reset`, etc.) — this process has no write permission on
`.git` anyway, so it would fail. The user handles all of git themselves; don't
prepare commands for them or remind them about pending git tasks.
