module Tests

open GraphVG
open CommonMath

open Xunit
open FsCheck.Xunit


[<Fact>]
let ``linear apply maps domain min to range min`` () =
    let scale = Scale.linear (0.0, 10.0) (0.0, 100.0)
    Assert.Equal(0.0, Scale.apply scale 0.0)

[<Fact>]
let ``linear apply maps domain max to range max`` () =
    let scale = Scale.linear (0.0, 10.0) (0.0, 100.0)
    Assert.Equal(100.0, Scale.apply scale 10.0)

[<Fact>]
let ``linear apply maps midpoint correctly`` () =
    let scale = Scale.linear (0.0, 10.0) (0.0, 100.0)
    Assert.Equal(50.0, Scale.apply scale 5.0)

[<Fact>]
let ``linear apply works with inverted range`` () =
    let scale = Scale.linear (0.0, 10.0) (100.0, 0.0)
    Assert.Equal(100.0, Scale.apply scale 0.0)
    Assert.Equal(0.0,   Scale.apply scale 10.0)

// Linear – invert

[<Fact>]
let ``linear invert is the inverse of apply`` () =
    let scale = Scale.linear (0.0, 10.0) (0.0, 100.0)
    let roundtrip value = value |> Scale.apply scale |> Scale.invert scale
    Assert.Equal(2.5, roundtrip 2.5)
    Assert.Equal(7.0, roundtrip 7.0)

// Linear – ticks

[<Fact>]
let ``linear ticks returns correct count`` () =
    let scale = Scale.linear (0.0, 10.0) (0.0, 100.0)
    Assert.Equal(5, Scale.ticks scale 5 |> List.length)

[<Fact>]
let ``linear ticks starts at domain min and ends at domain max`` () =
    let scale = Scale.linear (0.0, 10.0) (0.0, 100.0)
    let ticks = Scale.ticks scale 6
    Assert.Equal(0.0,  List.head ticks)
    Assert.Equal(10.0, List.last ticks)

[<Fact>]
let ``linear ticks count 1 returns domain min`` () =
    let scale = Scale.linear (2.0, 8.0) (0.0, 100.0)
    Assert.Equal<float list>([ 2.0 ], Scale.ticks scale 1)

// Log – apply

[<Fact>]
let ``log apply maps domain min to range min`` () =
    let scale = Scale.log (1.0, 1000.0) (0.0, 100.0) 10.0
    Assert.Equal(0.0, Scale.apply scale 1.0)

[<Fact>]
let ``log apply maps domain max to range max`` () =
    let scale = Scale.log (1.0, 1000.0) (0.0, 100.0) 10.0
    Assert.Equal(100.0, Scale.apply scale 1000.0, 10)

[<Fact>]
let ``log apply maps geometric midpoint to range midpoint`` () =
    let scale = Scale.log (1.0, 100.0) (0.0, 100.0) 10.0
    Assert.Equal(50.0, Scale.apply scale 10.0, 10)

// Log – invert

[<Fact>]
let ``log invert is the inverse of apply`` () =
    let scale = Scale.log (1.0, 1000.0) (0.0, 300.0) 10.0
    let roundtrip value = value |> Scale.apply scale |> Scale.invert scale
    Assert.Equal(10.0,  roundtrip 10.0,  10)
    Assert.Equal(100.0, roundtrip 100.0, 10)

// Log – ticks

[<Fact>]
let ``log ticks returns powers of base within domain`` () =
    let scale = Scale.log (1.0, 1000.0) (0.0, 100.0) 10.0
    Assert.Equal<float list>([ 1.0; 10.0; 100.0; 1000.0 ], Scale.ticks scale 0)

[<Fact>]
let ``log ticks base 2 returns correct powers`` () =
    let scale = Scale.log (1.0, 8.0) (0.0, 100.0) 2.0
    Assert.Equal<float list>([ 1.0; 2.0; 4.0; 8.0 ], Scale.ticks scale 0)

// Linear – properties

[<Property>]
let ``linear apply then invert is identity within domain`` (x: FsCheck.NormalFloat) =
    let lo, hi = 0.0, 100.0
    let scale = Scale.linear (lo, hi) (0.0, 1000.0)
    let clamped = max lo (min hi x.Get)
    isNear clamped (Scale.invert scale (Scale.apply scale clamped))

[<Property>]
let ``linear apply is order-preserving`` (x1: FsCheck.NormalFloat) (x2: FsCheck.NormalFloat) =
    let scale = Scale.linear (0.0, 100.0) (0.0, 1000.0)
    let a = min x1.Get x2.Get
    let b = max x1.Get x2.Get
    a = b || Scale.apply scale a <= Scale.apply scale b

[<Property>]
let ``linear ticks always returns exactly n items`` (n: FsCheck.PositiveInt) =
    let scale = Scale.linear (0.0, 100.0) (0.0, 1000.0)
    Scale.ticks scale n.Get |> List.length = n.Get

