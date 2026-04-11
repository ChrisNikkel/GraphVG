namespace GraphVG

open System
open SharpVG
open CommonMath

type RadarPoint =
    {
        Axes : string list
        Values : float list
    }

type RadarChart =
    {
        Series : RadarPoint list
        Labels : string option list
        Theme : Theme
        Title : string option
        TitleFontSize : float
        RingCount : int
    }

module RadarChart =

    let create (series : RadarPoint list) =
        {
            Series = series
            Labels = List.replicate series.Length None
            Theme = Theme.empty
            Title = None
            TitleFontSize = canvasSize * 0.022
            RingCount = 5
        }

    let withTheme theme chart = { chart with Theme = theme }
    let withTitle title chart = { chart with Title = Some title }
    let withTitleFontSize size chart = { chart with TitleFontSize = size }
    let withLabels (labels : string list) chart = { chart with Labels = labels |> List.map Some }
    let withRingCount n chart = { chart with RingCount = max 1 n }

    let private buildSvg (chart : RadarChart) =
        let cs = canvasSize
        let bgEl =
            Rect.create Point.origin (Area.ofFloats (cs, cs))
            |> Element.createWithStyle (Style.empty |> Style.withFill chart.Theme.Background)
        let emptyResult () =
            Svg.ofList [ bgEl ]
            |> Svg.withViewBox (ViewBox.create Point.origin (Area.ofFloats (cs, cs)))

        if chart.Series.IsEmpty then emptyResult ()
        else
            let allAxes = chart.Series.[0].Axes
            let n = allAxes.Length
            if n < 3 then emptyResult ()
            else
                let cx = cs / 2.0
                let cy = cs / 2.0
                let outerRadius = cs * 0.34
                let labelGap = cs * 0.055
                let rings = chart.RingCount
                let axisFontSize = cs * 0.013
                let ringFontSize = cs * 0.009

                let allValues = chart.Series |> List.collect (fun s -> s.Values)
                let rawMax = if allValues.IsEmpty then 1.0 else List.max allValues
                let maxVal = if rawMax <= 0.0 then 1.0 else rawMax

                // Axis 0 starts at the top (-π/2), proceeding clockwise
                let axisAngle i =
                    -Math.PI / 2.0 + 2.0 * Math.PI * float i / float n

                let axisXY i t =
                    let a = axisAngle i
                    cx + t * outerRadius * Math.Cos a, cy + t * outerRadius * Math.Sin a

                let axisPen = chart.Theme.AxisPen
                let gridPen =
                    chart.Theme.GridPen
                    |> Option.defaultValue (axisPen |> Pen.withOpacity 0.25 |> Pen.withWidth (Length.ofFloat 0.5))
                let gridTextStyle =
                    Style.empty |> Style.withFillPen (axisPen |> Pen.withOpacity 0.5)

                // Grid rings — closed polylines carry no SVG fill
                let ringEls =
                    [ 1 .. rings ]
                    |> List.map (fun ring ->
                        let t = float ring / float rings
                        let pts = [ 0 .. n - 1 ] |> List.map (fun i -> Point.ofFloats (axisXY i t))
                        Polyline.ofList (pts @ [ pts.[0] ])
                        |> Element.createWithStyle (Style.createWithPen gridPen))

                // Radial spokes from center to perimeter
                let spokeEls =
                    [ 0 .. n - 1 ]
                    |> List.map (fun i ->
                        Line.create
                            (Point.ofFloats (cx, cy))
                            (Point.ofFloats (axisXY i 1.0))
                        |> Element.createWithStyle (Style.createWithPen gridPen))

                // Ring value labels alongside axis 0 (top spoke)
                let ringLabelEls =
                    [ 1 .. rings ]
                    |> List.map (fun ring ->
                        let t = float ring / float rings
                        let lx, ly = axisXY 0 t
                        Text.create (Point.ofFloats (lx + cs * 0.006, ly)) (sprintf "%.4g" (t * maxVal))
                        |> Text.withFontSize ringFontSize
                        |> Text.withAnchor Start
                        |> Text.withBaseline CentralBaseline
                        |> Element.createWithStyle gridTextStyle)

                // Axis labels at the perimeter
                let axisLabelEls =
                    allAxes
                    |> List.mapi (fun i label ->
                        let a = axisAngle i
                        let lx = cx + (outerRadius + labelGap) * Math.Cos a
                        let ly = cy + (outerRadius + labelGap) * Math.Sin a
                        let anchor =
                            if Math.Abs (Math.Cos a) < 0.15 then Middle
                            elif Math.Cos a < 0.0 then End
                            else Start
                        let baseline =
                            if Math.Sin a < -0.3 then AlphabeticBaseline
                            elif Math.Sin a > 0.3 then HangingBaseline
                            else CentralBaseline
                        Text.create (Point.ofFloats (lx, ly)) label
                        |> Text.withFontSize axisFontSize
                        |> Text.withAnchor anchor
                        |> Text.withBaseline baseline
                        |> Element.createWithStyle (Style.empty |> Style.withFillPen axisPen))

                // Series polygons — fill element + stroke element kept separate so
                // the fill polygon doesn't accidentally inherit a black SVG default stroke
                let seriesEls =
                    chart.Series
                    |> List.mapi (fun i radarPt ->
                        if radarPt.Values.Length < n then []
                        else
                            let pen = Theme.penForSeries i chart.Theme
                            let pts =
                                [ 0 .. n - 1 ]
                                |> List.map (fun j ->
                                    let t = clamp 0.0 1.0 (radarPt.Values.[j] / maxVal)
                                    Point.ofFloats (axisXY j t))
                            let fillEl =
                                Polygon.ofList pts
                                |> Element.createWithStyle
                                    (Style.empty |> Style.withFillPen pen |> Style.withFillOpacity 0.2)
                            let strokeEl =
                                Polyline.ofList (pts @ [ pts.[0] ])
                                |> Element.createWithStyle (Style.createWithPen pen)
                            [ fillEl; strokeEl ])
                    |> List.concat

                // Inline legend (bottom-left) when labels are supplied
                let legendEls =
                    let labeled =
                        chart.Labels
                        |> List.mapi (fun i lo -> i, lo)
                        |> List.choose (fun (i, lo) -> lo |> Option.map (fun l -> i, l))
                    if labeled.IsEmpty then []
                    else
                        let legFontSize = cs * 0.012
                        let swatchSz = cs * 0.012
                        let entryGap = cs * 0.007
                        let entryH = swatchSz + entryGap
                        let startX = cs * 0.025
                        let startY = cs - float labeled.Length * entryH - cs * 0.025
                        labeled
                        |> List.mapi (fun row (idx, label) ->
                            let pen = Theme.penForSeries idx chart.Theme
                            let sy = startY + float row * entryH
                            let swatchEl =
                                Rect.create
                                    (Point.ofFloats (startX, sy))
                                    (Area.ofFloats (swatchSz, swatchSz))
                                |> Element.createWithStyle (Style.empty |> Style.withFillPen pen)
                            let labelEl =
                                Text.create
                                    (Point.ofFloats (startX + swatchSz + entryGap, sy + swatchSz / 2.0))
                                    label
                                |> Text.withFontSize legFontSize
                                |> Text.withBaseline CentralBaseline
                                |> Element.createWithStyle (Style.empty |> Style.withFillPen axisPen)
                            [ swatchEl; labelEl ])
                        |> List.concat

                // Title
                let titleEls =
                    chart.Title
                    |> Option.map (fun t ->
                        Text.create (Point.ofFloats (cx, cs * 0.02)) t
                        |> Text.withFontSize chart.TitleFontSize
                        |> Text.withAnchor Middle
                        |> Text.withBaseline HangingBaseline
                        |> Element.createWithStyle (Style.empty |> Style.withFillPen axisPen))
                    |> Option.toList

                [ [ bgEl ]; ringEls; spokeEls; ringLabelEls; axisLabelEls; seriesEls; legendEls; titleEls ]
                |> List.concat
                |> Svg.ofList
                |> Svg.withViewBox (ViewBox.create Point.origin (Area.ofFloats (cs, cs)))

    let toSvg (chart : RadarChart) =
        buildSvg chart |> Svg.toString

    let toHtml (chart : RadarChart) =
        let svgContent = buildSvg chart |> Svg.toString
        let css = "html,body{margin:0;height:100%;}body{display:flex;align-items:center;justify-content:center;background:#f5f5f5;}svg{width:100vmin;height:100vmin;}"
        "<!DOCTYPE html>\n<html>\n<head>\n<title>GraphVG</title>\n<style>" + css + "</style>\n</head>\n<body>\n" + svgContent + "\n</body>\n</html>\n"
