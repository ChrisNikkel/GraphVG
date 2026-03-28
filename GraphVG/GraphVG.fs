namespace GraphVG

open SharpVG

module GraphVG =

    let drawSeries graph =
        let viewBox = ViewBox.create Point.origin (Area.ofFloats (Graph.canvasSize, Graph.canvasSize))

        let xAxis = Axis.create Bottom (Scale.linear graph.Domain (0.0, Graph.canvasSize))
        let yAxis = Axis.create Left   (Scale.linear graph.Range  (Graph.canvasSize, 0.0))

        let elements =
            Graph.drawSeries graph
            @ Axis.toElements Theme.empty xAxis
            @ Axis.toElements Theme.empty yAxis

        elements |> Svg.ofList |> Svg.withViewBox viewBox |> Svg.toHtml "Test"
