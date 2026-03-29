namespace GraphVG

open SharpVG

type Theme =
    {
        Background : Color
        Pens       : Pen list
        AxisPen    : Pen
        GridPen    : Pen option
    }

module Theme =

    let withBackground background theme =
        { theme with Background = background }

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
        Pens       = [ Pen.steelBlue; Pen.orange; Pen.green; Pen.red; Pen.purple ]
        AxisPen    = Pen.gray
        GridPen    = None
    }

    let light = {
        Background = Color.ofName White
        Pens       = [ Pen.steelBlue; Pen.coral; Pen.seaGreen; Pen.tomato; Pen.mediumPurple; Pen.goldenRod ]
        AxisPen    = Pen.dimGray
        GridPen    = Some Pen.lightGray
    }

    let dark = {
        Background = Color.ofName DarkSlateGray
        Pens       = [ Pen.cornflowerBlue; Pen.coral; Pen.limeGreen; Pen.tomato; Pen.violet; Pen.gold ]
        AxisPen    = Pen.lightGray
        GridPen    = Some (Pen.dimGray |> Pen.withOpacity 0.5)
    }

    let turtle = {
        Background = Color.ofName Black
        Pens       = [ Pen.green ]
        AxisPen    = Pen.green
        GridPen    = None
    }