module SeriesTests =

    [<Fact>]
    let ``scatter sets kind to Scatter`` () =
        let series = Series.scatter [ 0.0, 0.0; 1.0, 1.0 ]
        Assert.Equal(Scatter, series.Kind)

    [<Fact>]
    let ``line sets kind to Line`` () =
        let series = Series.line [ 0.0, 0.0; 1.0, 1.0 ]
        Assert.Equal(Line, series.Kind)

    [<Fact>]
    let ``area sets kind to Area`` () =
        let series = Series.area [ 0.0, 0.0; 1.0, 1.0 ]
        Assert.Equal(Area, series.Kind)

    [<Fact>]
    let ``create has no label by default`` () =
        let series = Series.scatter [ 0.0, 0.0 ]
        Assert.Equal(None, series.Label)

    [<Fact>]
    let ``withLabel sets label`` () =
        let series = Series.scatter [ 0.0, 0.0 ] |> Series.withLabel "my series"
        Assert.Equal(Some "my series", series.Label)

    [<Fact>]
    let ``points are preserved`` () =
        let points = [ 1.0, 2.0; 3.0, 4.0 ]
        let series = Series.line points
        Assert.Equal<(float * float) list>(points, series.Points)

    [<Fact>]
    let ``ofFunction with samples=1 returns single point at tMin`` () =
        let series = Series.ofFunction Scatter (fun t -> t, t) 2.0 5.0 1
        Assert.Equal<(float * float) list>([ 2.0, 2.0 ], series.Points)

    [<Fact>]
    let ``lineOfFunction sets kind to Line`` () =
        let series = Series.lineOfFunction (fun t -> cos t, sin t) 0.0 1.0 10
        Assert.Equal(Line, series.Kind)

    [<Fact>]
    let ``scatterOfFunction sets kind to Scatter`` () =
        let series = Series.scatterOfFunction (fun t -> t, t) 0.0 1.0 10
        Assert.Equal(Scatter, series.Kind)

    [<Property>]
    let ``ofFunction produces exactly samples points`` (samples: FsCheck.PositiveInt) =
        let sampleCount = samples.Get
        let series = Series.ofFunction Line (fun t -> t, t) 0.0 1.0 sampleCount
        series.Points.Length = sampleCount

    [<Property>]
    let ``ofFunction first point maps tMin, last maps tMax`` (samples: FsCheck.PositiveInt) =
        let sampleCount = samples.Get
        let series = Series.ofFunction Line (fun t -> t, 0.0) 0.0 1.0 sampleCount
        let first = fst (List.head series.Points)
        let last = fst (List.last series.Points)
        if sampleCount = 1 then isNear 0.0 first
        else isNear 0.0 first && isNear 1.0 last

    [<Property>]
    let ``ofFunction preserves function values at each t`` (samples: FsCheck.PositiveInt) =
        let sampleCount = samples.Get
        let series = Series.ofFunction Line (fun t -> t, t * t) 0.0 1.0 sampleCount
        series.Points |> List.forall (fun (x, y) -> isNear (x * x) y)

    [<Property>]
    let ``ofFunction label is always None`` (samples: FsCheck.PositiveInt) =
        let series = Series.ofFunction Scatter (fun t -> t, t) 0.0 1.0 samples.Get
        series.Label = None

    [<Fact>]
    let ``create has no stroke width by default`` () =
        let series = Series.line [ 0.0, 0.0 ]
        Assert.Equal(None, series.StrokeWidth)

    [<Fact>]
    let ``create has no point radius by default`` () =
        let series = Series.scatter [ 0.0, 0.0 ]
        Assert.Equal(None, series.PointRadius)

    [<Fact>]
    let ``withStrokeWidth sets stroke width`` () =
        let series = Series.line [ 0.0, 0.0 ] |> Series.withStrokeWidth (SharpVG.Length.ofFloat 3.0)
        Assert.Equal(Some (SharpVG.Length.ofFloat 3.0), series.StrokeWidth)

    [<Fact>]
    let ``withPointRadius sets point radius`` () =
        let series = Series.scatter [ 0.0, 0.0 ] |> Series.withPointRadius (SharpVG.Length.ofFloat 7.0)
        Assert.Equal(Some (SharpVG.Length.ofFloat 7.0), series.PointRadius)

    // Visibility and opacity – REQ-21

    [<Fact>]
    let ``create is visible by default`` () =
        let series = Series.scatter [ 0.0, 0.0 ]
        Assert.Equal(true, series.Visible)

    [<Fact>]
    let ``create has full opacity by default`` () =
        let series = Series.scatter [ 0.0, 0.0 ]
        Assert.Equal(1.0, series.Opacity)

    [<Fact>]
    let ``withVisible false marks series hidden`` () =
        let series = Series.scatter [ 0.0, 0.0 ] |> Series.withVisible false
        Assert.Equal(false, series.Visible)

    [<Fact>]
    let ``withVisible true restores visibility`` () =
        let series = Series.scatter [ 0.0, 0.0 ] |> Series.withVisible false |> Series.withVisible true
        Assert.Equal(true, series.Visible)

    [<Fact>]
    let ``withOpacity sets opacity`` () =
        let series = Series.line [ 0.0, 0.0 ] |> Series.withOpacity 0.5
        Assert.Equal(0.5, series.Opacity)

    [<Fact>]
    let ``withOpacity clamps below zero to zero`` () =
        let series = Series.line [ 0.0, 0.0 ] |> Series.withOpacity -0.2
        Assert.Equal(0.0, series.Opacity)

    [<Fact>]
    let ``withOpacity clamps above one to one`` () =
        let series = Series.line [ 0.0, 0.0 ] |> Series.withOpacity 1.4
        Assert.Equal(1.0, series.Opacity)

