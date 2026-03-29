namespace GraphVG

open SharpVG

module GraphVG =

    let private margin = 20.0
    let private titlePadding = 16.0

    let private buildSvg (graph : Graph) =
        let topMargin =
            graph.Title
            |> Option.map (fun _ -> max margin (graph.TitleStyle.FontSize + titlePadding))
            |> Option.defaultValue margin
        let viewBox =
            ViewBox.create
                (Point.ofFloats (-margin, -topMargin))
                (Area.ofFloats (Canvas.canvasSize + 2.0 * margin, Canvas.canvasSize + topMargin + margin))
        let background =
            Rect.create (Point.ofFloats (-margin, -topMargin)) (Area.ofFloats (Canvas.canvasSize + 2.0 * margin, Canvas.canvasSize + topMargin + margin))
            |> Element.createWithStyle (Style.empty |> Style.withFill graph.Theme.Background)
        let plotBackground =
            graph.Theme.PlotBackground
            |> Option.map (fun color ->
                Rect.create (Point.origin) (Area.ofFloats (Canvas.canvasSize, Canvas.canvasSize))
                |> Element.createWithStyle (Style.empty |> Style.withFill color))
            |> Option.toList
        let axes = [ graph.XAxis; graph.YAxis ] |> List.choose id
        let gridElements = axes |> List.collect (Axis.toGridElements graph.Theme)
        let axisElements = axes |> List.collect (Axis.toElements graph.Theme)
        let titleElements =
            graph.Title
            |> Option.map (fun t ->
                let style = Style.empty |> Style.withFillPen Pen.black
                let pos = Point.ofFloats (Canvas.canvasSize / 2.0, -topMargin + 6.0)
                Text.create pos t
                |> Text.withFontSize graph.TitleStyle.FontSize
                |> Text.withAnchor graph.TitleStyle.Alignment
                |> Text.withBaseline HangingBaseline
                |> Element.createWithStyle style)
            |> Option.toList
        background :: plotBackground @ gridElements @ Graph.drawSeries graph @ axisElements @ titleElements
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
