# GraphVG Requirements

Vision: a functional F# charting library similar to D3.js, Plotly.NET, FSharp.Charting, and XPlot built on SharpVG's primitives. The API should feel idiomatic to both F# and SharpVG — pipeline-friendly, immutable records, discriminated unions for variants, and `create`/`with*`/`to*` module patterns throughout.

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



## REQ-24: Gantt Chart

Timeline bars showing task start and end positions for project scheduling and schedule visualization.

```fsharp
// (yRow, xStart, xEnd) triples — yRow is the task row position
Series.gantt : (float * float * float) list -> Series
// SeriesKind: GanttBar of ends: float list
// Points stored as (xStart, yRow); ends stored in kind

let tasks =
    Series.gantt [
        1.0, 0.0,  3.0   // Task A: days 0–3
        2.0, 2.0,  7.0   // Task B: days 2–7
        3.0, 5.0, 10.0   // Task C: days 5–10
    ]
    |> Series.withLabel "Phase 1"
```

**Acceptance criteria:**

- Each task renders as a horizontal filled rectangle from xStart to xEnd at yRow.
- Bar height derived from `withPointRadius` (default ~40px canvas units).
- Multiple `GanttBar` series group correctly (each series is a phase/resource category, colored distinctly).
- Works with standard axis labels and tick formatting to show dates or day numbers.


## REQ-25: Treemap

Display hierarchical or flat categorical data as space-filling rectangles sized by value — good for portfolios, file systems, and budget breakdowns.

```fsharp
// Flat treemap: (label, value) pairs
Series.treemap : (string * float) list -> Series
// SeriesKind: Treemap of labels: string list

let holdings =
    Series.treemap [
        "Equities",   52.0
        "Bonds",      23.0
        "Real Estate", 12.0
        "Commodities", 8.0
        "Cash",        5.0
    ]
    |> Graph.createWithSeries
    |> Graph.withTitle "Portfolio Allocation"
```

**Acceptance criteria:**

- Uses the squarified treemap algorithm to minimize aspect ratio of rectangles.
- Each rectangle labeled with name and optionally value/percentage.
- Colors cycle through theme pens.
- Axes suppressed automatically.
- Rectangles fill the full canvas area.


## REQ-26: Sunburst Diagram

Hierarchical radial chart where each ring represents a level of the hierarchy — an alternative to treemap for showing part-to-whole relationships in a circular layout.

```fsharp
type SunburstNode =
    {
        Label : string
        Value : float
        Parent : string option   // None = root node
    }

Series.sunburst : SunburstNode list -> Series
// SeriesKind: Sunburst of nodes: SunburstNode list

let org =
    Series.sunburst [
        { Label = "Engineering"; Value = 0.0; Parent = None }
        { Label = "Frontend";    Value = 12.0; Parent = Some "Engineering" }
        { Label = "Backend";     Value = 18.0; Parent = Some "Engineering" }
        { Label = "Design";      Value = 0.0; Parent = None }
        { Label = "UX";          Value =  8.0; Parent = Some "Design" }
    ]
```

**Acceptance criteria:**

- Root nodes form the inner ring; children form successive outer rings.
- Each node's arc is proportional to its value (or sum of children's values for branch nodes).
- Colors assigned by root-level category, children inherit with lighter opacity.
- Labels drawn inside arcs where space allows.
- Axes suppressed automatically.


## REQ-27: Bullet Chart

Compact performance visualization comparing an actual value against a target and qualitative range bands — a dashboard-friendly replacement for gauges.

```fsharp
type BulletRange =
    {
        Threshold : float
        Label : string option
    }

type BulletData =
    {
        Label : string option
        Actual : float
        Target : float
        Ranges : BulletRange list   // ordered low to high, colored light → dark
    }

Series.bullet : BulletData list -> Series
// SeriesKind: Bullet of bullets: BulletData list

let kpi =
    Series.bullet [
        { Label = Some "Revenue"
          Actual = 275.0
          Target = 260.0
          Ranges = [
            { Threshold = 200.0; Label = Some "Poor" }
            { Threshold = 250.0; Label = Some "OK" }
            { Threshold = 300.0; Label = Some "Good" }
          ] }
    ]
```

