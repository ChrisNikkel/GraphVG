module PlotCharts

open System
open GraphVG

// Helper: parse or fail loudly in the example runner.
let private expr s =
    match Plot.parse s with
    | Ok e -> e
    | Error msg -> failwithf "Plot.parse failed for %s: %s" s msg

// sin(x) and cos(x) over two full periods
let trigGraph =
    let domain = (-2.0 * Math.PI, 2.0 * Math.PI)
    let sinSeries = expr "sin(x)" |> Plot.toSeries domain 400 |> Series.withLabel "sin(x)"
    let cosSeries = expr "cos(x)" |> Plot.toSeries domain 400 |> Series.withLabel "cos(x)"
    Graph.create [ sinSeries; cosSeries ] domain (-1.3, 1.3)
    |> Graph.withTheme Theme.light
    |> Graph.withTitle "sin(x) and cos(x)"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Center)
    |> Graph.withAxes (
        Some (Axis.create Bottom (Scale.linear domain (0.0, CommonMath.canvasSize))),
        Some (Axis.create Left  (Scale.linear (-1.3, 1.3) (CommonMath.canvasSize, 0.0))))

// tan(x) — demonstrates discontinuity handling over one period
let tanGraph =
    let domain = (-Math.PI / 2.0 + 0.05, Math.PI / 2.0 * 3.0 - 0.05)
    let tanExpr = expr "tan(x)"
    let range = Plot.autoRange domain tanExpr
    let series = tanExpr |> Plot.toSeries domain 600 |> Series.withLabel "tan(x)"
    Graph.create [ series ] domain range
    |> Graph.withTheme Theme.light
    |> Graph.withTitle "tan(x) with asymptote breaks"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Center)
    |> Graph.withAxes (
        Some (Axis.create Bottom (Scale.linear domain (0.0, CommonMath.canvasSize))),
        Some (Axis.create Left  (Scale.linear range (CommonMath.canvasSize, 0.0))))

// Gaussian bell curve e^(-x^2) and its derivative
let gaussianGraph =
    let domain = (-3.5, 3.5)
    let gaussExpr = expr "exp(-x^2)"
    let derivExpr = Plot.derivative gaussExpr
    let gSeries = gaussExpr |> Plot.toSeries domain 400 |> Series.withLabel "exp(-x²)"
    let dSeries = derivExpr |> Plot.toSeries domain 400 |> Series.withLabel "d/dx exp(-x²)"
    Graph.create [ gSeries; dSeries ] domain (-1.0, 1.2)
    |> Graph.withTheme Theme.light
    |> Graph.withTitle "Gaussian and its derivative"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Center)
    |> Graph.withAxes (
        Some (Axis.create Bottom (Scale.linear domain (0.0, CommonMath.canvasSize))),
        Some (Axis.create Left  (Scale.linear (-1.0, 1.2) (CommonMath.canvasSize, 0.0))))
