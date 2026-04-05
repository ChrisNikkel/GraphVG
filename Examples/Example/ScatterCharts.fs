module ScatterCharts

open System
open GraphVG
open SharpVG

// GDP per capita (thousand USD) vs life expectancy (years), bubble size = population (millions).
// Approximate 2023 data; illustrates area-proportional bubble sizing across continents.
let bubbleChartGraph =
    let americas =
        Series.bubble [
            76.3, 78.5, 340.0   // United States
            52.0, 82.8, 38.0    // Canada
            10.7, 75.0, 128.0   // Mexico
            9.2,  75.1, 215.0   // Brazil
        ]
        |> Series.withLabel "Americas"

    let europe =
        Series.bubble [
            51.9, 81.1, 84.0    // Germany
            45.9, 81.3, 68.0    // United Kingdom
            43.0, 82.4, 68.0    // France
            33.2, 83.4, 60.0    // Italy
        ]
        |> Series.withLabel "Europe"

    let asia =
        Series.bubble [
            34.0, 84.3, 125.0   // Japan
            32.9, 83.5, 52.0    // South Korea
            12.1, 77.9, 1411.0  // China
            2.5,  69.9, 1440.0  // India
        ]
        |> Series.withLabel "Asia"

    let africa =
        Series.bubble [
            7.1,  64.7, 60.0    // South Africa
            3.9,  69.2, 46.0    // Algeria
            2.2,  54.7, 220.0   // Nigeria
            2.0,  66.5, 115.0   // Ethiopia
        ]
        |> Series.withLabel "Africa"

    Graph.create [ americas; europe; asia; africa ] (0.0, 85.0) (50.0, 90.0)
    |> Graph.withTheme Theme.light
    |> Graph.withTitle "GDP per Capita vs Life Expectancy"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Center)
    |> Graph.withLegend (Legend.create LegendRight)

// Scatter chart with per-point tooltips showing exact coordinates.
// Hover over any point to see its value — uses SVG <title> elements, no JavaScript.
let tooltipScatterGraph =
    let rng = Random(7)
    // Fictional sensor readings: temperature (°C) vs pressure (hPa), 20 samples
    let readings =
        [ for _ in 1 .. 20 ->
            let t = 15.0 + rng.NextDouble() * 20.0
            let p = 1000.0 + (t - 25.0) * 1.5 + (rng.NextDouble() - 0.5) * 12.0
            t, p ]
    let tooltip (x, y) = sprintf "Temp: %.1f °C\nPressure: %.1f hPa" x y
    Series.scatter readings
    |> Series.withTooltip tooltip
    |> Series.withLabel "Sensor A"
    |> Series.withPointRadius 5.0
    |> Graph.createWithSeries
    |> Graph.withTheme Theme.light
    |> Graph.withTitle "Temperature vs Pressure (hover for values)"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Center)
    |> Graph.withAxes (
        Some (Axis.create Bottom (Scale.linear (14.0, 36.0) (0.0, CommonMath.canvasSize))
              |> Axis.withTicks 6
              |> Axis.withLabel "Temperature (°C)"),
        Some (Axis.create Left (Scale.linear (988.0, 1022.0) (CommonMath.canvasSize, 0.0))
              |> Axis.withTicks 5
              |> Axis.withLabel "Pressure (hPa)"))

// 2D heatmap: weekly step counts by day and hour, showing activity patterns.
// Rows 0–6 = Mon–Sun; cols 0–23 = midnight–11 pm.
let heatmapGraph =
    let rng = Random(42)
    let activityWeight hour =
        // Morning and evening peaks; low overnight
        let morning = exp (-((float hour - 8.0) ** 2.0) / 8.0)
        let evening = exp (-((float hour - 18.0) ** 2.0) / 8.0)
        morning + evening * 0.8 + 0.05
    let weekendFactor day = if day >= 5 then 0.6 else 1.0
    let cells =
        [ for day in 0 .. 6 do
            for hour in 0 .. 23 ->
                float hour, float day,
                activityWeight hour * weekendFactor day * (200.0 + rng.NextDouble() * 300.0) ]
    Series.heatmap cells
    |> Graph.createWithSeries
    |> Graph.withTheme Theme.light
    |> Graph.withTitle "Weekly Step Counts by Hour"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Center)
    |> Graph.withAxes (
        Some (Axis.create Bottom (Scale.linear (-0.5, 23.5) (0.0, CommonMath.canvasSize))
              |> Axis.withTicks 24
              |> Axis.withTickFormat (fun h -> if h = 0.0 then "12a" elif h = 12.0 then "12p" elif h < 12.0 then sprintf "%ga" h else sprintf "%gp" (h - 12.0))
              |> Axis.hideBoundsTick),
        Some (Axis.create Left (Scale.linear (-0.5, 6.5) (CommonMath.canvasSize, 0.0))
              |> Axis.withTicks 7
              |> Axis.withTickFormat (fun d -> [| "Mon"; "Tue"; "Wed"; "Thu"; "Fri"; "Sat"; "Sun" |].[int d])
              |> Axis.hideBoundsTick))