module ThemeTests =

    open SharpVG

    [<Fact>]
    let ``empty has no grid pen`` () =
        Assert.Equal(None, Theme.empty.GridPen)

    [<Fact>]
    let ``light has grid pen`` () =
        Assert.True Theme.light.GridPen.IsSome

    [<Fact>]
    let ``dark has grid pen`` () =
        Assert.True Theme.dark.GridPen.IsSome

    [<Fact>]
    let ``withBackground replaces background`` () =
        let theme = Theme.empty |> Theme.withBackground (Color.ofName Black)
        Assert.Equal(Color.ofName Black, theme.Background)

    [<Fact>]
    let ``withPens replaces pens`` () =
        let pens = [ Pen.red; Pen.blue ]
        let theme = Theme.empty |> Theme.withPens pens
        Assert.Equal<Pen list>(pens, theme.Pens)

    [<Fact>]
    let ``withAxisPen replaces axis pen`` () =
        let theme = Theme.empty |> Theme.withAxisPen Pen.red
        Assert.Equal(Pen.red, theme.AxisPen)

    [<Fact>]
    let ``withGridPen sets grid pen to Some`` () =
        let theme = Theme.empty |> Theme.withGridPen Pen.lightGray
        Assert.Equal(Some Pen.lightGray, theme.GridPen)

    [<Fact>]
    let ``penForSeries 0 returns first pen`` () =
        Assert.Equal(Theme.empty.Pens.[0], Theme.penForSeries 0 Theme.empty)

    [<Fact>]
    let ``penForSeries cycles when index exceeds pen count`` () =
        let count = Theme.empty.Pens.Length
        Assert.Equal(Theme.penForSeries 0 Theme.empty, Theme.penForSeries count Theme.empty)

    [<Fact>]
    let ``builders chain without mutating original`` () =
        let original = Theme.empty
        let modified = original |> Theme.withAxisPen Pen.red |> Theme.withGridPen Pen.blue
        Assert.Equal(Pen.gray, original.AxisPen)
        Assert.Equal(Pen.red, modified.AxisPen)

    [<Property>]
    let ``penForSeries cycles with period equal to pen count`` (i: FsCheck.NonNegativeInt) =
        let theme = Theme.empty
        let count = theme.Pens.Length
        Theme.penForSeries i.Get theme = Theme.penForSeries (i.Get + count) theme

module AxisTests =

    let private xScale = Scale.linear (0.0, 10.0) (0.0, 1000.0)
    let private yScale = Scale.linear (0.0, 10.0) (1000.0, 0.0)

    // Builders

    [<Fact>]
    let ``create defaults to 5 ticks and no label`` () =
        let axis = Axis.create Bottom xScale
        Assert.Equal(TickCount 5, axis.Ticks)
        Assert.Equal(None, axis.Label)

    [<Fact>]
    let ``withTicks sets tick count`` () =
        let axis = Axis.create Bottom xScale |> Axis.withTicks 10
        Assert.Equal(TickCount 10, axis.Ticks)

    [<Fact>]
    let ``withTickInterval sets tick interval`` () =
        let axis = Axis.create Bottom xScale |> Axis.withTickInterval 2.5
        Assert.Equal(TickInterval 2.5, axis.Ticks)

    [<Fact>]
    let ``withLabel sets label`` () =
        let axis = Axis.create Bottom xScale |> Axis.withLabel "X axis"
        Assert.Equal(Some "X axis", axis.Label)

    // toElements – element counts

    [<Fact>]
    let ``bottom axis produces axis line plus 2 elements per tick`` () =
        let axis = Axis.create Bottom xScale |> Axis.withTicks 5
        let elements = Axis.toElements Theme.empty axis
        // 1 axis line + 5 ticks × (1 tick mark + 1 label) = 11
        Assert.Equal(11, elements.Length)

    [<Fact>]
    let ``bottom axis with label produces one extra element`` () =
        let axis = Axis.create Bottom xScale |> Axis.withTicks 5 |> Axis.withLabel "X"
        let elements = Axis.toElements Theme.empty axis
        Assert.Equal(12, elements.Length)

    [<Fact>]
    let ``left axis produces same structure as bottom`` () =
        let axis = Axis.create Left yScale |> Axis.withTicks 5
        let elements = Axis.toElements Theme.empty axis
        Assert.Equal(11, elements.Length)

    [<Fact>]
    let ``top and right axes produce same element count`` () =
        let top = Axis.create Top xScale |> Axis.withTicks 3 |> Axis.toElements Theme.empty
        let right = Axis.create Right yScale |> Axis.withTicks 3 |> Axis.toElements Theme.empty
        // 1 + 3×2 = 7 each
        Assert.Equal(7, top.Length)
        Assert.Equal(7, right.Length)

    [<Property>]
    let ``toElements count is 1 plus 2 times tick count for any count`` (n: FsCheck.PositiveInt) =
        let axis = Axis.create Bottom xScale |> Axis.withTicks n.Get
        Axis.toElements Theme.empty axis |> List.length = 1 + 2 * n.Get

