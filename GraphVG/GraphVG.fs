namespace GraphVG

open SharpVG

module GraphVG =

    let drawSeries graph =
        let margin  = 20.0
        let viewBox = ViewBox.create (Point.ofFloats (-margin, -margin)) (Area.ofFloats (Graph.canvasSize + 2.0 * margin, Graph.canvasSize + 2.0 * margin))

        let xScale = Scale.linear graph.Domain (0.0, Graph.canvasSize)
        let yScale = Scale.linear graph.Range  (Graph.canvasSize, 0.0)

        // Position axes at the canvas location of the data origin, clamped to canvas bounds
        let clamp lo hi v = max lo (min hi v)
        let xAxisY = Scale.apply yScale 0.0 |> clamp 0.0 Graph.canvasSize
        let yAxisX = Scale.apply xScale 0.0 |> clamp 0.0 Graph.canvasSize

        let xAxis = Axis.create (HorizontalAt xAxisY) xScale
        let yAxis = Axis.create (VerticalAt   yAxisX) yScale

        let elements =
            Graph.drawSeries graph
            @ Axis.toElements Theme.empty xAxis
            @ Axis.toElements Theme.empty yAxis

        elements
        |> Svg.ofList
        |> Svg.withViewBox viewBox
        |> Svg.withSize (Area.ofInts (800, 800))
        |> Svg.toHtml "GraphVG"
