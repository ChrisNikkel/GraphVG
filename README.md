# GraphVG

A .NET library for F# to generate graph and chart visuals as SVG using SharpVG.

_[Pull requests](https://github.com/ChrisNikkel/GraphVG/pulls) and [suggestions](https://github.com/ChrisNikkel/GraphVG/issues) are greatly appreciated._

[Source code on GitHub](https://github.com/ChrisNikkel/GraphVG)

[![CI](https://github.com/ChrisNikkel/GraphVG/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/ChrisNikkel/GraphVG/actions/workflows/dotnet-ci.yml)

## Why GraphVG?

- Build graphs with an F#-first API instead of writing raw SVG by hand.
- Generate clean SVG output that can be embedded anywhere HTML/SVG is supported.
- Compose charts from reusable pieces: scales, axes, series, themes, annotations, and legends.
- Supports common chart types out of the box: line, scatter, and area.
- Supports linear and logarithmic scales.
- Minimal dependency footprint: GraphVG depends on SharpVG only.

## GraphVG Goals

- Make graph generation in F# simple and pipeline-friendly.
- Keep rendering deterministic and testable.
- Use strong types to reduce invalid chart state.
- Keep visual customization explicit and composable.

## Gallery

<!-- GALLERY:START -->
<table>
<tr><td align="center" width="320"><img src="docs/examples/centered-axes.svg" width="280" alt="Centered Axes" /><br /><b>Centered Axes</b></td><td align="center" width="320"><img src="docs/examples/axis-styles.svg" width="280" alt="Axis Styling" /><br /><b>Axis Styling</b></td><td align="center" width="320"><img src="docs/examples/series-styles.svg" width="280" alt="Series Styling" /><br /><b>Series Styling</b></td></tr>
<tr><td align="center" width="320"><img src="docs/examples/step-line.svg" width="280" alt="Step Line" /><br /><b>Step Line</b></td><td align="center" width="320"><img src="docs/examples/confidence-band.svg" width="280" alt="Confidence Band" /><br /><b>Confidence Band</b></td><td align="center" width="320"><img src="docs/examples/log-scale.svg" width="280" alt="Log Scale" /><br /><b>Log Scale</b></td></tr>
<tr><td align="center" width="320"><img src="docs/examples/stacked-area.svg" width="280" alt="Stacked Area" /><br /><b>Stacked Area</b></td><td align="center" width="320"><img src="docs/examples/stacked-area-percent.svg" width="280" alt="Normalized Stacked Area" /><br /><b>Normalized Stacked Area</b></td><td align="center" width="320"><img src="docs/examples/streamgraph.svg" width="280" alt="Streamgraph" /><br /><b>Streamgraph</b></td></tr>
<tr><td align="center" width="320"><img src="docs/examples/bar-chart.svg" width="280" alt="Grouped Bar Chart" /><br /><b>Grouped Bar Chart</b></td><td align="center" width="320"><img src="docs/examples/waterfall.svg" width="280" alt="Waterfall Chart" /><br /><b>Waterfall Chart</b></td><td align="center" width="320"><img src="docs/examples/pie-chart.svg" width="280" alt="Pie Chart" /><br /><b>Pie Chart</b></td></tr>
<tr><td align="center" width="320"><img src="docs/examples/parallel-sets.svg" width="280" alt="Parallel Sets" /><br /><b>Parallel Sets</b></td><td align="center" width="320"><img src="docs/examples/horizontal-bar.svg" width="280" alt="Horizontal Bar Chart" /><br /><b>Horizontal Bar Chart</b></td><td align="center" width="320"><img src="docs/examples/tooltips.svg" width="280" alt="Tooltips" /><br /><b>Tooltips</b></td></tr>
<tr><td align="center" width="320"><img src="docs/examples/bubble-chart.svg" width="280" alt="Bubble Chart" /><br /><b>Bubble Chart</b></td><td align="center" width="320"><img src="docs/examples/heatmap.svg" width="280" alt="Heatmap" /><br /><b>Heatmap</b></td><td align="center" width="320"><img src="docs/examples/histogram.svg" width="280" alt="Histogram" /><br /><b>Histogram</b></td></tr>
<tr><td align="center" width="320"><img src="docs/examples/violin-plot.svg" width="280" alt="Violin Plot" /><br /><b>Violin Plot</b></td><td align="center" width="320"><img src="docs/examples/box-plot.svg" width="280" alt="Box Plot" /><br /><b>Box Plot</b></td><td align="center" width="320"><img src="docs/examples/radar-chart.svg" width="280" alt="Radar Chart" /><br /><b>Radar Chart</b></td></tr>
<tr><td align="center" width="320"><img src="docs/examples/pie-chart-standalone.svg" width="280" alt="Pie Chart" /><br /><b>Pie Chart</b></td><td align="center" width="320"><img src="docs/examples/donut-chart.svg" width="280" alt="Donut Chart" /><br /><b>Donut Chart</b></td><td align="center" width="320"><img src="docs/examples/plot-trig.svg" width="280" alt="Trig Functions" /><br /><b>Trig Functions</b></td></tr>
<tr><td align="center" width="320"><img src="docs/examples/plot-tan.svg" width="280" alt="tan(x) — Discontinuities" /><br /><b>tan(x) — Discontinuities</b></td><td align="center" width="320"><img src="docs/examples/plot-gaussian.svg" width="280" alt="Gaussian + Derivative" /><br /><b>Gaussian + Derivative</b></td><td align="center" width="320"><img src="docs/examples/theme-gameboy-green.svg" width="280" alt="Theme: Game Boy Green" /><br /><b>Theme: Game Boy Green</b></td></tr>
<tr><td align="center" width="320"><img src="docs/examples/theme-crispy-commodore.svg" width="280" alt="Theme: Crispy Commodore" /><br /><b>Theme: Crispy Commodore</b></td><td align="center" width="320"><img src="docs/examples/theme-ti-hue.svg" width="280" alt="Theme: TI Hue" /><br /><b>Theme: TI Hue</b></td><td align="center" width="320"><img src="docs/examples/theme-nes.svg" width="280" alt="Theme: NES" /><br /><b>Theme: NES</b></td></tr>
<tr><td align="center" width="320"><img src="docs/examples/theme-turtle.svg" width="280" alt="Theme: Turtle" /><br /><b>Theme: Turtle</b></td><td align="center" width="320"><img src="docs/examples/dual-axis.svg" width="280" alt="Dual Y-Axis" /><br /><b>Dual Y-Axis</b></td><td align="center" width="320"><img src="docs/examples/funnel-conversion.svg" width="280" alt="Funnel Chart" /><br /><b>Funnel Chart</b></td></tr>
<tr><td align="center" width="320"><img src="docs/examples/funnel-pipeline.svg" width="280" alt="Sales Pipeline Funnel" /><br /><b>Sales Pipeline Funnel</b></td><td></td><td></td></tr>
</table>
<!-- GALLERY:END -->

## Examples

```fsharp
open GraphVG
open SharpVG

let points =
    [
        1.0, 1.0
        2.0, 1.4
        5.0, 2.0
        10.0, 2.6
        20.0, 3.2
        50.0, 4.0
        100.0, 4.7
    ]

let response =
    points
    |> Series.line
    |> Series.withLabel "Response"
    |> Series.withStrokeWidth (Length.ofFloat 3.0)

let samples =
    points
    |> Series.scatter
    |> Series.withLabel "Samples"
    |> Series.withPointRadius (Length.ofFloat 6.0)

let xScale = Scale.log (1.0, 100.0) (0.0, CommonMath.canvasSize) 10.0
let yScale = Scale.linear (0.0, 5.0) (CommonMath.canvasSize, 0.0)

let graph =
    Graph.create [ response; samples ] (1.0, 100.0) (0.0, 5.0)
    |> Graph.withXScale xScale
    |> Graph.withYScale yScale
    |> Graph.withTitle "Log Scale"
    |> Graph.withAxes
        (
            Some (Axis.create Bottom xScale |> Axis.withLabel "Input Scale"),
            Some (Axis.create Left yScale |> Axis.withLabel "Response")
        )
    |> Graph.withLegend (Legend.create LegendRight)

let svg = GraphVG.toSvg graph
let html = GraphVG.toHtml graph
```

Simple start:

```fsharp
let html =
    Graph.create [ unitCircle; lissajous ] (-1.2, 1.2) (-1.2, 1.2)
    |> Graph.withTheme Theme.light
    |> GraphVG.toHtml
```

Layout spacing is configurable per graph:

```fsharp
let spacious =
    LayoutSpacing.create 28.0 24.0 10.0 6.0

let svg =
    Graph.create [ response; samples ] (1.0, 100.0) (0.0, 5.0)
    |> Graph.withLayoutSpacing spacious
    |> GraphVG.toSvg
```

## Building GraphVG

Clone the repository:

```bash
git clone https://github.com/ChrisNikkel/GraphVG.git
cd GraphVG
```

Build:

```bash
dotnet build
```

Run tests:

```bash
dotnet test
```

Run the example gallery generator:

```bash
dotnet run --project Examples/Example
```

## Project Structure

```text
REQUIREMENTS.md     # Planned requirements/bugs
DESIGN.md           # Architecture and design notes
GraphVG/            # Main library (net8.0)
Examples/Example/   # Executable usage demo
Tests/              # xUnit and FsCheck test suite
```

## Documentation

- Design notes: [DESIGN.md](DESIGN.md)
- Requirements/backlog: [REQUIREMENTS.md](REQUIREMENTS.md)
- Developer guidance: [CLAUDE.md](CLAUDE.md)
- Contributing guide: [CONTRIBUTING.md](CONTRIBUTING.md)
- Security policy: [SECURITY.md](SECURITY.md)

## Support

Please submit bugs and feature requests [here](https://github.com/ChrisNikkel/GraphVG/issues).

## Library License

The library is available under the MIT license. For more information see [LICENSE.md](LICENSE.md).

## Maintainer(s)

- [@ChrisNikkel](https://github.com/ChrisNikkel)
