# GraphVG Requirements

Vision: a functional F# charting library similar to D3.js, Plotly.NET, FSharp.Charting, and XPlot built on SharpVG's primitives. The API should feel idiomatic to both F# and SharpVG — pipeline-friendly, immutable records, discriminated unions for variants, and `create`/`with*`/`to*` module patterns throughout.

---

## REQ-40: Mathematical Expression Plotting

Plot continuous mathematical functions from string expressions using **MathNet.Symbolics** (MIT, NuGet `MathNet.Symbolics`). Expressions become `Series` values that plug directly into the existing `Graph` pipeline alongside data series.

> **New dependency:** `MathNet.Symbolics` — MIT licensed, pure F#, net8.0. Add to `GraphVG.fsproj` and update `CLAUDE.md` allowed dependencies when implementing.

```fsharp
// Opaque parsed expression — creation always goes through Plot.parse
type PlotExpr

module Plot =
    val parse        : string -> Result<PlotExpr, string>
    val toSeries     : domain : (float * float) -> samples : int -> PlotExpr -> Series
    val roots        : domain : (float * float) -> PlotExpr -> float list
    val autoRange    : domain : (float * float) -> PlotExpr -> float * float
    val derivative   : PlotExpr -> PlotExpr
```

**Typical usage:**

```fsharp
match Plot.parse "sin(x) * x" with
| Error msg -> failwith msg
| Ok expr ->
    let domain = (-2.0 * Math.PI, 2.0 * Math.PI)
    let range  = Plot.autoRange domain expr
    let series = Plot.toSeries domain 500 expr |> Series.withLabel "sin(x)·x"
    Graph.create [series] domain range
    |> Graph.withTheme Theme.light
    |> GraphVG.toHtml
```

**Auto-scaling strategy (`autoRange`):**
1. Sample the expression densely across the domain.
2. Use `Plot.derivative` to find critical points (solve `f'(x) = 0` numerically within the domain).
3. Return `(min, max)` of sampled values plus critical-point values, with 10% padding — same policy as `DomainPolicy.Padded`.
4. Filter out `nan` / `±infinity` before computing bounds (handles asymptotes gracefully).

**Discontinuity handling:**
- Consecutive samples where the absolute difference exceeds a threshold (default `1000 * (yMax - yMin)`) are treated as discontinuities; the path is broken at that point rather than drawing a near-vertical line through the asymptote.
- The threshold is not user-configurable in v1.

**Supported syntax (MathNet.Symbolics infix):**
- Arithmetic: `+`, `-`, `*`, `/`, `^`
- Functions: `sin`, `cos`, `tan`, `exp`, `log`, `sqrt`, `abs`
- Constants: `pi`, `e`
- Single variable: `x`
- Example expressions: `"x^2 - 4"`, `"sin(x)/x"`, `"exp(-x^2)"`, `"log(x)"`, `"tan(x)"`

**`Plot.roots`** — returns real roots in the domain found by bisection on sign-change intervals in the dense sample; useful for axis annotation or programmatic domain selection.

**Acceptance criteria:**

- `Plot.parse` returns `Ok` for well-formed expressions and `Error` (with message) for invalid ones.
- `Plot.toSeries` produces a `Line` series with the requested number of sample points within the domain.
- `Plot.autoRange` returns a finite `(min, max)` range even for expressions with asymptotes within the domain.
- Discontinuities (asymptotes) produce path breaks — not lines shooting to ±infinity.
- Multiple `PlotExpr` series can be added to a single `Graph` alongside data series.
- `Plot.roots` returns roots accurate to within `1e-6` of the domain span.

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


