module BulletCharts

open GraphVG

// Fictional quarterly KPI dashboard — three metrics against their targets.
let kpiGraph =
    Series.bullet [
        { Label = Some "Revenue ($M)"
          Actual = 275.0
          Target = 260.0
          Ranges = [
              { Threshold = 200.0; Label = Some "Poor" }
              { Threshold = 250.0; Label = Some "OK" }
              { Threshold = 300.0; Label = Some "Good" }
          ] }
        { Label = Some "Customer NPS"
          Actual = 42.0
          Target = 50.0
          Ranges = [
              { Threshold = 20.0; Label = Some "Poor" }
              { Threshold = 40.0; Label = Some "OK" }
              { Threshold = 70.0; Label = Some "Good" }
          ] }
        { Label = Some "Retention %"
          Actual = 88.0
          Target = 85.0
          Ranges = [
              { Threshold = 70.0; Label = Some "Poor" }
              { Threshold = 82.0; Label = Some "OK" }
              { Threshold = 95.0; Label = Some "Good" }
          ] }
    ]
    |> Graph.createWithSeries
    |> Graph.withTheme Theme.light
    |> Graph.withTitle "Q3 KPI Dashboard"

// Fictional manufacturing quality metrics — dark theme variant.
let manufacturingGraph =
    Series.bullet [
        { Label = Some "Yield Rate %"
          Actual = 96.2
          Target = 97.0
          Ranges = [
              { Threshold = 85.0; Label = None }
              { Threshold = 93.0; Label = None }
              { Threshold = 99.0; Label = None }
          ] }
        { Label = Some "Defects / 1000"
          Actual = 3.8
          Target = 3.0
          Ranges = [
              { Threshold = 2.0; Label = None }
              { Threshold = 5.0; Label = None }
              { Threshold = 8.0; Label = None }
          ] }
        { Label = Some "Uptime %"
          Actual = 91.5
          Target = 93.0
          Ranges = [
              { Threshold = 80.0; Label = None }
              { Threshold = 90.0; Label = None }
              { Threshold = 98.0; Label = None }
          ] }
        { Label = Some "OEE %"
          Actual = 78.3
          Target = 80.0
          Ranges = [
              { Threshold = 60.0; Label = None }
              { Threshold = 75.0; Label = None }
              { Threshold = 90.0; Label = None }
          ] }
    ]
    |> Graph.createWithSeries
    |> Graph.withTheme Theme.dark
    |> Graph.withTitle "Manufacturing Quality Metrics"
