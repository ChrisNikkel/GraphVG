namespace GraphVG

open SharpVG
open CommonMath
open Layout

module GraphVG =

    let private buildSvg (graph : Graph) =
        let padding = graphPadding graph
        let axes = [ graph.XAxis; graph.YAxis ] |> List.choose id
        let gridElements = axes |> List.collect (Axis.toGridElements graph.Theme)
        let axisElements = axes |> List.collect (Axis.toElements graph.Theme)
        let toSvgCoord = Graph.toScaledSvgCoordinates graph
        [
            [ backgroundElement graph.Theme.Background padding ]
            graph.Theme.PlotBackground |> Option.map plotBackground |> Option.toList
            gridElements
            Graph.drawSeries graph
            Annotation.toElements toSvgCoord graph.Theme.AxisPen graph.TitleStyle.FontSize graph.Annotations
            graph.Legend
            |> Option.map (fun legend ->
                Legend.toElements
                    (fun i -> Theme.penForSeries i graph.Theme)
                    graph.Theme.AxisPen
                    graph.Series
                    legend
                    padding)
            |> Option.defaultValue []
            Layout.heatmapRampElements graph.Theme.AxisPen graph
            axisElements
            graph.Title |> Option.map (fun title -> titleElement title graph.TitleStyle.FontSize graph.TitleStyle.Alignment padding) |> Option.toList
        ]
        |> List.concat
        |> Svg.ofList
        |> Svg.withViewBox (viewBoxForPadding padding)

    // ── Public API ────────────────────────────────────────────────────────────

    /// Returns a raw SVG string.
    let toSvg (graph : Graph) : string =
        buildSvg graph |> Svg.toString

    /// Returns a full HTML page with the graph centered and fit to the viewport.
    let toHtml (graph : Graph) : string =
        let svgContent = buildSvg graph |> Svg.toString
        let css = "html,body{margin:0;height:100%;}body{display:flex;align-items:center;justify-content:center;background:#f5f5f5;}svg{width:100vmin;height:100vmin;}"
        "<!DOCTYPE html>\n<html>\n<head>\n<title>GraphVG</title>\n<style>" + css + "</style>\n</head>\n<body>\n" + svgContent + "\n</body>\n</html>\n"

    /// Writes the SVG output to a file, overwriting if it exists.
    let writeSvg (path : string) (graph : Graph) =
        System.IO.File.WriteAllText(path, toSvg graph)

    /// Writes the HTML output to a file, overwriting if it exists.
    let writeHtml (path : string) (graph : Graph) =
        System.IO.File.WriteAllText(path, toHtml graph)
