namespace GraphVG

open SharpVG

type ThemePreset =
    | Light
    | Dark
    | HighContrast
    | Turtle
    | GameboyGreen
    | CrispyCommodore
    | TiHue
    | Nes

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

    let withSeriesColors colors theme =
        { theme with Pens = colors |> List.map Pen.create }

    let withAxisPen axisPen theme =
        { theme with AxisPen = axisPen }

    let withAxisColor color theme =
        { theme with AxisPen = { theme.AxisPen with Color = color } }

    let withGridPen gridPen theme =
        { theme with GridPen = Some gridPen }

    let withGridColor color theme =
        let pen = theme.GridPen |> Option.defaultWith (fun () -> Pen.create color)
        { theme with GridPen = Some { pen with Color = color } }

    let withGridOpacity opacity theme =
        let pen = theme.GridPen |> Option.defaultWith (fun () -> Pen.create (Color.ofName LightGray))
        { theme with GridPen = Some { pen with Opacity = opacity } }

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

    // Original Game Boy 4-shade green palette (DMG-001)
    let gameboyGreen =
        let gb0 = Color.ofValues (15uy, 56uy, 15uy)      // #0f380f — darkest
        let gb1 = Color.ofValues (48uy, 98uy, 48uy)      // #306230 — dark
        let gb2 = Color.ofValues (139uy, 172uy, 15uy)    // #8bac0f — medium
        let gb3 = Color.ofValues (155uy, 188uy, 15uy)    // #9bbc0f — lightest
        {
            Background = gb0
            PlotBackground = None
            Pens = [ Pen.create gb3; Pen.create gb2 ]
            AxisPen = Pen.create gb1
            GridPen = Some (Pen.create gb1 |> Pen.withOpacity 0.6)
            UpColor = gb3
            DownColor = gb1
        }

    // Commodore 64 default screen palette — blue background with C64 highlight colors
    let crispyCommodore =
        let bg = Color.ofValues (69uy, 50uy, 132uy)           // C64 blue background
        let c64Yellow = Color.ofValues (238uy, 238uy, 119uy)  // #EEEE77
        let c64Cyan = Color.ofValues (170uy, 255uy, 238uy)    // #AAFFEE
        let c64LightGreen = Color.ofValues (170uy, 255uy, 102uy) // #AAFF66
        let c64Orange = Color.ofValues (221uy, 136uy, 85uy)   // #DD8855
        let c64LightBlue = Color.ofValues (102uy, 136uy, 255uy) // #6688FF
        let c64White = Color.ofValues (255uy, 255uy, 255uy)
        let axisColor = Color.ofValues (170uy, 170uy, 255uy)  // light blue-purple
        let gridColor = Color.ofValues (100uy, 80uy, 180uy)   // darker purple-blue
        {
            Background = bg
            PlotBackground = None
            Pens = [ Pen.create c64Yellow; Pen.create c64Cyan; Pen.create c64LightGreen; Pen.create c64Orange; Pen.create c64LightBlue; Pen.create c64White ]
            AxisPen = Pen.create axisColor
            GridPen = Some (Pen.create gridColor |> Pen.withOpacity 0.7)
            UpColor = c64LightGreen
            DownColor = c64Orange
        }

    // TI-84 Plus CE calculator — dark navy with characteristic bright function colors
    let tiHue =
        let bg = Color.ofValues (26uy, 26uy, 42uy)            // dark navy
        let tiBlue = Color.ofValues (74uy, 158uy, 255uy)      // #4A9EFF
        let tiRed = Color.ofValues (255uy, 74uy, 74uy)        // #FF4A4A
        let tiGreen = Color.ofValues (74uy, 255uy, 74uy)      // #4AFF4A
        let tiOrange = Color.ofValues (255uy, 159uy, 74uy)    // #FF9F4A
        let tiPurple = Color.ofValues (208uy, 74uy, 255uy)    // #D04AFF
        let tiCyan = Color.ofValues (74uy, 255uy, 255uy)      // #4AFFFF
        let axisColor = Color.ofValues (136uy, 136uy, 170uy)  // muted blue-gray
        let gridColor = Color.ofValues (51uy, 51uy, 85uy)     // very dark blue
        {
            Background = bg
            PlotBackground = None
            Pens = [ Pen.create tiBlue; Pen.create tiRed; Pen.create tiGreen; Pen.create tiOrange; Pen.create tiPurple; Pen.create tiCyan ]
            AxisPen = Pen.create axisColor
            GridPen = Some (Pen.create gridColor |> Pen.withOpacity 0.8)
            UpColor = tiGreen
            DownColor = tiRed
        }

    // NES PPU color palette — black background with authentic NES hardware colors
    let nes =
        let nesBlue = Color.ofValues (0uy, 120uy, 248uy)      // #0078F8
        let nesRed = Color.ofValues (216uy, 0uy, 58uy)        // #D8003A
        let nesGreen = Color.ofValues (0uy, 184uy, 0uy)       // #00B800
        let nesYellow = Color.ofValues (248uy, 184uy, 0uy)    // #F8B800
        let nesCyan = Color.ofValues (0uy, 168uy, 248uy)      // #00A8F8
        let nesPurple = Color.ofValues (152uy, 56uy, 152uy)   // #983898
        let axisColor = Color.ofValues (144uy, 144uy, 144uy)  // medium gray
        let gridColor = Color.ofValues (48uy, 48uy, 48uy)     // very dark gray
        {
            Background = Color.ofName Black
            PlotBackground = None
            Pens = [ Pen.create nesBlue; Pen.create nesRed; Pen.create nesGreen; Pen.create nesYellow; Pen.create nesCyan; Pen.create nesPurple ]
            AxisPen = Pen.create axisColor
            GridPen = Some (Pen.create gridColor |> Pen.withOpacity 0.9)
            UpColor = nesGreen
            DownColor = nesRed
        }

    let preset (themePreset : ThemePreset) =
        match themePreset with
        | Light -> light
        | Dark -> dark
        | HighContrast -> highContrast
        | Turtle -> turtle
        | GameboyGreen -> gameboyGreen
        | CrispyCommodore -> crispyCommodore
        | TiHue -> tiHue
        | Nes -> nes

    /// Default sequential color scale: white (low) → steelblue (high).
    /// Takes the series min and max values and returns a function mapping a raw data value to a Color.
    let defaultHeatmapColorScale (minVal : float) (maxVal : float) (value : float) : Color =
        let t = if maxVal = minVal then 0.5 else (value - minVal) / (maxVal - minVal) |> max 0.0 |> min 1.0
        // White (255, 255, 255) → SteelBlue (70, 130, 180)
        let lerp lo hi = byte (int lo + int (t * float (int hi - int lo)))
        Color.ofValues (lerp 255uy 70uy, lerp 255uy 130uy, lerp 255uy 180uy)
