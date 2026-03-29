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

## REQ-17: Legend

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

## REQ-19: Scatter Point Shape

Scatter series render as circles. Comparable libraries (D3 symbols, Plotly markers) support multiple shapes for distinguishing series without relying on color alone (important for accessibility).

```fsharp
type PointShape = Circle | Square | Diamond | Cross | Triangle

val withPointShape : PointShape -> Series -> Series
```

Default remains `Circle`. Each shape renders at the same effective radius set by `withPointRadius`.

---

## REQ-20: Axis Scale Types and Tick Formatting

Support common axis scale modes and custom tick-label formatting for parity with D3/Plotly-style workflows.

```fsharp
type AxisScale = Linear | Log of baseValue : float | Time

val withScaleType : AxisScale -> Axis -> Axis
val withTickFormat : (float -> string) -> Axis -> Axis
```

**Acceptance criteria:**

- Linear and log scales render correctly with matching tick generation.
- Custom tick formatting is applied to all labels on the target axis.
- Existing behavior remains default (`Linear` scale, built-in numeric formatting).

---

## REQ-21: Series Visibility and Opacity

Enable visibility toggles and soft emphasis/de-emphasis without removing series data.

```fsharp
val withVisible : bool -> Series -> Series
val withOpacity : float -> Series -> Series
```

**Acceptance criteria:**

- Hidden series do not render.
- Opacity affects scatter markers, polylines, and area fills consistently.
- Default remains visible with full opacity (`1.0`).

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

## REQ-23: Annotation Primitives

Add explicit annotation support for common chart callouts.

```fsharp
type Annotation =
    | Text of position : float * float * content : string
    | Line of fromPoint : float * float * toPoint : float * float
    | Rect of origin : float * float * width : float * height : float

val addAnnotation : Annotation -> Graph -> Graph
```

**Acceptance criteria:**

- Text, line, and rectangle annotations render in data coordinates.
- Annotation order is deterministic and testable.
- Existing graphs render unchanged when no annotations are added.

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

## REQ-25: Distribution Chart Types

Add core distribution visualizations commonly available in peer libraries.

```fsharp
type SeriesKind =
    | Scatter
    | Line
    | Area
    | Histogram
    | Box

val histogram : float list -> Series
val box : float list -> Series
```

**Acceptance criteria:**

- Histogram supports configurable bin count/strategy.
- Box plot renders median, quartiles, and whiskers.
- Existing chart kinds remain behaviorally unchanged.

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

## REQ-27: Export and Persistence Helpers

Provide first-class helpers for writing chart output artifacts.

```fsharp
val writeSvg : path : string -> Graph -> unit
val writeHtml : path : string -> Graph -> unit
```

**Acceptance criteria:**

- Output files are deterministic for identical graph inputs.
- Parent directory handling and overwrite behavior are documented.
- Existing `render` and `toHtml` APIs remain unchanged.

---

## REQ-28: Theme Presets and Global Defaults

Support reusable style templates and configurable defaults.

```fsharp
type ThemePreset = Light | Dark | HighContrast

val preset : ThemePreset -> Theme
val withDefaultTheme : Theme -> Graph -> Graph
```

**Acceptance criteria:**

- Presets provide coherent, tested pen/background/grid combinations.
- Per-graph overrides remain possible after preset application.
- Existing default theme behavior remains backward-compatible.

---

## REQ-29: Domain Policy for Auto-Bounds

Control how automatic domain/range bounds are computed.

```fsharp
type DomainPolicy = IncludeZero | Tight | Padded of float

val withDomainPolicy : DomainPolicy -> Graph -> Graph
```

**Acceptance criteria:**

- Auto-bounds honor selected domain policy consistently.
- Policy interacts predictably with `addSeries` and `createWithSeries`.
- Manual domain/range settings still take precedence when explicitly provided.

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
