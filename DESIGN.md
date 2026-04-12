# GraphVG Design

Architecture reference for the GraphVG library. Focuses on the *why* behind structural decisions — for the *what*, read the source.

---

## Module architecture

```mermaid
flowchart TD
    subgraph data["Data"]
        SC[Scale]
        SR[Series]
        PL[Plot]
    end

    subgraph style["Style"]
        TH[Theme]
    end

    subgraph assembly["Assembly"]
        AX[Axis]
        LG[Legend]
        AN[Annotation]
        LY[Layout]
    end

    GR[Graph]
    GVG[GraphVG]

    subgraph polar["Polar Charts"]
        RC[RadarChart]
        PC[PieChart]
    end

    data --> assembly
    style --> assembly
    assembly --> GR
    GR --> GVG
    data --> polar
    style --> polar
```

Two inputs flow into the library — **Data** (series points, scales, expressions) and **Style** (theme colors and pens). Both feed **Assembly**, which builds the structural pieces: `Axis`, `Legend`, `Annotation`, and `Layout`. Assembly's output feeds `Graph`, which delegates final SVG rendering to `GraphVG`.

**Polar Charts** (`RadarChart`, `PieChart`) are self-contained: they take Data and Style directly, do their own layout and rendering, and produce SVG without going through the Assembly → Graph → GraphVG pipeline.

`CommonMath` is an internal utility (pure float math) that underpins Data and Assembly but is not a user-facing concept. `Legend` and `Annotation` take explicit parameters rather than a `Graph` record to avoid a circular dependency. `Layout` compiles after `Graph` because `heatmapRampElements` calls `Graph.canvasSizeOf`; all other Layout functions take an explicit `cs : float` and are consumed only by `GraphVG`.

---

## Standalone chart types

`RadarChart` and `PieChart` are standalone chart types: they own their own layout math, rendering, and `toSvg`/`toHtml` output. They do not use `Graph` or `GraphVG`.

**When to use standalone vs SeriesKind:** A new chart type is standalone when its geometry is fundamentally incompatible with the XY `Graph` coordinate system. Radar charts use a polar web of evenly-spaced radial axes; pie/donut charts have no axes at all. Forcing either into a `SeriesKind` would require the `Graph` pipeline to special-case non-XY rendering, which would break the clean data-to-pixel transform model. The standalone pattern keeps `Graph` purely XY and lets each polar type own its geometry completely.

**Shared infrastructure:** Standalone types still consume `Theme` for color palettes and `canvasSize` from `CommonMath` for proportional layout. They follow the same `create`/`with*`/`toSvg`/`toHtml` API pattern as the `Graph`/`GraphVG` pipeline so the library feels consistent.

---

## Plot module

`Plot` converts infix math expressions (strings) into `Series` values for use in the standard `Graph` pipeline. It depends on **MathNet.Symbolics** (MIT, NuGet) — the only dependency beyond SharpVG.

### PlotExpr — opaque type

```fsharp
type PlotExpr = private PlotExpr of MathNet.Symbolics.Expression
```

A single-case private DU wrapping the MathNet expression tree. The `private` case means consumers can only obtain a `PlotExpr` through `Plot.parse` — the underlying MathNet type never leaks into consumer code. If the expression-parsing backend changes, existing call sites remain unchanged.

### Integration with Graph

`Plot.toSeries` returns a `Series` of kind `SegmentedLine` (see below). This slots directly into `Graph.create [series] domain range` — same pipeline, same themes, same axes, same legend. Multiple `PlotExpr` series can coexist alongside data series in a single graph.

### SegmentedLine SeriesKind

`Line` series render as a single `Polyline` — one unbroken path. This is correct for data series but wrong for mathematical functions with discontinuities (`tan x`, `1/x`, `log x`): the polyline draws a near-vertical line straight through the asymptote.

A new `SegmentedLine` SeriesKind handles this:
- `Plot.toSeries` inserts `(nan, nan)` sentinel tuples at detected discontinuities.
- `Graph.drawSeries` renders `SegmentedLine` as a SharpVG `Path` using `Path.addMoveTo` / `Path.addLineTo`: each finite segment starts with `M` (pen-up) and continues with `L` (pen-down), producing natural breaks at asymptotes.
- `Series.bounds` filters out `(nan, nan)` pairs before computing min/max so auto-range is not corrupted.

