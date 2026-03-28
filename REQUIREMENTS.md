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

Currently draws lines only. Needs ticks, labels, and configurable positioning.

```fsharp
type AxisPosition = Bottom | Top | Left | Right

type Axis = {
    Position  : AxisPosition
    Scale     : Scale
    TickCount : int
    Label     : string option
}

module Axis =
    val create   : AxisPosition -> Scale -> Axis
    val withTicks : int    -> Axis -> Axis
    val withLabel : string -> Axis -> Axis
    val toElements : Theme -> Axis -> Element list
    // toElements renders: the axis line, tick marks, tick labels
```

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
