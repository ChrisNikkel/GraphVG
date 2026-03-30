module AreaCharts

open GraphVG
open SharpVG

// US Net Electricity Generation by Source (TWh), 2014–2024
// Source: EIA Electric Power Annual, Table 3.1.A

let private electricityPoints =
    let coal =       [ 2014.0, 1581.7; 2015.0, 1352.4; 2016.0, 1239.1; 2017.0, 1205.8; 2018.0, 1149.5
                       2019.0,  965.0; 2020.0,  773.4; 2021.0,  898.0; 2022.0,  831.5; 2023.0,  675.1; 2024.0,  652.2 ]
    let naturalGas = [ 2014.0, 1138.7; 2015.0, 1347.8; 2016.0, 1392.1; 2017.0, 1310.2; 2018.0, 1485.3
                       2019.0, 1601.1; 2020.0, 1638.6; 2021.0, 1590.6; 2022.0, 1698.8; 2023.0, 1817.8; 2024.0, 1880.7 ]
    let nuclear =    [ 2014.0,  797.2; 2015.0,  797.2; 2016.0,  805.7; 2017.0,  804.9; 2018.0,  807.1
                       2019.0,  809.4; 2020.0,  789.9; 2021.0,  779.6; 2022.0,  771.5; 2023.0,  774.9; 2024.0,  781.9 ]
    let hydro =      [ 2014.0,  259.4; 2015.0,  249.1; 2016.0,  267.8; 2017.0,  300.3; 2018.0,  292.5
                       2019.0,  287.9; 2020.0,  285.3; 2021.0,  251.6; 2022.0,  254.8; 2023.0,  245.0; 2024.0,  242.9 ]
    let wind =       [ 2014.0,  261.5; 2015.0,  270.3; 2016.0,  305.6; 2017.0,  333.0; 2018.0,  350.5
                       2019.0,  368.9; 2020.0,  408.5; 2021.0,  448.4; 2022.0,  502.2; 2023.0,  484.7; 2024.0,  513.7 ]
    let solar =      [ 2014.0,   28.9; 2015.0,   39.0; 2016.0,   54.9; 2017.0,   77.3; 2018.0,   93.4
                       2019.0,  106.9; 2020.0,  130.7; 2021.0,  164.4; 2022.0,  205.1; 2023.0,  238.9; 2024.0,  303.8 ]
    [ "Coal", coal; "Natural Gas", naturalGas; "Nuclear", nuclear; "Hydro", hydro; "Wind", wind; "Solar", solar ]

let stackedAreaGraph =
    let allSeries =
        electricityPoints
        |> List.map (fun (label, pts) -> pts |> Series.stackedArea |> Series.withLabel label)
    let xScale = Scale.linear (2014.0, 2024.0) (0.0, CommonMath.canvasSize)
    let yScale = Scale.linear (0.0, 4600.0) (CommonMath.canvasSize, 0.0)
    let blueScheme =
        Theme.light
        |> Theme.withPens [ Pen.navy; Pen.royalBlue; Pen.steelBlue; Pen.cornflowerBlue; Pen.lightSteelBlue; Pen.powderBlue ]
    Graph.create allSeries (2014.0, 2024.0) (0.0, 4600.0)
    |> Graph.withTheme blueScheme
    |> Graph.withTitle "US Electricity by Source"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Middle)
    |> Graph.withAxes (
        Some (Axis.create Bottom xScale |> Axis.withTickInterval 2.0 |> Axis.withTickFormat (sprintf "%.0f")),
        Some (Axis.create Left yScale |> Axis.withTicks 6 |> Axis.withLabel "TWh"))
    |> Graph.withLegend (Legend.create LegendTop)

