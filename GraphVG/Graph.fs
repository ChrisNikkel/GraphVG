namespace GraphVG

open SharpVG

type Graph = {
    Series: Series list
    Domain: float * float
    Range: float * float
}

module Graph =

    let canvasSize = 1000.0

    let private getDomainRange points =
        let xValues, yValues = points |> List.unzip
        let domain = List.reduce min xValues, List.reduce max xValues
        let range = List.reduce min yValues, List.reduce max yValues
        domain, range

    let toScaledSvgCoordinates graph (x, y) =
        let xScale = Scale.linear graph.Domain (0.0, canvasSize)
        let yScale = Scale.linear graph.Range (canvasSize, 0.0)
        Scale.apply xScale x, Scale.apply yScale y

    let create series domain range =
        { Series = series; Domain = domain; Range = range }

    let addPadding padPercent graph =
        let domainMin, domainMax = graph.Domain
        let rangeMin, rangeMax = graph.Range
        let domainPad = (domainMax - domainMin) * padPercent
        let rangePad  = (rangeMax - rangeMin)  * padPercent
        { graph with
            Domain = domainMin - domainPad, domainMax + domainPad
            Range  = rangeMin  - rangePad,  rangeMax  + rangePad }

    let withPadding padPercent graph =
        let allPoints = graph.Series |> List.collect (fun s -> s.Points)
        let domain, range = getDomainRange allPoints
        { graph with Domain = domain; Range = range } |> addPadding padPercent

    let createWithSeries (series : Series) =
        let domain, range = getDomainRange series.Points
        { Series = [ series ]; Domain = domain; Range = range }
            |> addPadding 0.1

    let addSeries series graph =
        { graph with Series = graph.Series @ [ series ] } |> withPadding 0.1

    let drawSeries theme graph =
        let toSvgPoint pt = pt |> toScaledSvgCoordinates graph |> Point.ofFloats
        let seriesToElements i (series : Series) =
            let pen = Theme.penForSeries i theme
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
