namespace GraphVG

open SharpVG

module Axis =

    let draw (graph : Graph) =
        // TODO: Add tick marks and labels
        let left, right = graph.Domain
        let bottom, top = graph.Range

        let styleName = "axisStyle"
        let axisStyle = Style.create (Color.ofName Colors.Black) (Color.ofName Colors.Blue) (Length.ofInt 3) 1.0 1.0 |> Style.withName styleName |> Element.create
        let toPoint (x, y) = Point.ofFloats (Graph.toScaledSvgCoordinates graph (x, y))
        let toLine point1 point2 = Line.create point1 point2 |> Element.create |> Element.withClass styleName
        let xAxisLine = toLine (toPoint (left, 0)) (toPoint (right, 0))
        let yAxisLine = toLine (toPoint (0, bottom)) (toPoint (0, top))

        [ axisStyle; xAxisLine; yAxisLine; ]
