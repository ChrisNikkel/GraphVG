open System
open System.IO
open GraphVG
open SharpVG

type ExamplePage =
    {
        FileName : string
        Title : string
        Description : string
        Graph : Graph
    }

let tau = 2.0 * Math.PI

let circlePoints sampleCount =
    [ for index in 0 .. sampleCount ->
        let parameter = float index * tau / float sampleCount
        Math.Cos parameter, Math.Sin parameter ]

let unitCircle =
    circlePoints 100
    |> Series.line
    |> Series.withLabel "Unit Circle"

let lissajous =
    let scale = 1.0 / Math.Sqrt 2.0
    [ for index in 0 .. 200 ->
        let parameter = float index * tau / 200.0
        scale * Math.Sin(3.0 * parameter), scale * Math.Sin(2.0 * parameter + Math.PI / 4.0) - 0.05 ]
    |> Series.line
    |> Series.withLabel "Lissajous"
    |> Series.withStrokeDash Dashed

let centeredAxesGraph =
    let xScale = Scale.linear (-1.2, 1.2) (0.0, Canvas.canvasSize)
    let yScale = Scale.linear (-1.2, 1.2) (Canvas.canvasSize, 0.0)
    Graph.create [ unitCircle; lissajous ] (-1.2, 1.2) (-1.2, 1.2)
    |> Graph.withTheme Theme.light
    |> Graph.withTitle "Centered Axes"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Middle)
    |> Graph.withAxes (
        Some (Axis.create (HorizontalAt (Scale.apply yScale 0.0)) xScale |> Axis.withTickInterval 0.5 |> Axis.hideOrigin),
        Some (Axis.create (VerticalAt (Scale.apply xScale 0.0)) yScale |> Axis.withTickInterval 0.5 |> Axis.hideOrigin))

let wavePoints phase =
    [ for index in 0 .. 160 ->
        let x = -Math.PI + float index * (2.0 * Math.PI / 160.0)
        x, Math.Sin(x + phase) ]

let axisStylesGraph =
    let months =
        [ 1.0, 18.0; 2.0, 24.0; 3.0, 29.0; 4.0, 39.0; 5.0, 43.0; 6.0, 52.0; 7.0, 61.0; 8.0, 68.0; 9.0, 74.0; 10.0, 82.0; 11.0, 87.0; 12.0, 94.0 ]
    let target =
        [ 1.0, 20.0; 12.0, 80.0 ]
        |> Series.line
        |> Series.withLabel "Target Band"
        |> Series.withStrokeDash Dashed
        |> Series.withOpacity 0.55
    let growth =
        months
        |> Series.line
        |> Series.withLabel "Adoption"
        |> Series.withStrokeWidth (Length.ofFloat 4.0)
    let milestones =
        months
        |> Series.scatter
        |> Series.withLabel "Checkpoints"
        |> Series.withPointRadius (Length.ofFloat 7.0)
    let xScale = Scale.linear (1.0, 12.0) (0.0, Canvas.canvasSize)
    let yScale = Scale.linear (0.0, 100.0) (Canvas.canvasSize, 0.0)
    let monthFormatter value =
        [
            1.0, "Jan"
            2.0, "Feb"
            3.0, "Mar"
            4.0, "Apr"
            5.0, "May"
            6.0, "Jun"
            7.0, "Jul"
            8.0, "Aug"
            9.0, "Sep"
            10.0, "Oct"
            11.0, "Nov"
            12.0, "Dec"
        ]
        |> List.tryPick (fun (tickValue, label) -> if CommonMath.isNear tickValue value then Some label else None)
        |> Option.defaultValue ""
    let percentFormatter value = sprintf "%.0f%%" value
    let themed =
        Theme.light
        |> Theme.withPlotBackground (Color.ofName HoneyDew)
        |> Theme.withPens [ Pen.seaGreen; Pen.tomato; Pen.steelBlue ]
        |> Theme.withGridPen (Pen.lightGray |> Pen.withOpacity 0.35)
    Graph.create [ target; growth; milestones ] (1.0, 12.0) (0.0, 100.0)
    |> Graph.withTheme themed
    |> Graph.withTitle "Axis Styling"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Middle)
    |> Graph.withAxes (
        Some (Axis.create Top xScale |> Axis.withTickInterval 2.0 |> Axis.withTickFormat monthFormatter |> Axis.withTickLength 10.0 |> Axis.withFontSize 14.0 |> Axis.withLabel "Campaign Timeline" |> Axis.hideBoundsTick |> Axis.hideBoundsLabel |> Axis.withSpine SpineStyle.Full),
        Some (Axis.create Right yScale |> Axis.withTicks 6 |> Axis.withTickFormat percentFormatter |> Axis.withFontSize 14.0 |> Axis.withLabel "Coverage" |> Axis.hideBoundsTick |> Axis.withSpine SpineStyle.Hidden))

