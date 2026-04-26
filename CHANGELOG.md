# Changelog

All notable changes to GraphVG will be documented in this file.

## [Unreleased]

### Changed
- Adaptive canvas resolution: annotation constants (tick length, font size, margins, legend swatches) are now expressed as fractions of `canvasSize`; `Graph.CanvasSize` is set automatically from the domain span via `adaptiveCanvasSize`, which scales up by one decade per decade of span above 1000 to maintain SVG coordinate precision for large-magnitude data

### Added
- `Treemap` series kind: `Series.treemap` takes `(label, value)` pairs and lays out space-filling rectangles using the squarified algorithm; each rectangle is labeled with its name and percentage of total; colors cycle through theme pens; axes suppressed automatically
- `Funnel` series kind: `Series.funnel` takes `(label, value)` pairs and renders horizontally-centered trapezoids top to bottom; each stage's width is proportional to its value; stage name and value drawn in white text inside each segment; colors cycle through theme pens; axes suppressed automatically
- Dual Y-axis: `Series.onRightAxis` tags a series to the right Y scale; `Graph.withRightYRange` sets the independent right domain and auto-creates a `Right`-positioned axis; `Graph.withRightAxis` allows full override; `recalcBounds` keeps left and right scales independent
- `PlotExpr` type and `Plot` module: parse infix math expressions (`"sin(x)*x^2"`) via `Plot.parse`; sample to a `SegmentedLine` series via `Plot.toSeries`; compute symbolic derivatives via `Plot.derivative`; auto-scale y-range via `Plot.autoRange`; find roots via `Plot.roots`; discontinuities (asymptotes) produce SVG path breaks rather than lines through infinity; backed by MathNet.Symbolics (MIT)
- `SegmentedLine` series kind: like `Line` but renders as a SharpVG `Path` with `M`/`L` sub-paths, supporting `(nan, nan)` break sentinels inserted by `Plot.toSeries` at discontinuities
- Retro themes: `Theme.gameboyGreen` (DMG-001 four-shade green palette), `Theme.crispyCommodore` (C64 blue background with hardware palette), `Theme.tiHue` (TI-84 Plus CE dark navy with bright function colors), `Theme.nes` (NES PPU hardware colors on black); all four added to `ThemePreset` DU alongside the existing `Turtle` case
- `PieChart` type and `PieChart.create`/`toSvg`/`toHtml`: standalone pie and donut chart; `withInnerRadius` (0 = full pie, clamped to 0.95) switches to donut mode and displays the total in the center hole; `withStartAngle` rotates the start position; percentage labels inside each slice; right-side legend with color swatch, label, and percentage; slice colors cycle through the active theme palette
- `RadarChart` type and `RadarChart.create`/`toSvg`/`toHtml`: radar (spider) chart with evenly-spaced radial axes, concentric ring grid, filled series polygons at 20% opacity with outlined borders, axis labels at the perimeter, ring value labels, and optional per-series legend; `withTheme`, `withTitle`, `withLabels`, `withRingCount`, `withTitleFontSize` modifiers
- `Series.withTooltip`: attach a `(float * float -> string)` function to any series; renders per-point SVG `<title>` elements as invisible hit-target circles; works in both `toSvg` and `toHtml` with no JavaScript; tooltip text is automatically XML-escaped
- `Violin` series kind: `Series.violin` and `Series.violinAt` take raw float values and render a symmetric KDE outline with a box-plot overlay (IQR box, median line, whiskers); bandwidth is chosen automatically via Silverman's rule; violin width is configurable via `withPointRadius`; `CommonMath.silvermanBandwidth` and `CommonMath.gaussianKde` are public helpers
- `Waterfall` series kind: `Series.waterfall` takes `(x, delta)` pairs and renders a cumulative bridge chart; `Series.withTotalAt` marks x positions as total bars that draw from zero; positive deltas use `Theme.UpColor`, negative use `DownColor`, totals use the axis pen color; dashed connector lines link consecutive bar tops; auto-bounds cover the full running-total range
- `PriceBar` type and `Candlestick`/`StockBar` series kinds: `Series.candlestick` renders filled bodies between open and close with wicks to high and low; `Series.stockBar` renders the classic bar form (wick + open/close ticks); body color follows `Theme.UpColor`/`DownColor`; `Theme.withUpColor`/`withDownColor` allow overrides; auto-bounds cover all four price values
- `Theme.UpColor`/`DownColor` fields added to all built-in themes (SeaGreen/Crimson for light, LimeGreen/Crimson for dark, Lime/Red for high-contrast)
- `ErrorBar` type and `Series.withErrorBars`: attach symmetric or asymmetric Y error bars to any series; returns `Result<Series, string>` on length mismatch; auto-bounds expand to cover the full error extent; each point renders as a vertical line with horizontal caps
- `LayoutSpacing` type and `Graph.withLayoutSpacing`: configure outer margin, title padding, axis-label padding, and tick-label padding per graph while preserving the previous built-in defaults
- `Band` series kind: `Series.band` takes `(x, yLow, yHigh)` triples and renders a filled region between the two bounds; fill defaults to 0.3 opacity; stroke added via `withStrokeWidth`; auto-bounds cover both yLow and yHigh values
- `StepLine` series kind: `Series.stepLine` connects points with horizontal-then-vertical segments; `Series.withStepMode` accepts `After` (default), `Before`, or `Mid`; compatible with `withStrokeDash`, `withStrokeWidth`, and all other series modifiers
- `Heatmap` series kind: `Series.heatmap` takes `(col, row, value)` triples and renders a grid of colored rectangles; `Series.withColorScale` accepts a custom `float -> Color` mapping; default palette interpolates white → steelblue; a color ramp with min/max labels is rendered automatically alongside the graph; cell size is inferred from grid spacing
- `Theme.defaultHeatmapColorScale`: built-in white-to-steelblue color scale exposed for use in custom pipelines
- `Bubble` series kind: `Series.bubble` takes `(x, y, size)` triples and renders area-proportional circles; `Series.withBubbleSizes` attaches sizes to an existing series; zero or negative sizes render as invisible; legend swatch is a circle
- `ThemePreset` type: `Light | Dark | HighContrast`; resolve to a `Theme` via `Theme.preset`
- `Theme.highContrast`: black background with bright primary-color pens and a subtle white grid
- `Graph.withDefaultTheme`: apply a preset or baseline theme with the same pipeline API as `withTheme`
- `DomainPolicy` type: `Padded of float | Tight | IncludeZero` controls how auto-bounds are computed; set via `Graph.withDomainPolicy`
- `Bar` series kind: `Series.bar` renders vertical grouped bars; multiple `Bar` series at the same x positions are automatically laid out side-by-side
- `HorizontalBar` series kind: `Series.horizontalBar` renders horizontal bars with values on the x-axis and categories on the y-axis; multiple series group automatically
- `Histogram` series kind: `Series.histogram` and `Series.histogramWithBins` create bar charts from raw float values using automatic or explicit bin counts (Sturges' rule by default)
- `Box` series kind: `Series.box` and `Series.boxAt` render median, quartiles, whiskers, and caps from raw float values; box width controlled via `withPointRadius`
- `StackedArea` series kind: `Series.stackedArea` stacks multiple area series with cumulative absolute values
- `NormalizedStackedArea` series kind: `Series.normalizedStackedArea` normalizes each column to 100%, showing proportional share
- `Streamgraph` series kind: `Series.streamgraph` applies the wiggle baseline so all streams are symmetrically centered around y=0

## [0.1.0] - 2026-03-29

### Added
- `Scale` module: linear and logarithmic data-to-pixel mapping with `apply` and `invert`
- `Series` module: `Scatter`, `Line`, and `Area` series kinds with per-series label and color overrides
- `Theme` module: `light` and `dark` built-in themes; pen cycling for multi-series charts
- `Axis` module: tick generation, tick labels, and optional grid lines for both axes
- `Legend` module: configurable `LegendPosition` (`TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`, `Hidden`) with color swatches
- `Annotation` module: chart title and subtitle with `TitleStyle` configuration
- `Graph` module: core graph record; `create` / `createWithSeries` builders; `with*` setters; automatic domain/range padding; coordinate transform (SVG origin inversion)
- `Layout` module: padding computation, viewBox, background, and plot-background elements
- `GraphVG` module: SVG and HTML assembly; `toSvg` / `toHtml` public API
- `CommonMath` module: pure float helpers — `estimatedTextWidth`, `centerPolygon`, `centerLines`, unit shapes
- xUnit + FsCheck property-based test suite
- `Examples/Example` executable gallery
