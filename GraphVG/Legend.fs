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

    let swatchWidth = 20.0
    let swatchHeight = 8.0
    let swatchLabelGap = 6.0
    let legendOuterMargin = 8.0
    let legendEntryGap = 8.0
    let legendHorizontalGap = 16.0

    let create position =
        { Position = position; FontSize = 12.0 }

    let withFontSize fontSize (legend : Legend) =
        { legend with FontSize = fontSize }

    let toElements (penForSeries : int -> Pen) (axisPen : Pen) (series : Series list) (legend : Legend) (padding : GraphPadding) =
        let labeled =
            series
            |> List.mapi (fun i s -> i, s)
            |> List.choose (fun (i, s) -> s.Label |> Option.map (fun l -> i, l))
        match labeled with
        | [] -> []
        | _ ->
            let textStyle = Style.empty |> Style.withFillPen axisPen
            let entryHeight = max swatchHeight legend.FontSize
            let mkEntry seriesIndex label swatchX swatchY =
                let pen = penForSeries seriesIndex
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
                let startY = (canvasSize - totalHeight) / 2.0
                labeled
                |> List.mapi (fun row (seriesIndex, label) ->
                    let swatchY = startY + float row * (entryHeight + legendEntryGap) + (entryHeight - swatchHeight) / 2.0
                    mkEntry seriesIndex label swatchX swatchY)
                |> List.concat
            let horizontalEntries swatchY =
                let entryWidths = labeled |> List.map (fun (_, l) -> swatchWidth + swatchLabelGap + estimatedTextWidth legend.FontSize l)
                let totalWidth = List.sum entryWidths + float (labeled.Length - 1) * legendHorizontalGap
                let startX = (canvasSize - totalWidth) / 2.0
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
            | LegendRight -> verticalEntries (canvasSize + legendOuterMargin)
            | LegendTop -> horizontalEntries (-padding.Top + legendOuterMargin)
            | LegendBottom -> horizontalEntries (canvasSize + legendOuterMargin)
