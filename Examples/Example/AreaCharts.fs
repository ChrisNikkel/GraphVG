module AreaCharts

open GraphVG
open SharpVG

// US Net Electricity Generation by Source (TWh), biennial 1990–2024
// Source: EIA Electric Power Annual, Table 3.1.A
// Tells the story of coal's rise and collapse, the gas surge, and renewables emerging from zero.

let private electricityPoints =
    let years = [ 1990.0; 1992.0; 1994.0; 1996.0; 1998.0; 2000.0; 2002.0; 2004.0; 2006.0; 2008.0; 2010.0; 2012.0; 2014.0; 2016.0; 2018.0; 2020.0; 2022.0; 2024.0 ]
    let zip values = List.zip years values
    let coal =      zip [ 1560.0; 1576.0; 1636.0; 1737.0; 1807.0; 1966.0; 1933.0; 1978.0; 2016.0; 1985.0; 1847.0; 1514.0; 1582.0; 1240.0; 1146.0;  774.0;  832.0;  652.0 ]
    let naturalGas= zip [  373.0;  379.0;  443.0;  455.0;  529.0;  601.0;  691.0;  710.0;  813.0;  920.0;  938.0; 1227.0; 1139.0; 1392.0; 1468.0; 1617.0; 1698.0; 1881.0 ]
    let nuclear =   zip [  577.0;  619.0;  640.0;  675.0;  673.0;  754.0;  780.0;  789.0;  787.0;  806.0;  807.0;  769.0;  797.0;  806.0;  807.0;  790.0;  772.0;  782.0 ]
    let petroleum = zip [  120.0;  103.0;   93.0;   83.0;   93.0;  111.0;   91.0;   91.0;   64.0;   46.0;   37.0;   30.0;   30.0;   23.0;   25.0;   16.0;   18.0;   16.0 ]
    let hydro =     zip [  297.0;  243.0;  260.0;  347.0;  321.0;  276.0;  264.0;  268.0;  289.0;  254.0;  257.0;  277.0;  259.0;  268.0;  293.0;  285.0;  255.0;  243.0 ]
    let wind =      zip [    3.0;    3.0;    4.0;    4.0;   14.0;   11.0;   11.0;   14.0;   26.0;   55.0;   95.0;  140.0;  182.0;  227.0;  275.0;  338.0;  434.0;  514.0 ]
    let solar =     zip [    0.0;    0.0;    0.0;    0.0;    1.0;    1.0;    1.0;    1.0;    1.0;    2.0;    4.0;    8.0;   29.0;   55.0;   93.0;  131.0;  205.0;  304.0 ]
    [ "Coal", coal; "Natural Gas", naturalGas; "Nuclear", nuclear; "Petroleum", petroleum; "Hydro", hydro; "Wind", wind; "Solar", solar ]

let stackedAreaGraph =
    let allSeries =
        electricityPoints
        |> List.map (fun (label, pts) -> pts |> Series.stackedArea |> Series.withLabel label)
    let xScale = Scale.linear (1990.0, 2024.0) (0.0, CommonMath.canvasSize)
    let yScale = Scale.linear (0.0, 4800.0) (CommonMath.canvasSize, 0.0)
    let blueScheme =
        Theme.light
        |> Theme.withPens [ Pen.navy; Pen.royalBlue; Pen.mediumSlateBlue; Pen.steelBlue; Pen.cadetBlue; Pen.cornflowerBlue; Pen.powderBlue ]
    Graph.create allSeries (1990.0, 2024.0) (0.0, 4800.0)
    |> Graph.withTheme blueScheme
    |> Graph.withTitle "US Electricity by Source"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Center)
    |> Graph.withAxes (
        Some (Axis.create Bottom xScale |> Axis.withTickInterval 10.0 |> Axis.withTickFormat (sprintf "%.0f")),
        Some (Axis.create Left yScale |> Axis.withTicks 6 |> Axis.withLabel "TWh"))
    |> Graph.withLegend (Legend.create LegendTop)

let normalizedStackedAreaGraph =
    let allSeries =
        electricityPoints
        |> List.map (fun (label, pts) -> pts |> Series.normalizedStackedArea |> Series.withLabel label)
    let xScale = Scale.linear (1990.0, 2024.0) (0.0, CommonMath.canvasSize)
    let yScale = Scale.linear (0.0, 100.0) (CommonMath.canvasSize, 0.0)
    let autumnScheme =
        Theme.light
        |> Theme.withPens [ Pen.fireBrick; Pen.sienna; Pen.peru; Pen.darkGoldenRod; Pen.sandyBrown; Pen.coral; Pen.peachPuff ]
    Graph.create allSeries (1990.0, 2024.0) (0.0, 100.0)
    |> Graph.withTheme autumnScheme
    |> Graph.withTitle "Normalized Stacked Area"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Center)
    |> Graph.withAxes (
        Some (Axis.create Bottom xScale |> Axis.withTickInterval 10.0 |> Axis.withTickFormat (sprintf "%.0f")),
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
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Center)
    |> Graph.withAxes (
        Some (Axis.create Bottom xScale |> Axis.withTickInterval 5.0 |> Axis.withTickFormat (sprintf "%.0f") |> Axis.withSpine SpineStyle.Hidden),
        None)
    |> Graph.withLegend (Legend.create LegendBottom)
