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
    NewlineOne --> WrapSlash["/"] --> FilterDelimiterKind
    Root --> Quote[">"] --> QuoteKind["Quote\n(one blockquote marker)"]
    Quote --> QuoteSpace[" "] --> QuoteKind
    Root --> Esc["backslash"] --> EscChar["« or » or backslash or ~"] --> EscapedKind["Escaped literal"]
    Root --> Colon[":"] --> BareColonKind["BareColon\n(malformed-filter signal)"]
    Colon --> ColonSpace[" "] --> ColonKind["Colon"]
    Root --> Dot["."] --> DotColon[":"] --> DotColonSpace[" "] --> LocalScopeKind["LocalScope\n(.: )"]
    Dot --> DotDot["."] --> DotDotColon[":"] --> DotDotColonSpace[" "] --> ParentScopeKind["ParentScope\n(..: )"]
    Root --> SpaceSlashSpace[" / "] --> FilterDelimiterKind["FilterDelimiter"]
    Root -.no match anywhere.-> LiteralKind["Literal (fallback)"]
```

A kind whose special meaning doesn't apply in context falls back to plain
literal text, so the tokenizer never has to understand context. One `Quote`
token is one marker, whatever the spacing, which is what makes a line's
blockquote depth a count of consecutive `Quote` tokens rather than a character
scan.
