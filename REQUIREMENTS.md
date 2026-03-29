# GraphVG Requirements

Vision: a functional F# charting library similar to D3.js, Plotly.NET, FSharp.Charting, and XPlot built on SharpVG's primitives. The API should feel idiomatic to both F# and SharpVG — pipeline-friendly, immutable records, discriminated unions for variants, and `create`/`with*`/`to*` module patterns throughout.

---

## Style Constraints

- Follow SharpVG conventions exactly: PascalCase types, camelCase functions, `create`/`with*`/`add*`/`to*` module patterns, static `ToTag`/`ToString` members where applicable.
- No new dependencies without explicit approval.
- `canvasSize` lives in one place (currently duplicated across `Graph` and `GraphVG`).
- No shadowing of SharpVG types — resolve the `Point` name conflict.

---

## REQ-1: Scales

D3's most fundamental abstraction. Maps a data domain to a visual range.

```fsharp
type Scale =
    | Linear of domain:(float * float) * range:(float * float)
    | Log    of domain:(float * float) * range:(float * float) * base':float

module Scale =
    let linear domain range : Scale
    let log domain range base' : Scale
    val apply : Scale -> float -> float        // map a data value to canvas value
    val invert : Scale -> float -> float       // map a canvas value back to data
    val ticks  : Scale -> int -> float list    // generate n evenly-spaced tick values
```

`Graph.toScaledSvgCoordinates` should be reimplemented on top of `Scale`.

---

## REQ-2: Series

Currently a plain type alias. Needs to become a proper record to carry metadata and style intent.

```fsharp
type SeriesKind =
    | Scatter
    | Line
    | Area           // filled region under a line

type Series = {
    Points  : (float * float) list
    Kind    : SeriesKind
    Label   : string option
}

module Series =
    let create kind points : Series
    let scatter points : Series
    let line    points : Series
    let area    points : Series
    val withLabel : string -> Series -> Series
```

---

## REQ-3: Theme

Currently one hardcoded theme, no builder. Should follow SharpVG's `with*` pattern.

```fsharp
type Theme = {
    Background : Color
    Pens       : Pen list          // one pen per series, cycled
    AxisPen    : Pen
    GridPen    : Pen option
}

module Theme =
    val empty   : Theme            // sensible white-background defaults
    val turtle  : Theme            // existing green-on-black
    val light   : Theme            // light/clean palette
    val dark    : Theme            // dark palette

    val withBackground : Color  -> Theme -> Theme
    val withPens       : Pen list -> Theme -> Theme
    val withAxisPen    : Pen    -> Theme -> Theme
    val withGridPen    : Pen    -> Theme -> Theme
    val penForSeries   : int    -> Theme -> Pen   // cycle through pens by index
```

---

## REQ-4: Axis

```fsharp
type AxisPosition = Bottom | Top | Left | Right | HorizontalAt of float | VerticalAt of float

type Axis = {
    Position    : AxisPosition
    Scale       : Scale
    TickCount   : int
    TickLength  : float             // pixel length of each tick mark line (default 6.0)
    Label       : string option
    SkipLabelAt : float option      // suppress tick label at this data value
    SkipTickAt  : float option      // suppress tick mark line at this data value
}

module Axis =
    val create          : AxisPosition -> Scale -> Axis
    val withTicks       : int    -> Axis -> Axis
    val withTickInterval: float  -> Axis -> Axis   // specify spacing in data units instead of count
    val withTickLength  : float  -> Axis -> Axis   // override pixel length of tick marks
    val withLabel       : string -> Axis -> Axis
    val withSkipLabelAt : float  -> Axis -> Axis   // suppress label at one data value
    val withSkipTickAt  : float  -> Axis -> Axis   // suppress tick mark at one data value
    val toElements      : Theme  -> Axis -> Element list
    val none            : Axis option * Axis option  // shorthand for (None, None)
```

**Notes:**

- `TickLength` is the **pixel length** of the drawn tick mark, independent of data spacing.
- Data spacing is controlled separately: `withTicks n` generates `n` evenly-spaced ticks across the domain; `withTickInterval step` generates ticks at every `step` in data units (e.g., `0.5` for labels at `-1.0, -0.5, 0.0, 0.5, 1.0`).
- Default crosshair axes (from `Graph.create`) automatically set `SkipLabelAt = Some 0.0` and `SkipTickAt = Some 0.0` on both axes to avoid clutter at the intersection. Single-axis layouts leave these as `None`.
- `withTickInterval` is defined on `Axis`, not `Scale`, because it is a rendering concern — how many marks to show — not a data transform.

---

## REQ-5: Graph

The central composition record. Currently conflates data bounds with rendering config; these should be separated clearly.

