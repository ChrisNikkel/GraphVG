# Changelog

All notable changes to GraphVG will be documented in this file.

## [Unreleased]

### Added
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
