# GraphVG Requirements

Vision: a functional F# charting library similar to D3.js, Plotly.NET, FSharp.Charting, and XPlot built on SharpVG's primitives. The API should feel idiomatic to both F# and SharpVG — pipeline-friendly, immutable records, discriminated unions for variants, and `create`/`with*`/`to*` module patterns throughout.

---

## REQ-10: Adaptive Canvas Resolution

The internal canvas size (currently fixed at 1000×1000) should adapt to the magnitude of the data being plotted. When data values are very large or very small, fixed-size annotation constants (tick lengths, font sizes, margins) become proportionally wrong — either invisible or dominating the plot area.

**Acceptance criteria:**

- Annotation constants (tick length, font size, margin) are expressed as fractions of canvas size rather than absolute pixel values.
- Canvas resolution scales up for large-magnitude data (e.g., domain spans 1e9) and down for small-magnitude data (e.g., domain spans 1e-6) to maintain adequate floating-point precision in SVG coordinates.
- Existing tests continue to pass (canvasSize = 1000 remains the default).
- The displayed chart is unaffected — only internal coordinate precision changes.

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

## REQ-39: Radar / Spider Chart

A polar chart with one axis per variable, used to compare multivariate observations. Each series is a polygon connecting values on each radial axis.

```fsharp
// axes: axis names; values per series must match axis count
type RadarPoint = { Axes : string list; Values : float list }

val radar : RadarPoint list -> RadarChart

module RadarChart =
    val toSvg : RadarChart -> Element
    val toHtml : RadarChart -> string
```

**Acceptance criteria:**

- Axes are evenly spaced around the circle with labels at the perimeter.
- Concentric grid rings represent equal value increments.
- Each series renders as a filled polygon (low opacity) with an outlined border.
- Multiple series can be overlaid.
- `toSvg` and `toHtml` follow existing conventions.

---
