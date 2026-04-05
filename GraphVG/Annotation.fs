namespace GraphVG

open SharpVG

type Annotation =
    | Text of x : float * y : float * content : string
    | Line of x1 : float * y1 : float * x2 : float * y2 : float
    | Rect of x : float * y : float * width : float * height : float

type TitleAlignment = Left | Center | Right

type TitleStyle =
    {
        FontSize : float
        Alignment : TitleAlignment
    }

module TitleStyle =

    let create fontSize alignment =
        { FontSize = fontSize; Alignment = alignment }

    let defaults =
        { FontSize = 16.0; Alignment = Center }

module Annotation =

    let toElements (toSvgCoord : float * float -> float * float) (axisPen : Pen) (fontSize : float) (annotations : Annotation list) =
        let strokeStyle = Style.createWithPen axisPen
        let fillStyle = Style.empty |> Style.withFillPen axisPen
        annotations
        |> List.map (fun annotation ->
            match annotation with
            | Text(x, y, content) ->
                let svgX, svgY = toSvgCoord (x, y)
                Text.create (Point.ofFloats (svgX, svgY)) content
                |> Text.withFontSize fontSize
                |> Element.createWithStyle fillStyle
            | Line(x1, y1, x2, y2) ->
                let svgX1, svgY1 = toSvgCoord (x1, y1)
                let svgX2, svgY2 = toSvgCoord (x2, y2)
                Line.create (Point.ofFloats (svgX1, svgY1)) (Point.ofFloats (svgX2, svgY2))
                |> Element.createWithStyle strokeStyle
            | Rect(x, y, width, height) ->
                let svgX, svgY = toSvgCoord (x, y + height)
                let svgX2, svgY2 = toSvgCoord (x + width, y)
                Rect.create
                    (Point.ofFloats (svgX, svgY))
                    (Area.ofFloats (svgX2 - svgX, svgY2 - svgY))
                |> Element.createWithStyle strokeStyle)
