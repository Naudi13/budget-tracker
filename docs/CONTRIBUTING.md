# Contributing

Contributions are welcome by pull request, and are released under the MIT
[LICENSE](LICENSE).

## Read these first

| Read | Why |
|---|---|
| [`00-sdd-guide.md`](00-sdd-guide.md) | The four phases, their gates, and the order the sections are filled in |
| [`00-governance/documentation-rules.md`](00-governance/documentation-rules.md) | When a document counts as done, and the review checklist it must pass |
| [`00-governance/git-conventions.md`](00-governance/git-conventions.md) | Branch names, conventional commits, merge policy |

Open an issue before any change that adds, removes or renames a document: the
section structure is this framework's public interface.

## Document conventions

- One question per document. If it answers two, it should have been two documents.
- 25-80 lines. Below 25 it is a stub; above 80 it holds two topics. `README.md` and
  `00-sdd-guide.md` are the only files exempt from the upper bound.
- Content is tables, lists and Mermaid diagrams; prose carries the "why" only.
- English throughout, relative links only, and no unresolved placeholder markers.
- Every document opens `# NN — Title Case Name` and closes with a `**Related:**` footer.
- Files named `_template-*` are copied into place, never edited where they sit.
- The subject is always one deployable application. Anything that only exists once
  parts are deployed separately belongs to the sibling framework linked from the
  README, not here.

## Verifying a change

1. Every relative link resolves, and every Mermaid diagram renders on GitHub.
2. The document passes the review checklist in `00-governance/documentation-rules.md`.
3. The line count is inside the range above.
4. An entry is added under `[Unreleased]` in [`CHANGELOG.md`](CHANGELOG.md).
5. A second person has reviewed it — only then is the INSTRUCTIONS block deleted.
