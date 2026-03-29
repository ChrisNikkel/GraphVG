namespace GraphVG

open SharpVG

type AxisPosition = Bottom | Top | Left | Right | HorizontalAt of float | VerticalAt of float

type AxisTicks =
    | TickCount    of int
    | TickInterval of float     // spacing in data units

type Axis = {
    Position          : AxisPosition
    Scale             : Scale
    Ticks             : AxisTicks
    Label             : string option
    HideOriginTick    : bool
    HideOriginLabel   : bool
    HideBoundsTick    : bool
    HideBoundsLabel   : bool
}

module Axis =

    let private tickLength = 6.0
    let private fontSize   = 12.0

    let create position scale : Axis =
        { Position = position; Scale = scale; Ticks = TickCount 5
          Label = None
          HideOriginTick = false; HideOriginLabel = false
          HideBoundsTick = false; HideBoundsLabel = false }

    let withTicks count (axis : Axis) =
        { axis with Ticks = TickCount count }

    let withTickInterval interval (axis : Axis) =
        { axis with Ticks = TickInterval interval }

    let withLabel label (axis : Axis) =
        { axis with Label = Some label }

    let hideOriginTick  (axis : Axis) = { axis with HideOriginTick  = true }
    let hideOriginLabel (axis : Axis) = { axis with HideOriginLabel = true }
    let hideOrigin      (axis : Axis) = axis |> hideOriginTick |> hideOriginLabel

    let hideBoundsTick  (axis : Axis) = { axis with HideBoundsTick  = true }
    let hideBoundsLabel (axis : Axis) = { axis with HideBoundsLabel = true }
    let hideBounds      (axis : Axis) = axis |> hideBoundsTick |> hideBoundsLabel

    /// Shorthand for suppressing both axes: pass to Graph.withAxes.
    let none : Axis option * Axis option = None, None

    let private strokeStyle (pen : Pen) =
        Style.create pen.Color pen.Color pen.Width pen.Opacity pen.Opacity

    let private fillStyle (pen : Pen) =
        Style.create pen.Color pen.Color (Length.ofInt 1) pen.Opacity pen.Opacity

    let private mkLine pen startPoint endPoint =
        Line.create startPoint endPoint |> Element.createWithStyle (strokeStyle pen)

    let private mkLabel pen anchor baseline body position =
        Text.create position body
        |> Text.withFontSize fontSize
        |> Text.withAnchor anchor
        |> Text.withBaseline baseline
        |> Element.createWithStyle (fillStyle pen)

    let toElements theme (axis : Axis) =
        let pen = theme.AxisPen
        let domainMin, domainMax = Scale.domain axis.Scale
        let startPixel  = Scale.apply axis.Scale domainMin
        let endPixel    = Scale.apply axis.Scale domainMax
        let midPixel    = (startPixel + endPixel) / 2.0
        let tickValues =
            match axis.Ticks with
            | TickCount count    -> Scale.ticks axis.Scale count
            | TickInterval interval ->
                let lower, upper = Scale.domain axis.Scale
                let first  = ceil (lower / interval) * interval
                [ for i in 0 .. int (floor ((upper - first) / interval)) -> first + float i * interval ]
        let formatValue value = sprintf "%.4g" value
        let near a b          = abs (a - b) < 1e-10
        let isOrigin value    = near value 0.0
        let isBound  value    = near value domainMin || near value domainMax
        let showTick  value   = not (axis.HideOriginTick  && isOrigin value) && not (axis.HideBoundsTick  && isBound value)
        let showLabel value   = not (axis.HideOriginLabel && isOrigin value) && not (axis.HideBoundsLabel && isBound value)
        let tickAndLabel value tick label =
            (if showTick  value then [ tick  ] else []) @
            if showLabel value then [ label ] else []
        let gridLines value isHorizontal =
            if not (showTick value) then []
            else
                match theme.GridPen with
                | None -> []
                | Some gridPen ->
                    let pixel = Scale.apply axis.Scale value
                    if isHorizontal then [ mkLine gridPen (Point.ofFloats (pixel, 0.0)) (Point.ofFloats (pixel, Canvas.canvasSize)) ]
                    else                 [ mkLine gridPen (Point.ofFloats (0.0, pixel)) (Point.ofFloats (Canvas.canvasSize, pixel)) ]

        match axis.Position with
        | Bottom ->
            let y        = Canvas.canvasSize
            let axisLine = mkLine pen (Point.ofFloats (startPixel, y)) (Point.ofFloats (endPixel, y))
            let ticks =
                tickValues |> List.collect (fun value ->
                    let x     = Scale.apply axis.Scale value
                    let tick  = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x, y + tickLength))
                    let label = mkLabel pen Middle HangingBaseline (formatValue value) (Point.ofFloats (x, y + tickLength + 2.0))
                    gridLines value true @ tickAndLabel value tick label)
            let labelEl =
                axis.Label
                |> Option.map (mkLabel pen Middle HangingBaseline >> (|>) (Point.ofFloats (midPixel, y + tickLength + fontSize + 6.0)))
                |> Option.toList
            axisLine :: ticks @ labelEl

        | Top ->
            let y        = 0.0
            let axisLine = mkLine pen (Point.ofFloats (startPixel, y)) (Point.ofFloats (endPixel, y))
            let ticks =
                tickValues |> List.collect (fun value ->
                    let x     = Scale.apply axis.Scale value
                    let tick  = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x, y - tickLength))
                    let label = mkLabel pen Middle AlphabeticBaseline (formatValue value) (Point.ofFloats (x, y - tickLength - 3.0))
                    gridLines value true @ tickAndLabel value tick label)
            let labelEl =
                axis.Label
                |> Option.map (mkLabel pen Middle AlphabeticBaseline >> (|>) (Point.ofFloats (midPixel, y - tickLength - fontSize - 4.0)))
                |> Option.toList
            axisLine :: ticks @ labelEl

        | Left ->
            let x        = 0.0
            let axisLine = mkLine pen (Point.ofFloats (x, startPixel)) (Point.ofFloats (x, endPixel))
            let ticks =
                tickValues |> List.collect (fun value ->
                    let y     = Scale.apply axis.Scale value
                    let tick  = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x - tickLength, y))
                    let label = mkLabel pen End CentralBaseline (formatValue value) (Point.ofFloats (x - tickLength - 4.0, y))
                    gridLines value false @ tickAndLabel value tick label)
            let labelEl =
                axis.Label
                |> Option.map (mkLabel pen Middle CentralBaseline >> (|>) (Point.ofFloats (x - tickLength - fontSize - 4.0, midPixel)))
                |> Option.toList
            axisLine :: ticks @ labelEl

        | Right ->
            let x        = Canvas.canvasSize
            let axisLine = mkLine pen (Point.ofFloats (x, startPixel)) (Point.ofFloats (x, endPixel))
            let ticks =
                tickValues |> List.collect (fun value ->
                    let y     = Scale.apply axis.Scale value
                    let tick  = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x + tickLength, y))
                    let label = mkLabel pen Start CentralBaseline (formatValue value) (Point.ofFloats (x + tickLength + 4.0, y))
                    gridLines value false @ tickAndLabel value tick label)
            let labelEl =
                axis.Label
                |> Option.map (mkLabel pen Start CentralBaseline >> (|>) (Point.ofFloats (x + tickLength + fontSize + 4.0, midPixel)))
                |> Option.toList
            axisLine :: ticks @ labelEl

        | HorizontalAt y ->
            let axisLine = mkLine pen (Point.ofFloats (startPixel, y)) (Point.ofFloats (endPixel, y))
            let ticks =
                tickValues |> List.collect (fun value ->
                    let x     = Scale.apply axis.Scale value
                    let tick  = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x, y + tickLength))
                    let label = mkLabel pen Middle HangingBaseline (formatValue value) (Point.ofFloats (x, y + tickLength + 2.0))
                    gridLines value true @ tickAndLabel value tick label)
            let labelEl =
                axis.Label
                |> Option.map (mkLabel pen Middle HangingBaseline >> (|>) (Point.ofFloats (midPixel, y + tickLength + fontSize + 6.0)))
                |> Option.toList
            axisLine :: ticks @ labelEl

        | VerticalAt x ->
            let axisLine  = mkLine pen (Point.ofFloats (x, startPixel)) (Point.ofFloats (x, endPixel))
            let leftSide  = x <= Canvas.canvasSize / 2.0
            let anchor    = if leftSide then Start else End
            let tickSign  = if leftSide then 1.0 else -1.0
            let ticks =
                tickValues |> List.collect (fun value ->
                    let y     = Scale.apply axis.Scale value
                    let tick  = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x + tickSign * tickLength, y))
                    let label = mkLabel pen anchor CentralBaseline (formatValue value) (Point.ofFloats (x + tickSign * (tickLength + 4.0), y))
                    gridLines value false @ tickAndLabel value tick label)
            let labelEl =
                axis.Label
                |> Option.map (mkLabel pen anchor CentralBaseline >> (|>) (Point.ofFloats (x + tickSign * (tickLength + fontSize + 4.0), midPixel)))
                |> Option.toList
            axisLine :: ticks @ labelEl
