# Changelog

All notable changes to GraphVG will be documented in this file.

## [Unreleased]

### Added
- `Histogram` series kind: `Series.histogram` and `Series.histogramWithBins` create bar charts from raw float values using automatic or explicit bin counts (Sturges' rule by default)
- `Box` series kind: `Series.box` and `Series.boxAt` render median, quartiles, whiskers, and caps from raw float values; box width controlled via `withPointRadius`

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
