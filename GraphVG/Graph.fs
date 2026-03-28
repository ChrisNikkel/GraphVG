namespace GraphVG

open SharpVG

type Graph = {
    Series : Series list
    XScale : Scale
    YScale : Scale
    XAxis  : Axis option
    YAxis  : Axis option
    Theme  : Theme
    Title  : string option
}

module Graph =

    // ── Internal helpers ────────────────────────────────────────────────────────

    let private clamp lo hi v = max lo (min hi v)

    let private pixelRangeOf (scale : Scale) =
        match scale with
        | Scale.Linear(_, r) -> r
        | Scale.Log(_, r, _) -> r

    let private withScaleDomain newDomain (scale : Scale) : Scale =
        match scale with
        | Scale.Linear(_, r) -> Scale.Linear(newDomain, r)
        | Scale.Log(_, r, b) -> Scale.Log(newDomain, r, b)

    let private defaultAxes xScale yScale =
        let xAxisY = Scale.apply yScale 0.0 |> clamp 0.0 Canvas.canvasSize
        let yAxisX = Scale.apply xScale 0.0 |> clamp 0.0 Canvas.canvasSize
        Some (Axis.create (HorizontalAt xAxisY) xScale),
        Some (Axis.create (VerticalAt   yAxisX) yScale)

    let private pointBounds (series : Series list) =
        let allPoints = series |> List.collect (fun s -> s.Points)
        let xs, ys    = allPoints |> List.unzip
        (List.reduce min xs, List.reduce max xs),
        (List.reduce min ys, List.reduce max ys)

    let private buildScales domain range =
        Scale.linear domain (0.0, Canvas.canvasSize),
        Scale.linear range  (Canvas.canvasSize, 0.0)

    // ── Coordinate transform ────────────────────────────────────────────────────

    let toScaledSvgCoordinates graph (x, y) =
        Scale.apply graph.XScale x, Scale.apply graph.YScale y

    // ── Constructors ────────────────────────────────────────────────────────────

    let create (series : Series list) domain range =
        let xScale, yScale = buildScales domain range
        let xAxis, yAxis   = defaultAxes xScale yScale
        { Series = series
          XScale = xScale
          YScale = yScale
          XAxis  = xAxis
          YAxis  = yAxis
          Theme  = Theme.empty
          Title  = None }

    let createWithSeries (series : Series) =
        let domain, range = pointBounds [ series ]
        let span (lo, hi) = hi - lo
        let pad (lo, hi) p = lo - (span (lo, hi)) * p, hi + (span (lo, hi)) * p
        let xScale, yScale = buildScales (pad domain 0.1) (pad range 0.1)
        let xAxis, yAxis   = defaultAxes xScale yScale
        { Series = [ series ]
          XScale = xScale
          YScale = yScale
          XAxis  = xAxis
          YAxis  = yAxis
          Theme  = Theme.empty
          Title  = None }

    // ── Bounds helpers ──────────────────────────────────────────────────────────

    let addPadding padPercent graph =
        let domainMin, domainMax = Scale.domain graph.XScale
        let rangeMin,  rangeMax  = Scale.domain graph.YScale
        let dp = (domainMax - domainMin) * padPercent
        let rp = (rangeMax  - rangeMin)  * padPercent
        { graph with
            XScale = withScaleDomain (domainMin - dp, domainMax + dp) graph.XScale
            YScale = withScaleDomain (rangeMin  - rp, rangeMax  + rp) graph.YScale }

    let private recalcBounds padPercent graph =
        let domain, range = pointBounds graph.Series
        let span (lo, hi) = hi - lo
        let pad (lo, hi) p = lo - (span (lo, hi)) * p, hi + (span (lo, hi)) * p
        let newXScale = Scale.linear (pad domain padPercent) (pixelRangeOf graph.XScale)
        let newYScale = Scale.linear (pad range  padPercent) (pixelRangeOf graph.YScale)
        let xAxis, yAxis = defaultAxes newXScale newYScale
        { graph with XScale = newXScale; YScale = newYScale; XAxis = xAxis; YAxis = yAxis }

    // ── Builders ────────────────────────────────────────────────────────────────

    let addSeries series graph =
        { graph with Series = graph.Series @ [ series ] } |> recalcBounds 0.1

    let withXScale xScale graph = { graph with XScale = xScale }
    let withYScale yScale graph = { graph with YScale = yScale }

    let withXAxis xAxis graph = { graph with XAxis = xAxis }
    let withYAxis yAxis graph = { graph with YAxis = yAxis }

    let withAxes (xAxis, yAxis) graph = { graph with XAxis = xAxis; YAxis = yAxis }

    let withTheme theme graph = { graph with Theme = theme }
    let withTitle title (graph : Graph) = { graph with Title = Some title }

    // ── Rendering ───────────────────────────────────────────────────────────────

    let drawSeries graph =
        let toSvgPoint pt = pt |> toScaledSvgCoordinates graph |> Point.ofFloats
        let seriesToElements i (series : Series) =
            let pen = Theme.penForSeries i graph.Theme
            match series.Kind with
            | Scatter ->
                let style = Style.empty |> Style.withFillPen pen
                series.Points
                |> List.map (fun pt -> Circle.create (toSvgPoint pt) (Length.ofInt 3) |> Element.createWithStyle style)
            | SeriesKind.Line ->
                let style = Style.createWithPen pen |> Style.withFillOpacity 0.0
                [ Polyline.ofList (series.Points |> List.map toSvgPoint) |> Element.createWithStyle style ]
            | Area ->
                let style = Style.createWithPen pen |> Style.withFillPen pen
                [ Polygon.ofList (series.Points |> List.map toSvgPoint) |> Element.createWithStyle style ]
        graph.Series |> List.mapi seriesToElements |> List.concat