let normalizedStackedAreaGraph =
    let allSeries =
        electricityPoints
        |> List.map (fun (label, pts) -> pts |> Series.normalizedStackedArea |> Series.withLabel label)
    let xScale = Scale.linear (2014.0, 2024.0) (0.0, CommonMath.canvasSize)
    let yScale = Scale.linear (0.0, 100.0) (CommonMath.canvasSize, 0.0)
    let autumnScheme =
        Theme.light
        |> Theme.withPens [ Pen.fireBrick; Pen.sienna; Pen.peru; Pen.darkGoldenRod; Pen.sandyBrown; Pen.coral ]
    Graph.create allSeries (2014.0, 2024.0) (0.0, 100.0)
    |> Graph.withTheme autumnScheme
    |> Graph.withTitle "Normalized Stacked Area"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Middle)
    |> Graph.withAxes (
        Some (Axis.create Bottom xScale |> Axis.withTickInterval 2.0 |> Axis.withTickFormat (sprintf "%.0f")),
        Some (Axis.create Left yScale |> Axis.withTicks 5 |> Axis.withTickFormat (sprintf "%.0f%%") |> Axis.withLabel "Share"))
    |> Graph.withLegend (Legend.create LegendTop)

let streamgraphGraph =
    // Annual console hardware sales (millions of units), 1995–2012
    // Source: Wikipedia console articles, VGChartz, Nintendo/Sony/Microsoft annual reports
    let years = [ for y in 1995 .. 2012 -> float y ]
    let mk label values = List.zip years values |> Series.streamgraph |> Series.withLabel label
    let ps1    = mk "PlayStation" [ 5.5; 15.0; 20.0; 21.0; 20.0; 10.0; 6.0; 4.0; 2.0; 1.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0 ]
    let n64    = mk "N64"         [ 0.0; 5.5; 10.0; 9.0; 9.0; 6.0; 3.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0 ]
    let saturn = mk "Saturn"      [ 2.5; 4.5; 4.0; 2.0; 0.5; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0 ]
    let dc     = mk "Dreamcast"   [ 0.0; 0.0; 0.0; 1.5; 4.5; 3.0; 1.5; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0 ]
    let ps2    = mk "PS2"         [ 0.0; 0.0; 0.0; 0.0; 0.0; 10.0; 22.0; 22.0; 22.0; 20.0; 16.0; 16.0; 14.0; 11.0; 7.3; 4.1; 4.0; 0.0 ]
    let xbox   = mk "Xbox"        [ 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 4.0; 7.0; 6.0; 6.5; 1.5; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0 ]
    let gc     = mk "GameCube"    [ 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 4.0; 6.0; 5.0; 4.0; 3.0; 1.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0 ]
    let ds     = mk "DS"          [ 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 2.5; 11.0; 17.0; 23.0; 31.0; 27.0; 18.0; 15.0; 10.0 ]
    let psp    = mk "PSP"         [ 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.8; 8.0; 10.0; 11.0; 11.0; 10.0; 8.0; 7.0; 4.0 ]
    let wii    = mk "Wii"         [ 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 5.5; 18.5; 26.0; 21.0; 15.0; 11.0; 3.9 ]
    let x360   = mk "Xbox 360"    [ 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 4.5; 10.0; 13.0; 11.0; 11.0; 13.7; 13.9; 11.6 ]
    let ps3    = mk "PS3"         [ 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 3.8; 9.0; 10.0; 13.0; 14.3; 14.0; 16.5 ]
    let allSeries = [ ps1; n64; saturn; dc; ps2; xbox; gc; ds; psp; wii; x360; ps3 ]
    let xScale = Scale.linear (1995.0, 2012.0) (0.0, CommonMath.canvasSize)
    let yScale = Scale.linear (-60.0, 60.0) (CommonMath.canvasSize, 0.0)
    let rainbowScheme =
        Theme.light
        |> Theme.withPens [ Pen.red; Pen.orangeRed; Pen.orange; Pen.goldenRod; Pen.yellowGreen; Pen.limeGreen; Pen.mediumSeaGreen; Pen.mediumAquamarine; Pen.cornflowerBlue; Pen.royalBlue; Pen.blueViolet; Pen.hotPink ]
    Graph.create allSeries (1995.0, 2012.0) (-60.0, 60.0)
    |> Graph.withTheme rainbowScheme
    |> Graph.withTitle "Console Wars 1995–2012"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Middle)
    |> Graph.withAxes (
        Some (Axis.create Bottom xScale |> Axis.withTickInterval 5.0 |> Axis.withTickFormat (sprintf "%.0f") |> Axis.withSpine SpineStyle.Hidden),
        None)
    |> Graph.withLegend (Legend.create LegendBottom)
