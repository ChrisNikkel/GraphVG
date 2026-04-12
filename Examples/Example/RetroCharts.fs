module RetroCharts

open System
open GraphVG

// Fictional monthly visitor counts (thousands) for two imaginary websites.
// Used to show multiple series cycling through each retro palette.
let private months = [ 1.0 .. 12.0 ]

let private siteA =
    List.zip months [ 12.0; 15.0; 14.0; 18.0; 22.0; 27.0; 31.0; 29.0; 24.0; 20.0; 16.0; 19.0 ]
    |> Series.line
    |> Series.withLabel "Site A"

let private siteB =
    List.zip months [ 8.0; 9.0; 11.0; 13.0; 17.0; 20.0; 23.0; 21.0; 18.0; 15.0; 12.0; 14.0 ]
    |> Series.line
    |> Series.withLabel "Site B"

let private siteC =
    List.zip months [ 5.0; 6.0; 7.0; 8.0; 10.0; 13.0; 16.0; 14.0; 11.0; 9.0; 7.0; 8.0 ]
    |> Series.line
    |> Series.withLabel "Site C"

let private baseGraph theme title =
    Graph.create [ siteA; siteB; siteC ] (1.0, 12.0) (0.0, 35.0)
    |> Graph.withTheme theme
    |> Graph.withTitle title
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Center)
    |> Graph.withAxes (
        Some (Axis.create Bottom (Scale.linear (1.0, 12.0) (0.0, CommonMath.canvasSize))
              |> Axis.withTicks 12
              |> Axis.withTickFormat (fun v ->
                  [| "Jan"; "Feb"; "Mar"; "Apr"; "May"; "Jun";
                     "Jul"; "Aug"; "Sep"; "Oct"; "Nov"; "Dec" |].[int v - 1])),
        Some (Axis.create Left (Scale.linear (0.0, 35.0) (CommonMath.canvasSize, 0.0))
              |> Axis.withTicks 7
              |> Axis.withLabel "Visitors (k)"))

let gameboyGreenGraph = baseGraph Theme.gameboyGreen "Game Boy Green"
let crispyCommodoreGraph = baseGraph Theme.crispyCommodore "Crispy Commodore"
let tiHueGraph = baseGraph Theme.tiHue "TI Hue"
let nesGraph = baseGraph Theme.nes "NES"
let turtleGraph = baseGraph Theme.turtle "Turtle"
