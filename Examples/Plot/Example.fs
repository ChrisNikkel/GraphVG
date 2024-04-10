open GraphVG

let series = [(-1.0, -1.0);(1.0, 1.0);(2.0, 2.0);(4.0, 4.0);(8.0, 8.0);(10.0, 10.0)]

let seriesToString series =
        series
            |> List.map (fun (x, y) -> (sprintf "(%A, %A), " x y))
            |> List.reduce (+)

let graph = Graph.createWithSeries series
let html = GraphVG.drawSeries graph

printfn "%A"  html

printfn "Input: %A" (series |> seriesToString)
printfn "Domain: %A" (graph.Domain)
printfn "Range: %A" (graph.Domain)
printfn "Size: %A" (Graph.getDomainRangeSize (graph.Domain, graph.Range))
printfn "(0, 0) -> %A" (Graph.toScaledSvgCoordinates graph (0.0, 0.0))
printfn "Output: %A" (series |> List.map (Graph.toScaledSvgCoordinates graph) |> seriesToString)