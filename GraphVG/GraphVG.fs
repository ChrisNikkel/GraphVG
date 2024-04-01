namespace GraphVG

open SharpVG

module GraphVG =
    let canvasSize = 500
    let halfCanvasSize = canvasSize / 2
    let majorTickSize = canvasSize / 10
    let minorTickSize = majorTickSize / 2
    let centerOffset = -halfCanvasSize, halfCanvasSize

    let toSvgCoordinates (x, y) =
        x, -y

    let axis =
        let leftCenter = (-halfCanvasSize, 0)
        let rightCenter = (halfCanvasSize, 0)
        let topCenter = (0, halfCanvasSize)
        let bottomCenter = (0, -halfCanvasSize)

        let leftEndTickTop = (-halfCanvasSize, majorTickSize)
        let leftEndTickBottom = (-halfCanvasSize, -majorTickSize)

        let rightEndTickTop = (halfCanvasSize, majorTickSize)
        let rightEndTickBottom = (halfCanvasSize, -majorTickSize)

        let topEndTickRight = (-majorTickSize, halfCanvasSize)
        let topEndTickLeft = (majorTickSize, halfCanvasSize)

        let bottomEndTickRight = (-majorTickSize, -halfCanvasSize)
        let bottomEndTickLeft = (majorTickSize, -halfCanvasSize)

        let styleName = "axisStyle"
        let axisStyle = Style.create (Color.ofName Colors.Black) (Color.ofName Colors.Black) (Length.ofInt 3) 1.0 1.0 |> Style.withName styleName |> Element.create

        let leftCenterPoint = leftCenter |> toSvgCoordinates |> Point.ofInts
        let rightCenterPoint = rightCenter |> toSvgCoordinates |> Point.ofInts
        let topCenterPoint = topCenter |> toSvgCoordinates |> Point.ofInts
        let bottomCenterPoint = bottomCenter |> toSvgCoordinates |> Point.ofInts


        let leftEndTickTopPoint= leftEndTickTop |> toSvgCoordinates |> Point.ofInts
        let leftEndTickBottomPoint = leftEndTickBottom |> toSvgCoordinates |> Point.ofInts

        let rightEndTickTopPoint = rightEndTickTop |> toSvgCoordinates |> Point.ofInts
        let rightEndTickBottomPoint = rightEndTickBottom |> toSvgCoordinates |> Point.ofInts

        let topEndTickRightPoint = topEndTickRight |> toSvgCoordinates |> Point.ofInts
        let topEndTickLeftPoint= topEndTickLeft |> toSvgCoordinates |> Point.ofInts

        let bottomEndTickRightPoint = bottomEndTickRight |> toSvgCoordinates |> Point.ofInts
        let bottomEndTickLeftPoint = bottomEndTickLeft |> toSvgCoordinates |> Point.ofInts

        let leftCenterLine = Line.create leftEndTickTopPoint leftEndTickBottomPoint |> Element.create |> Element.withClass styleName
        let rightCenterLine = Line.create rightEndTickTopPoint rightEndTickBottomPoint |> Element.create |> Element.withClass styleName
        let topCenterLine = Line.create topEndTickRightPoint topEndTickLeftPoint |> Element.create |> Element.withClass styleName
        let bottomCenterLine = Line.create bottomEndTickRightPoint bottomEndTickLeftPoint |> Element.create |> Element.withClass styleName


        let xAxisLine = Line.create topCenterPoint bottomCenterPoint |> Element.create |> Element.withClass styleName
        let yAxisLine = Line.create leftCenterPoint rightCenterPoint |> Element.create |> Element.withClass styleName

        let elements = [axisStyle; xAxisLine; yAxisLine; leftCenterLine; rightCenterLine; topCenterLine; bottomCenterLine]
        let svg = elements

        svg

    let plot (x, y) =
        let viewBoxPoint = centerOffset |> toSvgCoordinates |> Point.ofInts
        let viewBoxArea = Area.ofInts (canvasSize, canvasSize)
        let viewBox = ViewBox.create viewBoxPoint viewBoxArea
        let style = Style.create (Color.ofName Colors.Black) (Color.ofName Colors.Black) (Length.ofInt 3) 1.0 1.0

        let point = (x, y) |> toSvgCoordinates |> Point.ofInts
        let circle = Circle.create point (Length.ofInt 3) |> Element.createWithStyle style
        let svg = axis |> (List.singleton circle |> List.append) |> Svg.ofList |> Svg.withViewBox viewBox
        let html = svg |> Svg.toHtml "Test"
        html
