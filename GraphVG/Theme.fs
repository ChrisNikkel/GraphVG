namespace GraphVG

open SharpVG

type ThemePreset =
    | Light
    | Dark
    | HighContrast

type Theme =
    {
        Background : Color
        PlotBackground : Color option
        Pens : Pen list
        AxisPen : Pen
        GridPen : Pen option
    }

module Theme =

    let withBackground background theme =
        { theme with Background = background }

    let withPlotBackground color theme =
        { theme with PlotBackground = Some color }

    let withPens pens theme =
        { theme with Pens = pens }

    let withAxisPen axisPen theme =
        { theme with AxisPen = axisPen }

    let withGridPen gridPen theme =
        { theme with GridPen = Some gridPen }

    let penForSeries index theme =
        theme.Pens.[index % theme.Pens.Length]

    let empty = {
        Background = Color.ofName White
        PlotBackground = None
        Pens = [ Pen.steelBlue; Pen.orange; Pen.green; Pen.red; Pen.purple ]
        AxisPen = Pen.gray
        GridPen = None
    }

    let light = {
        Background = Color.ofName White
        PlotBackground = None
        Pens = [ Pen.steelBlue; Pen.coral; Pen.seaGreen; Pen.tomato; Pen.mediumPurple; Pen.goldenRod ]
        AxisPen = Pen.dimGray
        GridPen = Some Pen.lightGray
    }

    let dark = {
        Background = Color.ofName DarkSlateGray
        PlotBackground = None
        Pens = [ Pen.cornflowerBlue; Pen.coral; Pen.limeGreen; Pen.tomato; Pen.violet; Pen.gold ]
        AxisPen = Pen.lightGray
        GridPen = Some (Pen.dimGray |> Pen.withOpacity 0.5)
    }

    let highContrast = {
        Background = Color.ofName Black
        PlotBackground = None
        Pens = [ Pen.yellow; Pen.cyan; Pen.magenta; Pen.lime; Pen.orangeRed; Pen.white ]
        AxisPen = Pen.white
        GridPen = Some (Pen.white |> Pen.withOpacity 0.25)
    }

    let turtle = {
        Background = Color.ofName Black
        PlotBackground = None
        Pens = [ Pen.green ]
        AxisPen = Pen.green
        GridPen = None
    }

    let preset (themePreset : ThemePreset) =
        match themePreset with
        | Light -> light
        | Dark -> dark
        | HighContrast -> highContrast