```fsharp
type Graph = {
    Series   : Series list
    XScale   : Scale
    YScale   : Scale
    XAxis    : Axis option
    YAxis    : Axis option
    Theme    : Theme
    Title    : string option
}

module Graph =
    val create   : Series list -> Graph          // auto-compute scales with padding
    val withXScale : Scale -> Graph -> Graph
    val withYScale : Scale -> Graph -> Graph
    val withXAxis  : Axis  -> Graph -> Graph
    val withYAxis  : Axis  -> Graph -> Graph
    val withTheme  : Theme -> Graph -> Graph
    val withTitle  : string -> Graph -> Graph
    val addSeries  : Series -> Graph -> Graph
```

---

## REQ-6: Rendering per SeriesKind

Each `SeriesKind` maps to a different SharpVG primitive.

| Kind    | SharpVG primitive | Notes |
|---------|-------------------|-------|
| Scatter | `Circle`          | fixed radius, configurable via theme pen |
| Line    | `Polyline`        | stroke only, no fill |
| Area    | `Polygon`/`Path`  | filled region to x-axis baseline |

All renderers take a `Theme`, `Scale` pair and the `Series`, and return `Element list`.

---

## REQ-7: Grid Lines (optional)

When `Theme.GridPen` is `Some`, render light horizontal and vertical lines at tick positions before the data elements.

---

## REQ-8: Top-level API

```fsharp
module GraphVG =
    val render : Graph -> string    // returns SVG string
    val toHtml : string -> Graph -> string   // title -> Graph -> HTML page
```

Replace current `drawSeries` name — "render" matches SharpVG's vocabulary better and doesn't conflate what the function does with how data is modeled.

---

## REQ-9: Tests

Current test file is a placeholder. Each module above should have unit tests covering:
- Scale: `apply`, `invert`, `ticks`
- Theme: builder round-trips, pen cycling
- Axis: tick generation, element count
- Graph: auto-scale computation, series accumulation
- Rendering: smoke tests (non-empty element list for known inputs)

---

## Out of Scope (for now)

- Animations / transitions
- Logarithmic axis labels (deferred until REQ-1 log scale is validated)
- Pie / donut / bar charts
- Responsive/dynamic sizing (canvas is fixed 1000×1000 for now)
- Interactivity / JavaScript output

---

## REQ-11: Functional Series Generation

Currently, generating a series from a continuous function requires a manual list comprehension (as in the example). This pattern should be a first-class part of the Series API.

```fsharp
module Series =
    // Generate a parametric series by sampling f over [tMin, tMax] at n points.
    // f maps a parameter t to an (x, y) pair.
    val ofFunction : kind:SeriesKind -> f:(float -> float * float) -> tMin:float -> tMax:float -> samples:int -> Series

    // Convenience wrappers
    val lineOfFunction    : f:(float -> float * float) -> tMin:float -> tMax:float -> samples:int -> Series
    val scatterOfFunction : f:(float -> float * float) -> tMin:float -> tMax:float -> samples:int -> Series
```

**Example usage:**

```fsharp
// Unit circle — 200 samples over [0, 2π]
let circle = Series.lineOfFunction (fun t -> cos t, sin t) 0.0 (2.0 * Math.PI) 200

// Fewer samples = coarser approximation, more visible as polygon
let coarseCircle = Series.lineOfFunction (fun t -> cos t, sin t) 0.0 (2.0 * Math.PI) 12
```

**Acceptance criteria:**

- `ofFunction` produces exactly `samples` points evenly spaced over `[tMin, tMax]`.
- The first point corresponds to `t = tMin`, the last to `t = tMax`.
- `lineOfFunction` / `scatterOfFunction` are simple wrappers over `ofFunction`.
- Existing manual list-comprehension approach still works (no breaking change).

---

## REQ-10: Adaptive Canvas Resolution (deferred)

The internal canvas size (currently fixed at 1000×1000) should adapt to the magnitude of the data being plotted. When data values are very large or very small, fixed-size annotation constants (tick lengths, font sizes, margins) become proportionally wrong — either invisible or dominating the plot area.

**Acceptance criteria:**

- Annotation constants (tick length, font size, margin) are expressed as fractions of canvas size rather than absolute pixel values.
- Canvas resolution scales up for large-magnitude data (e.g., domain spans 1e9) and down for small-magnitude data (e.g., domain spans 1e-6) to maintain adequate floating-point precision in SVG coordinates.
- Existing tests continue to pass (canvasSize = 1000 remains the default).
- The displayed chart is unaffected — only internal coordinate precision changes.

---

## REQ-12: Axis Label Font (deferred)

Tick label font size is currently hardcoded at `12.0`. It should be configurable per axis.

```fsharp
val withFontSize : float -> Axis -> Axis
```

Default remains `12.0`. Affects tick labels and the axis title label.

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
