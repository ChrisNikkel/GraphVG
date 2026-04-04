module LineCharts

open System
open GraphVG
open SharpVG

let private tau = 2.0 * Math.PI

let private circlePoints sampleCount =
    [ for index in 0 .. sampleCount ->
        let parameter = float index * tau / float sampleCount
        Math.Cos parameter, Math.Sin parameter ]

let centeredAxesGraph =
    let unitCircle =
        circlePoints 100
        |> Series.line
        |> Series.withLabel "Unit Circle"
    let lissajous =
        let scale = 1.0 / Math.Sqrt 2.0
        [ for index in 0 .. 200 ->
            let parameter = float index * tau / 200.0
            scale * Math.Sin(3.0 * parameter), scale * Math.Sin(2.0 * parameter + Math.PI / 4.0) - 0.05 ]
        |> Series.line
        |> Series.withLabel "Lissajous"
        |> Series.withStrokeDash Dashed
    let xScale = Scale.linear (-1.2, 1.2) (0.0, CommonMath.canvasSize)
    let yScale = Scale.linear (-1.2, 1.2) (CommonMath.canvasSize, 0.0)
    Graph.create [ unitCircle; lissajous ] (-1.2, 1.2) (-1.2, 1.2)
    |> Graph.withTheme Theme.light
    |> Graph.withTitle "Centered Axes"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Middle)
    |> Graph.withAxes (
        Some (Axis.create (HorizontalAt (Scale.apply yScale 0.0)) xScale |> Axis.withTickInterval 0.5 |> Axis.hideOrigin),
        Some (Axis.create (VerticalAt (Scale.apply xScale 0.0)) yScale |> Axis.withTickInterval 0.5 |> Axis.hideOrigin))

let axisStylesGraph =
    let months =
        [ 1.0, 18.0; 2.0, 24.0; 3.0, 29.0; 4.0, 39.0; 5.0, 43.0; 6.0, 52.0
          7.0, 61.0; 8.0, 68.0; 9.0, 74.0; 10.0, 82.0; 11.0, 87.0; 12.0, 94.0 ]
    let target =
        [ 1.0, 20.0; 12.0, 80.0 ]
        |> Series.line
        |> Series.withLabel "Target Band"
        |> Series.withStrokeDash Dashed
        |> Series.withOpacity 0.55
    let growth =
        months
        |> Series.line
        |> Series.withLabel "Adoption"
        |> Series.withStrokeWidth (Length.ofFloat 4.0)
    let milestones =
        months
        |> Series.scatter
        |> Series.withLabel "Checkpoints"
        |> Series.withPointRadius (Length.ofFloat 7.0)
    let xScale = Scale.linear (1.0, 12.0) (0.0, CommonMath.canvasSize)
    let yScale = Scale.linear (0.0, 100.0) (CommonMath.canvasSize, 0.0)
    let monthFormatter value =
        [ 1.0, "Jan"; 2.0, "Feb"; 3.0, "Mar"; 4.0, "Apr"; 5.0, "May"; 6.0, "Jun"
          7.0, "Jul"; 8.0, "Aug"; 9.0, "Sep"; 10.0, "Oct"; 11.0, "Nov"; 12.0, "Dec" ]
        |> List.tryPick (fun (tickValue, label) -> if CommonMath.isNear tickValue value then Some label else None)
        |> Option.defaultValue ""
    let themed =
        Theme.light
        |> Theme.withPlotBackground (Color.ofName HoneyDew)
        |> Theme.withPens [ Pen.seaGreen; Pen.tomato; Pen.steelBlue ]
        |> Theme.withGridPen (Pen.lightGray |> Pen.withOpacity 0.35)
    Graph.create [ target; growth; milestones ] (1.0, 12.0) (0.0, 100.0)
    |> Graph.withTheme themed
    |> Graph.withTitle "Axis Styling"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Middle)
    |> Graph.withAxes (
        Some (Axis.create Top xScale |> Axis.withTickInterval 2.0 |> Axis.withTickFormat monthFormatter |> Axis.withTickLength 10.0 |> Axis.withFontSize 14.0 |> Axis.withLabel "Campaign Timeline" |> Axis.hideBoundsTick |> Axis.hideBoundsLabel |> Axis.withSpine SpineStyle.Full),
        Some (Axis.create Right yScale |> Axis.withTicks 6 |> Axis.withTickFormat (sprintf "%.0f%%") |> Axis.withFontSize 14.0 |> Axis.withLabel "Coverage" |> Axis.hideBoundsTick |> Axis.withSpine SpineStyle.Hidden))
    |> Graph.withLegend (Legend.create LegendLeft)

