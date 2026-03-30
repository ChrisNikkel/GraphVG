module DistributionCharts

open System
open GraphVG
open SharpVG

let private tau = 2.0 * Math.PI

let histogramGraph =
    let rng = Random(42)
    let values =
        [ for _ in 1 .. 300 ->
            let u1 = rng.NextDouble()
            let u2 = rng.NextDouble()
            5.0 + 1.5 * Math.Sqrt(-2.0 * Math.Log u1) * Math.Cos(tau / 2.0 * u2) ]
    Series.histogram values
    |> Series.withLabel "Count"
    |> Graph.createWithSeries
    |> Graph.withTheme (Theme.light |> Theme.withPlotBackground (Color.ofName AliceBlue))
    |> Graph.withTitle "Histogram"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Middle)

let boxPlotGraph =
    let rng = Random(7)
    let sample mean std n =
        [ for _ in 1 .. n ->
            let u1 = rng.NextDouble()
            let u2 = rng.NextDouble()
            mean + std * Math.Sqrt(-2.0 * Math.Log u1) * Math.Cos(tau / 2.0 * u2) ]
    let groupA = Series.boxAt 1.0 (sample 5.0 1.2 80) |> Series.withLabel "Group A"
    let groupB = Series.boxAt 2.0 (sample 6.5 0.8 80) |> Series.withLabel "Group B"
    let groupC = Series.boxAt 3.0 (sample 4.5 1.8 80) |> Series.withLabel "Group C"
    let xScale = Scale.linear (0.0, 4.0) (0.0, CommonMath.canvasSize)
    let yScale = Scale.linear (0.0, 12.0) (CommonMath.canvasSize, 0.0)
    let labelFormatter value =
        match value with
        | 1.0 -> "A"
        | 2.0 -> "B"
        | 3.0 -> "C"
        | _ -> ""
    Graph.create [ groupA; groupB; groupC ] (0.0, 4.0) (0.0, 12.0)
    |> Graph.withTheme (Theme.light |> Theme.withPens [ Pen.steelBlue; Pen.tomato; Pen.seaGreen ])
    |> Graph.withTitle "Box Plot"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Middle)
    |> Graph.withAxes (
        Some (Axis.create Bottom xScale |> Axis.withTicks 3 |> Axis.withTickFormat labelFormatter |> Axis.hideBoundsTick |> Axis.hideBoundsLabel),
        Some (Axis.create Left yScale |> Axis.withTicks 6))
    |> Graph.withLegend (Legend.create LegendRight)
