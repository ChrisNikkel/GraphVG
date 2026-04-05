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
        let cs =
            match axis.Scale with
            | Scale.Linear(_, (r1, r2)) -> max (abs r1) (abs r2)
            | Scale.Log(_, (r1, r2), _) -> max (abs r1) (abs r2)
        let sf = cs / canvasSize
        let maximumTickLabelWidth =
            Axis.formattedTickLabels axis
            |> List.map (estimatedTextWidth axis.FontSize)
            |> List.fold max 0.0
        match axis.Position with
        | Top ->
            let tickExtent = horizontalTickExtent (topTickLabelGap spacing) axis
            (max tickExtent (horizontalLabelExtent (topAxisLabelGap spacing) tickExtent axis)) * sf |> paddingWithTop
        | Bottom ->
            let tickExtent = horizontalTickExtent (bottomTickLabelGap spacing) axis
            (max tickExtent (horizontalLabelExtent (bottomAxisLabelGap spacing) tickExtent axis)) * sf |> paddingWithBottom
        | AxisPosition.Left ->
            let tickGap = verticalTickLabelGap spacing
            let tickExtent = verticalTickExtent tickGap maximumTickLabelWidth axis
            (max tickExtent (verticalLabelExtent tickGap spacing.AxisLabelPadding maximumTickLabelWidth tickExtent axis)) * sf |> paddingWithLeft
        | AxisPosition.Right ->
            let tickGap = verticalTickLabelGap spacing
            let tickExtent = verticalTickExtent tickGap maximumTickLabelWidth axis
            (max tickExtent (verticalLabelExtent tickGap spacing.AxisLabelPadding maximumTickLabelWidth tickExtent axis)) * sf |> paddingWithRight
        | HorizontalAt _
        | VerticalAt _ -> emptyPadding

    // ── Title padding ─────────────────────────────────────────────────────────

    let private titlePadding (graph : Graph) =
        let sf = Graph.canvasSizeOf graph / canvasSize
        graph.Title
        |> Option.map (fun _ -> (graph.TitleStyle.FontSize + graph.LayoutSpacing.TitlePadding) * sf)
        |> Option.defaultValue 0.0
        |> paddingWithTop

    // ── Legend padding ────────────────────────────────────────────────────────

    let private legendPadding (graph : Graph) =
        let sf = Graph.canvasSizeOf graph / canvasSize
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
                    (Legend.legendOuterMargin + Legend.swatchWidth + Legend.swatchLabelGap + maxLabelWidth + Legend.legendOuterMargin) * sf
                    |> paddingWithLeft
                | LegendRight ->
                    (Legend.legendOuterMargin + Legend.swatchWidth + Legend.swatchLabelGap + maxLabelWidth + Legend.legendOuterMargin) * sf
                    |> paddingWithRight
                | LegendTop ->
                    (Legend.legendOuterMargin + Legend.swatchHeight + Legend.legendOuterMargin) * sf
                    |> paddingWithTop
                | LegendBottom ->
                    (Legend.legendOuterMargin + Legend.swatchHeight + Legend.legendOuterMargin) * sf
                    |> paddingWithBottom

    // ── Heatmap color ramp ────────────────────────────────────────────────────

    let private rampMargin = canvasSize * 0.016
    let private rampBarWidth = canvasSize * 0.016
    let private rampLabelGap = canvasSize * 0.006
    let private rampFontSize = canvasSize * 0.011
    let private rampSegments = 20

    let private heatSeries (graph : Graph) =
        graph.Series |> List.filter (fun s -> match s.Kind with | Heatmap _ -> true | _ -> false)

    let private formatRampValue (v : float) =
        if v = 0.0 then "0"
        elif abs v >= 1000.0 || (abs v < 0.01 && v <> 0.0) then sprintf "%.2g" v
        else sprintf "%.3g" v

    let private heatmapRampPadding (graph : Graph) =
        let sf = Graph.canvasSizeOf graph / canvasSize
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
            (rampMargin + rampBarWidth + rampLabelGap + maxLabelWidth + rampMargin) * sf
            |> paddingWithRight

    let heatmapRampElements (axisPen : Pen) (graph : Graph) : Element list =
        let cs = Graph.canvasSizeOf graph
        let sf = cs / canvasSize
        match heatSeries graph with
        | [] -> []
        | first :: _ ->
            let heatValues = match first.Kind with | Heatmap values -> values | _ -> []
            let minVal = if List.isEmpty heatValues then 0.0 else List.min heatValues
            let maxVal = if List.isEmpty heatValues then 1.0 else List.max heatValues
            let colorScale = first.ColorScale |> Option.defaultValue (Theme.defaultHeatmapColorScale minVal maxVal)
            let rampX = cs + rampMargin * sf
            let labelX = rampX + rampBarWidth * sf + rampLabelGap * sf
            let segH = cs / float rampSegments
            let rampEls =
                [ 0 .. rampSegments - 1 ]
                |> List.map (fun i ->
                    // i=0 is the top segment (max value); i=rampSegments-1 is the bottom (min value)
                    let t = float (rampSegments - 1 - i) / float (rampSegments - 1)
                    let value = minVal + t * (maxVal - minVal)
                    let color = colorScale value
                    Rect.create
                        (Point.ofFloats (rampX, float i * segH))
                        (Area.ofFloats (rampBarWidth * sf, segH + sf))
                    |> Element.createWithStyle (Style.empty |> Style.withFill color))
            let textStyle = Style.empty |> Style.withFillPen axisPen
            let maxLabel =
                Text.create (Point.ofFloats (labelX, segH / 2.0)) (formatRampValue maxVal)
                |> Text.withFontSize (rampFontSize * sf)
                |> Text.withBaseline CentralBaseline
                |> Element.createWithStyle textStyle
            let minLabel =
                Text.create (Point.ofFloats (labelX, cs - segH / 2.0)) (formatRampValue minVal)
                |> Text.withFontSize (rampFontSize * sf)
                |> Text.withBaseline CentralBaseline
                |> Element.createWithStyle textStyle
            rampEls @ [ maxLabel; minLabel ]

    // ── Combined padding ──────────────────────────────────────────────────────

    let graphPadding (graph : Graph) =
        let sf = Graph.canvasSizeOf graph / canvasSize
        let outerMargin = graph.LayoutSpacing.OuterMargin * sf
        let fromAxes =
            [ graph.XAxis; graph.YAxis ]
            |> List.choose id
            |> List.map (axisPadding graph.LayoutSpacing)
            |> List.fold sumPadding emptyPadding
        let raw =
            [ fromAxes; titlePadding graph; legendPadding graph; heatmapRampPadding graph ]
            |> List.fold sumPadding emptyPadding
        {
            Top = max outerMargin raw.Top
            Right = max outerMargin raw.Right
            Bottom = max outerMargin raw.Bottom
            Left = max outerMargin raw.Left
        }

    // ── SVG element primitives ────────────────────────────────────────────────

    let viewBoxForPadding (padding : GraphPadding) (cs : float) =
        ViewBox.create
            (Point.ofFloats (-padding.Left, -padding.Top))
            (Area.ofFloats (cs + padding.Left + padding.Right, cs + padding.Top + padding.Bottom))

    let backgroundElement (backgroundColor : Color) (padding : GraphPadding) (cs : float) =
        Rect.create
            (Point.ofFloats (-padding.Left, -padding.Top))
            (Area.ofFloats (cs + padding.Left + padding.Right, cs + padding.Top + padding.Bottom))
        |> Element.createWithStyle (Style.empty |> Style.withFill backgroundColor)

    let plotBackground (color : Color) (cs : float) =
        Rect.create Point.origin (Area.ofFloats (cs, cs))
        |> Element.createWithStyle (Style.empty |> Style.withFill color)

    let private titleTopInset = canvasSize * 0.006

    let private titleAnchor = function
        | TitleAlignment.Left -> Start
        | TitleAlignment.Center -> Middle
        | TitleAlignment.Right -> End

    let titleElement (title : string) (fontSize : float) (alignment : TitleAlignment) (padding : GraphPadding) (cs : float) =
        let sf = cs / canvasSize
        Text.create (Point.ofFloats (cs / 2.0, -padding.Top + titleTopInset * sf)) title
        |> Text.withFontSize fontSize
        |> Text.withAnchor (titleAnchor alignment)
        |> Text.withBaseline HangingBaseline
        |> Element.createWithStyle (Style.empty |> Style.withFillPen Pen.black)
