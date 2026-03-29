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

    type private AxisBounds =
        {
            DomainMinimum : float
            DomainMaximum : float
            StartPixel : float
            EndPixel : float
            MidPixel : float
        }

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

    let private axisBounds (axis : Axis) =
        let domainMinimum, domainMaximum = Scale.domain axis.Scale
        let startPixel = Scale.apply axis.Scale domainMinimum
        let endPixel = Scale.apply axis.Scale domainMaximum
        {
            DomainMinimum = domainMinimum
            DomainMaximum = domainMaximum
            StartPixel = startPixel
            EndPixel = endPixel
            MidPixel = (startPixel + endPixel) / 2.0
        }

    let private tickValues (axis : Axis) =
        match axis.Ticks with
        | TickCount count -> Scale.ticks axis.Scale count
        | TickInterval interval ->
            let lower, upper = Scale.domain axis.Scale
            let first = ceil (lower / interval) * interval
            [ for index in 0 .. int (floor ((upper - first) / interval)) -> first + float index * interval ]

    let private showsTick (axis : Axis) value =
        let bounds = axisBounds axis
        let isOrigin = isNear 0.0 value
        let isBound = isNear bounds.DomainMinimum value || isNear bounds.DomainMaximum value
        not (axis.HideOriginTick && isOrigin) && not (axis.HideBoundsTick && isBound)

    let private showsLabel (axis : Axis) value =
        let bounds = axisBounds axis
        let isOrigin = isNear 0.0 value
        let isBound = isNear bounds.DomainMinimum value || isNear bounds.DomainMaximum value
        not (axis.HideOriginLabel && isOrigin) && not (axis.HideBoundsLabel && isBound)

    let private tickAndLabelElements (axis : Axis) value tickElement labelElement =
        (if showsTick axis value then [ tickElement ] else []) @
        (if showsLabel axis value then [ labelElement ] else [])

    let private spineElements pen (axis : Axis) startPoint endPoint =
        match axis.SpineStyle with
        | Full -> [ mkLine pen startPoint endPoint ]
        | Hidden -> []
        | Box ->
            let boxRect =
                Rect.create (Point.ofFloats (0.0, 0.0)) (Area.ofFloats (Canvas.canvasSize, Canvas.canvasSize))
                |> Element.createWithStyle (strokeStyle pen)
            [ boxRect ]

    let private formatValue (axis : Axis) =
        axis.TickFormat |> Option.defaultValue (sprintf "%.4g")

    let private horizontalAxisElements pen (axis : Axis) y tickDirection tickBaseline tickLabelOffset axisLabelBaseline axisLabelOffset =
        let bounds = axisBounds axis
        let formatter = formatValue axis
        let ticks =
            tickValues axis
            |> List.collect (fun value ->
                let x = Scale.apply axis.Scale value
                let tick = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x, y + tickDirection * axis.TickLength))
                let label =
                    mkLabel axis.FontSize pen Middle tickBaseline (formatter value)
                        (Point.ofFloats (x, y + tickDirection * axis.TickLength + tickLabelOffset))
                tickAndLabelElements axis value tick label)
        let axisLabel =
            axis.Label
            |> Option.map (fun body ->
                mkLabel axis.FontSize pen Middle axisLabelBaseline body (Point.ofFloats (bounds.MidPixel, y + axisLabelOffset)))
            |> Option.toList
        spineElements pen axis (Point.ofFloats (bounds.StartPixel, y)) (Point.ofFloats (bounds.EndPixel, y)) @ ticks @ axisLabel

    let private verticalAxisElements pen (axis : Axis) x tickDirection labelAnchor labelOffset axisLabelAnchor axisLabelOffset =
        let bounds = axisBounds axis
        let formatter = formatValue axis
        let ticks =
            tickValues axis
            |> List.collect (fun value ->
                let y = Scale.apply axis.Scale value
                let tick = mkLine pen (Point.ofFloats (x, y)) (Point.ofFloats (x + tickDirection * axis.TickLength, y))
                let label =
                    mkLabel axis.FontSize pen labelAnchor CentralBaseline (formatter value)
                        (Point.ofFloats (x + tickDirection * (axis.TickLength + labelOffset), y))
                tickAndLabelElements axis value tick label)
        let axisLabel =
            axis.Label
            |> Option.map (fun body ->
                mkLabel axis.FontSize pen axisLabelAnchor CentralBaseline body (Point.ofFloats (x + axisLabelOffset, bounds.MidPixel)))
            |> Option.toList
        spineElements pen axis (Point.ofFloats (x, bounds.StartPixel)) (Point.ofFloats (x, bounds.EndPixel)) @ ticks @ axisLabel

    let private isHorizontalAxis (axis : Axis) =
        match axis.Position with
        | Bottom | Top | HorizontalAt _ -> true
        | Left | Right | VerticalAt _ -> false

    let toGridElements theme (axis : Axis) =
        match theme.GridPen with
        | None -> []
        | Some gridPen ->
            tickValues axis
            |> List.filter (showsTick axis)
            |> List.map (fun value ->
                let pixel = Scale.apply axis.Scale value
                if isHorizontalAxis axis then mkLine gridPen (Point.ofFloats (pixel, 0.0)) (Point.ofFloats (pixel, Canvas.canvasSize))
                else mkLine gridPen (Point.ofFloats (0.0, pixel)) (Point.ofFloats (Canvas.canvasSize, pixel)))

    let toElements theme (axis : Axis) =
        let pen = theme.AxisPen
        match axis.Position with
        | Bottom ->
            horizontalAxisElements pen axis Canvas.canvasSize 1.0 HangingBaseline 2.0 HangingBaseline (axis.TickLength + axis.FontSize + 6.0)

        | Top ->
            horizontalAxisElements pen axis 0.0 -1.0 AlphabeticBaseline -3.0 AlphabeticBaseline (-axis.TickLength - axis.FontSize - 4.0)

        | Left ->
            verticalAxisElements pen axis 0.0 -1.0 End 4.0 Middle (-axis.TickLength - axis.FontSize - 4.0)

        | Right ->
            verticalAxisElements pen axis Canvas.canvasSize 1.0 Start 4.0 Start (axis.TickLength + axis.FontSize + 4.0)

        | HorizontalAt y ->
            horizontalAxisElements pen axis y 1.0 HangingBaseline 2.0 HangingBaseline (axis.TickLength + axis.FontSize + 6.0)

        | VerticalAt x ->
            let leftSide = x <= Canvas.canvasSize / 2.0
            let anchor = if leftSide then Start else End
            let tickSign = if leftSide then 1.0 else -1.0
            verticalAxisElements pen axis x tickSign anchor 4.0 anchor (tickSign * (axis.TickLength + axis.FontSize + 4.0))
