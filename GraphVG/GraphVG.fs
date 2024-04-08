namespace GraphVG

open SharpVG

module GraphVG =
    let canvasSize = 1000.0

    let drawSeries graph =
        let viewBoxArea = Area.ofFloats (canvasSize, canvasSize)
        let viewBox = ViewBox.create Point.origin viewBoxArea

        let graphElements = Graph.drawSeries graph

        let svg = (Axis.draw graph) |> (List.append graphElements) |> Svg.ofList |> Svg.withViewBox viewBox
        let html = svg |> Svg.toHtml "Test"
        html