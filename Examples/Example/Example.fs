open System
open System.IO
open GraphVG

type ExamplePage =
    {
        FileName : string
        Title : string
        Description : string
        Render : unit -> string
    }

let examples =
    [
        {
            FileName = "centered-axes.html"
            Title = "Centered Axes"
            Description = "HorizontalAt and VerticalAt axes with two styled line series."
            Render = fun () -> GraphVG.toSvg LineCharts.centeredAxesGraph
        }
        {
            FileName = "axis-styles.html"
            Title = "Axis Styling"
            Description = "Custom tick intervals, formatted labels, plot background, and non-default spine styles."
            Render = fun () -> GraphVG.toSvg LineCharts.axisStylesGraph
        }
        {
            FileName = "series-styles.html"
            Title = "Series Styling"
            Description = "Area, line, and scatter rendering with dash styles, opacity, point radius, and custom pens."
            Render = fun () -> GraphVG.toSvg LineCharts.styledSeriesGraph
        }
        {
            FileName = "step-line.html"
            Title = "Step Line"
            Description = "Electricity rate tiers that change at fixed hours — three step modes (After, Before, Mid) overlaid so you can compare the geometry."
            Render = fun () -> GraphVG.toSvg LineCharts.stepLineGraph
        }
        {
            FileName = "confidence-band.html"
            Title = "Confidence Band"
            Description = "Monthly mean temperature with a ±1σ uncertainty band layered under the mean line — a common pattern for showing forecast ranges or confidence intervals."
            Render = fun () -> GraphVG.toSvg LineCharts.bandGraph
        }
        {
            FileName = "log-scale.html"
            Title = "Log Scale"
            Description = "Logarithmic x-axis with explicit axis configuration and tick formatting."
            Render = fun () -> GraphVG.toSvg LineCharts.logScaleGraph
        }
        {
            FileName = "stacked-area.html"
            Title = "Stacked Area"
            Description = "US electricity generation 1990–2024 by source (EIA data): coal's mountain arc, the fracking gas surge, petroleum's near-disappearance, and renewables emerging from zero."
            Render = fun () -> GraphVG.toSvg AreaCharts.stackedAreaGraph
        }
        {
            FileName = "stacked-area-percent.html"
            Title = "Normalized Stacked Area"
            Description = "Same EIA electricity data 1990–2024 normalized to 100%, showing coal's share halving, gas doubling, and renewables rising from nothing."
            Render = fun () -> GraphVG.toSvg AreaCharts.normalizedStackedAreaGraph
        }
        {
            FileName = "streamgraph.html"
            Title = "Streamgraph"
            Description = "Annual console hardware sales 1995–2012: PS2 dominance, the Wii explosion, the DS tsunami, and Sega's dramatic exit."
            Render = fun () -> GraphVG.toSvg AreaCharts.streamgraphGraph
        }
        {
            FileName = "bar-chart.html"
            Title = "Grouped Bar Chart"
            Description = "Three product lines compared across four quarters — vertical grouped bars with a shared x-axis category."
            Render = fun () -> GraphVG.toSvg BarCharts.barChartGraph
        }
        {
            FileName = "waterfall.html"
            Title = "Waterfall Chart"
            Description = "Annual cash flow bridge showing starting balance, revenue inflows, and cost outflows — total bars drawn from zero, connector lines link consecutive bars."
            Render = fun () -> GraphVG.toSvg BarCharts.waterfallGraph
        }
        {
            FileName = "pie-chart.html"
            Title = "Pie Chart"
            Description = "Global electricity generation mix in 2023 — six sources sized by share, with inside labels."
            Render = fun () -> GraphVG.toSvg BarCharts.pieChartGraph
        }
        {
            FileName = "parallel-sets.html"
            Title = "Parallel Sets"
            Description = "Fictional retail traffic across three categorical dimensions — acquisition source, device type, and purchase outcome — with curved ribbons sized by visitor count."
            Render = fun () -> GraphVG.toSvg BarCharts.parallelSetsGraph
        }
        {
            FileName = "horizontal-bar.html"
            Title = "Horizontal Bar Chart"
            Description = "Average daily screen time by app category, sorted by usage — horizontal bars for easy label reading."
            Render = fun () -> GraphVG.toSvg BarCharts.horizontalBarGraph
        }
        {
            FileName = "tooltips.html"
            Title = "Tooltips"
            Description = "Scatter chart with per-point SVG title tooltips — hover any point to see temperature and pressure values. No JavaScript required."
            Render = fun () -> GraphVG.toSvg ScatterCharts.tooltipScatterGraph
        }
        {
            FileName = "bubble-chart.html"
            Title = "Bubble Chart"
            Description = "GDP per capita vs life expectancy for 16 countries across four continents — bubble area encodes population size."
            Render = fun () -> GraphVG.toSvg ScatterCharts.bubbleChartGraph
        }
        {
            FileName = "heatmap.html"
            Title = "Heatmap"
            Description = "Weekly step counts by hour of day — white-to-steelblue color scale shows morning and evening activity peaks with a muted weekend pattern."
            Render = fun () -> GraphVG.toSvg ScatterCharts.heatmapGraph
        }
        {
            FileName = "histogram.html"
            Title = "Histogram"
            Description = "300 normally distributed samples binned automatically using Sturges' rule."
            Render = fun () -> GraphVG.toSvg DistributionCharts.histogramGraph
        }
        {
            FileName = "violin-plot.html"
            Title = "Violin Plot"
            Description = "Three groups of 120 samples showing KDE shape, median, IQR box, and whiskers — wider regions are where data is more concentrated."
            Render = fun () -> GraphVG.toSvg DistributionCharts.violinPlotGraph
        }
        {
            FileName = "box-plot.html"
            Title = "Box Plot"
            Description = "Three groups of 80 samples showing median, quartiles, and whiskers."
            Render = fun () -> GraphVG.toSvg DistributionCharts.boxPlotGraph
        }
        {
            FileName = "radar-chart.html"
            Title = "Radar Chart"
            Description = "Fictional athletic combine results comparing two teams across six performance categories — filled polygons with 20% opacity overlaid on a 5-ring web."
            Render = fun () -> RadarChart.toSvg PolarCharts.radarChartGraph
        }
        {
            FileName = "pie-chart-standalone.html"
            Title = "Pie Chart"
            Description = "Fictional Q1 regional sales breakdown — five regions sized proportionally with inside percentage labels and a color-keyed legend."
            Render = fun () -> PieChart.toSvg PolarCharts.pieChartGraph
        }
        {
            FileName = "donut-chart.html"
            Title = "Donut Chart"
            Description = "Same fictional Q1 regional sales data as a donut, with the total displayed in the center hole."
            Render = fun () -> PieChart.toSvg PolarCharts.donutChartGraph
        }
        {
            FileName = "plot-trig.html"
            Title = "Trig Functions"
            Description = "sin(x) and cos(x) plotted from expressions — two full periods with auto-sampled points."
            Render = fun () -> GraphVG.toSvg PlotCharts.trigGraph
        }
        {
            FileName = "plot-tan.html"
            Title = "tan(x) — Discontinuities"
            Description = "tan(x) with path breaks at the vertical asymptotes — no lines drawn through discontinuities."
            Render = fun () -> GraphVG.toSvg PlotCharts.tanGraph
        }
        {
            FileName = "plot-gaussian.html"
            Title = "Gaussian + Derivative"
            Description = "exp(-x²) alongside its symbolic derivative — computed automatically via Plot.derivative."
            Render = fun () -> GraphVG.toSvg PlotCharts.gaussianGraph
        }
        {
            FileName = "theme-gameboy-green.html"
            Title = "Theme: Game Boy Green"
            Description = "The original DMG-001 four-shade green palette — darkest background with the two brightest greens cycling through series."
            Render = fun () -> GraphVG.toSvg RetroCharts.gameboyGreenGraph
        }
        {
            FileName = "theme-crispy-commodore.html"
            Title = "Theme: Crispy Commodore"
            Description = "Commodore 64 default screen colors — deep blue-purple background with the C64's yellow, cyan, and light-green palette."
            Render = fun () -> GraphVG.toSvg RetroCharts.crispyCommodoreGraph
        }
        {
            FileName = "theme-ti-hue.html"
            Title = "Theme: TI Hue"
            Description = "TI-84 Plus CE calculator aesthetic — dark navy with the bright function colors the calc assigns to Y1 through Y6."
            Render = fun () -> GraphVG.toSvg RetroCharts.tiHueGraph
        }
        {
            FileName = "theme-nes.html"
            Title = "Theme: NES"
            Description = "NES PPU hardware palette — black background with authentic NES blue, red, green, yellow, sky-blue, and purple."
            Render = fun () -> GraphVG.toSvg RetroCharts.nesGraph
        }
        {
            FileName = "theme-turtle.html"
            Title = "Theme: Turtle"
            Description = "Logo turtle graphics aesthetic — single bright green on black, no grid."
            Render = fun () -> GraphVG.toSvg RetroCharts.turtleGraph
        }
        {
            FileName = "dual-axis.html"
            Title = "Dual Y-Axis"
            Description = "Fictional 162-game baseball season: cumulative wins on the left axis, team batting average on the right — two independent scales on one chart."
            Render = fun () -> GraphVG.toSvg DualAxisCharts.dualAxisGraph
        }
        {
            FileName = "funnel-conversion.html"
            Title = "Funnel Chart"
            Description = "Fictional e-commerce conversion funnel: each trapezoid stage shows how many visitors remain after each step, from site visit to purchase."
            Render = fun () -> GraphVG.toSvg FunnelCharts.conversionFunnelGraph
        }
        {
            FileName = "funnel-pipeline.html"
            Title = "Sales Pipeline Funnel"
            Description = "Fictional SaaS sales pipeline showing six qualification stages from initial leads to closed-won deals."
            Render = fun () -> GraphVG.toSvg FunnelCharts.salesPipelineGraph
        }
    ]

