namespace GraphVG

open SharpVG
open Layout

module GraphVG =

    let private titleTopInset = 6.0

    // ── SVG element generation ────────────────────────────────────────────────

    let private viewBoxForPadding (padding : GraphPadding) =
        ViewBox.create
            (Point.ofFloats (-padding.Left, -padding.Top))
            (Area.ofFloats (Canvas.canvasSize + padding.Left + padding.Right, Canvas.canvasSize + padding.Top + padding.Bottom))

    let private backgroundElement (backgroundColor : Color) (padding : GraphPadding) =
        Rect.create
            (Point.ofFloats (-padding.Left, -padding.Top))
            (Area.ofFloats (Canvas.canvasSize + padding.Left + padding.Right, Canvas.canvasSize + padding.Top + padding.Bottom))
        |> Element.createWithStyle (Style.empty |> Style.withFill backgroundColor)

    let private plotBackgroundElements (graph : Graph) =
        graph.Theme.PlotBackground
        |> Option.map (fun color ->
            Rect.create (Point.origin) (Area.ofFloats (Canvas.canvasSize, Canvas.canvasSize))
            |> Element.createWithStyle (Style.empty |> Style.withFill color))
        |> Option.toList

    let private titleElements (graph : Graph) (padding : GraphPadding) =
        graph.Title
        |> Option.map (fun title ->
            Text.create (Point.ofFloats (Canvas.canvasSize / 2.0, -padding.Top + titleTopInset)) title
            |> Text.withFontSize graph.TitleStyle.FontSize
            |> Text.withAnchor graph.TitleStyle.Alignment
            |> Text.withBaseline HangingBaseline
            |> Element.createWithStyle (Style.empty |> Style.withFillPen Pen.black))
        |> Option.toList

    let private annotationElements (graph : Graph) =
        let strokeStyle = Style.createWithPen graph.Theme.AxisPen
        let fillStyle = Style.empty |> Style.withFillPen graph.Theme.AxisPen
        graph.Annotations
        |> List.map (fun annotation ->
            match annotation with
            | Annotation.Text(x, y, content) ->
                let svgX, svgY = Graph.toScaledSvgCoordinates graph (x, y)
                Text.create (Point.ofFloats (svgX, svgY)) content
                |> Text.withFontSize graph.TitleStyle.FontSize
                |> Element.createWithStyle fillStyle
            | Annotation.Line(x1, y1, x2, y2) ->
                let svgX1, svgY1 = Graph.toScaledSvgCoordinates graph (x1, y1)
                let svgX2, svgY2 = Graph.toScaledSvgCoordinates graph (x2, y2)
                Line.create (Point.ofFloats (svgX1, svgY1)) (Point.ofFloats (svgX2, svgY2))
                |> Element.createWithStyle strokeStyle
            | Annotation.Rect(x, y, width, height) ->
                let svgX, svgY = Graph.toScaledSvgCoordinates graph (x, y + height)
                let svgX2, svgY2 = Graph.toScaledSvgCoordinates graph (x + width, y)
                Rect.create
                    (Point.ofFloats (svgX, svgY))
                    (Area.ofFloats (svgX2 - svgX, svgY2 - svgY))
                |> Element.createWithStyle strokeStyle)

    let private legendElements (graph : Graph) (padding : GraphPadding) =
        match graph.Legend with
        | None -> []
        | Some legend ->
            let labeled =
                graph.Series
                |> List.mapi (fun i s -> i, s)
                |> List.choose (fun (i, s) -> s.Label |> Option.map (fun l -> i, l))
            match labeled with
            | [] -> []
            | _ ->
                let textStyle = Style.empty |> Style.withFillPen graph.Theme.AxisPen
                let entryHeight = max swatchHeight legend.FontSize
                let mkEntry seriesIndex label swatchX swatchY =
                    let pen = Theme.penForSeries seriesIndex graph.Theme
                    let swatch =
                        Rect.create
                            (Point.ofFloats (swatchX, swatchY))
                            (Area.ofFloats (swatchWidth, swatchHeight))
                        |> Element.createWithStyle (Style.empty |> Style.withFillPen pen)
                    let labelEl =
                        Text.create
                            (Point.ofFloats (swatchX + swatchWidth + swatchLabelGap, swatchY + swatchHeight / 2.0))
                            label
                        |> Text.withFontSize legend.FontSize
                        |> Text.withBaseline CentralBaseline
                        |> Element.createWithStyle textStyle
                    [ swatch; labelEl ]
                let verticalEntries swatchX =
                    let totalHeight = float labeled.Length * entryHeight + float (labeled.Length - 1) * legendEntryGap
                    let startY = (Canvas.canvasSize - totalHeight) / 2.0
                    labeled
                    |> List.mapi (fun row (seriesIndex, label) ->
                        let swatchY = startY + float row * (entryHeight + legendEntryGap) + (entryHeight - swatchHeight) / 2.0
                        mkEntry seriesIndex label swatchX swatchY)
                    |> List.concat
                let horizontalEntries swatchY =
                    let entryWidths = labeled |> List.map (fun (_, l) -> swatchWidth + swatchLabelGap + estimatedTextWidth legend.FontSize l)
                    let totalWidth = List.sum entryWidths + float (labeled.Length - 1) * legendHorizontalGap
                    let startX = (Canvas.canvasSize - totalWidth) / 2.0
                    let xStarts =
                        entryWidths
                        |> List.scan (+) 0.0
                        |> List.take labeled.Length
                        |> List.mapi (fun col w -> startX + w + float col * legendHorizontalGap)
                    List.map2 (fun (seriesIndex, label) swatchX ->
                        mkEntry seriesIndex label swatchX swatchY)
                        labeled xStarts
                    |> List.concat
                match legend.Position with
                | LegendHidden -> []
                | LegendLeft -> verticalEntries (-padding.Left + legendOuterMargin)
                | LegendRight -> verticalEntries (Canvas.canvasSize + legendOuterMargin)
                | LegendTop -> horizontalEntries (-padding.Top + legendOuterMargin)
                | LegendBottom -> horizontalEntries (Canvas.canvasSize + legendOuterMargin)

    // ── Assembly ──────────────────────────────────────────────────────────────

    let private buildSvg (graph : Graph) =
        let padding = graphPadding graph
        let axes = [ graph.XAxis; graph.YAxis ] |> List.choose id
        let gridElements = axes |> List.collect (Axis.toGridElements graph.Theme)
        let axisElements = axes |> List.collect (Axis.toElements graph.Theme)
        [
            [ backgroundElement graph.Theme.Background padding ]
            plotBackgroundElements graph
            gridElements
            Graph.drawSeries graph
            annotationElements graph
            legendElements graph padding
            axisElements
            titleElements graph padding
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
