open System
open System.IO
open GraphVG

// ── Data ─────────────────────────────────────────────────────────────────────

// Unit circle: x = cos(t), y = sin(t) — both axes span [-1, 1]
let unitCircle =
    [ for i in 0 .. 99 ->
        let t = float i * 2.0 * Math.PI / 100.0
        Math.Cos t, Math.Sin t ]
    |> Series.line
    |> Series.withLabel "unit circle"

// Lissajous figure scaled to fit inside the unit circle.
// Unscaled max radius is √2, so dividing by √2 keeps every point within radius 1.
let lissajous =
    let s = 1.0 / Math.Sqrt 2.0
    [ for i in 0 .. 199 ->
        let t = float i * 2.0 * Math.PI / 200.0
        s * Math.Sin(3.0 * t), s * Math.Sin(2.0 * t + Math.PI / 4.0) - 0.05 ]
    |> Series.line
    |> Series.withLabel "lissajous"

// ── Graph ────────────────────────────────────────────────────────────────────

let graph =
    Graph.createWithSeries unitCircle
    |> Graph.addSeries lissajous

// ── Render ───────────────────────────────────────────────────────────────────

let html = GraphVG.drawSeries graph

let outPath =
    Path.Combine(AppContext.BaseDirectory, "example.html")

File.WriteAllText(outPath, html)
printfn "\nOutput written to:\n  %s" outPath
printfn "Open in a browser to view the graph."
