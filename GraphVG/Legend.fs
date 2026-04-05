namespace GraphVG

open SharpVG
open CommonMath

type LegendPosition =
    | LegendTop
    | LegendBottom
    | LegendLeft
    | LegendRight
    | LegendHidden

type Legend =
    {
        Position : LegendPosition
        FontSize : float
    }

module Legend =

    let swatchWidth = canvasSize * 0.020
    let swatchHeight = canvasSize * 0.008
    let swatchLabelGap = canvasSize * 0.006
    let legendOuterMargin = canvasSize * 0.008
    let legendEntryGap = canvasSize * 0.008
    let legendHorizontalGap = canvasSize * 0.016

    let create position =
        { Position = position; FontSize = canvasSize * 0.012 }

    let withFontSize fontSize (legend : Legend) =
        { legend with FontSize = fontSize }

    let toElements (penForSeries : int -> Pen) (axisPen : Pen) (series : Series list) (legend : Legend) (padding : GraphPadding) (cs : float) =
        let sf = cs / canvasSize
        let swatchW = swatchWidth * sf
        let swatchH = swatchHeight * sf
        let labelGap = swatchLabelGap * sf
        let outerMargin = legendOuterMargin * sf
        let entryGap = legendEntryGap * sf
        let hGap = legendHorizontalGap * sf
        let fontSize = legend.FontSize * sf
        let labeled =
            series
            |> List.mapi (fun i s -> i, s)
            |> List.choose (fun (i, s) -> s.Label |> Option.map (fun l -> i, s.Kind, l))
        match labeled with
        | [] -> []
        | _ ->
            let textStyle = Style.empty |> Style.withFillPen axisPen
            let entryHeight = max swatchH fontSize
            let mkEntry seriesIndex kind label swatchX swatchY =
                let pen = penForSeries seriesIndex
                let swatch =
                    match kind with
                    | Bubble _ ->
                        let cx = swatchX + swatchW / 2.0
                        let cy = swatchY + swatchH / 2.0
                        Circle.create
                            (Point.ofFloats (cx, cy))
                            (Length.ofFloat (swatchH / 2.0))
                        |> Element.createWithStyle (Style.empty |> Style.withFillPen pen |> Style.withFillOpacity 0.5)
                    | _ ->
                        Rect.create
                            (Point.ofFloats (swatchX, swatchY))
                            (Area.ofFloats (swatchW, swatchH))
                        |> Element.createWithStyle (Style.empty |> Style.withFillPen pen)
                let labelEl =
                    Text.create
                        (Point.ofFloats (swatchX + swatchW + labelGap, swatchY + swatchH / 2.0))
                        label
                    |> Text.withFontSize fontSize
                    |> Text.withBaseline CentralBaseline
                    |> Element.createWithStyle textStyle
                [ swatch; labelEl ]
            let verticalEntries swatchX =
                let totalHeight = float labeled.Length * entryHeight + float (labeled.Length - 1) * entryGap
                let startY = (cs - totalHeight) / 2.0
                labeled
                |> List.mapi (fun row (seriesIndex, kind, label) ->
                    let swatchY = startY + float row * (entryHeight + entryGap) + (entryHeight - swatchH) / 2.0
                    mkEntry seriesIndex kind label swatchX swatchY)
                |> List.concat
            let horizontalEntries swatchY =
                let entryWidths = labeled |> List.map (fun (_, _, l) -> swatchW + labelGap + estimatedTextWidth fontSize l)
                let totalWidth = List.sum entryWidths + float (labeled.Length - 1) * hGap
                let startX = (cs - totalWidth) / 2.0
                let xStarts =
                    entryWidths
                    |> List.scan (+) 0.0
                    |> List.take labeled.Length
                    |> List.mapi (fun col w -> startX + w + float col * hGap)
                List.map2 (fun (seriesIndex, kind, label) swatchX ->
                    mkEntry seriesIndex kind label swatchX swatchY)
                    labeled xStarts
                |> List.concat
            match legend.Position with
            | LegendHidden -> []
            | LegendLeft -> verticalEntries (-padding.Left + outerMargin)
            | LegendRight -> verticalEntries (cs + outerMargin)
            | LegendTop -> horizontalEntries (-padding.Top + outerMargin)
            | LegendBottom -> horizontalEntries (cs + outerMargin)