let galleryHtml pages =
    let cards =
        pages
        |> List.map (fun page ->
            let svg = page.Render ()
            "<article class=\"card\">"
            + "<a class=\"frame\" href=\"" + page.FileName + "\">" + svg + "</a>"
            + "<div class=\"copy\">"
            + "<h2>" + page.Title + "</h2>"
            + "<p>" + page.Description + "</p>"
            + "<a href=\"" + page.FileName + "\">Open full page</a>"
            + "</div>"
            + "</article>")
        |> String.concat "\n"
    let css =
        "html,body{margin:0;padding:0;background:#f3efe7;color:#1f2a31;font-family:Georgia,\"Iowan Old Style\",serif;}"
        + "body{padding:40px 24px 56px;}"
        + ".wrap{max-width:1320px;margin:0 auto;}"
        + "h1{margin:0 0 12px;font-size:44px;line-height:1.05;}"
        + ".intro{max-width:760px;font-size:18px;line-height:1.5;margin:0 0 28px;color:#43505a;}"
        + ".grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:22px;}"
        + ".card{background:#fffdf8;border:1px solid #d8cfbf;border-radius:22px;overflow:hidden;box-shadow:0 18px 44px rgba(47,36,18,0.08);display:flex;flex-direction:column;}"
        + ".frame{display:block;padding:18px;background:linear-gradient(180deg,#f8f4ec 0%,#efe5d4 100%);border-bottom:1px solid #e2d8c8;cursor:pointer;}"
        + ".frame:hover{background:linear-gradient(180deg,#f0ead8 0%,#e5d8c4 100%);}"
        + ".frame svg{display:block;width:100%;height:auto;aspect-ratio:1/1;}"
        + ".copy{padding:18px 18px 20px;}"
        + ".copy h2{margin:0 0 10px;font-size:24px;}"
        + ".copy p{margin:0 0 14px;line-height:1.45;color:#5a6470;}"
        + ".copy a{color:#0d5c63;text-decoration:none;font-weight:600;}"
        + ".copy a:hover{text-decoration:underline;}"
        + "@media (max-width:720px){body{padding:24px 16px 40px;}h1{font-size:34px;}}"
    "<!DOCTYPE html>\n"
    + "<html>\n"
    + "<head>\n"
    + "<meta charset=\"utf-8\" />\n"
    + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />\n"
    + "<title>GraphVG Example Gallery</title>\n"
    + "<style>" + css + "</style>\n"
    + "</head>\n"
    + "<body>\n"
    + "<main class=\"wrap\">\n"
    + "<h1>GraphVG Example Gallery</h1>\n"
    + "<p class=\"intro\">A collection of focused examples covering line, step line, area, stacked and normalized area, streamgraphs, bar, waterfall, bubble, heatmap, histogram, box plot, violin plot, confidence band, radar, pie, and donut charts.</p>\n"
    + "<section class=\"grid\">\n"
    + cards + "\n"
    + "</section>\n"
    + "</main>\n"
    + "</body>\n"
    + "</html>\n"

