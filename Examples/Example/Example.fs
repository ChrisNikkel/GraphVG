open GraphVG

let series1 = [(-1.0, -1.0);(1.0, 1.0);(2.0, 2.0);(4.0, 4.0);(8.0, 8.0);(10.0, 10.0)]
let series2 = [(4.5, 2.5);(6.0, 3.0);(7.0, 4.0);]

let seriesToString series =
        series
            |> List.map (fun (x, y) -> (sprintf "(%A, %A), " x y))
            |> List.reduce (+)

let graph = Graph.createWithSeries series1 |> Graph.addSeries series2
let html = GraphVG.drawSeries graph

// Test by putting into https://codepen.io
printfn "%A"  html

printfn "Input: %A" (series1 |> seriesToString)
printfn "Domain: %A" (graph.Domain)
printfn "Range: %A" (graph.Domain)
printfn "Size: %A" (Graph.getDomainRangeSize (graph.Domain, graph.Range))
printfn "(0, 0) -> %A" (Graph.toScaledSvgCoordinates graph (0.0, 0.0))
printfn "Output: %A" (series1 |> List.map (Graph.toScaledSvgCoordinates graph) |> seriesToString)