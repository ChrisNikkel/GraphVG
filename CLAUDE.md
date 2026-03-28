# GraphVG

F# library for generating SVG-based 2D scatter plots and line graphs using SharpVG.

## Project Structure

```
GraphVG/           # Main library (net8.0)
  Series.fs        # Series type: (float * float) list
  Theme.fs         # Styling: Background + Pen list
  Graph.fs         # Core transforms and rendering
  Axis.fs          # X/Y axis drawing
  GraphVG.fs       # High-level API
Examples/Example/  # Executable usage demo
Tests/             # xUnit test suite
```

## Build & Test

```sh
dotnet build
dotnet test
dotnet run --project Examples/Example
```

## Key Types

```fsharp
type Series = (float * float) list          // (x, y) data points

type Graph = {
    Series: Series list
    Domain: float * float                   // (min_x, max_x)
    Range:  float * float                   // (min_y, max_y)
}
```

## Typical Usage

```fsharp
let series1 = [(0.0, 0.0); (1.0, 1.0); (2.0, 4.0)]
let series2 = [(0.5, 0.25); (1.5, 2.25)]

let html = Graph.createWithSeries series1
           |> Graph.addSeries series2
           |> GraphVG.drawSeries
```

`GraphVG.drawSeries` returns an HTML string with an embedded 1000×1000 SVG.

## Architecture Notes

- All data coordinates are transformed to a fixed 1000×1000 SVG canvas via `Graph.toScaledSvgCoordinates`.
- `Graph.createWithSeries` auto-calculates domain/range with 10% padding.
- `Graph.addSeries` / `Graph.withPadding` recalculate bounds across all series.
- Y-axis is inverted during coordinate transform (SVG origin is top-left).
