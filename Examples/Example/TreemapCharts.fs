module TreemapCharts

open GraphVG

// Fictional portfolio allocation by asset class.
let portfolioGraph =
    Series.treemap [
        "Equities",    52.0
        "Bonds",       23.0
        "Real Estate", 12.0
        "Commodities",  8.0
        "Cash",         5.0
    ]
    |> Graph.createWithSeries
    |> Graph.withTheme Theme.light
    |> Graph.withTitle "Portfolio Allocation"

// Fictional federal budget breakdown by department.
let budgetGraph =
    Series.treemap [
        "Defence",        18.0
        "Health",         25.0
        "Education",      12.0
        "Infrastructure",  9.0
        "Social Support", 22.0
        "Research",        5.0
        "Other",           9.0
    ]
    |> Graph.createWithSeries
    |> Graph.withTheme Theme.dark
    |> Graph.withTitle "Federal Budget by Department"
