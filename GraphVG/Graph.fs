namespace GraphVG

open System
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
        YScaleRight : Scale option
        XAxis : Axis option
        YAxis : Axis option
        RightAxis : Axis option
        Theme : Theme
        Title : string option
        TitleStyle : TitleStyle
        Annotations : Annotation list
        Legend : Legend option
        DomainPolicy : DomainPolicy
        LayoutSpacing : LayoutSpacing
        CanvasSize : float
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
        let cs = snd (pixelRangeOf xScale)
        let xAxisPosition = Scale.apply yScale 0.0 |> clamp 0.0 cs
        let yAxisPosition = Scale.apply xScale 0.0 |> clamp 0.0 cs
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
        let xSpan = abs (snd domain - fst domain)
        let ySpan = abs (snd range - fst range)
        let cs = adaptiveCanvasSize xSpan ySpan
        Scale.linear domain (0.0, cs),
        Scale.linear range (cs, 0.0)

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

    /// Returns the internal canvas size for the graph, derived from its X scale pixel range.
    let canvasSizeOf (graph : Graph) =
        snd (pixelRangeOf graph.XScale)

    // ── Constructors ────────────────────────────────────────────────────────────

    let create (series : Series list) domain range =
        let xScale, yScale = buildScales domain range
        let xAxis, yAxis = defaultAxes xScale yScale
        {
            Series = series
            XScale = xScale
            YScale = yScale
            YScaleRight = None
            XAxis = xAxis
            YAxis = yAxis
            RightAxis = None
            Theme = Theme.empty
            Title = None
            TitleStyle = TitleStyle.defaults
            Annotations = []
            Legend = None
            DomainPolicy = Padded 0.1
            LayoutSpacing = LayoutSpacing.defaults
            CanvasSize = snd (pixelRangeOf xScale)
        }

    let createWithSeries (series : Series) =
        let policy =
            match series.Kind with
            | Histogram _ | Bar | HorizontalBar | Waterfall _ | Lollipop | HorizontalLollipop -> IncludeZero
            | Heatmap _ | ParallelSets _ | Pie _ | Funnel _ | Treemap _ | Bullet _ -> Tight
            | _ -> Padded 0.1
        let domain, range = pointBounds [ series ]
        let xScale, yScale = buildScales (applyPolicy policy domain) (applyPolicy policy range)
        let xAxis, yAxis =
            match series.Kind with
            | ParallelSets _ | Pie _ | Funnel _ | Treemap _ | Bullet _ -> None, None
            | _ -> defaultAxes xScale yScale
        {
            Series = [ series ]
            XScale = xScale
            YScale = yScale
            YScaleRight = None
            XAxis = xAxis
            YAxis = yAxis
            RightAxis = None
            Theme = Theme.empty
            Title = None
            TitleStyle = TitleStyle.defaults
            Annotations = []
            Legend = None
            DomainPolicy = policy
            LayoutSpacing = LayoutSpacing.defaults
            CanvasSize = snd (pixelRangeOf xScale)
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
        let domain, _ = pointBounds graph.Series
        let leftSeries = graph.Series |> List.filter (fun s -> s.YAxis = YLeft)
        let _, leftRange =
            if leftSeries.IsEmpty then pointBounds graph.Series
            else pointBounds leftSeries
        let newXScale = Scale.linear (applyPolicy graph.DomainPolicy domain) (pixelRangeOf graph.XScale)
        let newYScale = Scale.linear (applyPolicy graph.DomainPolicy leftRange) (pixelRangeOf graph.YScale)
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

    let withRightYRange range (graph : Graph) =
        let cs = canvasSizeOf graph
        let yScaleRight = Scale.linear range (cs, 0.0)
        let rightAxis = Axis.create AxisPosition.Right yScaleRight
        { graph with YScaleRight = Some yScaleRight; RightAxis = Some rightAxis }

    let withRightAxis rightAxis (graph : Graph) = { graph with RightAxis = rightAxis }

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

    let private errorBarElements (pen : Pen) (points : (float * float) list) (errorBar : ErrorBar) (toCoord : float * float -> float * float) (cs : float) =
        let capHalfWidth = cs * 0.005
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
            let svgX, _ = toCoord (x, y)
            let _, svgYBottom = toCoord (x, y - errLow)
            let _, svgYTop = toCoord (x, y + errHigh)
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
        let stackingMap = computeStacking graph.Series
        let barLayouts = computeBarLayouts graph.Series
        let seriesToElements i (series : Series) =
            if series.Visible then
                let cs = snd (pixelRangeOf graph.XScale)
                let sf = cs / canvasSize
                let yScale =
                    match series.YAxis with
                    | YLeft -> graph.YScale
                    | YRight -> graph.YScaleRight |> Option.defaultValue graph.YScale
                let toCoord (x, y) = Scale.apply graph.XScale x, Scale.apply yScale y
                let toSvgPoint pt = toCoord pt |> Point.ofFloats
                let seriesPen = Theme.penForSeries i graph.Theme |> Pen.withOpacity series.Opacity
                match series.Kind with
                | Scatter ->
                    let r = (series.PointRadius |> Option.defaultValue (canvasSize * 0.003)) * sf
                    let fillStyle = Style.empty |> Style.withFillPen seriesPen
                    let crossPen = series.StrokeWidth |> Option.map (fun w -> seriesPen |> Pen.withWidth (Length.ofFloat (w * sf))) |> Option.defaultValue seriesPen
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
                            [ Circle.create svgPt (Length.ofFloat r) |> Element.createWithStyle fillStyle ]
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
                    let strokePen = series.StrokeWidth |> Option.map (fun width -> seriesPen |> Pen.withWidth (Length.ofFloat (width * sf))) |> Option.defaultValue seriesPen
                    let style =
                        Style.createWithPen strokePen
                        |> Style.withFillOpacity 0.0
                        |> applyDash series.StrokeDash
                    [ Polyline.ofList (series.Points |> List.map toSvgPoint) |> Element.createWithStyle style ]
                | SeriesKind.SegmentedLine ->
                    let strokePen = series.StrokeWidth |> Option.map (fun width -> seriesPen |> Pen.withWidth (Length.ofFloat (width * sf))) |> Option.defaultValue seriesPen
                    let style =
                        Style.createWithPen strokePen
                        |> Style.withFillOpacity 0.0
                        |> applyDash series.StrokeDash
                    let isBreak (x, y) = Double.IsNaN x || Double.IsNaN y
                    let path =
                        series.Points
                        |> List.fold (fun (path, started) pt ->
                            if isBreak pt then path, false
                            elif not started then Path.addMoveTo Absolute (toSvgPoint pt) path, true
                            else Path.addLineTo Absolute (toSvgPoint pt) path, true) (Path.empty, false)
                        |> fst
                    [ path |> Element.createWithStyle style ]
                | StepLine mode ->
                    let strokePen = series.StrokeWidth |> Option.map (fun width -> seriesPen |> Pen.withWidth (Length.ofFloat (width * sf))) |> Option.defaultValue seriesPen
                    let style =
                        Style.createWithPen strokePen
                        |> Style.withFillOpacity 0.0
                        |> applyDash series.StrokeDash
                    let stepPoints = expandStepPoints mode series.Points
                    [ Polyline.ofList (stepPoints |> List.map toSvgPoint) |> Element.createWithStyle style ]
                | Area ->
                    let strokePen = series.StrokeWidth |> Option.map (fun width -> seriesPen |> Pen.withWidth (Length.ofFloat (width * sf))) |> Option.defaultValue seriesPen
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
                        | Some w -> baseStyle |> Style.withStrokePen (seriesPen |> Pen.withWidth (Length.ofFloat (w * sf)))
                        | None -> baseStyle
                    let topPts = List.map2 (fun x yHigh -> toSvgPoint (x, yHigh)) xs highs
                    let botPts = List.map2 (fun x yLow -> toSvgPoint (x, yLow)) xs yLows
                    [ Polygon.ofList (topPts @ List.rev botPts) |> Element.createWithStyle style ]
                | Histogram binWidth ->
                    let fillStyle = Style.createWithPen seriesPen |> Style.withFillPen seriesPen
                    series.Points
                    |> List.map (fun (binLeft, count) ->
                        let svgX1, svgY1 = toCoord (binLeft, count)
                        let svgX2, svgY2 = toCoord (binLeft + binWidth, 0.0)
                        Rect.create
                            (Point.ofFloats (svgX1, svgY1))
                            (Area.ofFloats (svgX2 - svgX1, svgY2 - svgY1))
                        |> Element.createWithStyle fillStyle)
                | SeriesKind.Box ->
                    match series.Points with
                    | [ (xPos, yMin); (_, q1); (_, median); (_, q3); (_, yMax) ] ->
                        let halfWidth = (series.PointRadius |> Option.defaultValue (canvasSize * 0.04)) * sf
                        let svgX = fst (toCoord (xPos, 0.0))
                        let svgY value = snd (toCoord (0.0, value))
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
                | Violin rawValues ->
                    match series.Points with
                    | [ (xPos, yMin); (_, q1); (_, median); (_, q3); (_, yMax) ] when not rawValues.IsEmpty ->
                        let halfWidth = (series.PointRadius |> Option.defaultValue (canvasSize * 0.04)) * sf
                        let bandwidth = silvermanBandwidth rawValues
                        let nSamples = 100
                        let extent = 2.0 * bandwidth
                        let yStep = (yMax - yMin + 2.0 * extent) / float (nSamples - 1)
                        let yStart = yMin - extent
                        let ySamples = [ for k in 0 .. nSamples - 1 -> yStart + float k * yStep ]
                        let densities = ySamples |> List.map (gaussianKde bandwidth rawValues)
                        let maxDensity = densities |> List.max |> max epsilon
                        let svgX = fst (toCoord (xPos, 0.0))
                        let svgY y = snd (toCoord (0.0, y))
                        let rightPts =
                            List.map2 (fun y d ->
                                Point.ofFloats (svgX + (d / maxDensity) * halfWidth, svgY y))
                                ySamples densities
                        let leftPts =
                            List.map2 (fun y d ->
                                Point.ofFloats (svgX - (d / maxDensity) * halfWidth, svgY y))
                                ySamples densities
                        let violinStyle =
                            Style.empty
                            |> Style.withFillPen seriesPen
                            |> Style.withFillOpacity (series.Opacity * 0.3)
                            |> Style.withStrokePen seriesPen
                        let boxHalf = halfWidth * 0.25
                        let svgYQ1 = svgY q1
                        let svgYQ3 = svgY q3
                        let svgYMedian = svgY median
                        let svgYMin = svgY yMin
                        let svgYMax = svgY yMax
                        let strokeStyle = Style.createWithPen seriesPen
                        let boxFillStyle =
                            Style.createWithPen seriesPen
                            |> Style.withFillPen seriesPen
                            |> Style.withFillOpacity (series.Opacity * 0.6)
                        [
                            Polygon.ofList (rightPts @ List.rev leftPts)
                            |> Element.createWithStyle violinStyle
                            Rect.create
                                (Point.ofFloats (svgX - boxHalf, svgYQ3))
                                (Area.ofFloats (boxHalf * 2.0, svgYQ1 - svgYQ3))
                            |> Element.createWithStyle boxFillStyle
                            Line.create (Point.ofFloats (svgX - boxHalf, svgYMedian)) (Point.ofFloats (svgX + boxHalf, svgYMedian))
                            |> Element.createWithStyle strokeStyle
                            Line.create (Point.ofFloats (svgX, svgYQ1)) (Point.ofFloats (svgX, svgYMin))
                            |> Element.createWithStyle strokeStyle
                            Line.create (Point.ofFloats (svgX, svgYQ3)) (Point.ofFloats (svgX, svgYMax))
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
                            let svgX1, svgY1 = toCoord (catX + xOffset - barWidth / 2.0, value)
                            let svgX2, svgY2 = toCoord (catX + xOffset + barWidth / 2.0, 0.0)
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
                            let svgX1, svgY1 = toCoord (0.0, catY + yOffset + barHeight / 2.0)
                            let svgX2, svgY2 = toCoord (value, catY + yOffset - barHeight / 2.0)
                            Rect.create
                                (Point.ofFloats (min svgX1 svgX2, min svgY1 svgY2))
                                (Area.ofFloats (abs (svgX2 - svgX1), abs (svgY2 - svgY1)))
                            |> Element.createWithStyle fillStyle)
                | SeriesKind.Lollipop ->
                    let stemWidth = (series.StrokeWidth |> Option.defaultValue (canvasSize * 0.0015)) * sf
                    let dotRadius = (series.PointRadius |> Option.defaultValue (canvasSize * 0.006)) * sf
                    let stemStyle = Style.createWithPen (seriesPen |> Pen.withWidth (Length.ofFloat stemWidth)) |> Style.withFillOpacity 0.0
                    let dotStyle = Style.empty |> Style.withFillPen seriesPen
                    let crossStyle = Style.createWithPen seriesPen |> Style.withFillOpacity 0.0
                    series.Points
                    |> List.collect (fun (x, y) ->
                        let svgX, svgY = toCoord (x, y)
                        let _, svgY0 = toCoord (x, 0.0)
                        let stem = Line.create (Point.ofFloats (svgX, svgY0)) (Point.ofFloats (svgX, svgY)) |> Element.createWithStyle stemStyle
                        let cap =
                            match series.PointShape with
                            | PointShape.Circle -> [ Circle.create (Point.ofFloats (svgX, svgY)) (Length.ofFloat dotRadius) |> Element.createWithStyle dotStyle ]
                            | Square -> [ Polygon.ofList (centerPolygon (svgX, svgY) dotRadius squareUnit |> List.map Point.ofFloats) |> Element.createWithStyle dotStyle ]
                            | Diamond -> [ Polygon.ofList (centerPolygon (svgX, svgY) dotRadius diamondUnit |> List.map Point.ofFloats) |> Element.createWithStyle dotStyle ]
                            | Triangle -> [ Polygon.ofList (centerPolygon (svgX, svgY) dotRadius triangleUnit |> List.map Point.ofFloats) |> Element.createWithStyle dotStyle ]
                            | Cross -> centerLines (svgX, svgY) dotRadius crossUnit |> List.map (fun (from', to') -> Line.create (Point.ofFloats from') (Point.ofFloats to') |> Element.createWithStyle crossStyle)
                        stem :: cap)
                | SeriesKind.HorizontalLollipop ->
                    let stemWidth = (series.StrokeWidth |> Option.defaultValue (canvasSize * 0.0015)) * sf
                    let dotRadius = (series.PointRadius |> Option.defaultValue (canvasSize * 0.006)) * sf
                    let stemStyle = Style.createWithPen (seriesPen |> Pen.withWidth (Length.ofFloat stemWidth)) |> Style.withFillOpacity 0.0
                    let dotStyle = Style.empty |> Style.withFillPen seriesPen
                    let crossStyle = Style.createWithPen seriesPen |> Style.withFillOpacity 0.0
                    series.Points
                    |> List.collect (fun (x, y) ->
                        let svgX, svgY = toCoord (x, y)
                        let svgX0, _ = toCoord (0.0, y)
                        let stem = Line.create (Point.ofFloats (svgX0, svgY)) (Point.ofFloats (svgX, svgY)) |> Element.createWithStyle stemStyle
                        let cap =
                            match series.PointShape with
                            | PointShape.Circle -> [ Circle.create (Point.ofFloats (svgX, svgY)) (Length.ofFloat dotRadius) |> Element.createWithStyle dotStyle ]
                            | Square -> [ Polygon.ofList (centerPolygon (svgX, svgY) dotRadius squareUnit |> List.map Point.ofFloats) |> Element.createWithStyle dotStyle ]
                            | Diamond -> [ Polygon.ofList (centerPolygon (svgX, svgY) dotRadius diamondUnit |> List.map Point.ofFloats) |> Element.createWithStyle dotStyle ]
                            | Triangle -> [ Polygon.ofList (centerPolygon (svgX, svgY) dotRadius triangleUnit |> List.map Point.ofFloats) |> Element.createWithStyle dotStyle ]
                            | Cross -> centerLines (svgX, svgY) dotRadius crossUnit |> List.map (fun (from', to') -> Line.create (Point.ofFloats from') (Point.ofFloats to') |> Element.createWithStyle crossStyle)
                        stem :: cap)
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
                            let svgX1, svgY1 = toCoord (col - cellWidth / 2.0, row + cellHeight / 2.0)
                            let svgX2, svgY2 = toCoord (col + cellWidth / 2.0, row - cellHeight / 2.0)
                            let color = colorScale value
                            Rect.create
                                (Point.ofFloats (min svgX1 svgX2, min svgY1 svgY2))
                                (Area.ofFloats (abs (svgX2 - svgX1) + sf, abs (svgY2 - svgY1) + sf))
                            |> Element.createWithStyle (Style.empty |> Style.withFill color |> Style.withFillOpacity series.Opacity))
                | Bubble sizes ->
                    // Radius is area-proportional: a point with twice the size value renders with twice the area.
                    // The largest bubble has radius maxRadiusPx (default 40 px, or set via withPointRadius).
                    let maxSize = sizes |> List.fold max 0.0
                    let maxRadiusPx = (series.PointRadius |> Option.defaultValue (canvasSize * 0.04)) * sf
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
                | Candlestick priceBars | StockBar priceBars ->
                    if List.isEmpty priceBars then []
                    else
                        let isStockBarStyle = match series.Kind with | StockBar _ -> true | _ -> false
                        let halfBar = priceBars |> List.map (fun b -> b.X) |> inferMinSpacing |> fun s -> s * 0.4
                        let wickWidth = (series.StrokeWidth |> Option.defaultValue (canvasSize * 0.0015)) * sf
                        priceBars
                        |> List.collect (fun bar ->
                            let svgCX, _ = toCoord (bar.X, 0.0)
                            let svgLeft, _ = toCoord (bar.X - halfBar, 0.0)
                            let svgRight, _ = toCoord (bar.X + halfBar, 0.0)
                            let svgOpen = snd (toCoord (0.0, bar.Open))
                            let svgClose = snd (toCoord (0.0, bar.Close))
                            let svgHigh = snd (toCoord (0.0, bar.High))
                            let svgLow = snd (toCoord (0.0, bar.Low))
                            let fillColor = if bar.Close >= bar.Open then graph.Theme.UpColor else graph.Theme.DownColor
                            let colorPen = Pen.create fillColor |> Pen.withOpacity series.Opacity
                            let wickStyle = Style.createWithPen (colorPen |> Pen.withWidth (Length.ofFloat wickWidth)) |> Style.withFillOpacity 0.0
                            if isStockBarStyle then
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
                                let bodyHeight = max sf (svgBodyBottom - svgBodyTop)
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
                | Waterfall totalXValues ->
                    if series.Points.IsEmpty then []
                    else
                        let totalSet = Set.ofList totalXValues
                        let barHalfWidth =
                            series.Points |> List.map fst |> inferMinSpacing |> fun s -> s * 0.4
                        let barData, _ =
                            series.Points
                            |> List.mapFold (fun running (x, delta) ->
                                let isTotal = Set.contains x totalSet
                                let baseline = if isTotal then 0.0 else running
                                let top = if isTotal then running else running + delta
                                let next = if isTotal then running else running + delta
                                (x, baseline, top, isTotal), next) 0.0
                        let upColor = graph.Theme.UpColor
                        let downColor = graph.Theme.DownColor
                        let neutralPen = graph.Theme.AxisPen
                        let barElements =
                            barData
                            |> List.collect (fun (x, baseline, top, isTotal) ->
                                let svgX1, _ = toCoord (x - barHalfWidth, 0.0)
                                let svgX2, _ = toCoord (x + barHalfWidth, 0.0)
                                let _, svgTop = toCoord (0.0, max baseline top)
                                let _, svgBottom = toCoord (0.0, min baseline top)
                                let barHeight = max sf (svgBottom - svgTop)
                                let color =
                                    if isTotal then neutralPen.Color
                                    elif top >= baseline then upColor
                                    else downColor
                                let rectStyle =
                                    Style.empty
                                    |> Style.withFill color
                                    |> Style.withFillOpacity series.Opacity
                                [ Rect.create
                                    (Point.ofFloats (min svgX1 svgX2, svgTop))
                                    (Area.ofFloats (abs (svgX2 - svgX1), barHeight))
                                  |> Element.createWithStyle rectStyle ])
                        let connectorStyle =
                            Style.createWithPen neutralPen
                            |> Style.withFillOpacity 0.0
                            |> Style.withStrokeDashArray [ 6.0; 4.0 ]
                        let connectors =
                            barData
                            |> List.pairwise
                            |> List.collect (fun ((x1, _, top1, _), (x2, _, _, _)) ->
                                let _, svgY = toCoord (0.0, top1)
                                let svgRight, _ = toCoord (x1 + barHalfWidth, 0.0)
                                let svgLeft, _ = toCoord (x2 - barHalfWidth, 0.0)
                                [ Line.create (Point.ofFloats (svgRight, svgY)) (Point.ofFloats (svgLeft, svgY))
                                  |> Element.createWithStyle connectorStyle ])
                        barElements @ connectors
                | StackedArea | NormalizedStackedArea | Streamgraph ->
                    match Map.tryFind i stackingMap with
                    | None -> []
                    | Some (xValues, baselines, tops) ->
                        let strokePen = series.StrokeWidth |> Option.map (fun w -> seriesPen |> Pen.withWidth (Length.ofFloat (w * sf))) |> Option.defaultValue seriesPen
                        let style =
                            Style.createWithPen strokePen
                            |> Style.withFillPen strokePen
                            |> applyDash series.StrokeDash
                        let toSvgXY x y = toCoord (x, y) |> Point.ofFloats
                        let topPts = List.map2 toSvgXY xValues tops
                        let basePts = List.map2 toSvgXY xValues baselines
                        [ Polygon.ofList (topPts @ List.rev basePts) |> Element.createWithStyle style ]
                | ParallelSets(dimensions, flows) ->
                    let numDims = dimensions.Length
                    if numDims < 2 || flows.IsEmpty then []
                    else
                        let nodeWidth = canvasSize * 0.012 * sf
                        let nodeGap = canvasSize * 0.006 * sf
                        let labelGap = canvasSize * 0.006 * sf
                        let labelFontSize = canvasSize * 0.011 * sf
                        let titleFontSize = canvasSize * 0.012 * sf

                        let dimX d = cs * float d / float (numDims - 1)

                        let dimCategories =
                            [| for d in 0 .. numDims - 1 ->
                                flows
                                |> List.choose (fun f -> if d < f.Path.Length then Some f.Path.[d] else None)
                                |> List.distinct |]

                        let dimCatWeight d cat =
                            flows |> List.sumBy (fun f ->
                                if d < f.Path.Length && f.Path.[d] = cat then f.Weight else 0.0)

                        let dimNodePositions =
                            [| for d in 0 .. numDims - 1 ->
                                let cats = dimCategories.[d]
                                let total = cats |> List.sumBy (dimCatWeight d)
                                let available = cs - float (max 0 (cats.Length - 1)) * nodeGap
                                cats
                                |> List.mapFold (fun y cat ->
                                    let h = if total > 0.0 then dimCatWeight d cat / total * available else 0.0
                                    (cat, y, y + h), y + h + nodeGap) 0.0
                                |> fst |]

                        let nodeTop d cat =
                            dimNodePositions.[d]
                            |> List.tryFind (fun (c, _, _) -> c = cat)
                            |> Option.map (fun (_, t, _) -> t)
                            |> Option.defaultValue 0.0

                        let nodeHeight d cat =
                            dimNodePositions.[d]
                            |> List.tryFind (fun (c, _, _) -> c = cat)
                            |> Option.map (fun (_, t, b) -> b - t)
                            |> Option.defaultValue 0.0

                        let sourceCategories = dimCategories.[0]
                        let colorForFlow (flow : ParallelFlow) =
                            let sourceCat = if not flow.Path.IsEmpty then flow.Path.[0] else ""
                            let idx = sourceCategories |> List.tryFindIndex (fun c -> c = sourceCat) |> Option.defaultValue 0
                            (Theme.penForSeries idx graph.Theme).Color

                        let textStyle = Style.empty |> Style.withFillPen graph.Theme.AxisPen

                        let nodeElements =
                            [ for d in 0 .. numDims - 1 do
                                let x = dimX d
                                for (cat, yTop, yBottom) in dimNodePositions.[d] do
                                    let h = yBottom - yTop
                                    if h > 0.0 then
                                        let nodeStyle =
                                            Style.empty
                                            |> Style.withFill graph.Theme.AxisPen.Color
                                            |> Style.withFillOpacity 0.85
                                        yield Rect.create
                                            (Point.ofFloats (x - nodeWidth / 2.0, yTop))
                                            (Area.ofFloats (nodeWidth, h))
                                          |> Element.createWithStyle nodeStyle
                                        let labelX, anchor =
                                            if d = 0 then x - nodeWidth / 2.0 - labelGap, End
                                            else x + nodeWidth / 2.0 + labelGap, Start
                                        yield Text.create (Point.ofFloats (labelX, yTop + h / 2.0)) cat
                                            |> Text.withFontSize labelFontSize
                                            |> Text.withBaseline CentralBaseline
                                            |> Text.withAnchor anchor
                                            |> Element.createWithStyle textStyle ]

                        let titleElements =
                            [ for d in 0 .. numDims - 1 do
                                let x = dimX d
                                yield Text.create (Point.ofFloats (x, -titleFontSize - canvasSize * 0.004 * sf)) dimensions.[d]
                                    |> Text.withFontSize titleFontSize
                                    |> Text.withAnchor Middle
                                    |> Text.withBaseline HangingBaseline
                                    |> Element.createWithStyle textStyle ]

                        let ribbonElements =
                            [ for d in 0 .. numDims - 2 do
                                let xLeft = dimX d + nodeWidth / 2.0
                                let xRight = dimX (d + 1) - nodeWidth / 2.0
                                let midX = (xLeft + xRight) / 2.0

                                let initLeftCursors =
                                    dimCategories.[d] |> List.map (fun cat -> cat, nodeTop d cat) |> Map.ofList
                                let initRightCursors =
                                    dimCategories.[d + 1] |> List.map (fun cat -> cat, nodeTop (d + 1) cat) |> Map.ofList

                                let sortedFlows =
                                    flows
                                    |> List.filter (fun f -> d + 1 < f.Path.Length)
                                    |> List.sortBy (fun f ->
                                        let li = dimCategories.[d] |> List.tryFindIndex (fun c -> c = f.Path.[d]) |> Option.defaultValue 0
                                        let ri = dimCategories.[d + 1] |> List.tryFindIndex (fun c -> c = f.Path.[d + 1]) |> Option.defaultValue 0
                                        li, ri)

                                let ribbons, _ =
                                    sortedFlows
                                    |> List.mapFold (fun (leftCursors : Map<string, float>, rightCursors : Map<string, float>) flow ->
                                        let leftCat = flow.Path.[d]
                                        let rightCat = flow.Path.[d + 1]
                                        let leftH = nodeHeight d leftCat
                                        let rightH = nodeHeight (d + 1) rightCat
                                        let leftTotal = dimCatWeight d leftCat
                                        let rightTotal = dimCatWeight (d + 1) rightCat
                                        if leftTotal <= 0.0 || rightTotal <= 0.0 || leftH <= 0.0 || rightH <= 0.0 then
                                            None, (leftCursors, rightCursors)
                                        else
                                            let leftRibbonH = flow.Weight / leftTotal * leftH
                                            let rightRibbonH = flow.Weight / rightTotal * rightH
                                            let curL = Map.tryFind leftCat leftCursors |> Option.defaultValue (nodeTop d leftCat)
                                            let curR = Map.tryFind rightCat rightCursors |> Option.defaultValue (nodeTop (d + 1) rightCat)
                                            let newLeftCursors = Map.add leftCat (curL + leftRibbonH) leftCursors
                                            let newRightCursors = Map.add rightCat (curR + rightRibbonH) rightCursors
                                            Some (curL, curL + leftRibbonH, curR, curR + rightRibbonH, flow),
                                            (newLeftCursors, newRightCursors)
                                    ) (initLeftCursors, initRightCursors)

                                for ribbonOpt in ribbons do
                                    match ribbonOpt with
                                    | None -> ()
                                    | Some (yLT, yLB, yRT, yRB, flow) ->
                                        let color = colorForFlow flow
                                        let fillStyle =
                                            Style.empty
                                            |> Style.withFill color
                                            |> Style.withFillOpacity (series.Opacity * 0.5)
                                        let path =
                                            Path.empty
                                            |> Path.addMoveTo Absolute (Point.ofFloats (xLeft, yLT))
                                            |> Path.addCubicBezierCurveTo Absolute
                                                (Point.ofFloats (midX, yLT))
                                                (Point.ofFloats (midX, yRT))
                                                (Point.ofFloats (xRight, yRT))
                                            |> Path.addLineTo Absolute (Point.ofFloats (xRight, yRB))
                                            |> Path.addCubicBezierCurveTo Absolute
                                                (Point.ofFloats (midX, yRB))
                                                (Point.ofFloats (midX, yLB))
                                                (Point.ofFloats (xLeft, yLB))
                                            |> Path.addClosePath
                                        yield path |> Element.createWithStyle fillStyle ]

                        titleElements @ ribbonElements @ nodeElements
                | Funnel labels ->
                    if series.Points.IsEmpty then []
                    else
                        let values = series.Points |> List.map snd
                        let maxValue = values |> List.max |> max epsilon
                        let n = values.Length
                        let maxWidthFraction = 0.82
                        let gap = cs * 0.008
                        let topPad = cs * 0.04
                        let totalHeight = cs - 2.0 * topPad
                        let stageHeight = (totalHeight - float (n - 1) * gap) / float n
                        let cx = cs / 2.0
                        let labelFontSize = canvasSize * 0.028 * sf
                        let valueFontSize = canvasSize * 0.018 * sf
                        let textStyle = Style.empty |> Style.withFill (Color.ofName White)
                        values
                        |> List.mapi (fun i value ->
                            let topWidth = (value / maxValue) * cs * maxWidthFraction
                            let bottomValue = values |> List.tryItem (i + 1) |> Option.defaultValue value
                            let bottomWidth = (bottomValue / maxValue) * cs * maxWidthFraction
                            let yTop = topPad + float i * (stageHeight + gap)
                            let yBottom = yTop + stageHeight
                            let yMid = (yTop + yBottom) / 2.0
                            let color = (Theme.penForSeries i graph.Theme).Color
                            let fillStyle =
                                Style.empty
                                |> Style.withFill color
                                |> Style.withFillOpacity series.Opacity
                            let trapezoid =
                                Polygon.ofList [
                                    Point.ofFloats (cx - topWidth / 2.0, yTop)
                                    Point.ofFloats (cx + topWidth / 2.0, yTop)
                                    Point.ofFloats (cx + bottomWidth / 2.0, yBottom)
                                    Point.ofFloats (cx - bottomWidth / 2.0, yBottom)
                                ]
                                |> Element.createWithStyle fillStyle
                            let stageName = labels |> List.tryItem i |> Option.defaultValue ""
                            let nameEl =
                                Text.create (Point.ofFloats (cx, yMid - valueFontSize * 0.5)) stageName
                                |> Text.withFontSize labelFontSize
                                |> Text.withAnchor Middle
                                |> Text.withBaseline CentralBaseline
                                |> Element.createWithStyle textStyle
                            let valueEl =
                                Text.create (Point.ofFloats (cx, yMid + labelFontSize * 0.75)) (sprintf "%g" value)
                                |> Text.withFontSize valueFontSize
                                |> Text.withAnchor Middle
                                |> Text.withBaseline CentralBaseline
                                |> Element.createWithStyle textStyle
                            [ trapezoid; nameEl; valueEl ])
                        |> List.concat
                | Treemap labels ->
                    if series.Points.IsEmpty then []
                    else
                        let values = series.Points |> List.map snd
                        let padding = cs * 0.002
                        let rects = CommonMath.squarifiedTreemap 0.0 0.0 cs cs values
                        rects
                        |> List.collect (fun rect ->
                            let label = labels |> List.tryItem rect.Index |> Option.defaultValue ""
                            let value = values |> List.tryItem rect.Index |> Option.defaultValue 0.0
                            let color = (Theme.penForSeries rect.Index graph.Theme).Color
                            let rectStyle =
                                Style.empty
                                |> Style.withFill color
                                |> Style.withFillOpacity series.Opacity
                                |> Style.withStroke (graph.Theme.Background)
                                |> Style.withStrokeWidth (Length.ofFloat (cs * 0.003))
                            let rx = rect.X + padding
                            let ry = rect.Y + padding
                            let rw = max 0.0 (rect.W - 2.0 * padding)
                            let rh = max 0.0 (rect.H - 2.0 * padding)
                            let rectEl =
                                Rect.create (Point.ofFloats (rx, ry)) (Area.ofFloats (rw, rh))
                                |> Element.createWithStyle rectStyle
                            let minDim = min rw rh
                            if minDim < cs * 0.04 then [ rectEl ]
                            else
                                let fontSize = clamp (cs * 0.012) (cs * 0.028) (minDim * 0.18) * sf
                                let cx = rx + rw / 2.0
                                let cy = ry + rh / 2.0
                                let textColor = Color.ofName White
                                let textStyle = Style.empty |> Style.withFill textColor
                                let pct = if List.sum values > 0.0 then value / List.sum values * 100.0 else 0.0
                                let labelEl =
                                    Text.create (Point.ofFloats (cx, cy - fontSize * 0.4)) label
                                    |> Text.withFontSize fontSize
                                    |> Text.withAnchor Middle
                                    |> Text.withBaseline CentralBaseline
                                    |> Element.createWithStyle textStyle
                                let valueEl =
                                    Text.create (Point.ofFloats (cx, cy + fontSize * 0.9)) (sprintf "%.1f%%" pct)
                                    |> Text.withFontSize (fontSize * 0.75)
                                    |> Text.withAnchor Middle
                                    |> Text.withBaseline CentralBaseline
                                    |> Element.createWithStyle textStyle
                                [ rectEl; labelEl; valueEl ])
                | Bullet bullets ->
                    if bullets.IsEmpty then []
                    else
                        let n = bullets.Length
                        let labelAreaW = cs * 0.22
                        let barAreaX = labelAreaW + cs * 0.02
                        let barAreaW = cs - barAreaX - cs * 0.02
                        let rowH = cs / float n
                        let barH = rowH * 0.28
                        let actualH = rowH * 0.16
                        let tickH = rowH * 0.44
                        let maxThreshold =
                            bullets
                            |> List.collect (fun b -> b.Ranges |> List.map (fun r -> r.Threshold))
                            |> List.append (bullets |> List.map (fun b -> b.Actual))
                            |> List.append (bullets |> List.map (fun b -> b.Target))
                            |> List.max
                            |> max epsilon
                        let toX v = barAreaX + v / maxThreshold * barAreaW
                        let seriesPen = Theme.penForSeries i graph.Theme
                        let labelFontSize = rowH * 0.22 * sf
                        bullets
                        |> List.mapi (fun i b ->
                            let cy = (float i + 0.5) * rowH
                            let rangeCount = b.Ranges.Length
                            let rangeElements =
                                b.Ranges
                                |> List.mapi (fun ri range ->
                                    let opacity = 0.2 + 0.55 * float (ri + 1) / float (max 1 rangeCount)
                                    let prevThreshold = if ri = 0 then 0.0 else b.Ranges.[ri - 1].Threshold
                                    let x0 = toX prevThreshold
                                    let x1 = toX range.Threshold
                                    let bandStyle =
                                        Style.empty
                                        |> Style.withFill seriesPen.Color
                                        |> Style.withFillOpacity opacity
                                    Rect.create
                                        (Point.ofFloats (x0, cy - barH / 2.0))
                                        (Area.ofFloats (x1 - x0, barH))
                                    |> Element.createWithStyle bandStyle)
                            let actualStyle =
                                Style.empty
                                |> Style.withFill seriesPen.Color
                                |> Style.withFillOpacity series.Opacity
                            let actualEl =
                                Rect.create
                                    (Point.ofFloats (barAreaX, cy - actualH / 2.0))
                                    (Area.ofFloats (max 0.0 (toX b.Actual - barAreaX), actualH))
                                |> Element.createWithStyle actualStyle
                            let targetX = toX b.Target
                            let tickStyle =
                                Style.empty
                                |> Style.withFill seriesPen.Color
                                |> Style.withFillOpacity series.Opacity
                            let tickW = cs * 0.008
                            let tickEl =
                                Rect.create
                                    (Point.ofFloats (targetX - tickW / 2.0, cy - tickH / 2.0))
                                    (Area.ofFloats (tickW, tickH))
                                |> Element.createWithStyle tickStyle
                            let labelElements =
                                match b.Label with
                                | None -> []
                                | Some lbl ->
                                    let labelStyle =
                                        Style.empty
                                        |> Style.withFill graph.Theme.AxisPen.Color
                                    [ Text.create (Point.ofFloats (labelAreaW, cy)) lbl
                                      |> Text.withFontSize (labelFontSize)
                                      |> Text.withAnchor End
                                      |> Text.withBaseline CentralBaseline
                                      |> Element.createWithStyle labelStyle ]
                            labelElements @ rangeElements @ [ actualEl; tickEl ])
                        |> List.concat
                | Pie sliceLabels ->
                    if series.Points.IsEmpty then []
                    else
                        let cx = cs / 2.0
                        let cy = cs / 2.0
                        let outerRadius = cs * 0.42
                        let values = series.Points |> List.map snd
                        let total = List.sum values
                        if total <= 0.0 then []
                        else
                            let tau = 2.0 * System.Math.PI
                            let startOffset = -System.Math.PI / 2.0
                            let cumulative = values |> List.scan (+) 0.0
                            values
                            |> List.mapi (fun sliceIdx v ->
                                let startAngle = startOffset + cumulative.[sliceIdx] / total * tau
                                let endAngle = startOffset + cumulative.[sliceIdx + 1] / total * tau
                                let midAngle = (startAngle + endAngle) / 2.0
                                let color = (Theme.penForSeries sliceIdx graph.Theme).Color
                                let fillStyle =
                                    Style.empty
                                    |> Style.withFill color
                                    |> Style.withFillOpacity series.Opacity
                                let sliceEl =
                                    if endAngle - startAngle >= tau - 0.0001 then
                                        // Full circle: arc degenerates, use a circle element
                                        Circle.create (Point.ofFloats (cx, cy)) (Length.ofFloat outerRadius)
                                        |> Element.createWithStyle fillStyle
                                    else
                                        let sx = cx + outerRadius * cos startAngle
                                        let sy = cy + outerRadius * sin startAngle
                                        let ex = cx + outerRadius * cos endAngle
                                        let ey = cy + outerRadius * sin endAngle
                                        let largeArc = endAngle - startAngle > System.Math.PI
                                        Path.empty
                                        |> Path.addMoveTo Absolute (Point.ofFloats (cx, cy))
                                        |> Path.addLineTo Absolute (Point.ofFloats (sx, sy))
                                        |> Path.addEllipticalArcCurveTo Absolute
                                            (Point.ofFloats (outerRadius, outerRadius)) 0.0
                                            largeArc true (Point.ofFloats (ex, ey))
                                        |> Path.addClosePath
                                        |> Element.createWithStyle fillStyle
                                let labelEls =
                                    match sliceLabels |> List.tryItem sliceIdx |> Option.flatten with
                                    | None -> []
                                    | Some label ->
                                        let labelRadius = outerRadius * 0.65
                                        let lx = cx + labelRadius * cos midAngle
                                        let ly = cy + labelRadius * sin midAngle
                                        let labelStyle = Style.empty |> Style.withFill (Color.ofName White)
                                        [ Text.create (Point.ofFloats (lx, ly)) label
                                          |> Text.withFontSize (canvasSize * 0.011 * sf)
                                          |> Text.withAnchor Middle
                                          |> Text.withBaseline CentralBaseline
                                          |> Element.createWithStyle labelStyle ]
                                sliceEl :: labelEls)
                            |> List.concat
            else []
        let cs = snd (pixelRangeOf graph.XScale)
        let errorElements =
            graph.Series
            |> List.mapi (fun i series ->
                if series.Visible then
                    match series.ErrorBars with
                    | None -> []
                    | Some errorBar ->
                        let seriesPen = Theme.penForSeries i graph.Theme |> Pen.withOpacity series.Opacity
                        let yScale =
                            match series.YAxis with
                            | YLeft -> graph.YScale
                            | YRight -> graph.YScaleRight |> Option.defaultValue graph.YScale
                        let toCoord (x, y) = Scale.apply graph.XScale x, Scale.apply yScale y
                        errorBarElements seriesPen series.Points errorBar toCoord cs
                else [])
            |> List.concat
        (graph.Series |> List.mapi seriesToElements |> List.concat) @ errorElements
