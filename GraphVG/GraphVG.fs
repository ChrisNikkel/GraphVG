namespace GraphVG

open SharpVG

module GraphVG =

    let private margin = 20.0

    let private buildSvg (graph : Graph) =
        let viewBox =
            ViewBox.create
                (Point.ofFloats (-margin, -margin))
                (Area.ofFloats (Canvas.canvasSize + 2.0 * margin, Canvas.canvasSize + 2.0 * margin))
        let axisElements =
            [ graph.XAxis; graph.YAxis ]
            |> List.choose id
            |> List.collect (Axis.toElements graph.Theme)
        let titleElements =
            graph.Title
            |> Option.map (fun t ->
                let style = Style.create (Color.ofName Black) (Color.ofName Black) (Length.ofInt 1) 1.0 1.0
                let pos   = Point.ofFloats (Canvas.canvasSize / 2.0, -margin / 2.0)
                Text.create pos t
                |> Text.withFontSize 16.0
                |> Text.withAnchor Middle
                |> Element.createWithStyle style)
            |> Option.toList
        Graph.drawSeries graph @ axisElements @ titleElements
        |> Svg.ofList
        |> Svg.withViewBox viewBox

    /// Returns a raw SVG string.
    let render (graph : Graph) : string =
        buildSvg graph |> Svg.toString

    /// Returns a full HTML page with the graph embedded.
    let toHtml (graph : Graph) : string =
        buildSvg graph |> Svg.toHtml "GraphVG"