module GraphTests =

    let private points = [ 0.0, 0.0; 2.0, 4.0; 4.0, 2.0 ]
    let private series = Series.scatter points

    // create

    [<Fact>]
    let ``create stores series, domain, and range exactly`` () =
        let graph = Graph.create [ series ] (0.0, 10.0) (-5.0, 5.0)
        Assert.Equal<Series list>([ series ], graph.Series)
        Assert.Equal((0.0, 10.0), Scale.domain graph.XScale)
        Assert.Equal((-5.0, 5.0), Scale.domain graph.YScale)

    // addPadding

    [<Fact>]
    let ``addPadding expands domain and range symmetrically`` () =
        let graph = Graph.create [ series ] (0.0, 10.0) (0.0, 10.0)
        let paddedGraph = Graph.addPadding 0.1 graph
        let domainMinimum, domainMaximum = Scale.domain paddedGraph.XScale
        let rangeMinimum, rangeMaximum = Scale.domain paddedGraph.YScale
        Assert.Equal(-1.0, domainMinimum, 10)
        Assert.Equal(11.0, domainMaximum, 10)
        Assert.Equal(-1.0, rangeMinimum, 10)
        Assert.Equal(11.0, rangeMaximum, 10)

    [<Fact>]
    let ``addPadding 0 leaves bounds unchanged`` () =
        let graph = Graph.create [ series ] (0.0, 10.0) (0.0, 10.0)
        let paddedGraph = Graph.addPadding 0.0 graph
        Assert.Equal(Scale.domain graph.XScale, Scale.domain paddedGraph.XScale)
        Assert.Equal(Scale.domain graph.YScale, Scale.domain paddedGraph.YScale)

    // createWithSeries

    [<Fact>]
    let ``createWithSeries infers domain from points with 10 pct padding`` () =
        let graph = Graph.createWithSeries series
        let domainMinimum, domainMaximum = Scale.domain graph.XScale
        // raw x: 0..4, pad = 0.4
        Assert.Equal(-0.4, domainMinimum, 10)
        Assert.Equal(4.4, domainMaximum, 10)

    [<Fact>]
    let ``createWithSeries infers range from points with 10 pct padding`` () =
        let graph = Graph.createWithSeries series
        let rangeMinimum, rangeMaximum = Scale.domain graph.YScale
        // raw y: 0..4, pad = 0.4
        Assert.Equal(-0.4, rangeMinimum, 10)
        Assert.Equal(4.4, rangeMaximum, 10)

    [<Fact>]
    let ``createWithSeries stores exactly one series`` () =
        let graph = Graph.createWithSeries series
        Assert.Equal(1, graph.Series.Length)

    // addSeries

    [<Fact>]
    let ``addSeries appends series and recalculates bounds`` () =
        let secondSeries = Series.scatter [ -2.0, -2.0; 6.0, 6.0 ]
        let graph = Graph.createWithSeries series |> Graph.addSeries secondSeries
        Assert.Equal(2, graph.Series.Length)
        let domainMinimum, domainMaximum = Scale.domain graph.XScale
        let rangeMinimum, rangeMaximum = Scale.domain graph.YScale
        // raw x: -2..6, pad = 0.8
        Assert.Equal(-2.8, domainMinimum, 10)
        Assert.Equal(6.8, domainMaximum, 10)
        // raw y: -2..6, pad = 0.8
        Assert.Equal(-2.8, rangeMinimum, 10)
        Assert.Equal(6.8, rangeMaximum, 10)

    // toScaledSvgCoordinates

    [<Fact>]
    let ``toScaledSvgCoordinates maps domain min to left edge`` () =
        let graph = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0)
        let x, _ = Graph.toScaledSvgCoordinates graph (0.0, 0.0)
        Assert.Equal(0.0, x, 10)

    [<Fact>]
    let ``toScaledSvgCoordinates maps domain max to right edge`` () =
        let graph = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0)
        let x, _ = Graph.toScaledSvgCoordinates graph (4.0, 0.0)
        Assert.Equal(Canvas.canvasSize, x, 10)

    [<Fact>]
    let ``toScaledSvgCoordinates inverts y axis (range min maps to canvas bottom)`` () =
        let graph = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0)
        let _, y = Graph.toScaledSvgCoordinates graph (0.0, 0.0)
        Assert.Equal(Canvas.canvasSize, y, 10)

    [<Fact>]
    let ``toScaledSvgCoordinates inverts y axis (range max maps to canvas top)`` () =
        let graph = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0)
        let _, y = Graph.toScaledSvgCoordinates graph (0.0, 4.0)
        Assert.Equal(0.0, y, 10)

    // drawSeries – smoke tests (non-empty element lists per SeriesKind)

    [<Fact>]
    let ``drawSeries scatter produces one element per point`` () =
        let graph = Graph.create [ Series.scatter points ] (0.0, 4.0) (0.0, 4.0)
        let elements = Graph.drawSeries graph
        Assert.Equal(points.Length, elements.Length)

    [<Fact>]
    let ``drawSeries line produces exactly one polyline element`` () =
        let graph = Graph.create [ Series.line points ] (0.0, 4.0) (0.0, 4.0)
        let elements = Graph.drawSeries graph
        Assert.Equal(1, elements.Length)

    [<Fact>]
    let ``drawSeries area produces exactly one polygon element`` () =
        let graph = Graph.create [ Series.area points ] (0.0, 4.0) (0.0, 4.0)
        let elements = Graph.drawSeries graph
        Assert.Equal(1, elements.Length)

    [<Fact>]
    let ``drawSeries two series produces elements for both`` () =
        let graph =
            Graph.create
                [ Series.scatter points; Series.scatter points ]
                (0.0, 4.0) (0.0, 4.0)
        let elements = Graph.drawSeries graph
        Assert.Equal(points.Length * 2, elements.Length)

    [<Fact>]
    let ``drawSeries scatter with custom radius appears in SVG output`` () =
        let seriesWithRadius = Series.scatter points |> Series.withPointRadius (SharpVG.Length.ofFloat 8.0)
        let svgOutput = Graph.create [ seriesWithRadius ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        Assert.Contains("8", svgOutput)

    [<Fact>]
    let ``drawSeries line with custom stroke width appears in SVG output`` () =
        let seriesWithWidth = Series.line points |> Series.withStrokeWidth (SharpVG.Length.ofFloat 5.0)
        let svgOutput = Graph.create [ seriesWithWidth ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        Assert.Contains("5", svgOutput)

    [<Fact>]
    let ``drawSeries scatter custom radius produces different SVG than default`` () =
        let defaultSvg = Graph.create [ Series.scatter points ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        let customSvg = Graph.create [ Series.scatter points |> Series.withPointRadius (SharpVG.Length.ofFloat 9.0) ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        Assert.True(defaultSvg <> customSvg)

    [<Fact>]
    let ``drawSeries line custom stroke width produces different SVG than default`` () =
        let defaultSvg = Graph.create [ Series.line points ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        let customSvg = Graph.create [ Series.line points |> Series.withStrokeWidth (SharpVG.Length.ofFloat 4.0) ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        Assert.True(defaultSvg <> customSvg)

    // Visibility – REQ-21

    [<Fact>]
    let ``drawSeries hidden scatter produces no elements`` () =
        let graph = Graph.create [ Series.scatter points |> Series.withVisible false ] (0.0, 4.0) (0.0, 4.0)
        Assert.Empty(Graph.drawSeries graph)

    [<Fact>]
    let ``drawSeries hidden line produces no elements`` () =
        let graph = Graph.create [ Series.line points |> Series.withVisible false ] (0.0, 4.0) (0.0, 4.0)
        Assert.Empty(Graph.drawSeries graph)

    [<Fact>]
    let ``drawSeries hidden area produces no elements`` () =
        let graph = Graph.create [ Series.area points |> Series.withVisible false ] (0.0, 4.0) (0.0, 4.0)
        Assert.Empty(Graph.drawSeries graph)

    [<Fact>]
    let ``drawSeries one hidden one visible yields only visible elements`` () =
        let graph =
            Graph.create
                [ Series.scatter points |> Series.withVisible false; Series.scatter points ]
                (0.0, 4.0) (0.0, 4.0)
        Assert.Equal(points.Length, Graph.drawSeries graph |> List.length)

    // Opacity – REQ-21

    [<Fact>]
    let ``drawSeries scatter with reduced opacity produces different SVG than full opacity`` () =
        let fullSvg = Graph.create [ Series.scatter points ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        let dimSvg = Graph.create [ Series.scatter points |> Series.withOpacity 0.3 ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        Assert.True(fullSvg <> dimSvg)

    [<Fact>]
    let ``drawSeries line with reduced opacity produces different SVG than full opacity`` () =
        let fullSvg = Graph.create [ Series.line points ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        let dimSvg = Graph.create [ Series.line points |> Series.withOpacity 0.3 ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        Assert.True(fullSvg <> dimSvg)

    [<Fact>]
    let ``drawSeries area with reduced opacity produces different SVG than full opacity`` () =
        let fullSvg = Graph.create [ Series.area points ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        let dimSvg = Graph.create [ Series.area points |> Series.withOpacity 0.3 ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        Assert.True(fullSvg <> dimSvg)

    // withTheme

    [<Fact>]
    let ``withTheme replaces theme on graph`` () =
        let graph = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0)
        Assert.Equal(Theme.empty, graph.Theme)
        let graphWithTheme = graph |> Graph.withTheme Theme.light
        Assert.Equal(Theme.light, graphWithTheme.Theme)

    // withAxes / axis suppression

    [<Fact>]
    let ``default graph has both axes set`` () =
        let graph = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0)
        Assert.True(graph.XAxis.IsSome)
        Assert.True(graph.YAxis.IsSome)

    [<Fact>]
    let ``withAxes Axis.none suppresses both axes`` () =
        let graph = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0) |> Graph.withAxes Axis.none
        Assert.Equal(None, graph.XAxis)
        Assert.Equal(None, graph.YAxis)

    [<Fact>]
    let ``withXAxis None suppresses only x axis`` () =
        let graph = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0) |> Graph.withXAxis None
        Assert.Equal(None, graph.XAxis)
        Assert.True(graph.YAxis.IsSome)

    // withTitle

    [<Fact>]
    let ``withTitle sets title`` () =
        let graph = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0) |> Graph.withTitle "My Chart"
        Assert.Equal(Some "My Chart", graph.Title)

    [<Fact>]
    let ``default graph has no title`` () =
        let graph = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0)
        Assert.Equal(None, graph.Title)

    [<Property>]
    let ``drawSeries scatter produces one element per point for any count`` (n: FsCheck.PositiveInt) =
        let pts = List.init n.Get (fun i -> float i, float i)
        let graph = Graph.create [ Series.scatter pts ] (0.0, float n.Get) (0.0, float n.Get)
        Graph.drawSeries graph |> List.length = n.Get

    [<Property>]
    let ``addSeries always increases series count by exactly one`` (n: FsCheck.PositiveInt) =
        let pts = List.init n.Get (fun i -> float i, float i)
        let graph = Graph.create [ Series.scatter pts ] (0.0, float n.Get) (0.0, float n.Get)
        let after = (graph |> Graph.addSeries (Series.line pts)).Series.Length
        after = graph.Series.Length + 1

module GraphVGTests =

    let private points = [ 0.0, 0.0; 2.0, 4.0; 4.0, 2.0 ]
    let private series = Series.line points

    [<Fact>]
    let ``toHtml returns non-empty string`` () =
        let html = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toHtml
        Assert.True(html.Length > 0)

    [<Fact>]
    let ``toHtml output contains svg tag`` () =
        let html = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toHtml
        Assert.Contains("<svg", html)

    [<Fact>]
    let ``render returns svg string without html wrapper`` () =
        let svg = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        Assert.Contains("<svg", svg)
        Assert.DoesNotContain("<!DOCTYPE", svg)

    [<Fact>]
    let ``toHtml with title contains title text`` () =
        let html =
            Graph.create [ series ] (0.0, 4.0) (0.0, 4.0)
            |> Graph.withTitle "Test Title"
            |> GraphVG.toHtml
        Assert.Contains("Test Title", html)

    [<Fact>]
    let ``toHtml with Theme.light produces more elements than Theme.empty (grid lines)`` () =
        let graph = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0)
        // Theme.light has GridPen; Theme.empty does not — light should produce a longer SVG
        let svgLight = graph |> Graph.withTheme Theme.light |> GraphVG.render
        let svgEmpty = graph |> GraphVG.render
        Assert.True(svgLight.Length > svgEmpty.Length)

    [<Fact>]
    let ``toHtml with axes suppressed produces shorter output than with default axes`` () =
        let graph = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0)
        let withAxes = graph |> GraphVG.render
        let withoutAxes = graph |> Graph.withAxes Axis.none |> GraphVG.render
        Assert.True(withoutAxes.Length < withAxes.Length)

    [<Fact>]
    let ``render contains background rect`` () =
        let svg = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        Assert.Contains("<rect", svg)

    [<Fact>]
    let ``render background color reflects theme`` () =
        let svgDark = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0) |> Graph.withTheme Theme.dark |> GraphVG.render
        let svgLight = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0) |> Graph.withTheme Theme.light |> GraphVG.render
        Assert.True(svgDark <> svgLight)

module AxisHideTests =

    // Scale with 0.0 in domain so hideOrigin has a tick to suppress
    let private scale = Scale.linear (-5.0, 5.0) (0.0, 1000.0)

    [<Fact>]
    let ``hideOriginTick sets HideOriginTick only`` () =
        let axis = Axis.create Bottom scale |> Axis.hideOriginTick
        Assert.True(axis.HideOriginTick)
        Assert.False(axis.HideOriginLabel)

    [<Fact>]
    let ``hideOriginLabel sets HideOriginLabel only`` () =
        let axis = Axis.create Bottom scale |> Axis.hideOriginLabel
        Assert.False(axis.HideOriginTick)
        Assert.True(axis.HideOriginLabel)

    [<Fact>]
    let ``hideOrigin sets both HideOriginTick and HideOriginLabel`` () =
        let axis = Axis.create Bottom scale |> Axis.hideOrigin
        Assert.True(axis.HideOriginTick)
        Assert.True(axis.HideOriginLabel)

    [<Fact>]
    let ``hideBoundsTick sets HideBoundsTick only`` () =
        let axis = Axis.create Bottom scale |> Axis.hideBoundsTick
        Assert.True(axis.HideBoundsTick)
        Assert.False(axis.HideBoundsLabel)

    [<Fact>]
    let ``hideBoundsLabel sets HideBoundsLabel only`` () =
        let axis = Axis.create Bottom scale |> Axis.hideBoundsLabel
        Assert.False(axis.HideBoundsTick)
        Assert.True(axis.HideBoundsLabel)

    [<Fact>]
    let ``hideOriginTick removes tick element at origin`` () =
        let full = Axis.create Bottom scale |> Axis.withTicks 5 |> Axis.toElements Theme.empty
        let hidden = Axis.create Bottom scale |> Axis.withTicks 5 |> Axis.hideOriginTick |> Axis.toElements Theme.empty
        Assert.Equal(full.Length - 1, hidden.Length)

    [<Fact>]
    let ``hideOriginLabel removes label element at origin`` () =
        let full = Axis.create Bottom scale |> Axis.withTicks 5 |> Axis.toElements Theme.empty
        let hidden = Axis.create Bottom scale |> Axis.withTicks 5 |> Axis.hideOriginLabel |> Axis.toElements Theme.empty
        Assert.Equal(full.Length - 1, hidden.Length)

    [<Fact>]
    let ``hideOrigin removes both tick and label at origin`` () =
        let full = Axis.create Bottom scale |> Axis.withTicks 5 |> Axis.toElements Theme.empty
        let hidden = Axis.create Bottom scale |> Axis.withTicks 5 |> Axis.hideOrigin |> Axis.toElements Theme.empty
        Assert.Equal(full.Length - 2, hidden.Length)

    [<Fact>]
    let ``hideOrigin on axis with no tick at origin leaves count unchanged`` () =
        let noZeroScale = Scale.linear (1.0, 10.0) (0.0, 1000.0)
        let full = Axis.create Bottom noZeroScale |> Axis.withTicks 5 |> Axis.toElements Theme.empty
        let hidden = Axis.create Bottom noZeroScale |> Axis.withTicks 5 |> Axis.hideOrigin |> Axis.toElements Theme.empty
        Assert.Equal(full.Length, hidden.Length)

module AxisStyleTests =

    let private scale = Scale.linear (0.0, 10.0) (0.0, 1000.0)

    [<Fact>]
    let ``create defaults to TickLength 6 and FontSize 12`` () =
        let axis = Axis.create Bottom scale
        Assert.Equal(6.0,  axis.TickLength)
        Assert.Equal(12.0, axis.FontSize)

    [<Fact>]
    let ``withTickLength sets TickLength`` () =
        let axis = Axis.create Bottom scale |> Axis.withTickLength 15.0
        Assert.Equal(15.0, axis.TickLength)

    [<Fact>]
    let ``withFontSize sets FontSize`` () =
        let axis = Axis.create Bottom scale |> Axis.withFontSize 20.0
        Assert.Equal(20.0, axis.FontSize)

    [<Fact>]
    let ``withTickLength custom value produces different output than default`` () =
        let base' = Axis.create Bottom scale |> Axis.withTicks 3
        Assert.True(Axis.toElements Theme.empty base' <> Axis.toElements Theme.empty (base' |> Axis.withTickLength 20.0))

    [<Fact>]
    let ``withFontSize custom value produces different output than default`` () =
        let base' = Axis.create Bottom scale |> Axis.withTicks 3
        Assert.True(Axis.toElements Theme.empty base' <> Axis.toElements Theme.empty (base' |> Axis.withFontSize 24.0))

