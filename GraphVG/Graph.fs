namespace GraphVG

open SharpVG

type Graph = {
    Series: Series list
    Domain: float * float
    Range: float * float
}

module Graph =

    let canvasSize = 1000.0

    let getDomainRange series =
        let xValues, yValues = series |> List.unzip
        let domain = List.reduce min xValues, List.reduce max xValues
        let range = List.reduce min yValues, List.reduce max yValues
        domain, range

    let getDomainRangeSize (domain, range) =
        let left, right = domain
        let bottom, top = range
        (right - left, top - bottom)

    let toScaledSvgCoordinates graph (x, y) =
        let width, height = getDomainRangeSize (graph.Domain, graph.Range)
        let scaleHorizontally w = canvasSize * w / width
        let scaleVertically h = canvasSize * h / height
        let left= scaleHorizontally (fst graph.Domain)
        let top = scaleVertically (snd graph.Range)

        let outputX = (scaleHorizontally x) - left
        let outputY = -((scaleVertically y) - top)

        outputX, outputY

    let create series domain range padPercent =
        { Series = series; Domain = domain; Range = range }

    let addPadding padPercent graph =
        let domainMin, domainMax = graph.Domain
        let rangeMin, rangeMax = graph.Range
        let pad = 1.0 + padPercent
        { graph with Domain = (domainMin * pad, domainMax * pad); Range = (rangeMin * pad, rangeMax * pad) }

    let withPadding padPercent graph =
        let domain, range = getDomainRange (List.concat graph.Series)
        { graph with Domain = domain } |> addPadding padPercent

    let createWithSeries series =
        let domain, range = getDomainRange series
        { Series = [ series ]; Domain = domain; Range = range; }
            |> addPadding 0.1

    let addSeries series graph=
        { graph with Series = graph.Series @ [ series ] }

    let drawPoints series =
        series
//            |> List.map (fun point ->  point |> (toScaledSvgCoordinates graph) |> Point.ofFloats)
//            |> List.map (fun point -> Circle.create point (Length.ofInt 3) |> Element.createWithStyle style)

    let drawSeries graph =
        let style = Style.create (Color.ofName Colors.Black) (Color.ofName Colors.Black) (Length.ofInt 3) 1.0 1.0
        let seriesToDots series =
            series
                |> List.map (fun point ->  point |> (toScaledSvgCoordinates graph) |> Point.ofFloats)
                |> List.map (fun point -> Circle.create point (Length.ofInt 3) |> Element.createWithStyle style)

        // TODO: Use named style for points
        graph.Series |> List.map seriesToDots |> List.concat

