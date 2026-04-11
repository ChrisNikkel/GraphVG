# GraphVG Requirements

Vision: a functional F# charting library similar to D3.js, Plotly.NET, FSharp.Charting, and XPlot built on SharpVG's primitives. The API should feel idiomatic to both F# and SharpVG — pipeline-friendly, immutable records, discriminated unions for variants, and `create`/`with*`/`to*` module patterns throughout.

---

## REQ-22: Subplots / Small Multiples

Support rendering multiple related plots in one SVG document.

```fsharp
type Layout = Single | Grid of rows : int * cols : int

val withLayout : Layout -> Graph -> Graph
val withPanel : row : int * col : int -> Series -> Series
```

**Acceptance criteria:**

- Grid layout renders multiple panels with correct placement.
- Each panel has independent axes and series assignment.
- `Single` remains the default layout.


