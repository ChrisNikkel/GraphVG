module HexbinCharts

open GraphVG

// Fictional seismic event density — latitude/longitude offsets around a fault zone.
// Seeded pseudo-random generation for stable output.
let private seismicPoints =
    let rng = System.Random(42)
    let gauss () =
        let u1 = rng.NextDouble() + 1e-15
        let u2 = rng.NextDouble()
        sqrt (-2.0 * log u1) * cos (2.0 * System.Math.PI * u2)
    [ for _ in 1 .. 600 ->
        let cluster = rng.Next(3)
        match cluster with
        | 0 -> -1.2 + gauss () * 0.6, 0.5 + gauss () * 0.4
        | 1 -> 1.0 + gauss () * 0.5, -0.8 + gauss () * 0.5
        | _ -> 0.1 + gauss () * 1.2, 0.0 + gauss () * 0.9 ]

let densityGraph =
    Series.hexbin 0.3 seismicPoints
    |> Graph.createWithSeries
    |> Graph.withTheme Theme.light
    |> Graph.withTitle "Fictional Seismic Event Density"

// Fictional particle collision scatter — two overlapping gaussian sources.
let private collisionPoints =
    let rng = System.Random(7)
    let gauss () =
        let u1 = rng.NextDouble() + 1e-15
        let u2 = rng.NextDouble()
        sqrt (-2.0 * log u1) * cos (2.0 * System.Math.PI * u2)
    [ for _ in 1 .. 800 ->
        let source = rng.Next(2)
        if source = 0 then gauss () * 1.5, gauss () * 1.5
        else 0.5 + gauss () * 0.8, 0.5 + gauss () * 0.8 ]

let collisionGraph =
    Series.hexbin 0.25 collisionPoints
    |> Graph.createWithSeries
    |> Graph.withTheme Theme.dark
    |> Graph.withTitle "Fictional Particle Collision Density"
