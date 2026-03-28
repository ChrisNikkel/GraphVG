open System
open System.IO
open GraphVG

// ── Data ─────────────────────────────────────────────────────────────────────

// A sine wave sampled at 40 points
let sinWave =
    [ for i in 0 .. 39 ->
        let x = float i * Math.PI / 10.0
        x, Math.Sin x ]
    |> Series.line
    |> Series.withLabel "sin(x)"

// A cosine wave
let cosWave =
    [ for i in 0 .. 39 ->
        let x = float i * Math.PI / 10.0
        x, Math.Cos x ]
    |> Series.line
    |> Series.withLabel "cos(x)"

// A few highlighted scatter points at peaks/troughs
let peaks =
    [ Math.PI / 2.0, 1.0
      3.0 * Math.PI / 2.0, -1.0
      5.0 * Math.PI / 2.0, 1.0
      7.0 * Math.PI / 2.0, -1.0 ]
    |> Series.scatter
    |> Series.withLabel "peaks"

// ── Scales ───────────────────────────────────────────────────────────────────

let xDomain = 0.0, 4.0 * Math.PI          // one full period shown
let yDomain = -1.2, 1.2                    // a little padding above/below ±1

let xScale = Scale.linear xDomain (0.0, Graph.canvasSize)
let yScale = Scale.linear yDomain (Graph.canvasSize, 0.0)   // y inverted for SVG

printfn "X ticks: %A" (Scale.ticks xScale 5)
printfn "Y ticks: %A" (Scale.ticks yScale 5)

// ── Axes ─────────────────────────────────────────────────────────────────────

let xAxis =
    Axis.create Bottom xScale
    |> Axis.withTicks 5
    |> Axis.withLabel "x (radians)"

let yAxis =
    Axis.create Left yScale
    |> Axis.withTicks 5
    |> Axis.withLabel "amplitude"

// ── Theme ────────────────────────────────────────────────────────────────────

// Available presets: Theme.empty  Theme.light  Theme.dark  Theme.turtle
// Custom builder:
//   Theme.empty
//   |> Theme.withBackground (Color.ofName White)
//   |> Theme.withAxisPen Pen.dimGray
//   |> Theme.withGridPen Pen.lightGray

let theme = Theme.light

// ── Graph ────────────────────────────────────────────────────────────────────
// Note: Graph.createWithSeries/addSeries auto-computes bounds from data.
// REQ-5 will wire custom scales, axes, and theme directly into the Graph record.

let graph =
    Graph.createWithSeries sinWave
    |> Graph.addSeries cosWave
    |> Graph.addSeries peaks

// ── Render ───────────────────────────────────────────────────────────────────

let html = GraphVG.drawSeries graph

let outPath =
    Path.Combine(AppContext.BaseDirectory, "output.html")

File.WriteAllText(outPath, html)
printfn "\nOutput written to:\n  %s" outPath
printfn "Open in a browser to view the graph."