**Acceptance criteria:**

- Range bands render as horizontal bars of decreasing opacity (light = poor, dark = good).
- Actual value renders as a narrow foreground bar inside the ranges.
- Target renders as a short vertical tick mark.
- Multiple bullets stack vertically with consistent alignment.
- Axes suppressed automatically; label drawn to the left of each bullet row.


## REQ-28: Hexbin Chart

Aggregate scatter data into hexagonal bins to reveal density patterns in large datasets without overplotting.

```fsharp
// Input: same (x, y) scatter data — binning is automatic
Series.hexbin : float -> (float * float) list -> Series
// First arg: hex radius in data units
// SeriesKind: Hexbin of radius: float

let density =
    Series.hexbin 0.5 scatterPoints
    |> Graph.createWithSeries
    |> Graph.withTitle "Event Density"
```

**Acceptance criteria:**

- Scatter points are binned into a regular hexagonal grid.
- Each occupied bin renders as a hexagon; empty bins are invisible.
- Color encodes count per bin using the theme's heatmap color scale (or a custom `withColorScale`).
- A color ramp is rendered in the right margin (reusing the heatmap ramp).
- Hexagon orientation: flat-top.


## REQ-29: Ridgeline / Joy Plot

Compare distributions across many groups by stacking overlapping density curves vertically — useful when violin plots become too crowded.

```fsharp
// Each series is one group; yPosition controls vertical offset
Series.ridgeLine : float -> float list -> Series
// First arg: y position (group baseline in data space)
// Second arg: raw values for KDE
// SeriesKind: RidgeLine of rawValues: float list

let groups =
    [ "Q1", 1.0, q1Values
      "Q2", 2.0, q2Values
      "Q3", 3.0, q3Values ]
    |> List.map (fun (label, y, values) ->
        Series.ridgeLine y values |> Series.withLabel label)

let graph =
    Graph.create groups (xMin, xMax) (0.0, 4.0)
    |> Graph.withTheme Theme.light
```

**Acceptance criteria:**

- KDE is computed with Silverman's rule (reuse `CommonMath.gaussianKde`).
- Each group renders as a filled area curve at its y offset; the fill extends down to the baseline.
- Groups can overlap (ridgeline effect) — layering follows series order.
- Bandwidth adjustable via `Series.withPointRadius` (interpreted as a bandwidth multiplier).
- Y axis labels show group names via tick formatting.


## REQ-30: Calendar Heatmap

Day-of-week × week-of-year grid colored by value — the GitHub contribution graph pattern, useful for any daily time-series data.

```fsharp
// Input: (date, value) pairs — dates need not be contiguous
Series.calendarHeatmap : (System.DateTime * float) list -> Series
// SeriesKind: CalendarHeatmap
// Auto-lays out into week columns (X) × day-of-week rows (Y = 0..6, Mon–Sun)
// Missing days render as empty (no fill)

let activity =
    activityData
    |> Series.calendarHeatmap
    |> Graph.createWithSeries
    |> Graph.withTitle "Daily Commit Activity"
```

**Acceptance criteria:**

- Each day renders as a small square cell; cell size fills the canvas proportionally to the date span.
- X axis shows month abbreviations at the first week of each month.
- Y axis labels show day-of-week abbreviations (Mon–Sun).
- Color scale: white (zero/minimum) → accent color (maximum), using the theme's heatmap color scale.
- Color ramp rendered in right margin.
- Cells with missing data rendered as the background color.


## REQ-31: Dumbbell Chart

Show before/after change or two-group comparison per category using a line connecting two dots — cleaner than grouped bars for showing direction and magnitude of change.

