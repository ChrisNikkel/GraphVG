namespace GraphVG

open SharpVG
open CommonMath

type DomainPolicy =
    | IncludeZero
    | Tight
    | Padded of float

type Graph =
    {
        Series : Series list
        XScale : Scale
        YScale : Scale
        XAxis : Axis option
        YAxis : Axis option
        Theme : Theme
        Title : string option
        TitleStyle : TitleStyle
        Annotations : Annotation list
        Legend : Legend option
        DomainPolicy : DomainPolicy
        LayoutSpacing : LayoutSpacing
    }

module Graph =

    // ── Internal helpers ────────────────────────────────────────────────────────

    let private pixelRangeOf (scale : Scale) =
        match scale with
        | Scale.Linear(_, r) -> r
        | Scale.Log(_, r, _) -> r

    let private withScaleDomain newDomain (scale : Scale) : Scale =
        match scale with
        | Scale.Linear(_, r) -> Scale.Linear(newDomain, r)
        | Scale.Log(_, r, b) -> Scale.Log(newDomain, r, b)

    let private defaultAxes xScale yScale =
        let xAxisPosition = Scale.apply yScale 0.0 |> clamp 0.0 canvasSize
        let yAxisPosition = Scale.apply xScale 0.0 |> clamp 0.0 canvasSize
        Some (Axis.create (HorizontalAt xAxisPosition) xScale |> Axis.hideOrigin),
        Some (Axis.create (VerticalAt yAxisPosition) yScale |> Axis.hideOrigin)

    let private pointBounds (series : Series list) =
        let allBounds = series |> List.map Series.bounds
        let domainMin = allBounds |> List.map (fst >> fst) |> List.reduce min
        let domainMax = allBounds |> List.map (fst >> snd) |> List.reduce max
        let rangeMin = allBounds |> List.map (snd >> fst) |> List.reduce min
        let rangeMax = allBounds |> List.map (snd >> snd) |> List.reduce max
        (domainMin, domainMax), (rangeMin, rangeMax)

    let private buildScales domain range =
        Scale.linear domain (0.0, canvasSize),
        Scale.linear range (canvasSize, 0.0)

    let private applyPolicy (policy : DomainPolicy) (lo, hi) =
        let span = max (hi - lo) 1e-10
        match policy with
        | Tight -> lo, hi
        | Padded f -> lo - span * f, hi + span * f
        | IncludeZero ->
            let lo' = min 0.0 lo
            let hi' = max 0.0 hi
            let span' = max (hi' - lo') 1e-10
            let paddedLo = if lo' >= 0.0 then 0.0 else lo' - span' * 0.1
            paddedLo, hi' + span' * 0.1

    // ── Coordinate transform ────────────────────────────────────────────────────

    let toScaledSvgCoordinates graph (x, y) =
        Scale.apply graph.XScale x, Scale.apply graph.YScale y

    // ── Constructors ────────────────────────────────────────────────────────────

    let create (series : Series list) domain range =
        let xScale, yScale = buildScales domain range
        let xAxis, yAxis = defaultAxes xScale yScale
        {
            Series = series
            XScale = xScale
            YScale = yScale
            XAxis = xAxis
            YAxis = yAxis
            Theme = Theme.empty
            Title = None
            TitleStyle = TitleStyle.defaults
            Annotations = []
            Legend = None
            DomainPolicy = Padded 0.1
            LayoutSpacing = LayoutSpacing.defaults
        }

    let createWithSeries (series : Series) =
        let policy =
            match series.Kind with
            | Histogram _ | Bar | HorizontalBar -> IncludeZero
            | Heatmap _ -> Tight
            | _ -> Padded 0.1
        let domain, range = pointBounds [ series ]
        let xScale, yScale = buildScales (applyPolicy policy domain) (applyPolicy policy range)
        let xAxis, yAxis = defaultAxes xScale yScale
        {
            Series = [ series ]
            XScale = xScale
            YScale = yScale
            XAxis = xAxis
            YAxis = yAxis
            Theme = Theme.empty
            Title = None
            TitleStyle = TitleStyle.defaults
            Annotations = []
            Legend = None
            DomainPolicy = policy
            LayoutSpacing = LayoutSpacing.defaults
        }

    // ── Bounds helpers ──────────────────────────────────────────────────────────

    let addPadding padPercent graph =
        let domainMin, domainMax = Scale.domain graph.XScale
        let rangeMin, rangeMax = Scale.domain graph.YScale
        let domainPadding = (domainMax - domainMin) * padPercent
        let rangePadding = (rangeMax - rangeMin) * padPercent
        { graph with
            XScale = withScaleDomain (domainMin - domainPadding, domainMax + domainPadding) graph.XScale
            YScale = withScaleDomain (rangeMin - rangePadding, rangeMax + rangePadding) graph.YScale }

    let private recalcBounds graph =
        let domain, range = pointBounds graph.Series
        let newXScale = Scale.linear (applyPolicy graph.DomainPolicy domain) (pixelRangeOf graph.XScale)
        let newYScale = Scale.linear (applyPolicy graph.DomainPolicy range) (pixelRangeOf graph.YScale)
        let xAxis, yAxis = defaultAxes newXScale newYScale
        {
            graph with
                XScale = newXScale
                YScale = newYScale
                XAxis = xAxis
                YAxis = yAxis
        }

    // ── Builders ────────────────────────────────────────────────────────────────

    let addSeries series graph =
        { graph with Series = graph.Series @ [ series ] } |> recalcBounds

    let withDomainPolicy policy graph =
        { graph with DomainPolicy = policy } |> recalcBounds

    let withXScale xScale graph = { graph with XScale = xScale }
    let withYScale yScale graph = { graph with YScale = yScale }

    let withXAxis xAxis graph = { graph with XAxis = xAxis }
    let withYAxis yAxis graph = { graph with YAxis = yAxis }

    let withAxes (xAxis, yAxis) graph =
        {
            graph with
                XAxis = xAxis
                YAxis = yAxis
        }

    let withTheme theme graph = { graph with Theme = theme }
    let withDefaultTheme theme graph = { graph with Theme = theme }
    let withTitle title (graph : Graph) = { graph with Title = Some title }
    let withTitleStyle style (graph : Graph) = { graph with TitleStyle = style }
    let addAnnotation annotation (graph : Graph) = { graph with Annotations = graph.Annotations @ [ annotation ] }
    let withLegend legend (graph : Graph) = { graph with Legend = Some legend }
    let withLayoutSpacing spacing (graph : Graph) = { graph with LayoutSpacing = spacing }

    // ── Rendering ───────────────────────────────────────────────────────────────

    let private stackingFor (kind : SeriesKind) (isPercent : bool) (allSeries : Series list) =
        let grouped =
            allSeries
            |> List.mapi (fun i s -> i, s)
            |> List.filter (fun (_, s) -> s.Kind = kind)
        match grouped with
        | [] -> []
        | (_, first) :: _ ->
            let xValues = first.Points |> List.map fst
            let n = xValues.Length
            let rawY = grouped |> List.map (fun (_, s) -> s.Points |> List.map snd)
            let totals =
                [ 0 .. n - 1 ]
                |> List.map (fun k -> rawY |> List.sumBy (fun ys -> List.item k ys))
            let scaledY =
                if isPercent then
                    rawY |> List.map (fun ys ->
                        ys |> List.mapi (fun k y ->
                            if totals.[k] > 0.0 then y / totals.[k] * 100.0 else 0.0))
                else rawY
            let cumSums =
                scaledY
                |> List.scan (fun acc ys -> List.map2 (+) acc ys) (List.replicate n 0.0)
            List.zip grouped (List.pairwise cumSums)
            |> List.map (fun ((seriesIdx, _), (baselines, tops)) ->
                seriesIdx, (xValues, baselines, tops))

    let private streamgraphStacking (allSeries : Series list) =
        let grouped =
            allSeries
            |> List.mapi (fun i s -> i, s)
            |> List.filter (fun (_, s) -> s.Kind = Streamgraph)
        match grouped with
        | [] -> []
        | (_, first) :: _ ->
            let xValues = first.Points |> List.map fst
            let n = xValues.Length
            let rawY = grouped |> List.map (fun (_, s) -> s.Points |> List.map snd)
            let totals =
                [ 0 .. n - 1 ]
                |> List.map (fun k -> rawY |> List.sumBy (fun ys -> List.item k ys))
            let offsets = totals |> List.map (fun t -> -t / 2.0)
            let cumSums =
                rawY
                |> List.scan (fun acc ys -> List.map2 (+) acc ys) (List.replicate n 0.0)
            List.zip grouped (List.pairwise cumSums)
            |> List.map (fun ((seriesIdx, _), (baselines, tops)) ->
                let shiftedBase = List.map2 (+) baselines offsets
                let shiftedTop = List.map2 (+) tops offsets
                seriesIdx, (xValues, shiftedBase, shiftedTop))

    let private computeStacking (allSeries : Series list) =
        [ stackingFor StackedArea false allSeries
          stackingFor NormalizedStackedArea true allSeries
          streamgraphStacking allSeries ]
        |> List.concat
        |> Map.ofList

    let private inferMinSpacing (values : float list) =
        match List.sort values with
        | first :: second :: _ as sorted ->
            let spacing = sorted |> List.pairwise |> List.map (fun (a, b) -> b - a) |> List.min
            if spacing > 0.0 then spacing * 0.8 else abs (second - first) * 0.8
        | _ -> 0.8

    let private groupedBarLayout (kind : SeriesKind) (allSeries : Series list) =
        let grouped =
            allSeries
            |> List.mapi (fun i s -> i, s)
            |> List.filter (fun (_, s) -> s.Kind = kind)
        let count = grouped.Length
        grouped
        |> List.mapi (fun groupIdx (seriesIdx, _) -> seriesIdx, (groupIdx, count))
        |> Map.ofList

    let private computeBarLayouts (allSeries : Series list) =
        [ groupedBarLayout SeriesKind.Bar allSeries
          groupedBarLayout SeriesKind.HorizontalBar allSeries ]
        |> List.collect Map.toList
        |> Map.ofList

    let private expandStepPoints (mode : StepMode) (points : (float * float) list) =
        match points with
        | [] | [_] -> points
        | _ ->
            let intermediate =
                points
                |> List.pairwise
                |> List.collect (fun ((x1, y1), (x2, y2)) ->
                    match mode with
                    | After -> [ (x1, y1); (x2, y1) ]
                    | Before -> [ (x1, y1); (x1, y2) ]
                    | Mid ->
                        let xMid = (x1 + x2) / 2.0
                        [ (x1, y1); (xMid, y1); (xMid, y2) ])
            intermediate @ [ List.last points ]

    let private applyDash dash style =
        match dash with
        | Solid -> style
        | Dashed -> style |> Style.withStrokeDashArray [ 12.0; 6.0 ]
        | Dotted -> style |> Style.withStrokeDashArray [ 3.0; 6.0 ]
        | DashDot -> style |> Style.withStrokeDashArray [ 12.0; 6.0; 3.0; 6.0 ]

    let private errorBarElements (pen : Pen) (points : (float * float) list) (errorBar : ErrorBar) (graph : Graph) =
        let capHalfWidth = 5.0
        let style = Style.createWithPen pen
        let errorsFor i =
            match errorBar with
            | Symmetric errors ->
                let e = errors |> List.tryItem i |> Option.defaultValue 0.0
                e, e
            | Asymmetric (lows, highs) ->
                lows |> List.tryItem i |> Option.defaultValue 0.0,
                highs |> List.tryItem i |> Option.defaultValue 0.0
        points
        |> List.mapi (fun i (x, y) ->
            let errLow, errHigh = errorsFor i
            let svgX, _ = toScaledSvgCoordinates graph (x, y)
            let _, svgYBottom = toScaledSvgCoordinates graph (x, y - errLow)
            let _, svgYTop = toScaledSvgCoordinates graph (x, y + errHigh)
            [
                Line.create (Point.ofFloats (svgX, svgYBottom)) (Point.ofFloats (svgX, svgYTop))
                |> Element.createWithStyle style
                Line.create (Point.ofFloats (svgX - capHalfWidth, svgYBottom)) (Point.ofFloats (svgX + capHalfWidth, svgYBottom))
                |> Element.createWithStyle style
                Line.create (Point.ofFloats (svgX - capHalfWidth, svgYTop)) (Point.ofFloats (svgX + capHalfWidth, svgYTop))
                |> Element.createWithStyle style
            ])
        |> List.concat

    let drawSeries graph =
        let toSvgPoint point = point |> toScaledSvgCoordinates graph |> Point.ofFloats
        let stackingMap = computeStacking graph.Series
        let barLayouts = computeBarLayouts graph.Series
        let seriesToElements i (series : Series) =
            if series.Visible then
                let seriesPen = Theme.penForSeries i graph.Theme |> Pen.withOpacity series.Opacity
                match series.Kind with
                | Scatter ->
                    let radius = series.PointRadius |> Option.defaultValue (Length.ofFloat 3.0)
                    let r = Length.toFloat radius
                    let fillStyle = Style.empty |> Style.withFillPen seriesPen
                    let crossPen = series.StrokeWidth |> Option.map (fun w -> seriesPen |> Pen.withWidth w) |> Option.defaultValue seriesPen
                    let crossStyle = Style.createWithPen crossPen |> Style.withFillOpacity 0.0
                    series.Points
                    |> List.collect (fun pt ->
                        let svgPt = toSvgPoint pt
                        let cx, cy = Point.toFloats svgPt
                        let polygon unit =
                            Polygon.ofList (centerPolygon (cx, cy) r unit |> List.map Point.ofFloats)
                            |> Element.createWithStyle fillStyle
                        match series.PointShape with
                        | PointShape.Circle ->
                            [ Circle.create svgPt radius |> Element.createWithStyle fillStyle ]
                        | Square ->
                            [ polygon squareUnit ]
                        | Diamond ->
                            [ polygon diamondUnit ]
                        | Triangle ->
                            [ polygon triangleUnit ]
                        | Cross ->
                            centerLines (cx, cy) r crossUnit
                            |> List.map (fun (from', to') -> Line.create (Point.ofFloats from') (Point.ofFloats to') |> Element.createWithStyle crossStyle))
                | SeriesKind.Line ->
                    let strokePen = series.StrokeWidth |> Option.map (fun width -> seriesPen |> Pen.withWidth width) |> Option.defaultValue seriesPen
                    let style =
                        Style.createWithPen strokePen
                        |> Style.withFillOpacity 0.0
                        |> applyDash series.StrokeDash
                    [ Polyline.ofList (series.Points |> List.map toSvgPoint) |> Element.createWithStyle style ]
                | StepLine mode ->
                    let strokePen = series.StrokeWidth |> Option.map (fun width -> seriesPen |> Pen.withWidth width) |> Option.defaultValue seriesPen
                    let style =
                        Style.createWithPen strokePen
                        |> Style.withFillOpacity 0.0
                        |> applyDash series.StrokeDash
                    let stepPoints = expandStepPoints mode series.Points
                    [ Polyline.ofList (stepPoints |> List.map toSvgPoint) |> Element.createWithStyle style ]
                | Area ->
                    let strokePen = series.StrokeWidth |> Option.map (fun width -> seriesPen |> Pen.withWidth width) |> Option.defaultValue seriesPen
                    let style =
                        Style.createWithPen strokePen
                        |> Style.withFillPen strokePen
                        |> applyDash series.StrokeDash
                    [ Polygon.ofList (series.Points |> List.map toSvgPoint) |> Element.createWithStyle style ]
                | Band highs ->
                    let xs = series.Points |> List.map fst
                    let yLows = series.Points |> List.map snd
                    let fillPen = seriesPen |> Pen.withOpacity (series.Opacity * 0.3)
                    let baseStyle =
                        Style.empty
                        |> Style.withFillPen fillPen
                        |> applyDash series.StrokeDash
                    let style =
                        match series.StrokeWidth with
                        | Some w -> baseStyle |> Style.withStrokePen (seriesPen |> Pen.withWidth w)
                        | None -> baseStyle
                    let topPts = List.map2 (fun x yHigh -> toSvgPoint (x, yHigh)) xs highs
                    let botPts = List.map2 (fun x yLow -> toSvgPoint (x, yLow)) xs yLows
                    [ Polygon.ofList (topPts @ List.rev botPts) |> Element.createWithStyle style ]
                | Histogram binWidth ->
                    let fillStyle = Style.createWithPen seriesPen |> Style.withFillPen seriesPen
                    series.Points
                    |> List.map (fun (binLeft, count) ->
                        let svgX1, svgY1 = toScaledSvgCoordinates graph (binLeft, count)
                        let svgX2, svgY2 = toScaledSvgCoordinates graph (binLeft + binWidth, 0.0)
                        Rect.create
                            (Point.ofFloats (svgX1, svgY1))
                            (Area.ofFloats (svgX2 - svgX1, svgY2 - svgY1))
                        |> Element.createWithStyle fillStyle)
                | SeriesKind.Box ->
                    match series.Points with
                    | [ (xPos, yMin); (_, q1); (_, median); (_, q3); (_, yMax) ] ->
                        let halfWidth = series.PointRadius |> Option.map Length.toFloat |> Option.defaultValue 40.0
                        let svgX = fst (toScaledSvgCoordinates graph (xPos, 0.0))
                        let svgY value = snd (toScaledSvgCoordinates graph (0.0, value))
                        let svgMin = svgY yMin
                        let svgQ1 = svgY q1
                        let svgMedian = svgY median
                        let svgQ3 = svgY q3
                        let svgMax = svgY yMax
                        let fillStyle =
                            Style.createWithPen seriesPen
                            |> Style.withFillPen seriesPen
                            |> Style.withFillOpacity 0.2
                        let strokeStyle = Style.createWithPen seriesPen
                        [
                            Rect.create
                                (Point.ofFloats (svgX - halfWidth, svgQ3))
                                (Area.ofFloats (halfWidth * 2.0, svgQ1 - svgQ3))
                            |> Element.createWithStyle fillStyle
                            Line.create (Point.ofFloats (svgX - halfWidth, svgMedian)) (Point.ofFloats (svgX + halfWidth, svgMedian))
                            |> Element.createWithStyle strokeStyle
                            Line.create (Point.ofFloats (svgX, svgQ1)) (Point.ofFloats (svgX, svgMin))
                            |> Element.createWithStyle strokeStyle
                            Line.create (Point.ofFloats (svgX, svgQ3)) (Point.ofFloats (svgX, svgMax))
                            |> Element.createWithStyle strokeStyle
                            Line.create (Point.ofFloats (svgX - halfWidth / 2.0, svgMin)) (Point.ofFloats (svgX + halfWidth / 2.0, svgMin))
                            |> Element.createWithStyle strokeStyle
                            Line.create (Point.ofFloats (svgX - halfWidth / 2.0, svgMax)) (Point.ofFloats (svgX + halfWidth / 2.0, svgMax))
                            |> Element.createWithStyle strokeStyle
                        ]
                    | _ -> []
                | SeriesKind.Bar ->
                    match Map.tryFind i barLayouts with
                    | None -> []
                    | Some (groupIdx, groupCount) ->
                        let fillStyle = Style.createWithPen seriesPen |> Style.withFillPen seriesPen
                        let groupWidth = inferMinSpacing (series.Points |> List.map fst)
                        let barWidth = groupWidth / float groupCount
                        let xOffset = (float groupIdx - float (groupCount - 1) / 2.0) * barWidth
                        series.Points
                        |> List.map (fun (catX, value) ->
                            let svgX1, svgY1 = toScaledSvgCoordinates graph (catX + xOffset - barWidth / 2.0, value)
                            let svgX2, svgY2 = toScaledSvgCoordinates graph (catX + xOffset + barWidth / 2.0, 0.0)
                            Rect.create
                                (Point.ofFloats (min svgX1 svgX2, min svgY1 svgY2))
                                (Area.ofFloats (abs (svgX2 - svgX1), abs (svgY2 - svgY1)))
                            |> Element.createWithStyle fillStyle)
                | SeriesKind.HorizontalBar ->
                    match Map.tryFind i barLayouts with
                    | None -> []
                    | Some (groupIdx, groupCount) ->
                        let fillStyle = Style.createWithPen seriesPen |> Style.withFillPen seriesPen
                        let groupHeight = inferMinSpacing (series.Points |> List.map snd)
                        let barHeight = groupHeight / float groupCount
                        let yOffset = (float groupIdx - float (groupCount - 1) / 2.0) * barHeight
                        series.Points
                        |> List.map (fun (value, catY) ->
                            let svgX1, svgY1 = toScaledSvgCoordinates graph (0.0, catY + yOffset + barHeight / 2.0)
                            let svgX2, svgY2 = toScaledSvgCoordinates graph (value, catY + yOffset - barHeight / 2.0)
                            Rect.create
                                (Point.ofFloats (min svgX1 svgX2, min svgY1 svgY2))
                                (Area.ofFloats (abs (svgX2 - svgX1), abs (svgY2 - svgY1)))
                            |> Element.createWithStyle fillStyle)
                | Heatmap values ->
                    if List.isEmpty values then []
                    else
                        let minVal = List.min values
                        let maxVal = List.max values
                        let colorScale = series.ColorScale |> Option.defaultValue (Theme.defaultHeatmapColorScale minVal maxVal)
                        let xs = series.Points |> List.map fst
                        let ys = series.Points |> List.map snd
                        let minSpacing vs =
                            let sorted = vs |> List.sort |> List.distinct
                            match sorted |> List.pairwise |> List.map (fun (a, b) -> b - a) with
                            | spacings when not spacings.IsEmpty -> List.min spacings
                            | _ -> 1.0
                        let cellWidth = minSpacing xs
                        let cellHeight = minSpacing ys
                        List.zip series.Points values
                        |> List.map (fun ((col, row), value) ->
                            let svgX1, svgY1 = toScaledSvgCoordinates graph (col - cellWidth / 2.0, row + cellHeight / 2.0)
                            let svgX2, svgY2 = toScaledSvgCoordinates graph (col + cellWidth / 2.0, row - cellHeight / 2.0)
                            let color = colorScale value
                            Rect.create
                                (Point.ofFloats (min svgX1 svgX2, min svgY1 svgY2))
                                (Area.ofFloats (abs (svgX2 - svgX1) + 1.0, abs (svgY2 - svgY1) + 1.0))
                            |> Element.createWithStyle (Style.empty |> Style.withFill color |> Style.withFillOpacity series.Opacity))
                | Bubble sizes ->
                    // Radius is area-proportional: a point with twice the size value renders with twice the area.
                    // The largest bubble has radius maxRadiusPx (default 40 px, or set via withPointRadius).
                    let maxSize = sizes |> List.fold max 0.0
                    let maxRadiusPx = series.PointRadius |> Option.map Length.toFloat |> Option.defaultValue 40.0
                    let fillStyle = Style.empty |> Style.withFillPen seriesPen |> Style.withFillOpacity 0.5
                    series.Points
                    |> List.mapi (fun idx pt ->
                        let svgPt = toSvgPoint pt
                        let size = sizes |> List.tryItem idx |> Option.defaultValue 0.0
                        let radius =
                            if maxSize <= 0.0 || size <= 0.0 then 0.0
                            else maxRadiusPx * sqrt (size / maxSize)
                        if radius > 0.0 then
                            [ Circle.create svgPt (Length.ofFloat radius) |> Element.createWithStyle fillStyle ]
                        else [])
                    |> List.concat
                | Candlestick ohlcData | Ohlc ohlcData ->
                    if List.isEmpty ohlcData then []
                    else
                        let isOhlcStyle = match series.Kind with | Ohlc _ -> true | _ -> false
                        let halfBar = ohlcData |> List.map (fun b -> b.X) |> inferMinSpacing |> fun s -> s * 0.4
                        let wickWidth = series.StrokeWidth |> Option.defaultValue (Length.ofFloat 1.5)
                        ohlcData
                        |> List.collect (fun bar ->
                            let svgCX, _ = toScaledSvgCoordinates graph (bar.X, 0.0)
                            let svgLeft, _ = toScaledSvgCoordinates graph (bar.X - halfBar, 0.0)
                            let svgRight, _ = toScaledSvgCoordinates graph (bar.X + halfBar, 0.0)
                            let svgOpen = snd (toScaledSvgCoordinates graph (0.0, bar.Open))
                            let svgClose = snd (toScaledSvgCoordinates graph (0.0, bar.Close))
                            let svgHigh = snd (toScaledSvgCoordinates graph (0.0, bar.High))
                            let svgLow = snd (toScaledSvgCoordinates graph (0.0, bar.Low))
                            let fillColor = if bar.Close >= bar.Open then graph.Theme.UpColor else graph.Theme.DownColor
                            let colorPen = Pen.create fillColor |> Pen.withOpacity series.Opacity
                            let wickStyle = Style.createWithPen (colorPen |> Pen.withWidth wickWidth) |> Style.withFillOpacity 0.0
                            if isOhlcStyle then
                                [
                                    Line.create (Point.ofFloats (svgCX, svgHigh)) (Point.ofFloats (svgCX, svgLow))
                                    |> Element.createWithStyle wickStyle
                                    Line.create (Point.ofFloats (svgLeft, svgOpen)) (Point.ofFloats (svgCX, svgOpen))
                                    |> Element.createWithStyle wickStyle
                                    Line.create (Point.ofFloats (svgCX, svgClose)) (Point.ofFloats (svgRight, svgClose))
                                    |> Element.createWithStyle wickStyle
                                ]
                            else
                                let svgBodyTop = min svgOpen svgClose
                                let svgBodyBottom = max svgOpen svgClose
                                let bodyHeight = max 1.0 (svgBodyBottom - svgBodyTop)
                                let bodyStyle = Style.empty |> Style.withFill fillColor |> Style.withFillOpacity series.Opacity
                                [
                                    Line.create (Point.ofFloats (svgCX, svgHigh)) (Point.ofFloats (svgCX, svgBodyTop))
                                    |> Element.createWithStyle wickStyle
                                    Line.create (Point.ofFloats (svgCX, svgBodyBottom)) (Point.ofFloats (svgCX, svgLow))
                                    |> Element.createWithStyle wickStyle
                                    Rect.create
                                        (Point.ofFloats (svgLeft, svgBodyTop))
                                        (Area.ofFloats (svgRight - svgLeft, bodyHeight))
                                    |> Element.createWithStyle bodyStyle
                                ])
                | StackedArea | NormalizedStackedArea | Streamgraph ->
                    match Map.tryFind i stackingMap with
                    | None -> []
                    | Some (xValues, baselines, tops) ->
                        let strokePen = series.StrokeWidth |> Option.map (fun w -> seriesPen |> Pen.withWidth w) |> Option.defaultValue seriesPen
                        let style =
                            Style.createWithPen strokePen
                            |> Style.withFillPen strokePen
                            |> applyDash series.StrokeDash
                        let toSvgXY x y = toScaledSvgCoordinates graph (x, y) |> Point.ofFloats
                        let topPts = List.map2 toSvgXY xValues tops
                        let basePts = List.map2 toSvgXY xValues baselines
                        [ Polygon.ofList (topPts @ List.rev basePts) |> Element.createWithStyle style ]
            else []
        let errorElements =
            graph.Series
            |> List.mapi (fun i series ->
                if series.Visible then
                    match series.ErrorBars with
                    | None -> []
                    | Some errorBar ->
                        let seriesPen = Theme.penForSeries i graph.Theme |> Pen.withOpacity series.Opacity
                        errorBarElements seriesPen series.Points errorBar graph
                else [])
            |> List.concat
        (graph.Series |> List.mapi seriesToElements |> List.concat) @ errorElements
