module BarCharts

open GraphVG
open SharpVG

let barChartGraph =
    // Quarterly revenue (USD millions) by product line
    let quarters = [ 1.0; 2.0; 3.0; 4.0 ]
    let mkSeries label values =
        List.zip quarters values |> Series.bar |> Series.withLabel label
    let productA = mkSeries "Product A" [ 42.0; 55.0; 61.0; 73.0 ]
    let productB = mkSeries "Product B" [ 28.0; 31.0; 45.0; 52.0 ]
    let productC = mkSeries "Product C" [ 15.0; 22.0; 38.0; 41.0 ]
    let allSeries = [ productA; productB; productC ]
    let quarterFormatter value =
        match value with
        | 1.0 -> "Q1"
        | 2.0 -> "Q2"
        | 3.0 -> "Q3"
        | 4.0 -> "Q4"
        | _ -> ""
    let xScale = Scale.linear (0.5, 4.5) (0.0, CommonMath.canvasSize)
    let yScale = Scale.linear (0.0, 90.0) (CommonMath.canvasSize, 0.0)
    Graph.create allSeries (0.5, 4.5) (0.0, 90.0)
    |> Graph.withTheme (Theme.light |> Theme.withPens [ Pen.steelBlue; Pen.coral; Pen.mediumSeaGreen ])
    |> Graph.withTitle "Quarterly Revenue by Product"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Middle)
    |> Graph.withAxes (
        Some (Axis.create Bottom xScale |> Axis.withTicks 4 |> Axis.withTickFormat quarterFormatter |> Axis.hideBoundsTick |> Axis.hideBoundsLabel),
        Some (Axis.create Left yScale |> Axis.withTicks 5 |> Axis.withLabel "$M"))
    |> Graph.withLegend (Legend.create LegendTop)

let waterfallGraph =
    // Annual cash flow bridge: starting cash → operating inflows/outflows → end balance
    // Points are (period index, delta); period 5 is marked as the running total bar
    let points =
        [ 0.0, 120.0   // starting balance
          1.0,  45.0   // product revenue
          2.0,  18.0   // services revenue
          3.0, -32.0   // cost of goods
          4.0, -24.0   // operating expenses
          5.0, -15.0   // R&D
          6.0,   0.0 ] // ending balance (total bar)
    let labels = [| "Start"; "Product"; "Services"; "COGS"; "OpEx"; "R&D"; "End" |]
    let labelFormatter value =
        labels |> Array.tryItem (int value) |> Option.defaultValue ""
    let xScale = Scale.linear (-0.5, 6.5) (0.0, CommonMath.canvasSize)
    let yScale = Scale.linear (0.0, 165.0) (CommonMath.canvasSize, 0.0)
    Series.waterfall points
    |> Series.withTotalAt [ 0.0; 6.0 ]
    |> Series.withLabel "Cash ($M)"
    |> Graph.createWithSeries
    |> Graph.withTheme Theme.light
    |> Graph.withTitle "Annual Cash Flow Bridge"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Middle)
    |> Graph.withAxes (
        Some (Axis.create Bottom xScale |> Axis.withTicks 7 |> Axis.withTickFormat labelFormatter |> Axis.hideBoundsTick |> Axis.hideBoundsLabel),
        Some (Axis.create Left yScale |> Axis.withTicks 5 |> Axis.withLabel "$M"))

let horizontalBarGraph =
    // Average daily screen time (hours) by app category — Statista 2024 approximate
    let categories = [ 7.0; 6.0; 5.0; 4.0; 3.0; 2.0; 1.0 ]
    let labels = [| "Social"; "Video"; "Games"; "Browser"; "Music"; "Shopping"; "News" |]
    let values = [ 1.8; 1.5; 0.9; 0.7; 0.5; 0.4; 0.3 ]
    let points = List.zip values categories
    let series = points |> Series.horizontalBar |> Series.withLabel "Hours/day"
    let catFormatter value =
        labels |> Array.tryItem (7 - int value) |> Option.defaultValue ""
    let xScale = Scale.linear (0.0, 2.2) (0.0, CommonMath.canvasSize)
    let yScale = Scale.linear (0.5, 7.5) (CommonMath.canvasSize, 0.0)
    Graph.create [ series ] (0.0, 2.2) (0.5, 7.5)
    |> Graph.withTheme (Theme.light |> Theme.withPens [ Pen.cornflowerBlue ])
    |> Graph.withTitle "Daily Screen Time by Category"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Middle)
    |> Graph.withAxes (
        Some (Axis.create Bottom xScale |> Axis.withTicks 5 |> Axis.withTickFormat (sprintf "%.1fh") |> Axis.withLabel "Hours per day"),
        Some (Axis.create Left yScale |> Axis.withTicks 7 |> Axis.withTickFormat catFormatter |> Axis.hideBoundsTick |> Axis.hideBoundsLabel))
