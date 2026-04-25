module LollipopCharts

open GraphVG

// Fictional quarterly customer satisfaction scores by department.
let satisfactionGraph =
    Series.lollipop [ 1.0, 87.0; 2.0, 74.0; 3.0, 91.0; 4.0, 68.0; 5.0, 83.0; 6.0, 95.0 ]
    |> Series.withLabel "Score"
    |> Graph.createWithSeries
    |> Graph.withTheme Theme.light
    |> Graph.withTitle "Customer Satisfaction by Department"

// Fictional average monthly rainfall — horizontal lollipop variant.
let rainfallGraph =
    Series.horizontalLollipop
        [ 62.0, 1.0; 54.0, 2.0; 71.0, 3.0; 88.0, 4.0
          113.0, 5.0; 97.0, 6.0; 45.0, 7.0; 38.0, 8.0
          51.0, 9.0; 73.0, 10.0; 90.0, 11.0; 68.0, 12.0 ]
    |> Series.withLabel "Rainfall (mm)"
    |> Graph.createWithSeries
    |> Graph.withTheme Theme.light
    |> Graph.withTitle "Average Monthly Rainfall"

// Two overlaid lollipop series — prior year vs current year scores.
let comparisonGraph =
    let prior =
        Series.lollipop [ 1.0, 78.0; 2.0, 82.0; 3.0, 71.0; 4.0, 88.0; 5.0, 65.0 ]
        |> Series.withLabel "2023"
        |> Series.withOpacity 0.5
    let current =
        Series.lollipop [ 1.0, 85.0; 2.0, 79.0; 3.0, 90.0; 4.0, 83.0; 5.0, 76.0 ]
        |> Series.withLabel "2024"
    Graph.create [ prior; current ] (0.5, 5.5) (0.0, 100.0)
    |> Graph.withTheme Theme.light
    |> Graph.withTitle "Scores 2023 vs 2024"
    |> Graph.withLegend (Legend.create LegendTop)
