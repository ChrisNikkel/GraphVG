namespace GraphVG

open SharpVG

module GraphVG =

    let private clamp lo hi v = max lo (min hi v)

    let private margin = 20.0

    let render (graph : Graph) (theme : Theme) (axes : Axis list) =
        let viewBox = ViewBox.create (Point.ofFloats (-margin, -margin)) (Area.ofFloats (Graph.canvasSize + 2.0 * margin, Graph.canvasSize + 2.0 * margin))

        let elements =
            Graph.drawSeries theme graph
            @ (axes |> List.collect (Axis.toElements theme))

        elements
        |> Svg.ofList
        |> Svg.withViewBox viewBox
        |> Svg.withSize (Area.ofInts (800, 800))
        |> Svg.toHtml "GraphVG"

    let drawSeries graph =
        let xScale = Scale.linear graph.Domain (0.0, Graph.canvasSize)
        let yScale = Scale.linear graph.Range  (Graph.canvasSize, 0.0)

        let xAxisY = Scale.apply yScale 0.0 |> clamp 0.0 Graph.canvasSize
        let yAxisX = Scale.apply xScale 0.0 |> clamp 0.0 Graph.canvasSize

        let xAxis = Axis.create (HorizontalAt xAxisY) xScale
        let yAxis = Axis.create (VerticalAt   yAxisX) yScale

        render graph Theme.empty [ xAxis; yAxis ]
