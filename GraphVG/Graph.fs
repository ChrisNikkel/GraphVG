namespace GraphVG

open SharpVG
open CommonMath

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
        let allPoints = series |> List.collect (fun s -> s.Points)
        let xs, ys = allPoints |> List.unzip
        (List.reduce min xs, List.reduce max xs),
        (List.reduce min ys, List.reduce max ys)

    let private buildScales domain range =
        Scale.linear domain (0.0, canvasSize),
        Scale.linear range (canvasSize, 0.0)

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
        }

    let createWithSeries (series : Series) =
        let domain, range = pointBounds [ series ]
        let xScale, yScale = buildScales (padRange 0.1 domain) (padRange 0.1 range)
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

    let private recalcBounds padPercent graph =
        let domain, range = pointBounds graph.Series
        let newXScale = Scale.linear (padRange padPercent domain) (pixelRangeOf graph.XScale)
        let newYScale = Scale.linear (padRange padPercent range) (pixelRangeOf graph.YScale)
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
        { graph with Series = graph.Series @ [ series ] } |> recalcBounds 0.1

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
    let withTitle title (graph : Graph) = { graph with Title = Some title }
    let withTitleStyle style (graph : Graph) = { graph with TitleStyle = style }
    let addAnnotation annotation (graph : Graph) = { graph with Annotations = graph.Annotations @ [ annotation ] }
    let withLegend legend (graph : Graph) = { graph with Legend = Some legend }

    // ── Rendering ───────────────────────────────────────────────────────────────

    let private applyDash dash style =
        match dash with
        | Solid -> style
        | Dashed -> style |> Style.withStrokeDashArray [ 12.0; 6.0 ]
        | Dotted -> style |> Style.withStrokeDashArray [ 3.0; 6.0 ]
        | DashDot -> style |> Style.withStrokeDashArray [ 12.0; 6.0; 3.0; 6.0 ]

    let drawSeries graph =
        let toSvgPoint point = point |> toScaledSvgCoordinates graph |> Point.ofFloats
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
                | Area ->
                    let strokePen = series.StrokeWidth |> Option.map (fun width -> seriesPen |> Pen.withWidth width) |> Option.defaultValue seriesPen
                    let style =
                        Style.createWithPen strokePen
                        |> Style.withFillPen strokePen
                        |> applyDash series.StrokeDash
                    [ Polygon.ofList (series.Points |> List.map toSvgPoint) |> Element.createWithStyle style ]
            else []
        graph.Series |> List.mapi seriesToElements |> List.concat
