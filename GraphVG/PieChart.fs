namespace GraphVG

open System
open SharpVG
open CommonMath

type PieChart =
    {
        Slices : (string * float) list
        InnerRadius : float
        StartAngle : float
        Theme : Theme
        Title : string option
        TitleFontSize : float
    }

module PieChart =

    let create (slices : (string * float) list) =
        {
            Slices = slices
            InnerRadius = 0.0
            StartAngle = 0.0
            Theme = Theme.empty
            Title = None
            TitleFontSize = canvasSize * 0.022
        }

    let withInnerRadius r chart = { chart with InnerRadius = clamp 0.0 0.95 r }
    let withStartAngle a chart = { chart with StartAngle = a }
    let withTheme theme chart = { chart with Theme = theme }
    let withTitle title chart = { chart with Title = Some title }
    let withTitleFontSize size chart = { chart with TitleFontSize = size }

    // ── Arc helpers ───────────────────────────────────────────────────────────

    let private ptOn cx cy r a = Point.ofFloats (cx + r * Math.Cos a, cy + r * Math.Sin a)

    let private pieSlicePath cx cy r a1 a2 =
        let largeArc = a2 - a1 > Math.PI
        let rPt = Point.ofFloats (r, r)
        Path.empty
        |> Path.addMoveTo Absolute (Point.ofFloats (cx, cy))
        |> Path.addLineTo Absolute (ptOn cx cy r a1)
        |> Path.addEllipticalArcCurveTo Absolute rPt 0.0 largeArc true (ptOn cx cy r a2)
        |> Path.addClosePath

    let private donutSlicePath cx cy outerR innerR a1 a2 =
        let largeArc = a2 - a1 > Math.PI
        let orPt = Point.ofFloats (outerR, outerR)
        let irPt = Point.ofFloats (innerR, innerR)
        Path.empty
        |> Path.addMoveTo Absolute (ptOn cx cy outerR a1)
        |> Path.addEllipticalArcCurveTo Absolute orPt 0.0 largeArc true (ptOn cx cy outerR a2)
        |> Path.addLineTo Absolute (ptOn cx cy innerR a2)
        |> Path.addEllipticalArcCurveTo Absolute irPt 0.0 largeArc false (ptOn cx cy innerR a1)
        |> Path.addClosePath

    // ── Rendering ─────────────────────────────────────────────────────────────

    let private buildSvg (chart : PieChart) =
        let cs = canvasSize
        let bgEl =
            Rect.create Point.origin (Area.ofFloats (cs, cs))
            |> Element.createWithStyle (Style.empty |> Style.withFill chart.Theme.Background)
        let emptyResult () =
            Svg.ofList [ bgEl ]
            |> Svg.withViewBox (ViewBox.create Point.origin (Area.ofFloats (cs, cs)))

        let validSlices = chart.Slices |> List.filter (fun (_, v) -> v > 0.0)
        if validSlices.IsEmpty then emptyResult ()
        else
            let total = validSlices |> List.sumBy snd
            let hasLabels = not (List.isEmpty validSlices)
            let isDonut = chart.InnerRadius > 0.0
            let tau = 2.0 * Math.PI

            // Layout: leave right strip for legend
            let legendFontSize = cs * 0.012
            let swatchSz = cs * 0.012
            let entryGap = cs * 0.007
            let entryH = swatchSz + entryGap
            let legendX = cs * 0.70
            let cx = cs * 0.36
            let cy = cs * 0.5
            let outerR = cs * 0.28
            let innerR = outerR * chart.InnerRadius

            // Convert user startAngle (0=top, clockwise) to math angle (0=right, CCW)
            let baseAngle = -Math.PI / 2.0 + chart.StartAngle
            let cumFractions =
                validSlices |> List.map snd |> List.scan (+) 0.0 |> List.map (fun v -> v / total)

            let axisPen = chart.Theme.AxisPen

            // Slice sectors
            let sliceEls =
                validSlices
                |> List.mapi (fun idx (_, v) ->
                    let a1 = baseAngle + cumFractions.[idx] * tau
                    let a2 = baseAngle + cumFractions.[idx + 1] * tau
                    let span = a2 - a1
                    let color = (Theme.penForSeries idx chart.Theme).Color
                    let fillStyle = Style.empty |> Style.withFill color
                    if span >= tau - 1e-6 then
                        // Full circle — arc degenerates, use Circle
                        if isDonut then
                            let outerEl =
                                Circle.create (Point.ofFloats (cx, cy)) (Length.ofFloat outerR)
                                |> Element.createWithStyle fillStyle
                            let innerEl =
                                Circle.create (Point.ofFloats (cx, cy)) (Length.ofFloat innerR)
                                |> Element.createWithStyle (Style.empty |> Style.withFill chart.Theme.Background)
                            [ outerEl; innerEl ]
                        else
                            [ Circle.create (Point.ofFloats (cx, cy)) (Length.ofFloat outerR)
                              |> Element.createWithStyle fillStyle ]
                    else
                        let path =
                            if isDonut then donutSlicePath cx cy outerR innerR a1 a2
                            else pieSlicePath cx cy outerR a1 a2
                        [ path |> Element.createWithStyle fillStyle ])
                |> List.concat

            // Percentage labels inside each slice
            let pctFontSize = cs * 0.012
            let minSpanForLabel = 0.25  // ~14° — skip labels on tiny slices
            let percentEls =
                validSlices
                |> List.mapi (fun idx (_, v) ->
                    let a1 = baseAngle + cumFractions.[idx] * tau
                    let a2 = baseAngle + cumFractions.[idx + 1] * tau
                    let span = a2 - a1
                    if span < minSpanForLabel then []
                    else
                        let midA = (a1 + a2) / 2.0
                        let labelR =
                            if isDonut then (outerR + innerR) / 2.0
                            else outerR * 0.65
                        let lx = cx + labelR * Math.Cos midA
                        let ly = cy + labelR * Math.Sin midA
                        let pct = v / total * 100.0
                        let pctText = sprintf "%.0f%%" pct
                        [ Text.create (Point.ofFloats (lx, ly)) pctText
                          |> Text.withFontSize pctFontSize
                          |> Text.withAnchor Middle
                          |> Text.withBaseline CentralBaseline
                          |> Element.createWithStyle (Style.empty |> Style.withFill (Color.ofName White)) ])
                |> List.concat

            // Total value in center for donut mode
            let centerEls =
                if not isDonut then []
                else
                    let totalText =
                        if total = Math.Floor total then sprintf "%.0f" total
                        else sprintf "%.4g" total
                    let totalFontSize = innerR * 0.45
                    [ Text.create (Point.ofFloats (cx, cy)) totalText
                      |> Text.withFontSize totalFontSize
                      |> Text.withAnchor Middle
                      |> Text.withBaseline CentralBaseline
                      |> Element.createWithStyle (Style.empty |> Style.withFillPen axisPen) ]

            // Legend (slice color + label + percentage)
            let legendEls =
                if not hasLabels then []
                else
                    let totalEntries = validSlices.Length
                    let totalH = float totalEntries * entryH
                    let startY = (cs - totalH) / 2.0
                    validSlices
                    |> List.mapi (fun idx (label, v) ->
                        let color = (Theme.penForSeries idx chart.Theme).Color
                        let sy = startY + float idx * entryH
                        let pct = v / total * 100.0
                        let swatchEl =
                            Rect.create
                                (Point.ofFloats (legendX, sy))
                                (Area.ofFloats (swatchSz, swatchSz))
                            |> Element.createWithStyle (Style.empty |> Style.withFill color)
                        let labelEl =
                            Text.create
                                (Point.ofFloats (legendX + swatchSz + cs * 0.008, sy + swatchSz / 2.0))
                                (sprintf "%s  %.0f%%" label pct)
                            |> Text.withFontSize legendFontSize
                            |> Text.withBaseline CentralBaseline
                            |> Element.createWithStyle (Style.empty |> Style.withFillPen axisPen)
                        [ swatchEl; labelEl ])
                    |> List.concat

            // Title
            let titleEls =
                chart.Title
                |> Option.map (fun t ->
                    Text.create (Point.ofFloats (cs / 2.0, cs * 0.025)) t
                    |> Text.withFontSize chart.TitleFontSize
                    |> Text.withAnchor Middle
                    |> Text.withBaseline HangingBaseline
                    |> Element.createWithStyle (Style.empty |> Style.withFillPen axisPen))
                |> Option.toList

            [ [ bgEl ]; sliceEls; percentEls; centerEls; legendEls; titleEls ]
            |> List.concat
            |> Svg.ofList
            |> Svg.withViewBox (ViewBox.create Point.origin (Area.ofFloats (cs, cs)))

    let toSvg (chart : PieChart) =
        buildSvg chart |> Svg.toString

    let toHtml (chart : PieChart) =
        let svgContent = buildSvg chart |> Svg.toString
        let css = "html,body{margin:0;height:100%;}body{display:flex;align-items:center;justify-content:center;background:#f5f5f5;}svg{width:100vmin;height:100vmin;}"
        "<!DOCTYPE html>\n<html>\n<head>\n<title>GraphVG</title>\n<style>" + css + "</style>\n</head>\n<body>\n" + svgContent + "\n</body>\n</html>\n"
