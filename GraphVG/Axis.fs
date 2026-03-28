namespace GraphVG

open SharpVG

type AxisPosition = Bottom | Top | Left | Right | HorizontalAt of float | VerticalAt of float

type Axis = {
    Position  : AxisPosition
    Scale     : Scale
    TickCount : int
    Label     : string option
}

module Axis =

    let private tickLength  = 6.0
    let private fontSize    = 12.0

    let create position scale : Axis =
        { Position = position; Scale = scale; TickCount = 5; Label = None }

    let withTicks count axis =
        { axis with TickCount = count }

    let withLabel label (axis : Axis) =
        { axis with Label = Some label }

    /// Shorthand for suppressing both axes: pass to Graph.withAxes.
    let none : Axis option * Axis option = None, None

    let private strokeStyle (pen : Pen) =
        Style.create pen.Color pen.Color pen.Width pen.Opacity pen.Opacity

    let private fillStyle (pen : Pen) =
        Style.create pen.Color pen.Color (Length.ofInt 1) pen.Opacity pen.Opacity

    let private mkLine pen p1 p2 =
        Line.create p1 p2 |> Element.createWithStyle (strokeStyle pen)

    let private mkLabel pen anchor baseline body position =
        Text.create position body
        |> Text.withFontSize fontSize
        |> Text.withAnchor anchor
        |> Text.withBaseline baseline
        |> Element.createWithStyle (fillStyle pen)

    let toElements theme (axis : Axis) =
        let pen       = theme.AxisPen
        let domainMin, domainMax = Scale.domain axis.Scale
        let startPx   = Scale.apply axis.Scale domainMin
        let endPx     = Scale.apply axis.Scale domainMax
        let midPx     = (startPx + endPx) / 2.0
        let tickValues = Scale.ticks axis.Scale axis.TickCount
        let fmt v     = sprintf "%.4g" v
        let gridLines v isHorizontal =
            match theme.GridPen with
            | None -> []
            | Some gpen ->
                let px = Scale.apply axis.Scale v
                if isHorizontal then [ mkLine gpen (Point.ofFloats (px, 0.0)) (Point.ofFloats (px, Canvas.canvasSize)) ]
                else                 [ mkLine gpen (Point.ofFloats (0.0, px)) (Point.ofFloats (Canvas.canvasSize, px)) ]

        match axis.Position with
        | Bottom ->
            let y        = Canvas.canvasSize
            let axisLine = mkLine pen (Point.ofFloats (startPx, y)) (Point.ofFloats (endPx, y))
            let ticks =
                tickValues |> List.collect (fun v ->
                    let x    = Scale.apply axis.Scale v
                    let tick = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x, y + tickLength))
                    let lbl  = mkLabel pen Middle HangingBaseline (fmt v) (Point.ofFloats (x, y + tickLength + 2.0))
                    gridLines v true @ [ tick; lbl ])
            let labelEl =
                axis.Label
                |> Option.map (mkLabel pen Middle HangingBaseline >> (|>) (Point.ofFloats (midPx, y + tickLength + fontSize + 6.0)))
                |> Option.toList
            axisLine :: ticks @ labelEl

        | Top ->
            let y        = 0.0
            let axisLine = mkLine pen (Point.ofFloats (startPx, y)) (Point.ofFloats (endPx, y))
            let ticks =
                tickValues |> List.collect (fun v ->
                    let x    = Scale.apply axis.Scale v
                    let tick = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x, y - tickLength))
                    let lbl  = mkLabel pen Middle AlphabeticBaseline (fmt v) (Point.ofFloats (x, y - tickLength - 3.0))
                    gridLines v true @ [ tick; lbl ])
            let labelEl =
                axis.Label
                |> Option.map (mkLabel pen Middle AlphabeticBaseline >> (|>) (Point.ofFloats (midPx, y - tickLength - fontSize - 4.0)))
                |> Option.toList
            axisLine :: ticks @ labelEl

        | Left ->
            let x        = 0.0
            let axisLine = mkLine pen (Point.ofFloats (x, startPx)) (Point.ofFloats (x, endPx))
            let ticks =
                tickValues |> List.collect (fun v ->
                    let y    = Scale.apply axis.Scale v
                    let tick = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x - tickLength, y))
                    let lbl  = mkLabel pen End CentralBaseline (fmt v) (Point.ofFloats (x - tickLength - 4.0, y))
                    gridLines v false @ [ tick; lbl ])
            let labelEl =
                axis.Label
                |> Option.map (mkLabel pen Middle CentralBaseline >> (|>) (Point.ofFloats (x - tickLength - fontSize - 4.0, midPx)))
                |> Option.toList
            axisLine :: ticks @ labelEl

        | Right ->
            let x        = Canvas.canvasSize
            let axisLine = mkLine pen (Point.ofFloats (x, startPx)) (Point.ofFloats (x, endPx))
            let ticks =
                tickValues |> List.collect (fun v ->
                    let y    = Scale.apply axis.Scale v
                    let tick = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x + tickLength, y))
                    let lbl  = mkLabel pen Start CentralBaseline (fmt v) (Point.ofFloats (x + tickLength + 4.0, y))
                    gridLines v false @ [ tick; lbl ])
            let labelEl =
                axis.Label
                |> Option.map (mkLabel pen Start CentralBaseline >> (|>) (Point.ofFloats (x + tickLength + fontSize + 4.0, midPx)))
                |> Option.toList
            axisLine :: ticks @ labelEl

        | HorizontalAt y ->
            let axisLine = mkLine pen (Point.ofFloats (startPx, y)) (Point.ofFloats (endPx, y))
            let ticks =
                tickValues |> List.collect (fun v ->
                    let x    = Scale.apply axis.Scale v
                    let tick = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x, y + tickLength))
                    let lbl  = mkLabel pen Middle HangingBaseline (fmt v) (Point.ofFloats (x, y + tickLength + 2.0))
                    gridLines v true @ [ tick; lbl ])
            let labelEl =
                axis.Label
                |> Option.map (mkLabel pen Middle HangingBaseline >> (|>) (Point.ofFloats (midPx, y + tickLength + fontSize + 6.0)))
                |> Option.toList
            axisLine :: ticks @ labelEl

        | VerticalAt x ->
            let axisLine  = mkLine pen (Point.ofFloats (x, startPx)) (Point.ofFloats (x, endPx))
            let leftSide  = x <= Canvas.canvasSize / 2.0
            let anchor    = if leftSide then Start else End
            let tickSign  = if leftSide then 1.0 else -1.0
            let ticks =
                tickValues |> List.collect (fun v ->
                    let y    = Scale.apply axis.Scale v
                    let tick = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x + tickSign * tickLength, y))
                    let lbl  = mkLabel pen anchor CentralBaseline (fmt v) (Point.ofFloats (x + tickSign * (tickLength + 4.0), y))
                    gridLines v false @ [ tick; lbl ])
            let labelEl =
                axis.Label
                |> Option.map (mkLabel pen anchor CentralBaseline >> (|>) (Point.ofFloats (x + tickSign * (tickLength + fontSize + 4.0), midPx)))
                |> Option.toList
            axisLine :: ticks @ labelEl
