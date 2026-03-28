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
        let pad = 1.0 + padPercent
        { graph with Domain = (domainMin * pad, domainMax * pad); Range = (rangeMin * pad, rangeMax * pad) }

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

    let drawSeries graph =
        let style = Style.create (Color.ofName Colors.Black) (Color.ofName Colors.Black) (Length.ofInt 3) 1.0 1.0
        let seriesToElements (series : Series) =
            series.Points
                |> List.map (fun point -> point |> toScaledSvgCoordinates graph |> Point.ofFloats)
                |> List.map (fun point -> Circle.create point (Length.ofInt 3) |> Element.createWithStyle style)
        graph.Series |> List.collect seriesToElements
