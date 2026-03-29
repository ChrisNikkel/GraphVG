namespace GraphVG

/// Computes GraphPadding — the space each edge of the canvas needs to
/// accommodate axes, title, and legend before the viewBox is emitted.
module Layout =

    // ── Legend swatch geometry ────────────────────────────────────────────────
    // These are public because GraphVG.fs needs them to position swatch elements
    // at the same coordinates the padding computation reserved.

    let swatchWidth = 20.0
    let swatchHeight = 8.0
    let swatchLabelGap = 6.0
    let legendOuterMargin = 8.0
    let legendEntryGap = 8.0
    let legendHorizontalGap = 16.0

    // ── GraphPadding ──────────────────────────────────────────────────────────

    type GraphPadding =
        {
            Top : float
            Right : float
            Bottom : float
            Left : float
        }

    let private emptyPadding = { Top = 0.0; Right = 0.0; Bottom = 0.0; Left = 0.0 }

    let private paddingWithTop top = { emptyPadding with Top = top }
    let private paddingWithRight right = { emptyPadding with Right = right }
    let private paddingWithBottom bottom = { emptyPadding with Bottom = bottom }
    let private paddingWithLeft left = { emptyPadding with Left = left }

    let private sumPadding a b =
        { Top = a.Top + b.Top; Right = a.Right + b.Right; Bottom = a.Bottom + b.Bottom; Left = a.Left + b.Left }

    // ── Shared helpers ────────────────────────────────────────────────────────

    let estimatedTextWidth fontSize (text : string) =
        float text.Length * fontSize * 0.6

    // ── Axis padding ──────────────────────────────────────────────────────────

    let private defaultOuterMargin = 20.0
    let private topTickLabelGap = 3.0
    let private bottomTickLabelGap = 2.0
    let private topAxisLabelGap = 4.0
    let private bottomAxisLabelGap = 6.0
    let private verticalTickLabelGap = 4.0

    let private horizontalTickExtent tickLabelGap (axis : Axis) =
        axis.TickLength + axis.FontSize + tickLabelGap

    let private horizontalLabelExtent axisLabelGap tickExtent (axis : Axis) =
        axis.Label
        |> Option.map (fun _ -> axis.TickLength + 2.0 * axis.FontSize + axisLabelGap)
        |> Option.defaultValue tickExtent

    let private verticalTickExtent maximumTickLabelWidth (axis : Axis) =
        axis.TickLength + verticalTickLabelGap + maximumTickLabelWidth

    let private verticalLabelExtent maximumTickLabelWidth tickExtent (axis : Axis) =
        axis.Label
        |> Option.map (fun _ -> axis.TickLength + verticalTickLabelGap + maximumTickLabelWidth + axis.FontSize + verticalTickLabelGap)
        |> Option.defaultValue tickExtent

    let private axisPadding (axis : Axis) =
        let maximumTickLabelWidth =
            Axis.formattedTickLabels axis
            |> List.map (estimatedTextWidth axis.FontSize)
            |> List.fold max 0.0
        match axis.Position with
        | Top ->
            let tickExtent = horizontalTickExtent topTickLabelGap axis
            max tickExtent (horizontalLabelExtent topAxisLabelGap tickExtent axis) |> paddingWithTop
        | Bottom ->
            let tickExtent = horizontalTickExtent bottomTickLabelGap axis
            max tickExtent (horizontalLabelExtent bottomAxisLabelGap tickExtent axis) |> paddingWithBottom
        | Left ->
            let tickExtent = verticalTickExtent maximumTickLabelWidth axis
            max tickExtent (verticalLabelExtent maximumTickLabelWidth tickExtent axis) |> paddingWithLeft
        | Right ->
            let tickExtent = verticalTickExtent maximumTickLabelWidth axis
            max tickExtent (verticalLabelExtent maximumTickLabelWidth tickExtent axis) |> paddingWithRight
        | HorizontalAt _
        | VerticalAt _ -> emptyPadding

    // ── Title padding ─────────────────────────────────────────────────────────

    let private defaultTitlePadding = 16.0

    let private titlePadding (graph : Graph) =
        graph.Title
        |> Option.map (fun _ -> graph.TitleStyle.FontSize + defaultTitlePadding)
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
                    legendOuterMargin + swatchWidth + swatchLabelGap + maxLabelWidth + legendOuterMargin
                    |> paddingWithLeft
                | LegendRight ->
                    legendOuterMargin + swatchWidth + swatchLabelGap + maxLabelWidth + legendOuterMargin
                    |> paddingWithRight
                | LegendTop ->
                    legendOuterMargin + swatchHeight + legendOuterMargin
                    |> paddingWithTop
                | LegendBottom ->
                    legendOuterMargin + swatchHeight + legendOuterMargin
                    |> paddingWithBottom

    // ── Combined padding ──────────────────────────────────────────────────────

    let graphPadding (graph : Graph) =
        let fromAxes =
            [ graph.XAxis; graph.YAxis ]
            |> List.choose id
            |> List.map axisPadding
            |> List.fold sumPadding emptyPadding
        let raw =
            [ fromAxes; titlePadding graph; legendPadding graph ]
            |> List.fold sumPadding emptyPadding
        {
            Top = max defaultOuterMargin raw.Top
            Right = max defaultOuterMargin raw.Right
            Bottom = max defaultOuterMargin raw.Bottom
            Left = max defaultOuterMargin raw.Left
        }
