namespace GraphVG

open SharpVG

module GraphVG =

    let private margin = 20.0

    let render (graph : Graph) (theme : Theme) (axes : Axis list option) (size : Area option) =
        let viewBox = ViewBox.create (Point.ofFloats (-margin, -margin)) (Area.ofFloats (Graph.canvasSize + 2.0 * margin, Graph.canvasSize + 2.0 * margin))
        let resolvedAxes = axes |> Option.defaultWith (fun () -> Axis.defaults graph)

        let elements =
            Graph.drawSeries theme graph
            @ (resolvedAxes |> List.collect (Axis.toElements theme))

        let svg =
            elements
            |> Svg.ofList
            |> Svg.withViewBox viewBox

        let svg = size |> Option.fold (fun s a -> Svg.withSize a s) svg

        svg |> Svg.toHtml "GraphVG"

    let drawSeries graph =
        render graph Theme.empty None None
