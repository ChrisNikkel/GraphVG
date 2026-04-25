module FunnelCharts

open GraphVG

// Fictional e-commerce conversion funnel showing drop-off at each stage.
let conversionFunnelGraph =
    Series.funnel [
        "Site Visits",      8_400.0
        "Product Views",    5_200.0
        "Add to Cart",      2_100.0
        "Checkout",           980.0
        "Purchase",           310.0
    ]
    |> Graph.createWithSeries
    |> Graph.withTheme Theme.light
    |> Graph.withTitle "E-Commerce Conversion Funnel"

// Fictional SaaS sales pipeline with six qualification stages.
let salesPipelineGraph =
    Series.funnel [
        "Leads",           1_050.0
        "Qualified",         620.0
        "Demo Scheduled",    390.0
        "Proposal Sent",     210.0
        "Contract Review",   115.0
        "Closed Won",         68.0
    ]
    |> Graph.createWithSeries
    |> Graph.withTheme Theme.dark
    |> Graph.withTitle "Sales Pipeline — Q2"