let styledSeriesGraph =
    let baseline = 1.5
    let upperBand =
        [ for index in 0 .. 30 ->
            let x = float index / 3.0
            x, baseline + 2.0 + Math.Sin(x * 0.8) + 0.25 * Math.Cos(x * 2.4) ]
    let lowerBand = upperBand |> List.rev |> List.map (fun (x, _) -> x, baseline)
    let areaBand =
        upperBand @ lowerBand
        |> Series.area
        |> Series.withLabel "Band"
        |> Series.withOpacity 0.28
    let trendLine =
        upperBand
        |> Series.line
        |> Series.withLabel "Trend"
        |> Series.withStrokeWidth (Length.ofFloat 4.0)
        |> Series.withStrokeDash DashDot
    let highlights =
        upperBand
        |> List.indexed
        |> List.choose (fun (index, point) -> if index % 5 = 0 then Some point else None)
        |> Series.scatter
        |> Series.withLabel "Samples"
        |> Series.withPointRadius (Length.ofFloat 8.0)
    let themed =
        Theme.light
        |> Theme.withPlotBackground (Color.ofName FloralWhite)
        |> Theme.withPens [ Pen.steelBlue; Pen.tomato; Pen.darkGoldenRod ]
    Graph.create [ areaBand; trendLine; highlights ] (0.0, 10.0) (0.0, 5.5)
    |> Graph.withTheme themed
    |> Graph.withTitle "Series Styling"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Middle)

let logScaleGraph =
    let responsePoints =
        [ 1.0, 1.0; 2.0, 1.4; 5.0, 2.0; 10.0, 2.6; 20.0, 3.2; 50.0, 4.0; 100.0, 4.7; 200.0, 5.2; 500.0, 6.0; 1000.0, 6.6 ]
    let lineSeries =
        responsePoints
        |> Series.line
        |> Series.withLabel "Response"
        |> Series.withStrokeWidth (Length.ofFloat 3.5)
    let markerSeries =
        responsePoints
        |> Series.scatter
        |> Series.withLabel "Samples"
        |> Series.withPointRadius (Length.ofFloat 6.0)
    let xScale = Scale.log (1.0, 1000.0) (0.0, Canvas.canvasSize) 10.0
    let yScale = Scale.linear (0.0, 7.0) (Canvas.canvasSize, 0.0)
    let logTickFormatter value = sprintf "10^%.0f" (Math.Log10 value)
    Graph.create [ lineSeries; markerSeries ] (1.0, 1000.0) (0.0, 7.0)
    |> Graph.withTheme Theme.empty
    |> Graph.withTitle "Log Scale"
    |> Graph.withTitleStyle (TitleStyle.create 22.0 Middle)
    |> Graph.withXScale xScale
    |> Graph.withYScale yScale
    |> Graph.withAxes (
        Some (Axis.create Bottom xScale |> Axis.withTickFormat logTickFormatter |> Axis.withLabel "Input Scale"),
        Some (Axis.create Left yScale |> Axis.withLabel "Response"))

let examples =
    [
        {
            FileName = "centered-axes.html"
            Title = "Centered Axes"
            Description = "HorizontalAt and VerticalAt axes with two styled line series."
            Graph = centeredAxesGraph
        }
        {
            FileName = "axis-styles.html"
            Title = "Axis Styling"
            Description = "Custom tick intervals, formatted labels, plot background, and non-default spine styles."
            Graph = axisStylesGraph
        }
        {
            FileName = "series-styles.html"
            Title = "Series Styling"
            Description = "Area, line, and scatter rendering with dash styles, opacity, point radius, and custom pens."
            Graph = styledSeriesGraph
        }
        {
            FileName = "log-scale.html"
            Title = "Log Scale"
            Description = "Logarithmic x-axis with explicit axis configuration and tick formatting."
            Graph = logScaleGraph
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
    + "<p class=\"intro\">A small set of focused examples covering centered axes, axis styling, series styling, plot backgrounds, and logarithmic scales.</p>\n"
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