let styledSeriesGraph =
    let baseline = 1.5
    let upperBand =
        [ for index in 0 .. 30 ->
            let x = float index / 3.0
            x, baseline + 2.0 + Math.Sin(x * 0.8) + 0.25 * Math.Cos(x * 2.4) ]
    let lowerBand = upperBand |> List.rev |> List.map (fun (x, _) -> x, baseline)
    let areaBand =
        upperBand @ lowerBand
        |> Series.area
        |> Series.withLabel "Band"
        |> Series.withOpacity 0.28
    let trendLine =
        upperBand
        |> Series.line
        |> Series.withLabel "Trend"
        |> Series.withStrokeWidth (Length.ofFloat 4.0)
        |> Series.withStrokeDash DashDot
    let highlights =
        upperBand
        |> List.indexed
        |> List.choose (fun (index, point) -> if index % 5 = 0 then Some point else None)
        |> Series.scatter
        |> Series.withLabel "Samples"
        |> Series.withPointRadius (Length.ofFloat 8.0)
    let themed =
        Theme.light
        |> Theme.withPlotBackground (Color.ofName FloralWhite)
        |> Theme.withPens [ Pen.steelBlue; Pen.tomato; Pen.darkGoldenRod ]
    Graph.create [ areaBand; trendLine; highlights ] (0.0, 10.0) (0.0, 5.5)
    |> Graph.withTheme themed
    |> Graph.withTitle "Series Styling"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Middle)
    |> Graph.withLegend (Legend.create LegendRight)

let stepLineGraph =
    // Utility electricity rate tiers that change at fixed hours — a classic step function.
    // All three step modes (After, Before, Mid) are overlaid so you can compare their geometry.
    let hourlyRates =
        [ 0.0, 0.08; 6.0, 0.11; 9.0, 0.22; 17.0, 0.18; 21.0, 0.11; 24.0, 0.08 ]
    let after =
        hourlyRates
        |> Series.stepLine
        |> Series.withLabel "After (default)"
        |> Series.withStrokeWidth (Length.ofFloat 3.0)
    let before =
        hourlyRates
        |> Series.stepLine
        |> Series.withStepMode Before
        |> Series.withLabel "Before"
        |> Series.withStrokeWidth (Length.ofFloat 2.0)
        |> Series.withStrokeDash Dashed
    let mid =
        hourlyRates
        |> Series.stepLine
        |> Series.withStepMode Mid
        |> Series.withLabel "Mid"
        |> Series.withStrokeWidth (Length.ofFloat 2.0)
        |> Series.withStrokeDash Dotted
    let xScale = Scale.linear (0.0, 24.0) (0.0, CommonMath.canvasSize)
    let yScale = Scale.linear (0.0, 0.28) (CommonMath.canvasSize, 0.0)
    Graph.create [ after; before; mid ] (0.0, 24.0) (0.0, 0.28)
    |> Graph.withTheme (Theme.light |> Theme.withPens [ Pen.steelBlue; Pen.tomato; Pen.seaGreen ])
    |> Graph.withTitle "Step Line"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Middle)
    |> Graph.withAxes (
        Some (Axis.create Bottom xScale |> Axis.withTickInterval 6.0 |> Axis.withTickFormat (fun v -> sprintf "%.0fh" v) |> Axis.withLabel "Hour of Day"),
        Some (Axis.create Left yScale |> Axis.withTicks 5 |> Axis.withTickFormat (sprintf "$%.2f") |> Axis.withLabel "Rate ($/kWh)"))
    |> Graph.withLegend (Legend.create LegendRight)

