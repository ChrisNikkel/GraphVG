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

---

## REQ-38: Pie / Donut Chart

Proportional area chart showing parts of a whole as circular sectors. Requires a polar rendering path separate from the XY `Graph` API.

```fsharp
// Each slice: (label, value)
val pie : (string * float) list -> PieChart

val withInnerRadius : float -> PieChart -> PieChart    // 0.0 = full pie, 0.5 = donut
val withStartAngle : float -> PieChart -> PieChart     // radians, default 0

module PieChart =
    val toSvg : PieChart -> Element
    val toHtml : PieChart -> string
```

**Acceptance criteria:**

- Sectors are sized proportionally to their values; percentages are shown in labels.
- A legend maps slice colors to labels.
- `withInnerRadius` > 0 produces a donut; center can display a total value.
- Slice colors cycle through the active theme palette.
- `toSvg` and `toHtml` follow the same conventions as `GraphVG.toSvg`/`toHtml`.

---