```fsharp
// (y_row, x1, x2) triples — y_row is category position, x1/x2 are the two values
Series.dumbbell : (float * float * float) list -> Series
// SeriesKind: Dumbbell of x2Values: float list
// Points: (x1, y) pairs; x2 values stored in kind

let comparison =
    Series.dumbbell [
        1.0, 42.0, 67.0   // Category A: before=42, after=67
        2.0, 58.0, 51.0   // Category B: before=58, after=51
        3.0, 33.0, 78.0   // Category C: before=33, after=78
    ]
    |> Series.withLabel "Change 2023→2024"
```

**Acceptance criteria:**

- A horizontal line connects x1 to x2 at each y row.
- A filled dot (using `PointShape`) is drawn at both x1 and x2.
- Line color taken from series pen; dots match the line color.
- `withStrokeWidth` controls the connector line width.
- `withPointRadius` controls dot radius.


## REQ-32: Lollipop Chart

Bar-chart alternative that replaces filled rectangles with a thin stem and a dot — reduces visual weight and lets multiple series overlap more cleanly.

```fsharp
// Same data as bar: (x, value) pairs
Series.lollipop : (float * float) list -> Series
// SeriesKind: Lollipop

let scores =
    Series.lollipop [ 1.0, 72.0; 2.0, 88.0; 3.0, 61.0; 4.0, 95.0 ]
    |> Series.withLabel "Score"
```

**Acceptance criteria:**

- Each data point renders as a vertical line from y=0 to y=value, capped with a filled circle.
- `withStrokeWidth` controls stem width (default ~1.5 px canvas units).
- `withPointRadius` controls dot radius.
- `withPointShape` applies to the cap (circle, square, diamond, etc.).
- Horizontal variant supported: `Series.horizontalLollipop` mirrors `Series.horizontalBar`.
- Multiple lollipop series do NOT group side-by-side (unlike Bar) — they overlay.


## REQ-33: Bump Chart

Visualize rankings over time with smooth curves — ideal for showing how items rise and fall in position across ordered time periods.

```fsharp
// Same data as line: (x, rank) points — Y represents rank (1 = best, higher = worse)
Series.bumpLine : (float * float) list -> Series
// SeriesKind: BumpLine
// Renders smooth cubic bezier curves between points (not straight polyline segments)

let rankings =
    [ "Team A", [ 1.0, 3.0; 2.0, 1.0; 3.0, 2.0; 4.0, 4.0 ]
      "Team B", [ 1.0, 1.0; 2.0, 3.0; 3.0, 3.0; 4.0, 2.0 ] ]
    |> List.map (fun (label, pts) ->
        Series.bumpLine pts |> Series.withLabel label)
```

**Acceptance criteria:**

- Curves use cubic bezier interpolation between consecutive points (horizontal control points — S-curves).
- `withPointRadius > 0` draws a filled dot at each time step.
- Y axis tick labels can be set to category names via `withTickFormat` (to replace rank numbers with team/item names).
- All other `Line` modifiers (`withStrokeWidth`, `withStrokeDash`, `withOpacity`) apply.


## REQ-34: Dot Plot / Strip Plot

Show individual data points distributed along a single axis — better than a box/violin for small datasets (n < ~50) where individual points matter.

```fsharp
// Single-axis distribution: values placed along Y, jittered along X to reduce overlap
Series.stripPlot : float -> float list -> Series
// First arg: x position (category axis)
// Second arg: raw values (placed on Y axis)
// SeriesKind: StripPlot of xPosition: float
// Multiple strip series placed at different x positions for comparison

// Alternative: x-axis strip
Series.horizontalStrip : float -> float list -> Series
// yPosition: float, values on X axis

let control   = Series.stripPlot 1.0 controlValues   |> Series.withLabel "Control"
let treatment = Series.stripPlot 2.0 treatmentValues |> Series.withLabel "Treatment"
```

**Acceptance criteria:**

- Each value rendered as a small circle at (xPosition, value).
- Jitter applied along X (horizontal displacement) to prevent perfect stacking — jitter amount controllable via `withPointRadius` (doubles as jitter spread).
- `withPointShape` and `withPointRadius` apply to each dot.
- Deterministic jitter (seeded from point index) so SVG output is stable.
- Can be combined with `Series.boxAt` or `Series.violinAt` at the same x position for a layered view.
