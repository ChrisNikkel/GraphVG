namespace GraphVG

open SharpVG

module GraphVG =
    type Graph = {
        Series: (float * float) list
        Domain: float * float
        Range: float * float
    }

    let canvasSize = 1000.0
    let halfCanvasSize = canvasSize / 2.0
    let majorTickSize = canvasSize / 10.0
    let minorTickSize = majorTickSize / 2.0
    let centerCanvas = halfCanvasSize, halfCanvasSize

    let getDomainRange series =
        let xValues, yValues = series |> List.unzip
        let domain = List.reduce min xValues, List.reduce max xValues
        let range = List.reduce min yValues, List.reduce max yValues
        domain, range

    let getDomainRangeSize (domain, range) =
        let left, right = domain
        let bottom, top = range
        (right - left, top - bottom)

    let toScaledSvgCoordinates graph (x, y) =
        let width, height = getDomainRangeSize (graph.Domain, graph.Range)
        let scaleHorizontally w = canvasSize * w / width
        let scaleVertically h = canvasSize * h / height
        let left= scaleHorizontally (fst graph.Domain)
        let top = scaleVertically (snd graph.Range)

        let outputX = (scaleHorizontally x) - left
        let outputY = -((scaleVertically y) - top)

        outputX, outputY

    let create series domain range =
        { Series = series; Domain = domain; Range = range }

    let createWithSeries series =
        let domain, range = getDomainRange series
        { Series = series; Domain = domain; Range = range }

    let drawAxis graph =
        let domainRange: (float * float) * (float * float) = (graph.Domain, graph.Range)
        let domain, range = domainRange
        let left, right = domain
        let bottom, top = range

        let styleName = "axisStyle"
        let axisStyle = Style.create (Color.ofName Colors.Black) (Color.ofName Colors.Blue) (Length.ofInt 3) 1.0 1.0 |> Style.withName styleName |> Element.create
        let toPoint (x, y) = Point.ofFloats (toScaledSvgCoordinates graph (x, y))
        let toLine point1 point2 = Line.create point1 point2 |> Element.create |> Element.withClass styleName
        let xAxisLine = toLine (toPoint (left, 0)) (toPoint (right, 0))
        let yAxisLine = toLine (toPoint (0, bottom)) (toPoint (0, top))

        [ axisStyle; xAxisLine; yAxisLine; ]


    let drawSeries graph =
        let viewBoxArea = Area.ofFloats (canvasSize, canvasSize)
        let viewBox = ViewBox.create Point.origin viewBoxArea
        let style = Style.create (Color.ofName Colors.Black) (Color.ofName Colors.Black) (Length.ofInt 3) 1.0 1.0

        let points = graph.Series |> List.map (fun point -> point |> (toScaledSvgCoordinates graph) |> Point.ofFloats)
        let circles = points |> List.map (fun point -> Circle.create point (Length.ofInt 3) |> Element.createWithStyle style)
        let svg = (drawAxis graph) |> (circles |> List.append) |> Svg.ofList |> Svg.withViewBox viewBox
        let html = svg |> Svg.toHtml "Test"
        html
