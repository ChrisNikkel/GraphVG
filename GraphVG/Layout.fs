namespace GraphVG

open SharpVG
open CommonMath

/// Padding computation and SVG element primitives that directly derive from padding geometry.
module Layout =

    // ── Padding helpers ───────────────────────────────────────────────────────

    let private emptyPadding = { Top = 0.0; Right = 0.0; Bottom = 0.0; Left = 0.0 }

    let private paddingWithTop top = { emptyPadding with Top = top }
    let private paddingWithRight right = { emptyPadding with Right = right }
    let private paddingWithBottom bottom = { emptyPadding with Bottom = bottom }
    let private paddingWithLeft left = { emptyPadding with Left = left }

    let private sumPadding a b =
        { Top = a.Top + b.Top; Right = a.Right + b.Right; Bottom = a.Bottom + b.Bottom; Left = a.Left + b.Left }

    // ── Axis padding ──────────────────────────────────────────────────────────

    let private topTickLabelGap (spacing : LayoutSpacing) =
        spacing.TickLabelPadding - 1.0

    let private bottomTickLabelGap (spacing : LayoutSpacing) =
        spacing.TickLabelPadding - 2.0

    let private topAxisLabelGap (spacing : LayoutSpacing) =
        spacing.AxisLabelPadding

    let private bottomAxisLabelGap (spacing : LayoutSpacing) =
        spacing.AxisLabelPadding + 2.0

    let private verticalTickLabelGap (spacing : LayoutSpacing) =
        spacing.TickLabelPadding

    let private horizontalTickExtent tickLabelGap (axis : Axis) =
        axis.TickLength + axis.FontSize + tickLabelGap

    let private horizontalLabelExtent axisLabelGap tickExtent (axis : Axis) =
        axis.Label
        |> Option.map (fun _ -> axis.TickLength + 2.0 * axis.FontSize + axisLabelGap)
        |> Option.defaultValue tickExtent

    let private verticalTickExtent tickLabelGap maximumTickLabelWidth (axis : Axis) =
        axis.TickLength + tickLabelGap + maximumTickLabelWidth

    let private verticalLabelExtent tickLabelGap axisLabelGap maximumTickLabelWidth tickExtent (axis : Axis) =
        axis.Label
        |> Option.map (fun _ -> axis.TickLength + tickLabelGap + maximumTickLabelWidth + axis.FontSize + axisLabelGap)
        |> Option.defaultValue tickExtent

    let private axisPadding (spacing : LayoutSpacing) (axis : Axis) =
        let maximumTickLabelWidth =
            Axis.formattedTickLabels axis
            |> List.map (estimatedTextWidth axis.FontSize)
            |> List.fold max 0.0
        match axis.Position with
        | Top ->
            let tickExtent = horizontalTickExtent (topTickLabelGap spacing) axis
            max tickExtent (horizontalLabelExtent (topAxisLabelGap spacing) tickExtent axis) |> paddingWithTop
        | Bottom ->
            let tickExtent = horizontalTickExtent (bottomTickLabelGap spacing) axis
            max tickExtent (horizontalLabelExtent (bottomAxisLabelGap spacing) tickExtent axis) |> paddingWithBottom
        | AxisPosition.Left ->
            let tickGap = verticalTickLabelGap spacing
            let tickExtent = verticalTickExtent tickGap maximumTickLabelWidth axis
            max tickExtent (verticalLabelExtent tickGap spacing.AxisLabelPadding maximumTickLabelWidth tickExtent axis) |> paddingWithLeft
        | AxisPosition.Right ->
            let tickGap = verticalTickLabelGap spacing
            let tickExtent = verticalTickExtent tickGap maximumTickLabelWidth axis
            max tickExtent (verticalLabelExtent tickGap spacing.AxisLabelPadding maximumTickLabelWidth tickExtent axis) |> paddingWithRight
        | HorizontalAt _
        | VerticalAt _ -> emptyPadding

    // ── Title padding ─────────────────────────────────────────────────────────

    let private titlePadding (graph : Graph) =
        graph.Title
        |> Option.map (fun _ -> graph.TitleStyle.FontSize + graph.LayoutSpacing.TitlePadding)
        |> Option.defaultValue 0.0
        |> paddingWithTop

    // ── Legend padding ────────────────────────────────────────────────────────

    let private legendPadding (graph : Graph) =
        match graph.Legend with
        | None -> emptyPadding
        | Some legend ->
            let labeledSeries = graph.Series |> List.choose (fun s -> s.Label)
            if labeledSeries.IsEmpty then emptyPadding
            else
                let maxLabelWidth = labeledSeries |> List.map (estimatedTextWidth legend.FontSize) |> List.max
                match legend.Position with
                | LegendHidden -> emptyPadding
                | LegendLeft ->
                    Legend.legendOuterMargin + Legend.swatchWidth + Legend.swatchLabelGap + maxLabelWidth + Legend.legendOuterMargin
                    |> paddingWithLeft
                | LegendRight ->
                    Legend.legendOuterMargin + Legend.swatchWidth + Legend.swatchLabelGap + maxLabelWidth + Legend.legendOuterMargin
                    |> paddingWithRight
                | LegendTop ->
                    Legend.legendOuterMargin + Legend.swatchHeight + Legend.legendOuterMargin
                    |> paddingWithTop
                | LegendBottom ->
                    Legend.legendOuterMargin + Legend.swatchHeight + Legend.legendOuterMargin
                    |> paddingWithBottom

    // ── Heatmap color ramp ────────────────────────────────────────────────────

    let private rampMargin = 16.0
    let private rampBarWidth = 16.0
    let private rampLabelGap = 6.0
    let private rampFontSize = 11.0
    let private rampSegments = 20

    let private heatSeries (graph : Graph) =
        graph.Series |> List.filter (fun s -> match s.Kind with | Heatmap _ -> true | _ -> false)

    let private formatRampValue (v : float) =
        if v = 0.0 then "0"
        elif abs v >= 1000.0 || (abs v < 0.01 && v <> 0.0) then sprintf "%.2g" v
        else sprintf "%.3g" v

    let private heatmapRampPadding (graph : Graph) =
        match heatSeries graph with
        | [] -> emptyPadding
        | first :: _ ->
            let heatValues = match first.Kind with | Heatmap values -> values | _ -> []
            let minVal = if List.isEmpty heatValues then 0.0 else List.min heatValues
            let maxVal = if List.isEmpty heatValues then 1.0 else List.max heatValues
            let maxLabelWidth =
                [ formatRampValue minVal; formatRampValue maxVal ]
                |> List.map (estimatedTextWidth rampFontSize)
                |> List.max
            rampMargin + rampBarWidth + rampLabelGap + maxLabelWidth + rampMargin
            |> paddingWithRight

    let heatmapRampElements (axisPen : Pen) (graph : Graph) : Element list =
        match heatSeries graph with
        | [] -> []
        | first :: _ ->
            let heatValues = match first.Kind with | Heatmap values -> values | _ -> []
            let minVal = if List.isEmpty heatValues then 0.0 else List.min heatValues
            let maxVal = if List.isEmpty heatValues then 1.0 else List.max heatValues
            let colorScale = first.ColorScale |> Option.defaultValue (Theme.defaultHeatmapColorScale minVal maxVal)
            let rampX = canvasSize + rampMargin
            let labelX = rampX + rampBarWidth + rampLabelGap
            let segH = canvasSize / float rampSegments
            let rampEls =
                [ 0 .. rampSegments - 1 ]
                |> List.map (fun i ->
                    // i=0 is the top segment (max value); i=rampSegments-1 is the bottom (min value)
                    let t = float (rampSegments - 1 - i) / float (rampSegments - 1)
                    let value = minVal + t * (maxVal - minVal)
                    let color = colorScale value
                    Rect.create
                        (Point.ofFloats (rampX, float i * segH))
                        (Area.ofFloats (rampBarWidth, segH + 1.0))
                    |> Element.createWithStyle (Style.empty |> Style.withFill color))
            let textStyle = Style.empty |> Style.withFillPen axisPen
            let maxLabel =
                Text.create (Point.ofFloats (labelX, segH / 2.0)) (formatRampValue maxVal)
                |> Text.withFontSize rampFontSize
                |> Text.withBaseline CentralBaseline
                |> Element.createWithStyle textStyle
            let minLabel =
                Text.create (Point.ofFloats (labelX, canvasSize - segH / 2.0)) (formatRampValue minVal)
                |> Text.withFontSize rampFontSize
                |> Text.withBaseline CentralBaseline
                |> Element.createWithStyle textStyle
            rampEls @ [ maxLabel; minLabel ]

    // ── Combined padding ──────────────────────────────────────────────────────

    let graphPadding (graph : Graph) =
        let fromAxes =
            [ graph.XAxis; graph.YAxis ]
            |> List.choose id
            |> List.map (axisPadding graph.LayoutSpacing)
            |> List.fold sumPadding emptyPadding
        let raw =
            [ fromAxes; titlePadding graph; legendPadding graph; heatmapRampPadding graph ]
            |> List.fold sumPadding emptyPadding
        {
            Top = max graph.LayoutSpacing.OuterMargin raw.Top
            Right = max graph.LayoutSpacing.OuterMargin raw.Right
            Bottom = max graph.LayoutSpacing.OuterMargin raw.Bottom
            Left = max graph.LayoutSpacing.OuterMargin raw.Left
        }

    // ── SVG element primitives ────────────────────────────────────────────────

    let viewBoxForPadding (padding : GraphPadding) =
        ViewBox.create
            (Point.ofFloats (-padding.Left, -padding.Top))
            (Area.ofFloats (canvasSize + padding.Left + padding.Right, canvasSize + padding.Top + padding.Bottom))

    let backgroundElement (backgroundColor : Color) (padding : GraphPadding) =
        Rect.create
            (Point.ofFloats (-padding.Left, -padding.Top))
            (Area.ofFloats (canvasSize + padding.Left + padding.Right, canvasSize + padding.Top + padding.Bottom))
        |> Element.createWithStyle (Style.empty |> Style.withFill backgroundColor)

    let plotBackground (color : Color) =
        Rect.create Point.origin (Area.ofFloats (canvasSize, canvasSize))
        |> Element.createWithStyle (Style.empty |> Style.withFill color)

    let private titleTopInset = 6.0

    let private titleAnchor = function
        | TitleAlignment.Left -> Start
        | TitleAlignment.Center -> Middle
        | TitleAlignment.Right -> End

    let titleElement (title : string) (fontSize : float) (alignment : TitleAlignment) (padding : GraphPadding) =
        Text.create (Point.ofFloats (canvasSize / 2.0, -padding.Top + titleTopInset)) title
        |> Text.withFontSize fontSize
        |> Text.withAnchor (titleAnchor alignment)
        |> Text.withBaseline HangingBaseline
        |> Element.createWithStyle (Style.empty |> Style.withFillPen Pen.black)