module AxisTickFormatTests =

    let private scale = Scale.linear (0.0, 1.0) (0.0, 1000.0)

    [<Fact>]
    let ``default tick format uses %.4g`` () =
        let axis = Axis.create Bottom scale
        Assert.Equal(None, axis.TickFormat)

    [<Fact>]
    let ``withTickFormat sets formatter`` () =
        let fmt = sprintf "%.0f%%"
        let axis = Axis.create Bottom scale |> Axis.withTickFormat fmt
        Assert.Equal(Some fmt, axis.TickFormat)

    [<Fact>]
    let ``withTickFormat custom formatter produces different output than default`` () =
        let base' = Axis.create Bottom scale |> Axis.withTicks 3
        let defaultEl = Axis.toElements Theme.empty base'
        let customEl  = Axis.toElements Theme.empty (base' |> Axis.withTickFormat (sprintf "%.0f%%"))
        Assert.True(defaultEl <> customEl)

module StrokeDashTests =

    let private pts = [ 0.0, 0.0; 1.0, 1.0; 2.0, 0.0 ]

    [<Fact>]
    let ``create defaults to Solid`` () =
        Assert.Equal(Solid, (Series.line pts).StrokeDash)

    [<Fact>]
    let ``withStrokeDash sets dash`` () =
        let s = Series.line pts |> Series.withStrokeDash Dashed
        Assert.Equal(Dashed, s.StrokeDash)

    [<Fact>]
    let ``dashed line produces different SVG than solid`` () =
        let solid  = Graph.create [ Series.line pts ] (0.0, 2.0) (0.0, 2.0) |> GraphVG.render
        let dashed = Graph.create [ Series.line pts |> Series.withStrokeDash Dashed ] (0.0, 2.0) (0.0, 2.0) |> GraphVG.render
        Assert.True(solid <> dashed)

    [<Fact>]
    let ``dotted and dashed produce different SVG`` () =
        let dashed = Graph.create [ Series.line pts |> Series.withStrokeDash Dashed ] (0.0, 2.0) (0.0, 2.0) |> GraphVG.render
        let dotted = Graph.create [ Series.line pts |> Series.withStrokeDash Dotted ] (0.0, 2.0) (0.0, 2.0) |> GraphVG.render
        Assert.True(dashed <> dotted)

    [<Fact>]
    let ``stroke dash does not affect scatter series`` () =
        let s1 = Graph.create [ Series.scatter pts ] (0.0, 2.0) (0.0, 2.0) |> GraphVG.render
        let s2 = Graph.create [ Series.scatter pts |> Series.withStrokeDash Dashed ] (0.0, 2.0) (0.0, 2.0) |> GraphVG.render
        Assert.Equal(s1, s2)

module TitleStyleTests =

    let private series = Series.line [ 0.0, 0.0; 1.0, 1.0 ]

    [<Fact>]
    let ``default title style has FontSize 16 and Middle alignment`` () =
        let graph = Graph.create [ series ] (0.0, 1.0) (0.0, 1.0)
        Assert.Equal(16.0,                  graph.TitleStyle.FontSize)
        Assert.Equal(SharpVG.Middle,        graph.TitleStyle.Alignment)

    [<Fact>]
    let ``withTitleStyle sets both fields`` () =
        let style = TitleStyle.create 24.0 SharpVG.Start
        let graph = Graph.create [ series ] (0.0, 1.0) (0.0, 1.0) |> Graph.withTitleStyle style
        Assert.Equal(24.0,         graph.TitleStyle.FontSize)
        Assert.Equal(SharpVG.Start, graph.TitleStyle.Alignment)

    [<Fact>]
    let ``custom title style produces different SVG than default`` () =
        let base'    = Graph.create [ series ] (0.0, 1.0) (0.0, 1.0) |> Graph.withTitle "Hello"
        let default' = base' |> GraphVG.render
        let custom   = base' |> Graph.withTitleStyle (TitleStyle.create 32.0 SharpVG.Start) |> GraphVG.render
        Assert.True(default' <> custom)

module PlotBackgroundTests =

    open SharpVG

    let private series = Series.line [ 0.0, 0.0; 1.0, 1.0 ]
    let private graph = Graph.create [ series ] (0.0, 1.0) (0.0, 1.0)

    [<Fact>]
    let ``empty theme has no plot background`` () =
        Assert.Equal(None, Theme.empty.PlotBackground)

    [<Fact>]
    let ``withPlotBackground sets PlotBackground to Some`` () =
        let theme = Theme.empty |> Theme.withPlotBackground (Color.ofName Yellow)
        Assert.Equal(Some (Color.ofName Yellow), theme.PlotBackground)

    [<Fact>]
    let ``plot background renders a second rect in SVG`` () =
        let svgDefault = graph |> GraphVG.render
        let svgWithBg = graph |> Graph.withTheme (Theme.empty |> Theme.withPlotBackground (Color.ofName Yellow)) |> GraphVG.render
        let countRects (svg : string) = svg.Split("<rect") |> Array.length |> fun n -> n - 1
        Assert.Equal(countRects svgDefault + 1, countRects svgWithBg)

    [<Fact>]
    let ``plot background produces different SVG than no plot background`` () =
        let svgDefault = graph |> GraphVG.render
        let svgWithBg = graph |> Graph.withTheme (Theme.empty |> Theme.withPlotBackground (Color.ofName LightBlue)) |> GraphVG.render
        Assert.True(svgDefault <> svgWithBg)

module SpineStyleTests =

    let private scale = Scale.linear (0.0, 10.0) (0.0, 1000.0)
    let private pts = [ 0.0, 0.0; 1.0, 1.0 ]

    [<Fact>]
    let ``create defaults to Full spine`` () =
        let axis = Axis.create Bottom scale
        Assert.Equal(Full, axis.SpineStyle)

    [<Fact>]
    let ``withSpine sets SpineStyle`` () =
        let axis = Axis.create Bottom scale |> Axis.withSpine Hidden
        Assert.Equal(Hidden, axis.SpineStyle)

    [<Fact>]
    let ``Hidden spine produces one fewer element than Full`` () =
        let full = Axis.create Bottom scale |> Axis.withTicks 3 |> Axis.toElements Theme.empty
        let hidden = Axis.create Bottom scale |> Axis.withTicks 3 |> Axis.withSpine Hidden |> Axis.toElements Theme.empty
        Assert.Equal(full.Length - 1, hidden.Length)

    [<Fact>]
    let ``Box spine produces same element count as Full`` () =
        let full = Axis.create Bottom scale |> Axis.withTicks 3 |> Axis.toElements Theme.empty
        let box = Axis.create Bottom scale |> Axis.withTicks 3 |> Axis.withSpine Box |> Axis.toElements Theme.empty
        Assert.Equal(full.Length, box.Length)

    [<Fact>]
    let ``Hidden spine produces different SVG than Full`` () =
        let svgFull = Graph.create [ Series.line pts ] (0.0, 1.0) (0.0, 1.0) |> GraphVG.render
        let svgHidden =
            Graph.create [ Series.line pts ] (0.0, 1.0) (0.0, 1.0)
            |> Graph.withXAxis (Some (Axis.create (HorizontalAt 1000.0) (Scale.linear (0.0, 1.0) (0.0, 1000.0)) |> Axis.withSpine Hidden))
            |> GraphVG.render
        Assert.True(svgFull <> svgHidden)

    [<Fact>]
    let ``Box spine produces different SVG than Full`` () =
        let axis = Axis.create Bottom scale |> Axis.withTicks 3
        let full = Axis.toElements Theme.empty axis
        let box = Axis.toElements Theme.empty (axis |> Axis.withSpine Box)
        Assert.True(full <> box)