let bandGraph =
    // Monthly mean temperature with ±1σ uncertainty band, layered so the band renders below the line.
    let months = [ 1.0 .. 12.0 ]
    let means  = [ 3.2; 4.1; 7.8; 12.5; 17.3; 21.0; 23.4; 22.8; 18.6; 12.9; 7.2; 4.0 ]
    let stds   = [ 3.5; 3.2; 3.0; 2.8; 2.5; 2.2; 2.0; 2.2; 2.5; 3.0; 3.5; 3.8 ]
    let bandTriples = List.map3 (fun x m s -> x, m - s, m + s) months means stds
    let uncertainty =
        bandTriples
        |> Series.band
        |> Series.withLabel "±1σ"
    let meanLine =
        List.zip months means
        |> Series.line
        |> Series.withLabel "Mean"
        |> Series.withStrokeWidth (Length.ofFloat 3.5)
    let xScale = Scale.linear (1.0, 12.0) (0.0, CommonMath.canvasSize)
    let yScale = Scale.linear (-5.0, 30.0) (CommonMath.canvasSize, 0.0)
    let monthFormatter value =
        [ 1.0, "Jan"; 2.0, "Feb"; 3.0, "Mar"; 4.0, "Apr"; 5.0, "May"; 6.0, "Jun"
          7.0, "Jul"; 8.0, "Aug"; 9.0, "Sep"; 10.0, "Oct"; 11.0, "Nov"; 12.0, "Dec" ]
        |> List.tryPick (fun (v, l) -> if CommonMath.isNear v value then Some l else None)
        |> Option.defaultValue ""
    Graph.create [ uncertainty; meanLine ] (1.0, 12.0) (-5.0, 30.0)
    |> Graph.withTheme (Theme.light |> Theme.withPens [ Pen.steelBlue; Pen.steelBlue ])
    |> Graph.withTitle "Confidence Band"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Middle)
    |> Graph.withAxes (
        Some (Axis.create Bottom xScale |> Axis.withTicks 12 |> Axis.withTickFormat monthFormatter |> Axis.hideBoundsTick |> Axis.hideBoundsLabel),
        Some (Axis.create Left yScale |> Axis.withTicks 7 |> Axis.withTickFormat (sprintf "%.0f°C") |> Axis.withLabel "Temperature"))
    |> Graph.withLegend (Legend.create LegendRight)

let logScaleGraph =
    let responsePoints =
        [ 1.0, 1.0; 2.0, 1.4; 5.0, 2.0; 10.0, 2.6; 20.0, 3.2; 50.0, 4.0; 100.0, 4.7; 200.0, 5.2; 500.0, 6.0; 1000.0, 6.6 ]
    let lineSeries =
        responsePoints
        |> Series.line
        |> Series.withLabel "Response"
        |> Series.withStrokeWidth (Length.ofFloat 3.5)
    let markerSeries =
        responsePoints
        |> Series.scatter
        |> Series.withLabel "Samples"
        |> Series.withPointRadius (Length.ofFloat 6.0)
    let xScale = Scale.log (1.0, 1000.0) (0.0, CommonMath.canvasSize) 10.0
    let yScale = Scale.linear (0.0, 7.0) (CommonMath.canvasSize, 0.0)
    Graph.create [ lineSeries; markerSeries ] (1.0, 1000.0) (0.0, 7.0)
    |> Graph.withTheme Theme.empty
    |> Graph.withTitle "Log Scale"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Middle)
    |> Graph.withXScale xScale
    |> Graph.withYScale yScale
    |> Graph.withAxes (
        Some (Axis.create Bottom xScale |> Axis.withTickFormat (fun v -> sprintf "10^%.0f" (Math.Log10 v)) |> Axis.withLabel "Input Scale"),
        Some (Axis.create Left yScale |> Axis.withLabel "Response"))
    |> Graph.withLegend (Legend.create LegendRight)
