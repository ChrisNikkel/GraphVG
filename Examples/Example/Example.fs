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

// ── Scales ───────────────────────────────────────────────────────────────────

let domain = -1.2, 1.2
let range  = -1.2, 1.2

let xScale = Scale.linear domain (0.0, Graph.canvasSize)
let yScale = Scale.linear range  (Graph.canvasSize, 0.0)

// ── Axes ─────────────────────────────────────────────────────────────────────

let clamp lo hi v = max lo (min hi v)
let xAxisY = Scale.apply yScale 0.0 |> clamp 0.0 Graph.canvasSize
let yAxisX = Scale.apply xScale 0.0 |> clamp 0.0 Graph.canvasSize

let xAxis = Axis.create (HorizontalAt xAxisY) xScale |> Axis.withTicks 5
let yAxis = Axis.create (VerticalAt   yAxisX) yScale |> Axis.withTicks 5

// ── Graph ────────────────────────────────────────────────────────────────────

let graph =
    Graph.create [ unitCircle; lissajous ] domain range

// ── Render ───────────────────────────────────────────────────────────────────

let html = GraphVG.render graph Theme.light [ xAxis; yAxis ]

let outPath =
    Path.Combine(AppContext.BaseDirectory, "example.html")

File.WriteAllText(outPath, html)
printfn "\nOutput written to:\n  %s" outPath
printfn "Open in a browser to view the graph."
