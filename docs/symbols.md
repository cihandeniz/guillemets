# Symbols

The concrete symbol table `Tokenization` recognizes. For the shape of the engine
see [architecture.md](architecture.md); for what these symbols *mean* to a
template author see [specs.md](specs.md).

Symbols are declared once, in `Symbols.cs`. Each character walks one level
deeper into the trie, and a node with a `TokenKind` attached is a match. Longest
match wins, so depth (`«`, `««`, `«««`, ...) and the scope-navigation markers
(`.: `, `..: `) cost nothing extra — they share prefixes with shorter symbols.
Adding a symbol or a repeating run is one line there and nothing else in the
tokenizer changes.

The `: `, `.: ` and `..: ` markers each also match with a newline in place of
their trailing space, and ` / ` takes a newline on either side. That is what
lets a `«...»` be hard-wrapped across lines — see Line Wrapping in
[specs.md](specs.md). `> ` and `| ` have no such variant; their space is
optional rather than substitutable.

```mermaid
flowchart TB
    Root(("(root)"))
    Root --> Open["«"] --> OpenKind["Open"]
    Open --> OpenOpen["« (loops on «)"] --> OpenBlockKind["OpenBlock\n(depth = run length)"]
    Root --> Close["»"] --> CloseKind["Close\n(literal text if nothing's open)"]
    Close --> CloseClose["» (loops on »)"] --> CloseBlockKind["CloseBlock\n(depth = run length)"]
    OpenOpen --> OpenTrim["~"] --> OpenBlockKind
    Root --> Tilde["~"] --> ElseKind["Else"]
    Tilde --> TildeClose["»» (loops on »)"] --> CloseBlockKind
    Root --> NewlineOne["newline"] --> NewlineKind["Newline\n(run length = how many)"]
    NewlineOne --> NewlineRest["newline (loops on newline)"] --> NewlineKind
    NewlineOne --> WrapSlash["/"] --> SlashEnd["space or newline"] --> FilterDelimiterKind["FilterDelimiter"]
    Root --> Quote[">"] --> QuoteKind["Quote\n(one blockquote marker)"]
    Quote --> QuoteSpace[" "] --> QuoteKind
    Root --> Pipe["|"] --> PipeKind["Pipe\n(one table cell boundary)"]
    Pipe --> PipeSpace[" "] --> PipeKind
    Root --> Esc["backslash"] --> EscChar["« or » or backslash or ~"] --> EscapedKind["Escaped literal"]
    Root --> Colon[":"] --> BareColonKind["BareColon\n(malformed-filter signal)"]
    Colon --> ColonEnd["space or newline"] --> ColonKind["Colon"]
    Root --> Dot["."] --> DotColon[":"] --> DotColonEnd["space or newline"] --> LocalScopeKind["LocalScope\n(.: )"]
    Dot --> DotDot["."] --> DotDotColon[":"] --> DotDotColonEnd["space or newline"] --> ParentScopeKind["ParentScope\n(..: )"]
    Root --> Space["space"] --> SpaceSlash["/"] --> SlashEnd
    Root --> Bang["!"] --> NegationKind["Negation\n(negates the chain it prefixes)"]
    Root --> Equals["="] --> AssignKind["Assign\n(variable definition)"]
    Root -.no match anywhere.-> LiteralKind["Literal (fallback)"]
```

A kind whose special meaning doesn't apply in context falls back to plain
literal text, so the tokenizer never has to understand context. That is what
carries `Negation` and `Assign`, which mean something only inside an open
`«...»` and are ordinary text everywhere else. `Quote` and `Pipe` share a shape:
one token is one marker whatever the spacing, which is what makes a line's
blockquote depth a count of consecutive `Quote` tokens and a table row a list of
cells split on `Pipe` tokens, rather than either being a character scan.
