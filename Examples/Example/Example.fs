open System
open System.IO
open GraphVG

// ── Data ─────────────────────────────────────────────────────────────────────

// Unit circle: x = cos(t), y = sin(t) — both axes span [-1, 1]
let unitCircle =
    [ for i in 0 .. 100 ->
        let t = float i * 2.0 * Math.PI / 100.0
        Math.Cos t, Math.Sin t ]
    |> Series.line
    |> Series.withLabel "unit circle"

// Lissajous figure scaled to fit inside the unit circle.
// Unscaled max radius is √2, so dividing by √2 keeps every point within radius 1.
let lissajous =
    let s = 1.0 / Math.Sqrt 2.0
    [ for i in 0 .. 200 ->
        let t = float i * 2.0 * Math.PI / 200.0
        s * Math.Sin(3.0 * t), s * Math.Sin(2.0 * t + Math.PI / 4.0) - 0.05 ]
    |> Series.line
    |> Series.withLabel "lissajous"

// ── Graph ────────────────────────────────────────────────────────────────────

let xScale = Scale.linear (-1.2, 1.2) (0.0, Canvas.canvasSize)
let yScale = Scale.linear (-1.2, 1.2) (Canvas.canvasSize, 0.0)

let graph =
    Graph.create [ unitCircle; lissajous ] (-1.2, 1.2) (-1.2, 1.2)
    |> Graph.withTheme Theme.light
    |> Graph.withAxes (
        Some (Axis.create (HorizontalAt (Scale.apply yScale 0.0)) xScale |> Axis.withTickInterval 0.5 |> Axis.hideOrigin),
        Some (Axis.create (VerticalAt   (Scale.apply xScale 0.0)) yScale |> Axis.withTickInterval 0.5 |> Axis.hideOrigin))

// ── Render ───────────────────────────────────────────────────────────────────

let html = GraphVG.toHtml graph

let outPath =
    Path.Combine(AppContext.BaseDirectory, "example.html")

File.WriteAllText(outPath, html)
printfn "\nOutput written to:\n  %s" outPath
printfn "Open in a browser to view the graph."
