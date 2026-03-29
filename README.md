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
