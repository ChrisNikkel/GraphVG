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
        UpColor : Color
        DownColor : Color
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

    let withUpColor color theme =
        { theme with UpColor = color }

    let withDownColor color theme =
        { theme with DownColor = color }

    let penForSeries index theme =
        theme.Pens.[index % theme.Pens.Length]

    let empty = {
        Background = Color.ofName White
        PlotBackground = None
        Pens = [ Pen.steelBlue; Pen.orange; Pen.green; Pen.red; Pen.purple ]
        AxisPen = Pen.gray
        GridPen = None
        UpColor = Color.ofName SeaGreen
        DownColor = Color.ofName Crimson
    }

    let light = {
        Background = Color.ofName White
        PlotBackground = None
        Pens = [ Pen.steelBlue; Pen.coral; Pen.seaGreen; Pen.tomato; Pen.mediumPurple; Pen.goldenRod ]
        AxisPen = Pen.dimGray
        GridPen = Some Pen.lightGray
        UpColor = Color.ofName SeaGreen
        DownColor = Color.ofName Crimson
    }

    let dark = {
        Background = Color.ofName DarkSlateGray
        PlotBackground = None
        Pens = [ Pen.cornflowerBlue; Pen.coral; Pen.limeGreen; Pen.tomato; Pen.violet; Pen.gold ]
        AxisPen = Pen.lightGray
        GridPen = Some (Pen.dimGray |> Pen.withOpacity 0.5)
        UpColor = Color.ofName LimeGreen
        DownColor = Color.ofName Crimson
    }

    let highContrast = {
        Background = Color.ofName Black
        PlotBackground = None
        Pens = [ Pen.yellow; Pen.cyan; Pen.magenta; Pen.lime; Pen.orangeRed; Pen.white ]
        AxisPen = Pen.white
        GridPen = Some (Pen.white |> Pen.withOpacity 0.25)
        UpColor = Color.ofName Lime
        DownColor = Color.ofName Red
    }

    let turtle = {
        Background = Color.ofName Black
        PlotBackground = None
        Pens = [ Pen.green ]
        AxisPen = Pen.green
        GridPen = None
        UpColor = Color.ofName Green
        DownColor = Color.ofName Red
    }

    let preset (themePreset : ThemePreset) =
        match themePreset with
        | Light -> light
        | Dark -> dark
        | HighContrast -> highContrast

    /// Default sequential color scale: white (low) → steelblue (high).
    /// Takes the series min and max values and returns a function mapping a raw data value to a Color.
    let defaultHeatmapColorScale (minVal : float) (maxVal : float) (value : float) : Color =
        let t = if maxVal = minVal then 0.5 else (value - minVal) / (maxVal - minVal) |> max 0.0 |> min 1.0
        // White (255, 255, 255) → SteelBlue (70, 130, 180)
        let lerp lo hi = byte (int lo + int (t * float (int hi - int lo)))
        Color.ofValues (lerp 255uy 70uy, lerp 255uy 130uy, lerp 255uy 180uy)
