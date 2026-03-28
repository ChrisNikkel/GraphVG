open GraphVG

let series1 = Series.scatter [ -1.0, -1.0; 1.0, 1.0; 2.0, 2.0; 4.0, 4.0; 8.0, 8.0; 10.0, 10.0 ]
let series2 = Series.scatter [ 4.5, 2.5; 6.0, 3.0; 7.0, 4.0 ]

let graph = Graph.createWithSeries series1 |> Graph.addSeries series2
let html = GraphVG.drawSeries graph

// Test by putting into https://codepen.io
printfn "%A" html

let pointsToString points =
    points
        |> List.map (fun (x, y) -> sprintf "(%A, %A), " x y)
        |> List.reduce (+)

printfn "Input: %A" (series1.Points |> pointsToString)
printfn "Domain: %A" graph.Domain
printfn "Range: %A"  graph.Range
printfn "(0, 0) -> %A" (Graph.toScaledSvgCoordinates graph (0.0, 0.0))
printfn "Output: %A" (series1.Points |> List.map (Graph.toScaledSvgCoordinates graph) |> pointsToString)