let examplePageHtml homeFileName page =
    let svg = page.Render ()
    let css =
        "html,body{margin:0;padding:0;background:#f3efe7;color:#1f2a31;font-family:Georgia,\"Iowan Old Style\",serif;}"
        + "body{padding:28px 18px 36px;}"
        + ".wrap{max-width:1180px;margin:0 auto;}"
        + ".nav{display:inline-flex;align-items:center;gap:8px;color:#0d5c63;text-decoration:none;font-weight:700;letter-spacing:0.02em;margin-bottom:20px;}"
        + ".nav:hover{text-decoration:underline;}"
        + "h1{margin:0 0 10px;font-size:40px;line-height:1.05;}"
        + ".intro{max-width:760px;margin:0 0 22px;color:#53616c;font-size:18px;line-height:1.45;}"
        + ".panel{background:#fffdf8;border:1px solid #d8cfbf;border-radius:24px;padding:18px;box-shadow:0 18px 44px rgba(47,36,18,0.08);}"
        + ".panel svg{display:block;width:min(100%,980px);height:auto;margin:0 auto;aspect-ratio:1/1;}"
        + "@media (max-width:720px){body{padding:20px 12px 28px;}h1{font-size:32px;}.intro{font-size:16px;}}"
    "<!DOCTYPE html>\n"
    + "<html>\n"
    + "<head>\n"
    + "<meta charset=\"utf-8\" />\n"
    + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />\n"
    + "<title>" + page.Title + "</title>\n"
    + "<style>" + css + "</style>\n"
    + "</head>\n"
    + "<body>\n"
    + "<main class=\"wrap\">\n"
    + "<a class=\"nav\" href=\"" + homeFileName + "\">← Back to examples</a>\n"
    + "<h1>" + page.Title + "</h1>\n"
    + "<p class=\"intro\">" + page.Description + "</p>\n"
    + "<section class=\"panel\">\n"
    + svg + "\n"
    + "</section>\n"
    + "</main>\n"
    + "</body>\n"
    + "</html>\n"