Existing `Line` series are unaffected. `SegmentedLine` is the only Series kind that may carry `(nan, nan)` sentinels.

### Discontinuity detection

Detection runs in two passes over the sampled points:

1. **First pass** — evaluate at all sample positions, collect finite y values, compute `ySpan = yMax − yMin` (default `1.0` if no finite values).
2. **Second pass** — walk adjacent pairs; insert `(nan, nan)` when `|y[i+1] − y[i]| > 1000 × ySpan` or either value is non-finite.

The threshold `1000 × ySpan` is intentionally large: it passes through legitimate steep slopes while catching true asymptotes where the function jumps by orders of magnitude within one sample interval.

### autoRange algorithm

```
1. Dense sample (1000 points) — filter out non-finite values.
2. Compute symbolic derivative via MathNet.Symbolics.Calculus.differentiate.
3. Find critical points: evaluate derivative at all sample positions, collect sign-change intervals, bisect each to tolerance 1e-6.
4. Evaluate expression at each critical point; add to finite value list.
5. Return CommonMath.padRange 0.10 (min finiteValues, max finiteValues).
```

Step 3 reuses the private `bisect` helper defined in `Plot.fs` (not promoted to `CommonMath` — only one consumer).

### roots algorithm

```
1. Dense sample over domain.
2. Collect adjacent pairs where sign(y[i]) ≠ sign(y[i+1]) and both are finite.
3. Bisect each pair to tolerance 1e-6 × domain span.
4. Return list of roots.
```

---

## Coordinate system

Data coordinates live in a conventional mathematical space (Y up). SVG coordinates have Y pointing down. The transform pipeline applies a linear scale to each axis and inverts Y:

```
  Data space              SVG pixel space

  y                       x →
  ▲  · (xMax, yMax)       0 ──────────── 1000
  │                       │
  │  · (x, y)        →    │  · (px, py)
  │                       │
  │  · (xMin, yMin)       1000
  └──────────────── x →   y ↓
```

The X and Y scales map independently:

```
  data x:  xMin ──────────────── xMax
                  Scale.apply (linear)
  SVG px:     0 ──────────────── 1000

  data y:  yMin ──────────────── yMax
                  Scale.apply (inverted)
  SVG py:  1000 ──────────────────── 0
```

Y inversion is built into the Y scale at construction time — the pixel range is `(canvasSize, 0.0)` rather than `(0.0, canvasSize)`. No special-casing is needed at the point of use.

---

## Canvas and ViewBox

The inner canvas is always `1000×1000` in SVG user units. `GraphVG.fs` computes a padding margin for each side based on axis labels, ticks, and title, then expands the `viewBox` outward to fit:

```
  ┌──────────────────────────────────────────┐
  │               padding.Top                │
  │        ┌──────────────────────┐          │
  │        │                      │          │
  │ pad.   │  canvas              │  pad.    │
  │ Left   │  (0, 0) → (1000,1000)│  Right   │
  │        │                      │          │
  │        └──────────────────────┘          │
  │              padding.Bottom              │
  └──────────────────────────────────────────┘
    ViewBox origin = (-padding.Left, -padding.Top)
```

`GraphPadding` is a private four-sided record (`Top`, `Right`, `Bottom`, `Left`). It is intentionally separate from SharpVG's `Area` type, which only models width and height — independent sides are needed because each edge reserves a different amount of space. SharpVG primitives (`Point`, `Area`, `ViewBox`, `Rect`) are only constructed at the final step where geometry is emitted.

---

## CommonMath — unit-shape pattern

Shapes are defined as **unit offsets**: vertices relative to a center at the origin with a radius of 1. `centerPolygon` / `centerLines` place any unit shape at a real center and scale by multiplying each offset by `r` and translating by `(cx, cy)`:

```
  Unit diamond (r = 1)           At center (cx, cy), radius r

         (0, -1)                       (cx, cy-r)
            ·                              ·
           / \                            / \
  (-1, 0) ·   · (1, 0)    →   (cx-r, cy) ·   · (cx+r, cy)
           \ /                            \ /
            ·                              ·
         (0, +1)                       (cx, cy+r)
```

The same transform works for any shape. Adding a new scatter point shape is a new list of unit offsets — no new arithmetic function needed.

