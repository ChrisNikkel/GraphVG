open System
open System.IO
open GraphVG

type ExamplePage =
    {
        FileName : string
        Title : string
        Description : string
        Graph : Graph
    }

let examples =
    [
        {
            FileName = "centered-axes.html"
            Title = "Centered Axes"
            Description = "HorizontalAt and VerticalAt axes with two styled line series."
            Graph = LineCharts.centeredAxesGraph
        }
        {
            FileName = "axis-styles.html"
            Title = "Axis Styling"
            Description = "Custom tick intervals, formatted labels, plot background, and non-default spine styles."
            Graph = LineCharts.axisStylesGraph
        }
        {
            FileName = "series-styles.html"
            Title = "Series Styling"
            Description = "Area, line, and scatter rendering with dash styles, opacity, point radius, and custom pens."
            Graph = LineCharts.styledSeriesGraph
        }
        {
            FileName = "log-scale.html"
            Title = "Log Scale"
            Description = "Logarithmic x-axis with explicit axis configuration and tick formatting."
            Graph = LineCharts.logScaleGraph
        }
        {
            FileName = "stacked-area.html"
            Title = "Stacked Area"
            Description = "US electricity generation 2014–2024 by source (EIA data): coal declining, natural gas dominant, wind and solar surging."
            Graph = AreaCharts.stackedAreaGraph
        }
        {
            FileName = "stacked-area-percent.html"
            Title = "Normalized Stacked Area"
            Description = "Same EIA electricity data normalized to 100%, showing each source's share of total generation over time."
            Graph = AreaCharts.normalizedStackedAreaGraph
        }
        {
            FileName = "streamgraph.html"
            Title = "Streamgraph"
            Description = "Annual console hardware sales 1995–2012: PS2 dominance, the Wii explosion, the DS tsunami, and Sega's dramatic exit."
            Graph = AreaCharts.streamgraphGraph
        }
        {
            FileName = "bar-chart.html"
            Title = "Grouped Bar Chart"
            Description = "Three product lines compared across four quarters — vertical grouped bars with a shared x-axis category."
            Graph = BarCharts.barChartGraph
        }
        {
            FileName = "horizontal-bar.html"
            Title = "Horizontal Bar Chart"
            Description = "Average daily screen time by app category, sorted by usage — horizontal bars for easy label reading."
            Graph = BarCharts.horizontalBarGraph
        }
        {
            FileName = "histogram.html"
            Title = "Histogram"
            Description = "300 normally distributed samples binned automatically using Sturges' rule."
            Graph = DistributionCharts.histogramGraph
        }
        {
            FileName = "box-plot.html"
            Title = "Box Plot"
            Description = "Three groups of 80 samples showing median, quartiles, and whiskers."
            Graph = DistributionCharts.boxPlotGraph
        }
    ]

let galleryHtml pages =
    let cards =
        pages
        |> List.map (fun page ->
            let svg = GraphVG.toSvg page.Graph
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
    + "<p class=\"intro\">A collection of focused examples covering line charts, stacked and normalized area, streamgraphs, bar charts, histograms, and box plots.</p>\n"
    + "<section class=\"grid\">\n"
    + cards + "\n"
    + "</section>\n"
    + "</main>\n"
    + "</body>\n"
    + "</html>\n"

let examplePageHtml homeFileName page =
    let svg = GraphVG.toSvg page.Graph
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
