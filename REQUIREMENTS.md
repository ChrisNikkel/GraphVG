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

## REQ-24: Error Bars

Support uncertainty visualization for scatter and line series.

```fsharp
type ErrorBar =
    | Symmetric of float list
    | Asymmetric of low : float list * high : float list

val withErrorBars : ErrorBar -> Series -> Series
```

**Acceptance criteria:**

- Error bars render correctly for each point.
- Length mismatches are handled explicitly (e.g., result/error return).
- No visual change when error bars are not configured.

---

## REQ-26: Tooltip Metadata for HTML Output

Allow optional per-point tooltip content when rendering to HTML.

```fsharp
val withTooltip : (float * float -> string) -> Series -> Series
```

**Acceptance criteria:**

- `toHtml` includes hoverable tooltip content for configured series.
- Raw `render` output remains valid SVG with no required JS runtime.
- Default remains no tooltip output unless explicitly configured.

---

## REQ-30: Layout Spacing Configuration

Expose graph layout spacing controls instead of relying on internal fixed margins for titles, axes, and labels.

```fsharp
type LayoutSpacing =
    {
        OuterMargin : float
        TitlePadding : float
        AxisLabelPadding : float
        TickLabelPadding : float
    }

val withLayoutSpacing : LayoutSpacing -> Graph -> Graph
```

**Acceptance criteria:**

- Title, tick-label, and axis-label spacing can be configured per graph.
- Default behavior remains backward-compatible with current built-in spacing values.
- Large titles and top/right axis labels do not overlap when default spacing is used.

---

## REQ-35: Violin Plot

A distribution visualization that shows a mirrored kernel density estimate (KDE) alongside optional box-plot summary statistics. Natural companion to the existing `Box` series.

```fsharp
// Same constructor as boxAt — raw values at a categorical position
val violinAt : float -> float list -> Series
val violin : float list -> Series
```

**Acceptance criteria:**

- Violin series renders a symmetric KDE outline centered on the position value.
- Box-plot summary (median line, IQR box, whiskers) is overlaid inside the violin by default.
- KDE bandwidth is chosen automatically (Silverman's rule).
- Violin width in data coordinates is configurable via `withPointRadius` (reused as half-width).
- Works in multi-series graphs with multiple violins side by side.

---

## REQ-36: Candlestick / OHLC

Financial price-action chart showing open, high, low, and close values per time period.

```fsharp
// Points are (x, open, high, low, close)
type OhlcPoint = { X : float; Open : float; High : float; Low : float; Close : float }

val candlestick : OhlcPoint list -> Series
val ohlc : OhlcPoint list -> Series          // classic bar form, no filled body
```

**Acceptance criteria:**

- `Candlestick` renders a filled rectangle between open and close (green if close ≥ open, red otherwise) with wicks to high and low.
- `Ohlc` renders a vertical line from low to high with left tick (open) and right tick (close).
- Colors follow the active theme's up/down palette, overridable via `withTheme`.
- Auto-bounds cover all four OHLC values.

---

## REQ-37: Waterfall Chart

A cumulative bar chart where each bar shows the incremental change from the previous total. Used for financial statements, cost breakdowns, and bridge charts.

```fsharp
// Points are (x, delta) — positive deltas go up, negative go down
val waterfall : (float * float) list -> Series

// Mark a bar as a running total (draws from zero rather than cumulative baseline)
val withTotalAt : float list -> Series -> Series   // list of x values that are totals
```

**Acceptance criteria:**

- Each bar starts at the running cumulative total and extends by its delta value.
- Positive deltas use the theme's "up" color; negative deltas use the "down" color.
- Bars marked as totals (via `withTotalAt`) draw from zero and use a neutral color.
- Auto-bounds cover the full range of running totals, not just delta values.
- Connector lines between bar tops are rendered as dashed lines.

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
