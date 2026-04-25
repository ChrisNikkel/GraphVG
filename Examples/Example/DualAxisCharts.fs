module DualAxisCharts

open GraphVG

// Fictional MLB-style season: games 1–162, tracking wins and team batting average.
// Wins (integer, left axis) grow monotonically with occasional losses.
// Batting average (float, right axis) fluctuates through the season.

let private winsByGame =
    // Cumulative wins after each game played (162-game season)
    let mutable wins = 0
    [ for game in 1 .. 162 ->
        // ~56% win rate on average, with some hot/cold streaks
        let streak = (game / 10) % 3
        let winProb = if streak = 0 then 0.65 elif streak = 1 then 0.55 else 0.48
        let pseudo = (game * 1103515245 + 12345) % 65536
        if float pseudo / 65536.0 < winProb then wins <- wins + 1
        float game, float wins ]

let private battingAvgByGame =
    // Team batting average fluctuates around .265 with seasonal patterns
    [ for game in 1 .. 162 ->
        let t = float game / 162.0
        let seasonal = 0.010 * sin (t * System.Math.PI * 2.0)
        let slump = if game >= 80 && game <= 100 then -0.015 else 0.0
        let pseudo = float ((game * 214013 + 2531011) % 1000) / 1000.0
        let noise = (pseudo - 0.5) * 0.008
        float game, 0.265 + seasonal + slump + noise ]

let dualAxisGraph =
    let winsLine =
        Series.line winsByGame
        |> Series.withLabel "Wins"
        |> Series.withStrokeWidth 2.0

    let battingLine =
        Series.line battingAvgByGame
        |> Series.withLabel "Batting Avg"
        |> Series.withStrokeDash Dashed
        |> Series.withStrokeWidth 1.5
        |> Series.onRightAxis

    let g = Graph.create [ winsLine; battingLine ] (1.0, 162.0) (0.0, 95.0)
    let cs = Graph.canvasSizeOf g
    g
    |> Graph.withRightYRange (0.230, 0.300)
    |> Graph.withXAxis (Some (Axis.create Bottom (Scale.linear (1.0, 162.0) (0.0, cs)) |> Axis.withLabel "Game"))
    |> Graph.withYAxis (Some (Axis.create AxisPosition.Left (Scale.linear (0.0, 95.0) (cs, 0.0)) |> Axis.withLabel "Wins"))
    |> Graph.withRightAxis (
        Some (Axis.create AxisPosition.Right (Scale.linear (0.230, 0.300) (cs, 0.0))
              |> Axis.withLabel "Batting Average"
              |> Axis.withTickFormat (sprintf "%.3f")))
    |> Graph.withTheme Theme.light
    |> Graph.withTitle "Season Performance: Wins vs. Batting Average"
    |> Graph.withLegend (Legend.create LegendTop)
