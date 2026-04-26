module RidgelineCharts

open GraphVG

// Fictional quarterly response-time distributions (milliseconds) across server regions.
let private regionData =
    let rng = System.Random(17)
    let gauss mu sigma =
        let u1 = rng.NextDouble() + 1e-15
        let u2 = rng.NextDouble()
        let z = sqrt (-2.0 * log u1) * cos (2.0 * System.Math.PI * u2)
        mu + z * sigma
    let sample n mu sigma = [ for _ in 1 .. n -> gauss mu sigma ]
    [ "US-East",   1.0, sample 120 42.0 8.0
      "US-West",   2.0, sample 120 55.0 12.0
      "EU-West",   3.0, sample 120 68.0 10.0
      "AP-South",  4.0, sample 120 95.0 18.0
      "AP-East",   5.0, sample 120 110.0 22.0 ]

let responseTimeGraph =
    let series =
        regionData
        |> List.map (fun (label, y, values) ->
            Series.ridgeLine y values |> Series.withLabel label)
    Graph.create series (10.0, 160.0) (0.5, 5.9)
    |> Graph.withTheme Theme.light
    |> Graph.withTitle "Response Time Distribution by Region"

// Fictional exam score distributions across five cohorts — dark theme.
let private examData =
    let rng = System.Random(31)
    let gauss mu sigma =
        let u1 = rng.NextDouble() + 1e-15
        let u2 = rng.NextDouble()
        let z = sqrt (-2.0 * log u1) * cos (2.0 * System.Math.PI * u2)
        mu + z * sigma
    let sample n mu sigma = [ for _ in 1 .. n -> gauss mu sigma |> max 0.0 |> min 100.0 ]
    [ "2020", 1.0, sample 80 62.0 14.0
      "2021", 2.0, sample 80 65.0 12.0
      "2022", 3.0, sample 80 70.0 10.0
      "2023", 4.0, sample 80 68.0 11.0
      "2024", 5.0, sample 80 74.0  9.0 ]

let examScoreGraph =
    let series =
        examData
        |> List.map (fun (label, y, values) ->
            Series.ridgeLine y values |> Series.withLabel label)
    Graph.create series (20.0, 100.0) (0.5, 5.9)
    |> Graph.withTheme Theme.dark
    |> Graph.withTitle "Exam Score Distribution by Cohort"