let writeExamplePage outputDirectory page =
    let outputPath = Path.Combine(outputDirectory, page.FileName)
    File.WriteAllText(outputPath, examplePageHtml "example.html" page)
    outputPath

let outputDirectory = AppContext.BaseDirectory

let writtenPages = examples |> List.map (writeExamplePage outputDirectory)

let galleryPath = Path.Combine(outputDirectory, "example.html")

File.WriteAllText(galleryPath, galleryHtml examples)

printfn "\nGallery written to:\n  %s" galleryPath
printfn "\nExample pages:"
writtenPages |> List.iter (printfn "  %s")

// Write SVG thumbnails to docs/examples/ and update README.md gallery section
let repoRoot = Directory.GetCurrentDirectory()
let docsExamplesDir = Path.Combine(repoRoot, "docs", "examples")

let readmeGallery () =
    Directory.CreateDirectory(docsExamplesDir) |> ignore
    let svgNames =
        examples
        |> List.map (fun page ->
            let svgFileName = Path.GetFileNameWithoutExtension(page.FileName) + ".svg"
            let svgPath = Path.Combine(docsExamplesDir, svgFileName)
            File.WriteAllText(svgPath, page.Render ())
            svgFileName, page.Title)
    let cols = 3
    let rows =
        svgNames
        |> List.chunkBySize cols
        |> List.map (fun rowItems ->
            let cells =
                rowItems
                |> List.map (fun (svgFileName, title) ->
                    "<td align=\"center\" width=\"320\">"
                    + "<img src=\"docs/examples/" + svgFileName + "\" width=\"280\" alt=\"" + title + "\" />"
                    + "<br /><b>" + title + "</b>"
                    + "</td>")
            let padded = cells @ List.replicate (cols - cells.Length) "<td></td>"
            "<tr>" + String.concat "" padded + "</tr>")
    let table = "<table>\n" + String.concat "\n" rows + "\n</table>"
    let readmePath = Path.Combine(repoRoot, "README.md")
    let readme = File.ReadAllText(readmePath)
    let startMarker = "<!-- GALLERY:START -->"
    let endMarker = "<!-- GALLERY:END -->"
    let startIdx = readme.IndexOf(startMarker)
    let endIdx = readme.IndexOf(endMarker)
    if startIdx >= 0 && endIdx > startIdx then
        let before = readme.[0 .. startIdx + startMarker.Length - 1]
        let after = readme.[endIdx ..]
        let updated = before + "\n" + table + "\n" + after
        File.WriteAllText(readmePath, updated)
        printfn "\nREADME.md gallery updated (%d charts)" examples.Length
    else
        printfn "\nWARNING: README.md gallery markers not found — skipping README update"

readmeGallery ()
