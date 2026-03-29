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

    /// Returns a full HTML page with the graph centered and fit to the viewport.
    let toHtml (graph : Graph) : string =
        let svgContent = buildSvg graph |> Svg.toString
        let css = "html,body{margin:0;height:100%;}body{display:flex;align-items:center;justify-content:center;background:#f5f5f5;}svg{width:100vmin;height:100vmin;}"
        "<!DOCTYPE html>\n<html>\n<head>\n<title>GraphVG</title>\n<style>" + css + "</style>\n</head>\n<body>\n" + svgContent + "\n</body>\n</html>\n"
