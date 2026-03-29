# GraphVG Requirements

Vision: a functional F# charting library similar to D3.js, Plotly.NET, FSharp.Charting, and XPlot built on SharpVG's primitives. The API should feel idiomatic to both F# and SharpVG — pipeline-friendly, immutable records, discriminated unions for variants, and `create`/`with*`/`to*` module patterns throughout.

---


## REQ-10: Adaptive Canvas Resolution (deferred)

The internal canvas size (currently fixed at 1000×1000) should adapt to the magnitude of the data being plotted. When data values are very large or very small, fixed-size annotation constants (tick lengths, font sizes, margins) become proportionally wrong — either invisible or dominating the plot area.

**Acceptance criteria:**

- Annotation constants (tick length, font size, margin) are expressed as fractions of canvas size rather than absolute pixel values.
- Canvas resolution scales up for large-magnitude data (e.g., domain spans 1e9) and down for small-magnitude data (e.g., domain spans 1e-6) to maintain adequate floating-point precision in SVG coordinates.
- Existing tests continue to pass (canvasSize = 1000 remains the default).
- The displayed chart is unaffected — only internal coordinate precision changes.

---

## REQ-13: Axis Tick Label Formatter (deferred)

Tick labels are currently formatted with `"%.4g"`. Users should be able to supply a custom formatter.

```fsharp
val withTickFormat : (float -> string) -> Axis -> Axis
```

Example uses: `sprintf "%.0f%%"` for percentages, `sprintf "$%.2f"` for currency, `fun v -> if v >= 1000.0 then sprintf "%.0fk" (v/1000.0) else sprintf "%.0f" v` for compact large numbers.

---


## REQ-15: Graph Background and Plot Area (deferred)

`Theme.Background` sets a color but is not currently rendered as an SVG rect. The plot area (inside the axes) and the full SVG background should be independently styleable.

```fsharp
type Theme = {
    ...
    Background  : Color          // full SVG canvas background
    PlotBackground : Color option  // inner plot area; None = transparent
}
val withPlotBackground : Color -> Theme -> Theme
```

D3 and Plotly both distinguish canvas background from plot area background; this enables the common "white plot on gray page" or "dark plot on white page" patterns.

---

## REQ-16: Axis Placement and Spine Visibility (deferred)

Currently axes are always drawn as a single line (the spine). Some chart styles omit the spine entirely and rely only on tick marks, or draw all four sides as a box. This should be controllable.

```fsharp
type SpineStyle = Full | None | Box

val withSpine : SpineStyle -> Axis -> Axis
```

Inspired by Matplotlib's `ax.spines` and ggplot2's `theme(panel.border = ...)`.

---

## REQ-17: Legend (deferred)

When series have labels (`Series.withLabel`), a legend should be renderable. Comparable libraries (Plotly, ggplot2) auto-show a legend when series are named.

```fsharp
type LegendPosition = TopRight | TopLeft | BottomRight | BottomLeft | Hidden

type Legend = {
    Position : LegendPosition
    FontSize : float
}

module Legend =
    val create   : LegendPosition -> Legend
    val withFontSize : float -> Legend -> Legend

// on Graph:
val withLegend : Legend -> Graph -> Graph
```

Default: `Hidden` (no legend unless explicitly added). Renders as a small box of colored swatches and labels, positioned inside the plot area margin.

---

## REQ-18: Graph Title Font and Alignment (deferred)

`Graph.withTitle` sets a title string but font size and alignment are hardcoded. These should be configurable.

```fsharp
type TitleStyle = {
    FontSize  : float
    Alignment : TextAnchor   // Start | Middle | End
}

// on Graph or Theme:
val withTitleStyle : TitleStyle -> Graph -> Graph
```

Default: `{ FontSize = 16.0; Alignment = Middle }`.

---

## REQ-19: Scatter Point Shape (deferred)

Scatter series render as circles. Comparable libraries (D3 symbols, Plotly markers) support multiple shapes for distinguishing series without relying on color alone (important for accessibility).

```fsharp
type PointShape = Circle | Square | Diamond | Cross | Triangle

val withPointShape : PointShape -> Series -> Series
```

Default remains `Circle`. Each shape renders at the same effective radius set by `withPointRadius`.

---

## REQ-20: Dashed / Dotted Line Style (deferred)

Line and area series currently render as solid strokes. SVG natively supports `stroke-dasharray`; this should be exposed.

```fsharp
type StrokeDash = Solid | Dashed | Dotted | DashDot

val withStrokeDash : StrokeDash -> Series -> Series
```

Useful for distinguishing series in black-and-white output or when color is insufficient.
