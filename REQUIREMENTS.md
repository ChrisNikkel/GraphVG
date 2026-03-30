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
