namespace GraphVG

open SharpVG
open CommonMath

module GraphVG =

    let private defaultOuterMargin = 20.0
    let private defaultTitlePadding = 16.0
    let private titleTopInset = 6.0
    let private estimatedCharacterWidthFactor = 0.6
    let private topTickLabelGap = 3.0
    let private bottomTickLabelGap = 2.0
    let private topAxisLabelGap = 4.0
    let private bottomAxisLabelGap = 6.0
    let private verticalTickLabelGap = 4.0

    type private GraphPadding =
        {
            Top : float
            Right : float
            Bottom : float
            Left : float
        }

    let private emptyPadding =
        {
            Top = 0.0
            Right = 0.0
            Bottom = 0.0
            Left = 0.0
        }

    let private paddingWithTop top =
        { emptyPadding with Top = top }

    let private paddingWithRight right =
        { emptyPadding with Right = right }

    let private paddingWithBottom bottom =
        { emptyPadding with Bottom = bottom }

    let private paddingWithLeft left =
        { emptyPadding with Left = left }

    let private sumPadding left right =
        {
            Top = left.Top + right.Top
            Right = left.Right + right.Right
            Bottom = left.Bottom + right.Bottom
            Left = left.Left + right.Left
        }

    let private estimatedTextWidth fontSize (text : string) =
        float text.Length * fontSize * estimatedCharacterWidthFactor

    let private tickValues (axis : Axis) =
        match axis.Ticks with
        | TickCount count -> Scale.ticks axis.Scale count
        | TickInterval interval ->
            let lower, upper = Scale.domain axis.Scale
            let first = ceil (lower / interval) * interval
            [ for index in 0 .. int (floor ((upper - first) / interval)) -> first + float index * interval ]

    let private showsLabel (axis : Axis) value =
        let domainMinimum, domainMaximum = Scale.domain axis.Scale
        let isOrigin = isNear 0.0 value
        let isBound = isNear domainMinimum value || isNear domainMaximum value
        not (axis.HideOriginLabel && isOrigin) && not (axis.HideBoundsLabel && isBound)

    let private formattedTickLabels (axis : Axis) =
        let formatter = axis.TickFormat |> Option.defaultValue (sprintf "%.4g")
        tickValues axis
        |> List.filter (showsLabel axis)
        |> List.map formatter

    let private horizontalTickExtent tickLabelGap (axis : Axis) =
        axis.TickLength + axis.FontSize + tickLabelGap

    let private horizontalLabelExtent axisLabelGap tickExtent (axis : Axis) =
        axis.Label
        |> Option.map (fun _ -> axis.TickLength + 2.0 * axis.FontSize + axisLabelGap)
        |> Option.defaultValue tickExtent

    let private verticalTickExtent maximumTickLabelWidth (axis : Axis) =
        axis.TickLength + verticalTickLabelGap + maximumTickLabelWidth

    let private leftLabelExtent tickExtent (axis : Axis) =
        axis.Label
        |> Option.map (fun label -> axis.TickLength + axis.FontSize + verticalTickLabelGap + estimatedTextWidth axis.FontSize label / 2.0)
        |> Option.defaultValue tickExtent

    let private rightLabelExtent tickExtent (axis : Axis) =
        axis.Label
        |> Option.map (fun label -> axis.TickLength + axis.FontSize + verticalTickLabelGap + estimatedTextWidth axis.FontSize label)
        |> Option.defaultValue tickExtent

    let private topAxisPadding (axis : Axis) =
        let tickExtent = horizontalTickExtent topTickLabelGap axis
        let labelExtent = horizontalLabelExtent topAxisLabelGap tickExtent axis
        max tickExtent labelExtent |> paddingWithTop

    let private bottomAxisPadding (axis : Axis) =
        let tickExtent = horizontalTickExtent bottomTickLabelGap axis
        let labelExtent = horizontalLabelExtent bottomAxisLabelGap tickExtent axis
        max tickExtent labelExtent |> paddingWithBottom

    let private leftAxisPadding maximumTickLabelWidth (axis : Axis) =
        let tickExtent = verticalTickExtent maximumTickLabelWidth axis
        let labelExtent = leftLabelExtent tickExtent axis
        max tickExtent labelExtent |> paddingWithLeft

    let private rightAxisPadding maximumTickLabelWidth (axis : Axis) =
        let tickExtent = verticalTickExtent maximumTickLabelWidth axis
        let labelExtent = rightLabelExtent tickExtent axis
        max tickExtent labelExtent |> paddingWithRight

    let private titleExtent (graph : Graph) =
        graph.Title
        |> Option.map (fun _ -> graph.TitleStyle.FontSize + defaultTitlePadding)
        |> Option.defaultValue 0.0

    let private axisPadding (axis : Axis) =
        let tickLabels = formattedTickLabels axis
        let maximumTickLabelWidth =
            tickLabels
            |> List.map (estimatedTextWidth axis.FontSize)
            |> List.fold max 0.0
        match axis.Position with
        | Top -> topAxisPadding axis
        | Bottom -> bottomAxisPadding axis
        | Left -> leftAxisPadding maximumTickLabelWidth axis
        | Right -> rightAxisPadding maximumTickLabelWidth axis
        | HorizontalAt _
        | VerticalAt _ -> emptyPadding

    let private graphPadding (graph : Graph) =
        let axisPadding =
            [ graph.XAxis; graph.YAxis ]
            |> List.choose id
            |> List.map axisPadding
            |> List.fold sumPadding emptyPadding
        let titleExtent = titleExtent graph
        {
            Top = max defaultOuterMargin (axisPadding.Top + titleExtent)
            Right = max defaultOuterMargin axisPadding.Right
            Bottom = max defaultOuterMargin axisPadding.Bottom
            Left = max defaultOuterMargin axisPadding.Left
        }

    let private viewBoxForPadding padding =
        ViewBox.create
            (Point.ofFloats (-padding.Left, -padding.Top))
            (Area.ofFloats (Canvas.canvasSize + padding.Left + padding.Right, Canvas.canvasSize + padding.Top + padding.Bottom))

    let private backgroundElementForPadding backgroundColor padding =
        Rect.create
            (Point.ofFloats (-padding.Left, -padding.Top))
            (Area.ofFloats (Canvas.canvasSize + padding.Left + padding.Right, Canvas.canvasSize + padding.Top + padding.Bottom))
        |> Element.createWithStyle (Style.empty |> Style.withFill backgroundColor)

    let private plotBackgroundElements (graph : Graph) =
        graph.Theme.PlotBackground
        |> Option.map (fun color ->
            Rect.create (Point.origin) (Area.ofFloats (Canvas.canvasSize, Canvas.canvasSize))
            |> Element.createWithStyle (Style.empty |> Style.withFill color))
        |> Option.toList

    let private titleElements (graph : Graph) padding =
        graph.Title
        |> Option.map (fun title ->
            let style = Style.empty |> Style.withFillPen Pen.black
            let position = Point.ofFloats (Canvas.canvasSize / 2.0, -padding.Top + titleTopInset)
            Text.create position title
            |> Text.withFontSize graph.TitleStyle.FontSize
            |> Text.withAnchor graph.TitleStyle.Alignment
            |> Text.withBaseline HangingBaseline
            |> Element.createWithStyle style)
        |> Option.toList

    let private buildSvg (graph : Graph) =
        let padding = graphPadding graph
        let viewBox = viewBoxForPadding padding
        let background = backgroundElementForPadding graph.Theme.Background padding
        let axes = [ graph.XAxis; graph.YAxis ] |> List.choose id
        let gridElements = axes |> List.collect (Axis.toGridElements graph.Theme)
        let axisElements = axes |> List.collect (Axis.toElements graph.Theme)
        background :: (plotBackgroundElements graph) @ gridElements @ Graph.drawSeries graph @ axisElements @ (titleElements graph padding)
        |> Svg.ofList
        |> Svg.withViewBox viewBox

    /// Returns a raw SVG string.
    let render (graph : Graph) : string =
        buildSvg graph |> Svg.toString

    /// Returns a full HTML page with the graph centered and fit to the viewport.
    let toHtml (graph : Graph) : string =
        let svgContent = buildSvg graph |> Svg.toString
        let css = "html,body{margin:0;height:100%;}body{display:flex;align-items:center;justify-content:center;background:#f5f5f5;}svg{width:100vmin;height:100vmin;}"
        "<!DOCTYPE html>\n<html>\n<head>\n<title>GraphVG</title>\n<style>" + css + "</style>\n</head>\n<body>\n" + svgContent + "\n</body>\n</html>\n"
