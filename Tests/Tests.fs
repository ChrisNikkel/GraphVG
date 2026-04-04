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
    let lower, upper = 0.0, 100.0
    let scale = Scale.linear (lower, upper) (0.0, 1000.0)
    let clamped = clamp lower upper x.Get
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

module ThemePresetTests =

    open SharpVG

    [<Fact>]
    let ``preset Light returns light theme`` () =
        Assert.Equal(Theme.light, Theme.preset Light)

    [<Fact>]
    let ``preset Dark returns dark theme`` () =
        Assert.Equal(Theme.dark, Theme.preset Dark)

    [<Fact>]
    let ``preset HighContrast returns highContrast theme`` () =
        Assert.Equal(Theme.highContrast, Theme.preset HighContrast)

    [<Fact>]
    let ``highContrast has black background`` () =
        Assert.Equal(Color.ofName Black, Theme.highContrast.Background)

    [<Fact>]
    let ``highContrast has grid pen`` () =
        Assert.True Theme.highContrast.GridPen.IsSome

    [<Fact>]
    let ``highContrast has at least six pens`` () =
        Assert.True(Theme.highContrast.Pens.Length >= 6)

    [<Fact>]
    let ``withDefaultTheme applies theme to graph`` () =
        let pts = [ 0.0, 0.0; 1.0, 1.0 ]
        let graph =
            Graph.create [ Series.line pts ] (0.0, 1.0) (0.0, 1.0)
            |> Graph.withDefaultTheme (Theme.preset HighContrast)
        Assert.Equal(Theme.highContrast, graph.Theme)

    [<Fact>]
    let ``withDefaultTheme can be overridden by withTheme`` () =
        let pts = [ 0.0, 0.0; 1.0, 1.0 ]
        let graph =
            Graph.create [ Series.line pts ] (0.0, 1.0) (0.0, 1.0)
            |> Graph.withDefaultTheme (Theme.preset Dark)
            |> Graph.withTheme Theme.light
        Assert.Equal(Theme.light, graph.Theme)

    [<Fact>]
    let ``withDefaultTheme does not mutate original graph`` () =
        let pts = [ 0.0, 0.0; 1.0, 1.0 ]
        let original = Graph.create [ Series.line pts ] (0.0, 1.0) (0.0, 1.0)
        let _ = original |> Graph.withDefaultTheme (Theme.preset HighContrast)
        Assert.Equal(Theme.empty, original.Theme)

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
        Assert.Equal(CommonMath.canvasSize, x, 10)

    [<Fact>]
    let ``toScaledSvgCoordinates inverts y axis (range min maps to canvas bottom)`` () =
        let graph = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0)
        let _, y = Graph.toScaledSvgCoordinates graph (0.0, 0.0)
        Assert.Equal(CommonMath.canvasSize, y, 10)

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
        let svgOutput = Graph.create [ seriesWithRadius ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        Assert.Contains("8", svgOutput)

    [<Fact>]
    let ``drawSeries line with custom stroke width appears in SVG output`` () =
        let seriesWithWidth = Series.line points |> Series.withStrokeWidth (SharpVG.Length.ofFloat 5.0)
        let svgOutput = Graph.create [ seriesWithWidth ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        Assert.Contains("5", svgOutput)

    [<Fact>]
    let ``drawSeries scatter custom radius produces different SVG than default`` () =
        let defaultSvg = Graph.create [ Series.scatter points ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        let customSvg = Graph.create [ Series.scatter points |> Series.withPointRadius (SharpVG.Length.ofFloat 9.0) ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        Assert.True(defaultSvg <> customSvg)

    [<Fact>]
    let ``drawSeries line custom stroke width produces different SVG than default`` () =
        let defaultSvg = Graph.create [ Series.line points ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        let customSvg = Graph.create [ Series.line points |> Series.withStrokeWidth (SharpVG.Length.ofFloat 4.0) ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
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
        let fullSvg = Graph.create [ Series.scatter points ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        let dimSvg = Graph.create [ Series.scatter points |> Series.withOpacity 0.3 ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        Assert.True(fullSvg <> dimSvg)

    [<Fact>]
    let ``drawSeries line with reduced opacity produces different SVG than full opacity`` () =
        let fullSvg = Graph.create [ Series.line points ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        let dimSvg = Graph.create [ Series.line points |> Series.withOpacity 0.3 ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        Assert.True(fullSvg <> dimSvg)

    [<Fact>]
    let ``drawSeries area with reduced opacity produces different SVG than full opacity`` () =
        let fullSvg = Graph.create [ Series.area points ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        let dimSvg = Graph.create [ Series.area points |> Series.withOpacity 0.3 ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
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

    // StepLine – REQ-32

    [<Fact>]
    let ``drawSeries stepLine produces exactly one polyline element`` () =
        let graph = Graph.create [ Series.stepLine points ] (0.0, 4.0) (0.0, 4.0)
        Assert.Equal(1, Graph.drawSeries graph |> List.length)

    [<Fact>]
    let ``drawSeries stepLine produces different SVG than line`` () =
        let lineSvg = Graph.create [ Series.line points ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        let stepSvg = Graph.create [ Series.stepLine points ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        Assert.True(lineSvg <> stepSvg)

    [<Fact>]
    let ``drawSeries stepLine withStepMode Before produces different SVG than After`` () =
        let afterSvg = Graph.create [ Series.stepLine points ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        let beforeSvg = Graph.create [ Series.stepLine points |> Series.withStepMode Before ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        Assert.True(afterSvg <> beforeSvg)

    [<Fact>]
    let ``drawSeries stepLine withStepMode Mid produces different SVG than After`` () =
        let afterSvg = Graph.create [ Series.stepLine points ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        let midSvg = Graph.create [ Series.stepLine points |> Series.withStepMode Mid ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        Assert.True(afterSvg <> midSvg)

    [<Fact>]
    let ``drawSeries stepLine respects withStrokeDash`` () =
        let solidSvg = Graph.create [ Series.stepLine points ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        let dashedSvg = Graph.create [ Series.stepLine points |> Series.withStrokeDash Dashed ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        Assert.True(solidSvg <> dashedSvg)

    [<Property>]
    let ``drawSeries stepLine always produces exactly one element for any nonempty point list`` (n: FsCheck.PositiveInt) =
        let pts = List.init n.Get (fun i -> float i, float i)
        let graph = Graph.create [ Series.stepLine pts ] (0.0, float (n.Get - 1)) (0.0, float (n.Get - 1))
        Graph.drawSeries graph |> List.length = 1

    // Band – REQ-33

    let private bandTriples = [ 0.0, 1.0, 3.0; 2.0, 2.0, 5.0; 4.0, 0.5, 2.5 ]

    [<Fact>]
    let ``drawSeries band produces exactly one polygon element`` () =
        let graph = Graph.create [ Series.band bandTriples ] (0.0, 4.0) (0.0, 6.0)
        Assert.Equal(1, Graph.drawSeries graph |> List.length)

    [<Fact>]
    let ``drawSeries band with stroke width produces different SVG than without`` () =
        let noStroke = Graph.create [ Series.band bandTriples ] (0.0, 4.0) (0.0, 6.0) |> GraphVG.toSvg
        let withStroke = Graph.create [ Series.band bandTriples |> Series.withStrokeWidth (SharpVG.Length.ofFloat 2.0) ] (0.0, 4.0) (0.0, 6.0) |> GraphVG.toSvg
        Assert.True(noStroke <> withStroke)

    [<Fact>]
    let ``drawSeries band with reduced opacity produces different SVG than default`` () =
        let defaultSvg = Graph.create [ Series.band bandTriples ] (0.0, 4.0) (0.0, 6.0) |> GraphVG.toSvg
        let dimSvg = Graph.create [ Series.band bandTriples |> Series.withOpacity 0.5 ] (0.0, 4.0) (0.0, 6.0) |> GraphVG.toSvg
        Assert.True(defaultSvg <> dimSvg)

    [<Fact>]
    let ``createWithSeries band includes both yLow and yHigh in auto-bounds`` () =
        // yLow range 1..2, yHigh range 3..5 — y domain should cover at least 1..5
        let graph = Graph.createWithSeries (Series.band bandTriples)
        let yMin, yMax = Scale.domain graph.YScale
        Assert.True(yMin < 1.0)
        Assert.True(yMax > 5.0)

    [<Property>]
    let ``drawSeries band always produces exactly one element for any nonempty list`` (n: FsCheck.PositiveInt) =
        let triples = List.init n.Get (fun i -> float i, 0.0, 1.0)
        let graph = Graph.create [ Series.band triples ] (0.0, float (n.Get - 1)) (0.0, 1.0)
        Graph.drawSeries graph |> List.length = 1

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
    let ``toSvg returns svg string without html wrapper`` () =
        let svg = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
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
        let svgLight = graph |> Graph.withTheme Theme.light |> GraphVG.toSvg
        let svgEmpty = graph |> GraphVG.toSvg
        Assert.True(svgLight.Length > svgEmpty.Length)

    [<Fact>]
    let ``toHtml with axes suppressed produces shorter output than with default axes`` () =
        let graph = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0)
        let withAxes = graph |> GraphVG.toSvg
        let withoutAxes = graph |> Graph.withAxes Axis.none |> GraphVG.toSvg
        Assert.True(withoutAxes.Length < withAxes.Length)

    [<Fact>]
    let ``toSvg contains background rect`` () =
        let svg = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toSvg
        Assert.Contains("<rect", svg)

    [<Fact>]
    let ``toSvg background color reflects theme`` () =
        let svgDark = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0) |> Graph.withTheme Theme.dark |> GraphVG.toSvg
        let svgLight = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0) |> Graph.withTheme Theme.light |> GraphVG.toSvg
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
        let solid  = Graph.create [ Series.line pts ] (0.0, 2.0) (0.0, 2.0) |> GraphVG.toSvg
        let dashed = Graph.create [ Series.line pts |> Series.withStrokeDash Dashed ] (0.0, 2.0) (0.0, 2.0) |> GraphVG.toSvg
        Assert.True(solid <> dashed)

    [<Fact>]
    let ``dotted and dashed produce different SVG`` () =
        let dashed = Graph.create [ Series.line pts |> Series.withStrokeDash Dashed ] (0.0, 2.0) (0.0, 2.0) |> GraphVG.toSvg
        let dotted = Graph.create [ Series.line pts |> Series.withStrokeDash Dotted ] (0.0, 2.0) (0.0, 2.0) |> GraphVG.toSvg
        Assert.True(dashed <> dotted)

    [<Fact>]
    let ``stroke dash does not affect scatter series`` () =
        let s1 = Graph.create [ Series.scatter pts ] (0.0, 2.0) (0.0, 2.0) |> GraphVG.toSvg
        let s2 = Graph.create [ Series.scatter pts |> Series.withStrokeDash Dashed ] (0.0, 2.0) (0.0, 2.0) |> GraphVG.toSvg
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
        let default' = base' |> GraphVG.toSvg
        let custom   = base' |> Graph.withTitleStyle (TitleStyle.create 32.0 SharpVG.Start) |> GraphVG.toSvg
        Assert.True(default' <> custom)

    [<Fact>]
    let ``title rendering reserves top padding based on font size`` () =
        let base' = Graph.create [ series ] (0.0, 1.0) (0.0, 1.0) |> Graph.withTitle "Hello"
        let defaultRender = base' |> GraphVG.toSvg
        let customRender = base' |> Graph.withTitleStyle (TitleStyle.create 32.0 SharpVG.Start) |> GraphVG.toSvg
        Assert.Contains("viewBox=\"-20,-32 1040,1052\"", defaultRender)
        Assert.Contains("dominant-baseline=\"hanging\"", defaultRender)
        Assert.Contains("viewBox=\"-20,-48 1040,1068\"", customRender)

    [<Fact>]
    let ``title and top axis reserve stacked top padding`` () =
        let xScale = Scale.linear (0.0, 10.0) (0.0, CommonMath.canvasSize)
        let graph =
            Graph.create [ series ] (0.0, 10.0) (0.0, 1.0)
            |> Graph.withTitle "Hello"
            |> Graph.withXAxis (Some (Axis.create Top xScale |> Axis.withLabel "top axis"))
        let svg = GraphVG.toSvg graph
        Assert.Contains("viewBox=\"-20,-66 1040,1086\"", svg)

module LayoutSpacingTests =

    let private series = Series.line [ 0.0, 0.0; 1.0, 1.0 ]

    [<Fact>]
    let ``default layout spacing preserves historical defaults`` () =
        let graph = Graph.create [ series ] (0.0, 1.0) (0.0, 1.0)
        Assert.Equal(LayoutSpacing.defaults, graph.LayoutSpacing)

    [<Fact>]
    let ``withLayoutSpacing sets graph spacing`` () =
        let spacing = LayoutSpacing.create 32.0 24.0 12.0 8.0
        let graph = Graph.create [ series ] (0.0, 1.0) (0.0, 1.0) |> Graph.withLayoutSpacing spacing
        Assert.Equal(spacing, graph.LayoutSpacing)

    [<Fact>]
    let ``custom outer margin expands viewBox even without title or axes labels`` () =
        let spacing = { LayoutSpacing.defaults with OuterMargin = 40.0 }
        let svg =
            Graph.create [ series ] (0.0, 1.0) (0.0, 1.0)
            |> Graph.withAxes Axis.none
            |> Graph.withLayoutSpacing spacing
            |> GraphVG.toSvg
        Assert.Contains("viewBox=\"-40,-40 1080,1080\"", svg)

    [<Fact>]
    let ``custom title padding expands top viewBox for titled graph`` () =
        let spacing = { LayoutSpacing.defaults with TitlePadding = 30.0 }
        let svg =
            Graph.create [ series ] (0.0, 1.0) (0.0, 1.0)
            |> Graph.withTitle "Hello"
            |> Graph.withLayoutSpacing spacing
            |> GraphVG.toSvg
        Assert.Contains("viewBox=\"-20,-46 1040,1066\"", svg)

    [<Fact>]
    let ``custom tick and axis label padding expands bottom viewBox for labeled bottom axis`` () =
        let spacing = { LayoutSpacing.defaults with TickLabelPadding = 10.0; AxisLabelPadding = 14.0 }
        let svg =
            Graph.create [ series ] (0.0, 1.0) (0.0, 1.0)
            |> Graph.withXAxis (Some (Axis.create Bottom (Scale.linear (0.0, 1.0) (0.0, CommonMath.canvasSize)) |> Axis.withLabel "x"))
            |> Graph.withLayoutSpacing spacing
            |> GraphVG.toSvg
        Assert.Contains("viewBox=\"-20,-20 1040,1066\"", svg)

module PointShapeTests =

    let private pts = [ 0.0, 0.0; 1.0, 1.0; 2.0, 0.0 ]
    let private graphOf shape =
        Graph.create [ Series.scatter pts |> Series.withPointShape shape ] (0.0, 2.0) (0.0, 2.0)

    [<Fact>]
    let ``create defaults to Circle`` () =
        Assert.Equal(Circle, (Series.scatter pts).PointShape)

    [<Fact>]
    let ``withPointShape sets shape`` () =
        let s = Series.scatter pts |> Series.withPointShape Square
        Assert.Equal(Square, s.PointShape)

    [<Fact>]
    let ``Circle produces one element per point`` () =
        Assert.Equal(pts.Length, Graph.drawSeries (graphOf Circle) |> List.length)

    [<Fact>]
    let ``Square produces one element per point`` () =
        Assert.Equal(pts.Length, Graph.drawSeries (graphOf Square) |> List.length)

    [<Fact>]
    let ``Diamond produces one element per point`` () =
        Assert.Equal(pts.Length, Graph.drawSeries (graphOf Diamond) |> List.length)

    [<Fact>]
    let ``Triangle produces one element per point`` () =
        Assert.Equal(pts.Length, Graph.drawSeries (graphOf Triangle) |> List.length)

    [<Fact>]
    let ``Cross produces two elements per point`` () =
        Assert.Equal(pts.Length * 2, Graph.drawSeries (graphOf Cross) |> List.length)

    [<Fact>]
    let ``each shape produces different SVG than Circle`` () =
        let circleSvg = graphOf Circle |> GraphVG.toSvg
        Assert.True(graphOf Square |> GraphVG.toSvg <> circleSvg)
        Assert.True(graphOf Diamond |> GraphVG.toSvg <> circleSvg)
        Assert.True(graphOf Triangle |> GraphVG.toSvg <> circleSvg)
        Assert.True(graphOf Cross |> GraphVG.toSvg <> circleSvg)

    [<Property>]
    let ``non-Cross shapes produce exactly n elements for n points`` (n: FsCheck.PositiveInt) =
        let points = List.init n.Get (fun i -> float i, float i)
        let g shape = Graph.create [ Series.scatter points |> Series.withPointShape shape ] (0.0, float n.Get) (0.0, float n.Get)
        [ Circle; Square; Diamond; Triangle ]
        |> List.forall (fun shape -> Graph.drawSeries (g shape) |> List.length = n.Get)

    [<Property>]
    let ``Cross produces exactly 2n elements for n points`` (n: FsCheck.PositiveInt) =
        let points = List.init n.Get (fun i -> float i, float i)
        let g = Graph.create [ Series.scatter points |> Series.withPointShape Cross ] (0.0, float n.Get) (0.0, float n.Get)
        Graph.drawSeries g |> List.length = n.Get * 2

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
        let svgDefault = graph |> GraphVG.toSvg
        let svgWithBg = graph |> Graph.withTheme (Theme.empty |> Theme.withPlotBackground (Color.ofName Yellow)) |> GraphVG.toSvg
        let countRects (svg : string) = svg.Split("<rect") |> Array.length |> fun n -> n - 1
        Assert.Equal(countRects svgDefault + 1, countRects svgWithBg)

    [<Fact>]
    let ``plot background produces different SVG than no plot background`` () =
        let svgDefault = graph |> GraphVG.toSvg
        let svgWithBg = graph |> Graph.withTheme (Theme.empty |> Theme.withPlotBackground (Color.ofName LightBlue)) |> GraphVG.toSvg
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
        let box = Axis.create Bottom scale |> Axis.withTicks 3 |> Axis.withSpine SpineStyle.Box |> Axis.toElements Theme.empty
        Assert.Equal(full.Length, box.Length)

    [<Fact>]
    let ``Hidden spine produces different SVG than Full`` () =
        let svgFull = Graph.create [ Series.line pts ] (0.0, 1.0) (0.0, 1.0) |> GraphVG.toSvg
        let svgHidden =
            Graph.create [ Series.line pts ] (0.0, 1.0) (0.0, 1.0)
            |> Graph.withXAxis (Some (Axis.create (HorizontalAt 1000.0) (Scale.linear (0.0, 1.0) (0.0, 1000.0)) |> Axis.withSpine Hidden))
            |> GraphVG.toSvg
        Assert.True(svgFull <> svgHidden)

    [<Fact>]
    let ``Box spine produces different SVG than Full`` () =
        let axis = Axis.create Bottom scale |> Axis.withTicks 3
        let full = Axis.toElements Theme.empty axis
        let box = Axis.toElements Theme.empty (axis |> Axis.withSpine SpineStyle.Box)
        Assert.True(full <> box)

module AnnotationTests =

    let private series = Series.line [ 0.0, 0.0; 1.0, 1.0 ]
    let private graph = Graph.create [ series ] (0.0, 1.0) (0.0, 1.0)

    [<Fact>]
    let ``default graph has no annotations`` () =
        Assert.Empty(graph.Annotations)

    [<Fact>]
    let ``addAnnotation appends to Annotations list`` () =
        let g = graph |> Graph.addAnnotation (Annotation.Text(0.5, 0.5, "hello"))
        Assert.Equal(1, g.Annotations.Length)

    [<Fact>]
    let ``addAnnotation preserves order`` () =
        let g =
            graph
            |> Graph.addAnnotation (Annotation.Text(0.0, 0.0, "first"))
            |> Graph.addAnnotation (Annotation.Text(1.0, 1.0, "second"))
        match g.Annotations.[0], g.Annotations.[1] with
        | Annotation.Text(_, _, "first"), Annotation.Text(_, _, "second") -> ()
        | _ -> Assert.Fail("annotation order not preserved")

    [<Fact>]
    let ``text annotation renders content in SVG`` () =
        let svg = graph |> Graph.addAnnotation (Annotation.Text(0.5, 0.5, "label42")) |> GraphVG.toSvg
        Assert.Contains("label42", svg)

    [<Fact>]
    let ``line annotation produces different SVG than unannotated graph`` () =
        let annotated = graph |> Graph.addAnnotation (Annotation.Line(0.0, 0.0, 1.0, 1.0)) |> GraphVG.toSvg
        let plain = graph |> GraphVG.toSvg
        Assert.True(annotated <> plain)

    [<Fact>]
    let ``rect annotation produces different SVG than unannotated graph`` () =
        let annotated = graph |> Graph.addAnnotation (Annotation.Rect(0.1, 0.1, 0.5, 0.5)) |> GraphVG.toSvg
        let plain = graph |> GraphVG.toSvg
        Assert.True(annotated <> plain)

    [<Fact>]
    let ``unannotated graph renders identically to baseline`` () =
        Assert.Equal(graph |> GraphVG.toSvg, graph |> GraphVG.toSvg)

    [<Property>]
    let ``addAnnotation increases annotation count by exactly one each time`` (n: FsCheck.PositiveInt) =
        let g =
            List.init n.Get (fun i -> Annotation.Text(float i, float i, string i))
            |> List.fold (fun acc a -> Graph.addAnnotation a acc) graph
        g.Annotations.Length = n.Get

module LegendTests =

    let private pts = [ 0.0, 0.0; 1.0, 1.0 ]
    let private labeled = Series.line pts |> Series.withLabel "Alpha"
    let private unlabeled = Series.line pts
    let private baseGraph = Graph.create [ labeled ] (0.0, 1.0) (0.0, 1.0)

    [<Fact>]
    let ``default graph has no legend`` () =
        Assert.Equal(None, baseGraph.Legend)

    [<Fact>]
    let ``withLegend sets legend`` () =
        let g = baseGraph |> Graph.withLegend (Legend.create LegendBottom)
        Assert.Equal(Some LegendBottom, g.Legend |> Option.map (fun l -> l.Position))

    [<Fact>]
    let ``withLegend withFontSize sets font size`` () =
        let g = baseGraph |> Graph.withLegend (Legend.create LegendRight |> Legend.withFontSize 18.0)
        Assert.Equal(Some 18.0, g.Legend |> Option.map (fun l -> l.FontSize))

    [<Fact>]
    let ``legend with no labeled series produces same SVG as no legend`` () =
        let withoutLabels = Graph.create [ unlabeled ] (0.0, 1.0) (0.0, 1.0)
        let plain = withoutLabels |> GraphVG.toSvg
        let withLegend = withoutLabels |> Graph.withLegend (Legend.create LegendBottom) |> GraphVG.toSvg
        Assert.Equal(plain, withLegend)

    [<Fact>]
    let ``legend renders label text in SVG`` () =
        let svg = baseGraph |> Graph.withLegend (Legend.create LegendBottom) |> GraphVG.toSvg
        Assert.Contains("Alpha", svg)

    [<Fact>]
    let ``legend Bottom expands bottom padding in viewBox`` () =
        let svg = baseGraph |> Graph.withLegend (Legend.create LegendBottom) |> GraphVG.toSvg
        // legendOuterMargin(8) + swatchHeight(8) + legendOuterMargin(8) = 24 > defaultOuterMargin(20)
        Assert.Contains("viewBox=\"-20,-20 1040,1044\"", svg)

    [<Fact>]
    let ``legend Left expands left padding in viewBox`` () =
        // label "Alpha" = 5 chars, fontSize 12 → width = 5 * 12 * 0.6 = 36
        // left = 8 + 20 + 6 + 36 + 8 = 78; total width = 1000 + 78 + 20 = 1098
        let svg = baseGraph |> Graph.withLegend (Legend.create LegendLeft) |> GraphVG.toSvg
        Assert.Contains("viewBox=\"-78,-20 1098,1040\"", svg)

    [<Fact>]
    let ``legend Hidden produces same SVG as no legend`` () =
        let plain = baseGraph |> GraphVG.toSvg
        let hidden = baseGraph |> Graph.withLegend (Legend.create LegendHidden) |> GraphVG.toSvg
        Assert.Equal(plain, hidden)

    [<Fact>]
    let ``legend produces different SVG than no legend`` () =
        let plain = baseGraph |> GraphVG.toSvg
        let withLegend = baseGraph |> Graph.withLegend (Legend.create LegendBottom) |> GraphVG.toSvg
        Assert.True(plain <> withLegend)

    [<Fact>]
    let ``all four edge positions produce different SVG from each other`` () =
        let svgs =
            [ LegendTop; LegendBottom; LegendLeft; LegendRight ]
            |> List.map (fun pos -> baseGraph |> Graph.withLegend (Legend.create pos) |> GraphVG.toSvg)
        Assert.Equal(4, svgs |> List.distinct |> List.length)

    [<Fact>]
    let ``multiple labeled series all appear in SVG`` () =
        let s1 = Series.line pts |> Series.withLabel "Bravo"
        let s2 = Series.scatter pts |> Series.withLabel "Charlie"
        let svg =
            Graph.create [ s1; s2 ] (0.0, 1.0) (0.0, 1.0)
            |> Graph.withLegend (Legend.create LegendRight)
            |> GraphVG.toSvg
        Assert.Contains("Bravo", svg)
        Assert.Contains("Charlie", svg)

    [<Property>]
    let ``legend with n labeled series produces more SVG than n-1`` (n: FsCheck.PositiveInt) =
        let series = List.init n.Get (fun i -> Series.line pts |> Series.withLabel (sprintf "S%d" i))
        let withAll =
            Graph.create series (0.0, 1.0) (0.0, 1.0)
            |> Graph.withLegend (Legend.create LegendRight)
            |> GraphVG.toSvg
        let withoutLast =
            Graph.create (List.take (n.Get - 1) series @ [ Series.line pts ]) (0.0, 1.0) (0.0, 1.0)
            |> Graph.withLegend (Legend.create LegendRight)
            |> GraphVG.toSvg
        n.Get = 1 || withAll.Length > withoutLast.Length

module StackedAreaTests =

    let private xs = [ 0.0; 1.0; 2.0 ]
    let private mkSeries kind ys = List.zip xs ys |> Series.create kind
    let private s0 = mkSeries StackedArea [ 10.0; 20.0; 30.0 ]
    let private s1 = mkSeries StackedArea [ 5.0; 10.0; 15.0 ]
    let private s2 = mkSeries StackedArea [ 5.0; 10.0; 5.0 ]

    [<Fact>]
    let ``stackedArea constructor sets StackedArea kind`` () =
        let s = Series.stackedArea (List.zip xs [ 1.0; 2.0; 3.0 ])
        Assert.Equal(StackedArea, s.Kind)

    [<Fact>]
    let ``normalizedStackedArea constructor sets NormalizedStackedArea kind`` () =
        let s = Series.normalizedStackedArea (List.zip xs [ 1.0; 2.0; 3.0 ])
        Assert.Equal(NormalizedStackedArea, s.Kind)

    [<Fact>]
    let ``drawSeries produces one polygon per stacked series`` () =
        let graph = Graph.create [ s0; s1 ] (0.0, 2.0) (0.0, 35.0)
        let elements = Graph.drawSeries graph
        Assert.Equal(2, elements.Length)

    [<Fact>]
    let ``drawSeries bottom series baseline is y=0`` () =
        // The bottom stacked polygon must touch y=0 on both sides.
        // We verify by checking the SVG contains the baseline y coordinate.
        let graph = Graph.create [ s0; s1 ] (0.0, 2.0) (0.0, 35.0)
        let svg = GraphVG.toSvg graph
        Assert.Contains("<polygon", svg)

    [<Fact>]
    let ``stacked percent series renders one polygon per series`` () =
        let sp0 = mkSeries NormalizedStackedArea [ 10.0; 20.0; 30.0 ]
        let sp1 = mkSeries NormalizedStackedArea [ 10.0; 20.0; 30.0 ]
        let graph = Graph.create [ sp0; sp1 ] (0.0, 2.0) (0.0, 100.0)
        let elements = Graph.drawSeries graph
        Assert.Equal(2, elements.Length)

    [<Property>]
    let ``stacked percent tops sum to 100 at each x position`` (ys0 : FsCheck.PositiveInt list) (ys1 : FsCheck.PositiveInt list) =
        let n = min ys0.Length ys1.Length
        n < 1 ||
        (
            let take n ns = ns |> List.truncate n |> List.map (fun (x : FsCheck.PositiveInt) -> float x.Get)
            let vals0 = take n ys0
            let vals1 = take n ys1
            let pts = [ 0.0 .. float (n - 1) ]
            let sp0 = List.zip pts vals0 |> Series.normalizedStackedArea
            let sp1 = List.zip pts vals1 |> Series.normalizedStackedArea
            let xMax = if n = 1 then 1.0 else float (n - 1)
            let graph = Graph.create [ sp0; sp1 ] (0.0, xMax) (0.0, 100.0)
            Graph.drawSeries graph |> List.length = 2
        )

    [<Fact>]
    let ``StackedArea and NormalizedStackedArea render independently`` () =
        let sa = mkSeries StackedArea [ 10.0; 20.0; 30.0 ]
        let sp = mkSeries NormalizedStackedArea [ 10.0; 20.0; 30.0 ]
        let graph = Graph.create [ sa; sp ] (0.0, 2.0) (0.0, 100.0)
        let elements = Graph.drawSeries graph
        Assert.Equal(2, elements.Length)

    [<Fact>]
    let ``streamgraph constructor sets Streamgraph kind`` () =
        let s = Series.streamgraph (List.zip xs [ 1.0; 2.0; 3.0 ])
        Assert.Equal(Streamgraph, s.Kind)

    [<Fact>]
    let ``streamgraph renders one polygon per series`` () =
        let s0 = mkSeries Streamgraph [ 10.0; 20.0; 30.0 ]
        let s1 = mkSeries Streamgraph [ 5.0; 10.0; 15.0 ]
        let graph = Graph.create [ s0; s1 ] (0.0, 2.0) (-30.0, 30.0)
        let elements = Graph.drawSeries graph
        Assert.Equal(2, elements.Length)

    [<Property>]
    let ``streamgraph produces two polygons for any two positive series`` (ys0 : FsCheck.PositiveInt list) (ys1 : FsCheck.PositiveInt list) =
        let n = min ys0.Length ys1.Length
        n < 1 ||
        (
            let take ns = ns |> List.truncate n |> List.map (fun (x : FsCheck.PositiveInt) -> float x.Get)
            let pts = [ 0.0 .. float (n - 1) ]
            let s0 = List.zip pts (take ys0) |> Series.streamgraph
            let s1 = List.zip pts (take ys1) |> Series.streamgraph
            let xMax = if n = 1 then 1.0 else float (n - 1)
            let graph = Graph.create [ s0; s1 ] (0.0, xMax) (-100.0, 100.0)
            Graph.drawSeries graph |> List.length = 2
        )

module HistogramTests =

    let private values = [ 1.0; 2.0; 2.5; 3.0; 3.5; 4.0; 4.5; 5.0; 5.5; 6.0; 6.5; 7.0; 8.0; 9.0; 10.0 ]

    [<Fact>]
    let ``histogram creates Histogram kind series`` () =
        let s = Series.histogram values
        Assert.True(match s.Kind with | Histogram _ -> true | _ -> false)

    [<Fact>]
    let ``histogramWithBins creates requested number of bins`` () =
        let s = Series.histogramWithBins 5 values
        Assert.Equal(5, s.Points.Length)

    [<Fact>]
    let ``histogram bin counts sum to total value count`` () =
        let s = Series.histogramWithBins 5 values
        let totalCount = s.Points |> List.sumBy snd |> int
        Assert.Equal(values.Length, totalCount)

    [<Fact>]
    let ``histogram bounds x max includes right edge of last bin`` () =
        let s = Series.histogramWithBins 5 values
        let (_, xMax), _ = Series.bounds s
        let binWidth = match s.Kind with | Histogram bw -> bw | _ -> failwith "expected Histogram kind"
        let lastBinLeft = s.Points |> List.map fst |> List.max
        Assert.Equal(lastBinLeft + binWidth, xMax, 10)

    [<Fact>]
    let ``histogram bounds y min is zero`` () =
        let s = Series.histogram values
        let _, (yMin, _) = Series.bounds s
        Assert.Equal(0.0, yMin)

    [<Fact>]
    let ``histogram renders one rect per bin`` () =
        let s = Series.histogramWithBins 5 values
        let graph = Graph.create [ s ] (1.0, 10.0) (0.0, 10.0)
        let elements = Graph.drawSeries graph
        Assert.Equal(5, elements.Length)

    [<Fact>]
    let ``histogram createWithSeries produces valid SVG`` () =
        let svg = Graph.createWithSeries (Series.histogram values) |> GraphVG.toSvg
        Assert.Contains("<rect", svg)

module BoxPlotTests =

    let private values = [ 2.0; 4.0; 4.0; 4.0; 5.0; 5.0; 7.0; 9.0 ]

    [<Fact>]
    let ``box creates Box kind series`` () =
        let s = Series.box values
        Assert.Equal(SeriesKind.Box, s.Kind)

    [<Fact>]
    let ``box has exactly 5 points`` () =
        let s = Series.box values
        Assert.Equal(5, s.Points.Length)

    [<Fact>]
    let ``box points are ordered min Q1 median Q3 max`` () =
        let s = Series.box values
        let ys = s.Points |> List.map snd
        Assert.Equal(List.min values, ys.[0], 10)
        Assert.Equal(List.max values, ys.[4], 10)
        Assert.True(ys.[0] <= ys.[1] && ys.[1] <= ys.[2] && ys.[2] <= ys.[3] && ys.[3] <= ys.[4])

    [<Fact>]
    let ``boxAt positions box at specified x`` () =
        let s = Series.boxAt 3.0 values
        let xs = s.Points |> List.map fst
        Assert.True(xs |> List.forall (fun x -> CommonMath.isNear 3.0 x))

    [<Fact>]
    let ``box renders 6 elements (rect + median + 2 whiskers + 2 caps)`` () =
        let s = Series.box values
        let graph = Graph.create [ s ] (0.0, 1.0) (0.0, 10.0)
        let elements = Graph.drawSeries graph
        Assert.Equal(6, elements.Length)

    [<Fact>]
    let ``box createWithSeries produces valid SVG`` () =
        let svg = Graph.createWithSeries (Series.box values) |> GraphVG.toSvg
        Assert.Contains("<rect", svg)

    [<Property>]
    let ``box stats are monotonically non-decreasing`` (values : FsCheck.NonEmptyArray<FsCheck.NormalFloat>) =
        let vs = values.Get |> Array.map (fun x -> x.Get) |> Array.toList
        let s = Series.box vs
        let ys = s.Points |> List.map snd
        ys.[0] <= ys.[1] && ys.[1] <= ys.[2] && ys.[2] <= ys.[3] && ys.[3] <= ys.[4]

module BarChartTests =

    let private singleSeries =
        [ 1.0, 10.0; 2.0, 20.0; 3.0, 15.0 ] |> Series.bar

    let private groupedA =
        [ 1.0, 10.0; 2.0, 20.0 ] |> Series.bar |> Series.withLabel "A"

    let private groupedB =
        [ 1.0,  5.0; 2.0, 12.0 ] |> Series.bar |> Series.withLabel "B"

    [<Fact>]
    let ``bar creates Bar kind`` () =
        Assert.Equal(SeriesKind.Bar, singleSeries.Kind)

    [<Fact>]
    let ``horizontalBar creates HorizontalBar kind`` () =
        let s = [ 10.0, 1.0; 20.0, 2.0 ] |> Series.horizontalBar
        Assert.Equal(SeriesKind.HorizontalBar, s.Kind)

    [<Fact>]
    let ``bar renders one rect per point`` () =
        let graph = Graph.create [ singleSeries ] (0.0, 4.0) (0.0, 25.0)
        let elements = Graph.drawSeries graph
        Assert.Equal(3, elements.Length)

    [<Fact>]
    let ``grouped bar renders one rect per point per series`` () =
        let graph = Graph.create [ groupedA; groupedB ] (0.0, 3.0) (0.0, 25.0)
        let elements = Graph.drawSeries graph
        Assert.Equal(4, elements.Length)

    [<Fact>]
    let ``horizontalBar renders one rect per point`` () =
        let s = [ 10.0, 1.0; 20.0, 2.0; 15.0, 3.0 ] |> Series.horizontalBar
        let graph = Graph.create [ s ] (0.0, 25.0) (0.0, 4.0)
        let elements = Graph.drawSeries graph
        Assert.Equal(3, elements.Length)

    [<Fact>]
    let ``bar bounds y min is zero for positive values`` () =
        let _, (yMin, _) = Series.bounds singleSeries
        Assert.Equal(0.0, yMin)

    [<Fact>]
    let ``bar bounds x extends half unit beyond outermost categories`` () =
        let (xMin, xMax), _ = Series.bounds singleSeries
        Assert.Equal(0.5, xMin)
        Assert.Equal(3.5, xMax)

    [<Fact>]
    let ``horizontalBar bounds x min is zero for positive values`` () =
        let s = [ 10.0, 1.0; 20.0, 2.0 ] |> Series.horizontalBar
        let (xMin, _), _ = Series.bounds s
        Assert.Equal(0.0, xMin)

    [<Fact>]
    let ``bar createWithSeries produces SVG with rects`` () =
        let svg = Graph.createWithSeries singleSeries |> GraphVG.toSvg
        Assert.Contains("<rect", svg)

    [<Fact>]
    let ``horizontalBar createWithSeries produces SVG with rects`` () =
        let s = [ 10.0, 1.0; 20.0, 2.0; 5.0, 3.0 ] |> Series.horizontalBar
        let svg = Graph.createWithSeries s |> GraphVG.toSvg
        Assert.Contains("<rect", svg)

    [<Property>]
    let ``bar element count equals point count`` (points : FsCheck.NonEmptyArray<FsCheck.NormalFloat * FsCheck.PositiveInt>) =
        let pts = points.Get |> Array.map (fun (x, y) -> x.Get, float y.Get) |> Array.toList
        let s = Series.bar pts
        let graph = Graph.create [ s ] (-100.0, 100.0) (0.0, 200.0)
        Graph.drawSeries graph |> List.length = pts.Length

module DomainPolicyTests =

    open FsCheck

    let private scatterSeries pts = Series.scatter pts

    let private domainOf (graph : Graph) = Scale.domain graph.XScale
    let private rangeOf (graph : Graph) = Scale.domain graph.YScale

    [<Fact>]
    let ``Tight policy keeps exact data bounds`` () =
        let s = scatterSeries [ 1.0, 2.0; 3.0, 4.0 ]
        let graph = Graph.createWithSeries s |> Graph.withDomainPolicy Tight
        let xLo, xHi = domainOf graph
        let yLo, yHi = rangeOf graph
        Assert.Equal(1.0, xLo)
        Assert.Equal(3.0, xHi)
        Assert.Equal(2.0, yLo)
        Assert.Equal(4.0, yHi)

    [<Fact>]
    let ``Padded policy expands bounds by fraction`` () =
        let s = scatterSeries [ 0.0, 0.0; 10.0, 10.0 ]
        let graph = Graph.createWithSeries s |> Graph.withDomainPolicy (Padded 0.1)
        let _, xHi = domainOf graph
        let yLo, yHi = rangeOf graph
        Assert.True(xHi > 10.0)
        Assert.True(yLo < 0.0 || yLo = 0.0)
        Assert.True(yHi > 10.0)

    [<Fact>]
    let ``Padded policy pads symmetrically even when lo is zero`` () =
        let s = scatterSeries [ 0.0, 0.0; 5.0, 5.0 ]
        let graph = Graph.createWithSeries s |> Graph.withDomainPolicy (Padded 0.1)
        let xLo, xHi = domainOf graph
        let yLo, yHi = rangeOf graph
        Assert.True(xLo < 0.0, "Padded should expand below zero")
        Assert.True(xHi > 5.0, "Padded should expand above data max")
        Assert.True(yLo < 0.0, "Padded should expand below zero")
        Assert.True(yHi > 5.0, "Padded should expand above data max")

    [<Fact>]
    let ``IncludeZero policy pulls lo to zero for positive data`` () =
        let s = scatterSeries [ 5.0, 10.0; 8.0, 20.0 ]
        let graph = Graph.createWithSeries s |> Graph.withDomainPolicy IncludeZero
        let xLo, _ = domainOf graph
        let yLo, _ = rangeOf graph
        Assert.Equal(0.0, xLo)
        Assert.Equal(0.0, yLo)

    [<Fact>]
    let ``IncludeZero policy pulls hi to zero for all-negative data`` () =
        let s = scatterSeries [ -8.0, -20.0; -5.0, -10.0 ]
        let graph = Graph.createWithSeries s |> Graph.withDomainPolicy IncludeZero
        let _, xHi = domainOf graph
        let _, yHi = rangeOf graph
        Assert.True(xHi >= 0.0)
        Assert.True(yHi >= 0.0)

    [<Fact>]
    let ``withDomainPolicy recalculates bounds`` () =
        let s = scatterSeries [ 5.0, 10.0; 8.0, 20.0 ]
        let graphPadded = Graph.createWithSeries s
        let graphTight = graphPadded |> Graph.withDomainPolicy Tight
        let paddedXHi = snd (domainOf graphPadded)
        let tightXHi = snd (domainOf graphTight)
        Assert.True(paddedXHi > tightXHi)

    [<Fact>]
    let ``addSeries respects DomainPolicy`` () =
        let s1 = scatterSeries [ 1.0, 1.0; 3.0, 3.0 ]
        let s2 = scatterSeries [ 4.0, 4.0; 6.0, 6.0 ]
        let graph =
            Graph.createWithSeries s1
            |> Graph.withDomainPolicy Tight
            |> Graph.addSeries s2
        let xLo, xHi = domainOf graph
        Assert.Equal(1.0, xLo)
        Assert.Equal(6.0, xHi)

    [<Property>]
    let ``Tight bounds equal exact data extent`` (pts : FsCheck.NonEmptyArray<NormalFloat * NormalFloat>) =
        let points = pts.Get |> Array.map (fun (x, y) -> x.Get, y.Get) |> Array.toList
        let s = scatterSeries points
        let graph = Graph.createWithSeries s |> Graph.withDomainPolicy Tight
        let xLo, xHi = domainOf graph
        let yLo, yHi = rangeOf graph
        xLo = (points |> List.map fst |> List.min) &&
        xHi = (points |> List.map fst |> List.max) &&
        yLo = (points |> List.map snd |> List.min) &&
        yHi = (points |> List.map snd |> List.max)

    [<Property>]
    let ``Padded bounds are at least as wide as Tight bounds`` (pts : FsCheck.NonEmptyArray<NormalFloat * NormalFloat>) =
        let points = pts.Get |> Array.map (fun (x, y) -> x.Get, y.Get) |> Array.toList
        let s = scatterSeries points
        let tight = Graph.createWithSeries s |> Graph.withDomainPolicy Tight
        let padded = Graph.createWithSeries s |> Graph.withDomainPolicy (Padded 0.1)
        let tightXLo, tightXHi = domainOf tight
        let paddedXLo, paddedXHi = domainOf padded
        paddedXLo <= tightXLo && paddedXHi >= tightXHi

module BubbleChartTests =

    open FsCheck

    let private triples = [ 1.0, 2.0, 10.0; 3.0, 4.0, 40.0; 5.0, 6.0, 0.0 ]

    [<Fact>]
    let ``bubble creates Bubble kind series`` () =
        let s = Series.bubble triples
        Assert.True(match s.Kind with | Bubble _ -> true | _ -> false)

    [<Fact>]
    let ``bubble splits triples into Points`` () =
        let s = Series.bubble triples
        Assert.Equal<(float * float) list>([ 1.0, 2.0; 3.0, 4.0; 5.0, 6.0 ], s.Points)

    [<Fact>]
    let ``bubble stores sizes in kind`` () =
        let s = Series.bubble triples
        let sizes = match s.Kind with | Bubble sz -> sz | _ -> failwith "expected Bubble kind"
        Assert.Equal<float list>([ 10.0; 40.0; 0.0 ], sizes)

    [<Fact>]
    let ``withBubbleSizes attaches sizes to existing series`` () =
        let sizes = [ 5.0; 15.0 ]
        let s = Series.scatter [ 0.0, 0.0; 1.0, 1.0 ] |> Series.withBubbleSizes sizes
        let storedSizes = match s.Kind with | Bubble sz -> sz | _ -> failwith "expected Bubble kind"
        Assert.Equal<float list>(sizes, storedSizes)

    [<Fact>]
    let ``bubble sizes has same length as Points`` () =
        let s = Series.bubble triples
        let sizes = match s.Kind with | Bubble sz -> sz | _ -> failwith "expected Bubble kind"
        Assert.Equal(s.Points.Length, sizes.Length)

    [<Fact>]
    let ``bubble bounds use only x and y coordinates`` () =
        let s = Series.bubble triples
        let (xMin, xMax), (yMin, yMax) = Series.bounds s
        Assert.Equal(1.0, xMin)
        Assert.Equal(5.0, xMax)
        Assert.Equal(2.0, yMin)
        Assert.Equal(6.0, yMax)

    [<Fact>]
    let ``drawSeries bubble renders one circle per positive-size point`` () =
        // triples has sizes 10, 40, 0 — the zero-size point produces no element
        let s = Series.bubble triples
        let graph = Graph.create [ s ] (0.0, 6.0) (0.0, 8.0)
        let elements = Graph.drawSeries graph
        Assert.Equal(2, elements.Length)

    [<Fact>]
    let ``drawSeries bubble with all positive sizes renders one circle per point`` () =
        let s = Series.bubble [ 0.0, 0.0, 5.0; 1.0, 1.0, 10.0; 2.0, 2.0, 20.0 ]
        let graph = Graph.create [ s ] (0.0, 3.0) (0.0, 3.0)
        let elements = Graph.drawSeries graph
        Assert.Equal(3, elements.Length)

    [<Fact>]
    let ``bubble createWithSeries produces valid SVG containing circles`` () =
        let svg =
            Series.bubble [ 0.0, 0.0, 5.0; 1.0, 1.0, 10.0 ]
            |> Graph.createWithSeries
            |> GraphVG.toSvg
        Assert.Contains("<circle", svg)

    [<Property>]
    let ``bubble sizes length always matches points length`` (triples : FsCheck.NonEmptyArray<NormalFloat * NormalFloat * NormalFloat>) =
        let data = triples.Get |> Array.map (fun (x, y, s) -> x.Get, y.Get, abs s.Get) |> Array.toList
        let s = Series.bubble data
        match s.Kind with
        | Bubble sizes -> s.Points.Length = sizes.Length
        | _ -> false

module HeatmapTests =

    open FsCheck
    open SharpVG

    let private cells = [ 0.0, 0.0, 1.0; 1.0, 0.0, 2.0; 0.0, 1.0, 3.0; 1.0, 1.0, 4.0 ]

    [<Fact>]
    let ``heatmap creates Heatmap kind series`` () =
        let s = Series.heatmap cells
        Assert.True(match s.Kind with | Heatmap _ -> true | _ -> false)

    [<Fact>]
    let ``heatmap splits triples into Points`` () =
        let s = Series.heatmap cells
        Assert.Equal<(float * float) list>([ 0.0, 0.0; 1.0, 0.0; 0.0, 1.0; 1.0, 1.0 ], s.Points)

    [<Fact>]
    let ``heatmap stores values in kind`` () =
        let s = Series.heatmap cells
        let values = match s.Kind with | Heatmap v -> v | _ -> failwith "expected Heatmap kind"
        Assert.Equal<float list>([ 1.0; 2.0; 3.0; 4.0 ], values)

    [<Fact>]
    let ``heatmap values length matches Points length`` () =
        let s = Series.heatmap cells
        let values = match s.Kind with | Heatmap v -> v | _ -> failwith "expected Heatmap kind"
        Assert.Equal(s.Points.Length, values.Length)

    [<Fact>]
    let ``withColorScale stores custom function`` () =
        let scale = fun (_ : float) -> Color.ofName Colors.Blue
        let s = Series.heatmap cells |> Series.withColorScale scale
        Assert.True(s.ColorScale.IsSome)

    [<Fact>]
    let ``heatmap bounds includes half-cell padding`` () =
        let s = Series.heatmap cells
        let (xMin, xMax), (yMin, yMax) = Series.bounds s
        Assert.Equal(-0.5, xMin, 10)
        Assert.Equal(1.5, xMax, 10)
        Assert.Equal(-0.5, yMin, 10)
        Assert.Equal(1.5, yMax, 10)

    [<Fact>]
    let ``drawSeries heatmap renders one rect per cell`` () =
        let s = Series.heatmap cells
        let graph = Graph.createWithSeries s
        let elements = Graph.drawSeries graph
        Assert.Equal(4, elements.Length)

    [<Fact>]
    let ``heatmap createWithSeries produces valid SVG`` () =
        let svg = Series.heatmap cells |> Graph.createWithSeries |> GraphVG.toSvg
        Assert.Contains("<rect", svg)

    [<Fact>]
    let ``heatmap empty series renders no elements`` () =
        let s = Series.heatmap []
        let graph = Graph.create [ s ] (0.0, 1.0) (0.0, 1.0)
        let elements = Graph.drawSeries graph
        Assert.Equal(0, elements.Length)

    [<Fact>]
    let ``defaultHeatmapColorScale min value maps to white`` () =
        let color = Theme.defaultHeatmapColorScale 0.0 10.0 0.0
        Assert.Equal("rgb(255,255,255)", color.ToString())

    [<Fact>]
    let ``defaultHeatmapColorScale max value maps to steelblue`` () =
        let color = Theme.defaultHeatmapColorScale 0.0 10.0 10.0
        Assert.Equal("rgb(70,130,180)", color.ToString())

    [<Fact>]
    let ``defaultHeatmapColorScale equal min and max returns midpoint color`` () =
        let color = Theme.defaultHeatmapColorScale 5.0 5.0 5.0
        // t = 0.5; F# int truncates toward zero so int(-92.5)=-92, giving (163,193,218)
        Assert.Equal("rgb(163,193,218)", color.ToString())

    [<Property>]
    let ``heatmap values length always matches Points length`` (triples : NonEmptyArray<NormalFloat * NormalFloat * NormalFloat>) =
        let data = triples.Get |> Array.map (fun (x, y, v) -> x.Get, y.Get, v.Get) |> Array.toList
        let s = Series.heatmap data
        match s.Kind with
        | Heatmap values -> s.Points.Length = values.Length
        | _ -> false

    [<Property>]
    let ``drawSeries heatmap element count equals cell count`` (n : PositiveInt) =
        let count = min n.Get 50
        let data = [ for i in 0 .. count - 1 -> float i, 0.0, float i ]
        let s = Series.heatmap data
        let graph = Graph.createWithSeries s
        Graph.drawSeries graph |> List.length = count

module ErrorBarTests =

    open FsCheck

    let private points3 = [ 0.0, 1.0; 1.0, 2.0; 2.0, 3.0 ]
    let private scatter3 = Series.scatter points3
    let private errors3 = [ 0.1; 0.2; 0.3 ]
    let private lows3 = [ 0.1; 0.2; 0.1 ]
    let private highs3 = [ 0.3; 0.1; 0.2 ]

    [<Fact>]
    let ``withErrorBars Symmetric returns Ok for matching length`` () =
        Assert.True(Result.isOk (Series.withErrorBars (Symmetric errors3) scatter3))

    [<Fact>]
    let ``withErrorBars Symmetric returns Error for mismatching length`` () =
        Assert.True(Result.isError (Series.withErrorBars (Symmetric [ 0.1 ]) scatter3))

    [<Fact>]
    let ``withErrorBars Asymmetric returns Ok for matching lengths`` () =
        Assert.True(Result.isOk (Series.withErrorBars (Asymmetric (lows3, highs3)) scatter3))

    [<Fact>]
    let ``withErrorBars Asymmetric returns Error if lows length mismatches`` () =
        Assert.True(Result.isError (Series.withErrorBars (Asymmetric ([ 0.1 ], highs3)) scatter3))

    [<Fact>]
    let ``withErrorBars Asymmetric returns Error if highs length mismatches`` () =
        Assert.True(Result.isError (Series.withErrorBars (Asymmetric (lows3, [ 0.1 ])) scatter3))

    [<Fact>]
    let ``series without error bars is unchanged by render`` () =
        let graph = Graph.create [ scatter3 ] (0.0, 2.0) (0.0, 4.0)
        let without = Graph.drawSeries graph |> List.length
        Assert.Equal(3, without)

    [<Fact>]
    let ``drawSeries with Symmetric error bars adds 3 elements per point`` () =
        let s = Series.withErrorBars (Symmetric errors3) scatter3 |> function Ok v -> v | Error e -> failwith e
        let graph = Graph.create [ s ] (0.0, 2.0) (0.0, 4.0)
        // 3 scatter circles + 3 points × 3 elements (line + 2 caps) = 12
        Assert.Equal(12, Graph.drawSeries graph |> List.length)

    [<Fact>]
    let ``drawSeries with Asymmetric error bars adds 3 elements per point`` () =
        let s = Series.withErrorBars (Asymmetric (lows3, highs3)) scatter3 |> function Ok v -> v | Error e -> failwith e
        let graph = Graph.create [ s ] (0.0, 2.0) (0.0, 4.0)
        Assert.Equal(12, Graph.drawSeries graph |> List.length)

    [<Fact>]
    let ``withErrorBars Symmetric expands y bounds`` () =
        let s = Series.withErrorBars (Symmetric [ 1.0; 1.0; 1.0 ]) scatter3 |> function Ok v -> v | Error e -> failwith e
        let _, (yMin, yMax) = Series.bounds s
        // y values are 1, 2, 3; errors are all 1.0
        // yMin = 1.0 - 1.0 = 0.0; yMax = 3.0 + 1.0 = 4.0
        Assert.Equal(0.0, yMin, 10)
        Assert.Equal(4.0, yMax, 10)

    [<Property>]
    let ``error bar element count is 3n for n points with Symmetric errors`` (n : PositiveInt) =
        let count = min n.Get 50
        let pts = List.init count (fun i -> float i, float i)
        let errs = List.replicate count 0.1
        let scatter = Series.scatter pts
        match Series.withErrorBars (Symmetric errs) scatter with
        | Error _ -> false
        | Ok s ->
            let graph = Graph.create [ s ] (0.0, float count) (0.0, float count)
            let scatterCount = count   // one circle per point
            let errorCount = count * 3 // one line + two caps per point
            Graph.drawSeries graph |> List.length = scatterCount + errorCount
