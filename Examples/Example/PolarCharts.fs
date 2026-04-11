module PolarCharts

open GraphVG

// Fictional athletic combine results for two teams across six performance categories.
// Values are normalised scores on a 0–100 scale invented for this example.
let radarChartGraph =
    let axes = [ "Speed"; "Endurance"; "Strength"; "Agility"; "Reaction"; "Power" ]

    let teamAlpha =
        {
            Axes = axes
            Values = [ 88.0; 72.0; 65.0; 91.0; 78.0; 60.0 ]
        }

    let teamBeta =
        {
            Axes = axes
            Values = [ 64.0; 85.0; 80.0; 70.0; 68.0; 88.0 ]
        }

    RadarChart.create [ teamAlpha; teamBeta ]
    |> RadarChart.withTheme Theme.light
    |> RadarChart.withTitle "Athletic Combine — Team Alpha vs Team Beta"
    |> RadarChart.withLabels [ "Team Alpha"; "Team Beta" ]
    |> RadarChart.withRingCount 5
