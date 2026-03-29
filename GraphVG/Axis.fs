namespace GraphVG

open SharpVG
open CommonMath

type SpineStyle = Full | Hidden | Box

type AxisPosition =
    | Bottom
    | Top
    | Left
    | Right
    | HorizontalAt of float
    | VerticalAt of float

type AxisTicks =
    | TickCount    of int
    | TickInterval of float     // spacing in data units

type Axis =
    {
        Position : AxisPosition
        Scale : Scale
        Ticks : AxisTicks
        Label : string option
        HideOriginTick : bool
        HideOriginLabel : bool
        HideBoundsTick : bool
        HideBoundsLabel : bool
        TickLength : float
        FontSize : float
        TickFormat : (float -> string) option
        SpineStyle : SpineStyle
    }

module Axis =

    let create position scale : Axis =
        {
            Position = position
            Scale = scale
            Ticks = TickCount 5
            Label = None
            HideOriginTick = false
            HideOriginLabel = false
            HideBoundsTick = false
            HideBoundsLabel = false
            TickLength = 6.0
            FontSize = 12.0
            TickFormat = None
            SpineStyle = Full
        }

    let withTicks count (axis : Axis) =
        { axis with Ticks = TickCount count }

    let withTickInterval interval (axis : Axis) =
        { axis with Ticks = TickInterval interval }

    let withLabel label (axis : Axis) =
        { axis with Label = Some label }

    let withTickLength tickLength (axis : Axis) =
        { axis with TickLength = tickLength }

    let withFontSize fontSize (axis : Axis) =
        { axis with FontSize = fontSize }

    let withTickFormat format (axis : Axis) =
        { axis with TickFormat = Some format }

    let withSpine spineStyle (axis : Axis) =
        { axis with SpineStyle = spineStyle }

    let hideOriginTick (axis : Axis) = { axis with HideOriginTick = true }
    let hideOriginLabel (axis : Axis) = { axis with HideOriginLabel = true }
    let hideOrigin (axis : Axis) = axis |> hideOriginTick |> hideOriginLabel

    let hideBoundsTick (axis : Axis) = { axis with HideBoundsTick = true }
    let hideBoundsLabel (axis : Axis) = { axis with HideBoundsLabel = true }
    let hideBounds (axis : Axis) = axis |> hideBoundsTick |> hideBoundsLabel

    /// Shorthand for suppressing both axes: pass to Graph.withAxes.
    let none : Axis option * Axis option = None, None

    let private strokeStyle (pen : Pen) =
        Style.createWithPen pen

    let private fillStyle (pen : Pen) =
        Style.empty |> Style.withFillPen pen

    let private mkLine pen startPoint endPoint =
        Line.create startPoint endPoint |> Element.createWithStyle (strokeStyle pen)

    let private mkLabel fontSize pen anchor baseline body position =
        Text.create position body
        |> Text.withFontSize fontSize
        |> Text.withAnchor anchor
        |> Text.withBaseline baseline
        |> Element.createWithStyle (fillStyle pen)

    let toGridElements theme (axis : Axis) =
        match theme.GridPen with
        | None -> []
        | Some gridPen ->
            let domainMin, domainMax = Scale.domain axis.Scale
            let isOrigin v = isNear 0.0 v
            let isBound v = isNear domainMin v || isNear domainMax v
            let showTick v = not (axis.HideOriginTick && isOrigin v) && not (axis.HideBoundsTick && isBound v)
            let tickValues =
                match axis.Ticks with
                | TickCount count -> Scale.ticks axis.Scale count
                | TickInterval interval ->
                    let lower, upper = Scale.domain axis.Scale
                    let first = ceil (lower / interval) * interval
                    [ for i in 0 .. int (floor ((upper - first) / interval)) -> first + float i * interval ]
            let isHorizontal =
                match axis.Position with
                | Bottom | Top | HorizontalAt _ -> true
                | Left | Right | VerticalAt _ -> false
            tickValues
            |> List.filter showTick
            |> List.map (fun value ->
                let pixel = Scale.apply axis.Scale value
                if isHorizontal then mkLine gridPen (Point.ofFloats (pixel, 0.0)) (Point.ofFloats (pixel, Canvas.canvasSize))
                else                 mkLine gridPen (Point.ofFloats (0.0, pixel)) (Point.ofFloats (Canvas.canvasSize, pixel)))

    let toElements theme (axis : Axis) =
        let pen = theme.AxisPen
        let tickLength = axis.TickLength
        let fontSize = axis.FontSize
        let spineElements startPoint endPoint =
            match axis.SpineStyle with
            | Full -> [ mkLine pen startPoint endPoint ]
            | Hidden -> []
            | Box ->
                let boxRect =
                    Rect.create (Point.ofFloats (0.0, 0.0)) (Area.ofFloats (Canvas.canvasSize, Canvas.canvasSize))
                    |> Element.createWithStyle (strokeStyle pen)
                [ boxRect ]
        let domainMin, domainMax = Scale.domain axis.Scale
        let startPixel = Scale.apply axis.Scale domainMin
        let endPixel   = Scale.apply axis.Scale domainMax
        let midPixel   = (startPixel + endPixel) / 2.0
        let tickValues =
            match axis.Ticks with
            | TickCount count -> Scale.ticks axis.Scale count
            | TickInterval interval ->
                let lower, upper = Scale.domain axis.Scale
                let first = ceil (lower / interval) * interval
                [ for i in 0 .. int (floor ((upper - first) / interval)) -> first + float i * interval ]
        let formatValue = axis.TickFormat |> Option.defaultValue (sprintf "%.4g")
        let isOrigin value = isNear 0.0 value
        let isBound value = isNear domainMin value || isNear domainMax value
        let showTick value = not (axis.HideOriginTick && isOrigin value) && not (axis.HideBoundsTick && isBound value)
        let showLabel value = not (axis.HideOriginLabel && isOrigin value) && not (axis.HideBoundsLabel && isBound value)
        let tickAndLabel value tick label =
            (if showTick value then [ tick ] else []) @
            if showLabel value then [ label ] else []
        match axis.Position with
        | Bottom ->
            let y = Canvas.canvasSize
            let ticks =
                tickValues |> List.collect (fun value ->
                    let x = Scale.apply axis.Scale value
                    let tick = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x, y + tickLength))
                    let label = mkLabel fontSize pen Middle HangingBaseline (formatValue value) (Point.ofFloats (x, y + tickLength + 2.0))
                    tickAndLabel value tick label)
            let labelEl =
                axis.Label
                |> Option.map (mkLabel fontSize pen Middle HangingBaseline >> (|>) (Point.ofFloats (midPixel, y + tickLength + fontSize + 6.0)))
                |> Option.toList
            spineElements (Point.ofFloats (startPixel, y)) (Point.ofFloats (endPixel, y)) @ ticks @ labelEl

        | Top ->
            let y = 0.0
            let ticks =
                tickValues |> List.collect (fun value ->
                    let x = Scale.apply axis.Scale value
                    let tick = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x, y - tickLength))
                    let label = mkLabel fontSize pen Middle AlphabeticBaseline (formatValue value) (Point.ofFloats (x, y - tickLength - 3.0))
                    tickAndLabel value tick label)
            let labelEl =
                axis.Label
                |> Option.map (mkLabel fontSize pen Middle AlphabeticBaseline >> (|>) (Point.ofFloats (midPixel, y - tickLength - fontSize - 4.0)))
                |> Option.toList
            spineElements (Point.ofFloats (startPixel, y)) (Point.ofFloats (endPixel, y)) @ ticks @ labelEl

        | Left ->
            let x = 0.0
            let ticks =
                tickValues |> List.collect (fun value ->
                    let y = Scale.apply axis.Scale value
                    let tick = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x - tickLength, y))
                    let label = mkLabel fontSize pen End CentralBaseline (formatValue value) (Point.ofFloats (x - tickLength - 4.0, y))
                    tickAndLabel value tick label)
            let labelEl =
                axis.Label
                |> Option.map (mkLabel fontSize pen Middle CentralBaseline >> (|>) (Point.ofFloats (x - tickLength - fontSize - 4.0, midPixel)))
                |> Option.toList
            spineElements (Point.ofFloats (x, startPixel)) (Point.ofFloats (x, endPixel)) @ ticks @ labelEl

        | Right ->
            let x = Canvas.canvasSize
            let ticks =
                tickValues |> List.collect (fun value ->
                    let y = Scale.apply axis.Scale value
                    let tick = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x + tickLength, y))
                    let label = mkLabel fontSize pen Start CentralBaseline (formatValue value) (Point.ofFloats (x + tickLength + 4.0, y))
                    tickAndLabel value tick label)
            let labelEl =
                axis.Label
                |> Option.map (mkLabel fontSize pen Start CentralBaseline >> (|>) (Point.ofFloats (x + tickLength + fontSize + 4.0, midPixel)))
                |> Option.toList
            spineElements (Point.ofFloats (x, startPixel)) (Point.ofFloats (x, endPixel)) @ ticks @ labelEl

        | HorizontalAt y ->
            let ticks =
                tickValues |> List.collect (fun value ->
                    let x = Scale.apply axis.Scale value
                    let tick = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x, y + tickLength))
                    let label = mkLabel fontSize pen Middle HangingBaseline (formatValue value) (Point.ofFloats (x, y + tickLength + 2.0))
                    tickAndLabel value tick label)
            let labelEl =
                axis.Label
                |> Option.map (mkLabel fontSize pen Middle HangingBaseline >> (|>) (Point.ofFloats (midPixel, y + tickLength + fontSize + 6.0)))
                |> Option.toList
            spineElements (Point.ofFloats (startPixel, y)) (Point.ofFloats (endPixel, y)) @ ticks @ labelEl

        | VerticalAt x ->
            let leftSide = x <= Canvas.canvasSize / 2.0
            let anchor = if leftSide then Start else End
            let tickSign = if leftSide then 1.0 else -1.0
            let ticks =
                tickValues |> List.collect (fun value ->
                    let y = Scale.apply axis.Scale value
                    let tick = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x + tickSign * tickLength, y))
                    let label = mkLabel fontSize pen anchor CentralBaseline (formatValue value) (Point.ofFloats (x + tickSign * (tickLength + 4.0), y))
                    tickAndLabel value tick label)
            let labelEl =
                axis.Label
                |> Option.map (mkLabel fontSize pen anchor CentralBaseline >> (|>) (Point.ofFloats (x + tickSign * (tickLength + fontSize + 4.0), midPixel)))
                |> Option.toList
            spineElements (Point.ofFloats (x, startPixel)) (Point.ofFloats (x, endPixel)) @ ticks @ labelEl
