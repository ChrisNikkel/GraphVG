module Tests

open GraphVG

open Xunit
open FsCheck.Xunit


[<Fact>]
let ``linear apply maps domain min to range min`` () =
    let s = Scale.linear (0.0, 10.0) (0.0, 100.0)
    Assert.Equal(0.0, Scale.apply s 0.0)

[<Fact>]
let ``linear apply maps domain max to range max`` () =
    let s = Scale.linear (0.0, 10.0) (0.0, 100.0)
    Assert.Equal(100.0, Scale.apply s 10.0)

[<Fact>]
let ``linear apply maps midpoint correctly`` () =
    let s = Scale.linear (0.0, 10.0) (0.0, 100.0)
    Assert.Equal(50.0, Scale.apply s 5.0)

[<Fact>]
let ``linear apply works with inverted range`` () =
    let s = Scale.linear (0.0, 10.0) (100.0, 0.0)
    Assert.Equal(100.0, Scale.apply s 0.0)
    Assert.Equal(0.0,   Scale.apply s 10.0)

// Linear – invert

[<Fact>]
let ``linear invert is the inverse of apply`` () =
    let s = Scale.linear (0.0, 10.0) (0.0, 100.0)
    let roundtrip v = v |> Scale.apply s |> Scale.invert s
    Assert.Equal(2.5, roundtrip 2.5)
    Assert.Equal(7.0, roundtrip 7.0)

// Linear – ticks

[<Fact>]
let ``linear ticks returns correct count`` () =
    let s = Scale.linear (0.0, 10.0) (0.0, 100.0)
    Assert.Equal(5, Scale.ticks s 5 |> List.length)

[<Fact>]
let ``linear ticks starts at domain min and ends at domain max`` () =
    let s = Scale.linear (0.0, 10.0) (0.0, 100.0)
    let t = Scale.ticks s 6
    Assert.Equal(0.0,  List.head t)
    Assert.Equal(10.0, List.last t)

[<Fact>]
let ``linear ticks count 1 returns domain min`` () =
    let s = Scale.linear (2.0, 8.0) (0.0, 100.0)
    Assert.Equal<float list>([ 2.0 ], Scale.ticks s 1)

// Log – apply

[<Fact>]
let ``log apply maps domain min to range min`` () =
    let s = Scale.log (1.0, 1000.0) (0.0, 100.0) 10.0
    Assert.Equal(0.0, Scale.apply s 1.0)

[<Fact>]
let ``log apply maps domain max to range max`` () =
    let s = Scale.log (1.0, 1000.0) (0.0, 100.0) 10.0
    Assert.Equal(100.0, Scale.apply s 1000.0, 10)

[<Fact>]
let ``log apply maps geometric midpoint to range midpoint`` () =
    let s = Scale.log (1.0, 100.0) (0.0, 100.0) 10.0
    Assert.Equal(50.0, Scale.apply s 10.0, 10)

// Log – invert

[<Fact>]
let ``log invert is the inverse of apply`` () =
    let s = Scale.log (1.0, 1000.0) (0.0, 300.0) 10.0
    let roundtrip v = v |> Scale.apply s |> Scale.invert s
    Assert.Equal(10.0,  roundtrip 10.0,  10)
    Assert.Equal(100.0, roundtrip 100.0, 10)

// Log – ticks

[<Fact>]
let ``log ticks returns powers of base within domain`` () =
    let s = Scale.log (1.0, 1000.0) (0.0, 100.0) 10.0
    Assert.Equal<float list>([ 1.0; 10.0; 100.0; 1000.0 ], Scale.ticks s 0)

[<Fact>]
let ``log ticks base 2 returns correct powers`` () =
    let s = Scale.log (1.0, 8.0) (0.0, 100.0) 2.0
    Assert.Equal<float list>([ 1.0; 2.0; 4.0; 8.0 ], Scale.ticks s 0)

module SeriesTests =

    [<Fact>]
    let ``scatter sets kind to Scatter`` () =
        let s = Series.scatter [ 0.0, 0.0; 1.0, 1.0 ]
        Assert.Equal(Scatter, s.Kind)

    [<Fact>]
    let ``line sets kind to Line`` () =
        let s = Series.line [ 0.0, 0.0; 1.0, 1.0 ]
        Assert.Equal(Line, s.Kind)

    [<Fact>]
    let ``area sets kind to Area`` () =
        let s = Series.area [ 0.0, 0.0; 1.0, 1.0 ]
        Assert.Equal(Area, s.Kind)

    [<Fact>]
    let ``create has no label by default`` () =
        let s = Series.scatter [ 0.0, 0.0 ]
        Assert.Equal(None, s.Label)

    [<Fact>]
    let ``withLabel sets label`` () =
        let s = Series.scatter [ 0.0, 0.0 ] |> Series.withLabel "my series"
        Assert.Equal(Some "my series", s.Label)

    [<Fact>]
    let ``points are preserved`` () =
        let pts = [ 1.0, 2.0; 3.0, 4.0 ]
        let s = Series.line pts
        Assert.Equal<(float * float) list>(pts, s.Points)

    [<Fact>]
    let ``ofFunction with samples=1 returns single point at tMin`` () =
        let s = Series.ofFunction Scatter (fun t -> t, t) 2.0 5.0 1
        Assert.Equal<(float * float) list>([ 2.0, 2.0 ], s.Points)

    [<Fact>]
    let ``lineOfFunction sets kind to Line`` () =
        let s = Series.lineOfFunction (fun t -> cos t, sin t) 0.0 1.0 10
        Assert.Equal(Line, s.Kind)

    [<Fact>]
    let ``scatterOfFunction sets kind to Scatter`` () =
        let s = Series.scatterOfFunction (fun t -> t, t) 0.0 1.0 10
        Assert.Equal(Scatter, s.Kind)

    [<Property>]
    let ``ofFunction produces exactly samples points`` (samples: FsCheck.PositiveInt) =
        let n = samples.Get
        let s = Series.ofFunction Line (fun t -> t, t) 0.0 1.0 n
        s.Points.Length = n

    [<Property>]
    let ``ofFunction first point maps tMin, last maps tMax`` (samples: FsCheck.PositiveInt) =
        let n = samples.Get
        let s = Series.ofFunction Line (fun t -> t, 0.0) 0.0 1.0 n
        let first = fst (List.head s.Points)
        let last  = fst (List.last s.Points)
        if n = 1 then first = 0.0
        else first = 0.0 && last = 1.0

    [<Property>]
    let ``ofFunction preserves function values at each t`` (samples: FsCheck.PositiveInt) =
        let n = samples.Get
        let s = Series.ofFunction Line (fun t -> t, t * t) 0.0 1.0 n
        s.Points |> List.forall (fun (x, y) -> abs (y - x * x) < 1e-10)

    [<Property>]
    let ``ofFunction label is always None`` (samples: FsCheck.PositiveInt) =
        let s = Series.ofFunction Scatter (fun t -> t, t) 0.0 1.0 samples.Get
        s.Label = None

    [<Fact>]
    let ``create has no stroke width by default`` () =
        let s = Series.line [ 0.0, 0.0 ]
        Assert.Equal(None, s.StrokeWidth)

    [<Fact>]
    let ``create has no point radius by default`` () =
        let s = Series.scatter [ 0.0, 0.0 ]
        Assert.Equal(None, s.PointRadius)

    [<Fact>]
    let ``withStrokeWidth sets stroke width`` () =
        let s = Series.line [ 0.0, 0.0 ] |> Series.withStrokeWidth (SharpVG.Length.ofFloat 3.0)
        Assert.Equal(Some (SharpVG.Length.ofFloat 3.0), s.StrokeWidth)

    [<Fact>]
    let ``withPointRadius sets point radius`` () =
        let s = Series.scatter [ 0.0, 0.0 ] |> Series.withPointRadius (SharpVG.Length.ofFloat 7.0)
        Assert.Equal(Some (SharpVG.Length.ofFloat 7.0), s.PointRadius)

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
        let t = Theme.empty |> Theme.withBackground (Color.ofName Black)
        Assert.Equal(Color.ofName Black, t.Background)

    [<Fact>]
    let ``withPens replaces pens`` () =
        let pens = [ Pen.red; Pen.blue ]
        let t = Theme.empty |> Theme.withPens pens
        Assert.Equal<Pen list>(pens, t.Pens)

    [<Fact>]
    let ``withAxisPen replaces axis pen`` () =
        let t = Theme.empty |> Theme.withAxisPen Pen.red
        Assert.Equal(Pen.red, t.AxisPen)

    [<Fact>]
    let ``withGridPen sets grid pen to Some`` () =
        let t = Theme.empty |> Theme.withGridPen Pen.lightGray
        Assert.Equal(Some Pen.lightGray, t.GridPen)

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
        Assert.Equal(Pen.red,  modified.AxisPen)

module AxisTests =

    let private xScale = Scale.linear (0.0, 10.0) (0.0, 1000.0)
    let private yScale = Scale.linear (0.0, 10.0) (1000.0, 0.0)

    // Builders

    [<Fact>]
    let ``create defaults to 5 ticks and no label`` () =
        let a = Axis.create Bottom xScale
        Assert.Equal(TickCount 5, a.Ticks)
        Assert.Equal(None, a.Label)

    [<Fact>]
    let ``withTicks sets tick count`` () =
        let a = Axis.create Bottom xScale |> Axis.withTicks 10
        Assert.Equal(TickCount 10, a.Ticks)

    [<Fact>]
    let ``withTickInterval sets tick interval`` () =
        let a = Axis.create Bottom xScale |> Axis.withTickInterval 2.5
        Assert.Equal(TickInterval 2.5, a.Ticks)

    [<Fact>]
    let ``withLabel sets label`` () =
        let a = Axis.create Bottom xScale |> Axis.withLabel "X axis"
        Assert.Equal(Some "X axis", a.Label)

    // toElements – element counts

    [<Fact>]
    let ``bottom axis produces axis line plus 2 elements per tick`` () =
        let a     = Axis.create Bottom xScale |> Axis.withTicks 5
        let elems = Axis.toElements Theme.empty a
        // 1 axis line + 5 ticks × (1 tick mark + 1 label) = 11
        Assert.Equal(11, elems.Length)

    [<Fact>]
    let ``bottom axis with label produces one extra element`` () =
        let a     = Axis.create Bottom xScale |> Axis.withTicks 5 |> Axis.withLabel "X"
        let elems = Axis.toElements Theme.empty a
        Assert.Equal(12, elems.Length)

    [<Fact>]
    let ``left axis produces same structure as bottom`` () =
        let a     = Axis.create Left yScale |> Axis.withTicks 5
        let elems = Axis.toElements Theme.empty a
        Assert.Equal(11, elems.Length)

    [<Fact>]
    let ``top and right axes produce same element count`` () =
        let top   = Axis.create Top   xScale |> Axis.withTicks 3 |> Axis.toElements Theme.empty
        let right = Axis.create Right yScale |> Axis.withTicks 3 |> Axis.toElements Theme.empty
        // 1 + 3×2 = 7 each
        Assert.Equal(7, top.Length)
        Assert.Equal(7, right.Length)

module GraphTests =

    let private pts = [ 0.0, 0.0; 2.0, 4.0; 4.0, 2.0 ]
    let private s   = Series.scatter pts

    // create

    [<Fact>]
    let ``create stores series, domain, and range exactly`` () =
        let g = Graph.create [ s ] (0.0, 10.0) (-5.0, 5.0)
        Assert.Equal<Series list>([ s ], g.Series)
        Assert.Equal((0.0, 10.0), Scale.domain g.XScale)
        Assert.Equal((-5.0, 5.0), Scale.domain g.YScale)

    // addPadding

    [<Fact>]
    let ``addPadding expands domain and range symmetrically`` () =
        let g  = Graph.create [ s ] (0.0, 10.0) (0.0, 10.0)
        let g' = Graph.addPadding 0.1 g
        let dMin, dMax = Scale.domain g'.XScale
        let rMin, rMax = Scale.domain g'.YScale
        Assert.Equal(-1.0, dMin, 10)
        Assert.Equal(11.0, dMax, 10)
        Assert.Equal(-1.0, rMin, 10)
        Assert.Equal(11.0, rMax, 10)

    [<Fact>]
    let ``addPadding 0 leaves bounds unchanged`` () =
        let g  = Graph.create [ s ] (0.0, 10.0) (0.0, 10.0)
        let g' = Graph.addPadding 0.0 g
        Assert.Equal(Scale.domain g.XScale, Scale.domain g'.XScale)
        Assert.Equal(Scale.domain g.YScale, Scale.domain g'.YScale)

    // createWithSeries

    [<Fact>]
    let ``createWithSeries infers domain from points with 10 pct padding`` () =
        let g = Graph.createWithSeries s
        let dMin, dMax = Scale.domain g.XScale
        // raw x: 0..4, pad = 0.4
        Assert.Equal(-0.4, dMin, 10)
        Assert.Equal( 4.4, dMax, 10)

    [<Fact>]
    let ``createWithSeries infers range from points with 10 pct padding`` () =
        let g = Graph.createWithSeries s
        let rMin, rMax = Scale.domain g.YScale
        // raw y: 0..4, pad = 0.4
        Assert.Equal(-0.4, rMin, 10)
        Assert.Equal( 4.4, rMax, 10)

    [<Fact>]
    let ``createWithSeries stores exactly one series`` () =
        let g = Graph.createWithSeries s
        Assert.Equal(1, g.Series.Length)

    // addSeries

    [<Fact>]
    let ``addSeries appends series and recalculates bounds`` () =
        let s2 = Series.scatter [ -2.0, -2.0; 6.0, 6.0 ]
        let g  = Graph.createWithSeries s |> Graph.addSeries s2
        Assert.Equal(2, g.Series.Length)
        let dMin, dMax = Scale.domain g.XScale
        let rMin, rMax = Scale.domain g.YScale
        // raw x: -2..6, pad = 0.8
        Assert.Equal(-2.8, dMin, 10)
        Assert.Equal( 6.8, dMax, 10)
        // raw y: -2..6, pad = 0.8
        Assert.Equal(-2.8, rMin, 10)
        Assert.Equal( 6.8, rMax, 10)

    // toScaledSvgCoordinates

    [<Fact>]
    let ``toScaledSvgCoordinates maps domain min to left edge`` () =
        let g = Graph.create [ s ] (0.0, 4.0) (0.0, 4.0)
        let x, _ = Graph.toScaledSvgCoordinates g (0.0, 0.0)
        Assert.Equal(0.0, x, 10)

    [<Fact>]
    let ``toScaledSvgCoordinates maps domain max to right edge`` () =
        let g = Graph.create [ s ] (0.0, 4.0) (0.0, 4.0)
        let x, _ = Graph.toScaledSvgCoordinates g (4.0, 0.0)
        Assert.Equal(Canvas.canvasSize, x, 10)

    [<Fact>]
    let ``toScaledSvgCoordinates inverts y axis (range min maps to canvas bottom)`` () =
        let g = Graph.create [ s ] (0.0, 4.0) (0.0, 4.0)
        let _, y = Graph.toScaledSvgCoordinates g (0.0, 0.0)
        Assert.Equal(Canvas.canvasSize, y, 10)

    [<Fact>]
    let ``toScaledSvgCoordinates inverts y axis (range max maps to canvas top)`` () =
        let g = Graph.create [ s ] (0.0, 4.0) (0.0, 4.0)
        let _, y = Graph.toScaledSvgCoordinates g (0.0, 4.0)
        Assert.Equal(0.0, y, 10)

    // drawSeries – smoke tests (non-empty element lists per SeriesKind)

    [<Fact>]
    let ``drawSeries scatter produces one element per point`` () =
        let g    = Graph.create [ Series.scatter pts ] (0.0, 4.0) (0.0, 4.0)
        let elms = Graph.drawSeries g
        Assert.Equal(pts.Length, elms.Length)

    [<Fact>]
    let ``drawSeries line produces exactly one polyline element`` () =
        let g    = Graph.create [ Series.line pts ] (0.0, 4.0) (0.0, 4.0)
        let elms = Graph.drawSeries g
        Assert.Equal(1, elms.Length)

    [<Fact>]
    let ``drawSeries area produces exactly one polygon element`` () =
        let g    = Graph.create [ Series.area pts ] (0.0, 4.0) (0.0, 4.0)
        let elms = Graph.drawSeries g
        Assert.Equal(1, elms.Length)

    [<Fact>]
    let ``drawSeries two series produces elements for both`` () =
        let g =
            Graph.create
                [ Series.scatter pts; Series.scatter pts ]
                (0.0, 4.0) (0.0, 4.0)
        let elms = Graph.drawSeries g
        Assert.Equal(pts.Length * 2, elms.Length)

    [<Fact>]
    let ``drawSeries scatter with custom radius appears in SVG output`` () =
        let series = Series.scatter pts |> Series.withPointRadius (SharpVG.Length.ofFloat 8.0)
        let svg = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        Assert.Contains("8", svg)

    [<Fact>]
    let ``drawSeries line with custom stroke width appears in SVG output`` () =
        let series = Series.line pts |> Series.withStrokeWidth (SharpVG.Length.ofFloat 5.0)
        let svg = Graph.create [ series ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        Assert.Contains("5", svg)

    [<Fact>]
    let ``drawSeries scatter custom radius produces different SVG than default`` () =
        let defaultSvg = Graph.create [ Series.scatter pts ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        let customSvg  = Graph.create [ Series.scatter pts |> Series.withPointRadius (SharpVG.Length.ofFloat 9.0) ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        Assert.True(defaultSvg <> customSvg)

    [<Fact>]
    let ``drawSeries line custom stroke width produces different SVG than default`` () =
        let defaultSvg = Graph.create [ Series.line pts ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        let customSvg  = Graph.create [ Series.line pts |> Series.withStrokeWidth (SharpVG.Length.ofFloat 4.0) ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        Assert.True(defaultSvg <> customSvg)

    // withTheme

    [<Fact>]
    let ``withTheme replaces theme on graph`` () =
        let g = Graph.create [ s ] (0.0, 4.0) (0.0, 4.0)
        Assert.Equal(Theme.empty, g.Theme)
        let g' = g |> Graph.withTheme Theme.light
        Assert.Equal(Theme.light, g'.Theme)

    // withAxes / axis suppression

    [<Fact>]
    let ``default graph has both axes set`` () =
        let g = Graph.create [ s ] (0.0, 4.0) (0.0, 4.0)
        Assert.True(g.XAxis.IsSome)
        Assert.True(g.YAxis.IsSome)

    [<Fact>]
    let ``withAxes Axis.none suppresses both axes`` () =
        let g = Graph.create [ s ] (0.0, 4.0) (0.0, 4.0) |> Graph.withAxes Axis.none
        Assert.Equal(None, g.XAxis)
        Assert.Equal(None, g.YAxis)

    [<Fact>]
    let ``withXAxis None suppresses only x axis`` () =
        let g = Graph.create [ s ] (0.0, 4.0) (0.0, 4.0) |> Graph.withXAxis None
        Assert.Equal(None, g.XAxis)
        Assert.True(g.YAxis.IsSome)

    // withTitle

    [<Fact>]
    let ``withTitle sets title`` () =
        let g = Graph.create [ s ] (0.0, 4.0) (0.0, 4.0) |> Graph.withTitle "My Chart"
        Assert.Equal(Some "My Chart", g.Title)

    [<Fact>]
    let ``default graph has no title`` () =
        let g = Graph.create [ s ] (0.0, 4.0) (0.0, 4.0)
        Assert.Equal(None, g.Title)

module GraphVGTests =

    let private pts = [ 0.0, 0.0; 2.0, 4.0; 4.0, 2.0 ]
    let private s   = Series.line pts

    [<Fact>]
    let ``toHtml returns non-empty string`` () =
        let html = Graph.create [ s ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toHtml
        Assert.True(html.Length > 0)

    [<Fact>]
    let ``toHtml output contains svg tag`` () =
        let html = Graph.create [ s ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.toHtml
        Assert.Contains("<svg", html)

    [<Fact>]
    let ``render returns svg string without html wrapper`` () =
        let svg = Graph.create [ s ] (0.0, 4.0) (0.0, 4.0) |> GraphVG.render
        Assert.Contains("<svg", svg)
        Assert.DoesNotContain("<!DOCTYPE", svg)

    [<Fact>]
    let ``toHtml with title contains title text`` () =
        let html =
            Graph.create [ s ] (0.0, 4.0) (0.0, 4.0)
            |> Graph.withTitle "Test Title"
            |> GraphVG.toHtml
        Assert.Contains("Test Title", html)

    [<Fact>]
    let ``toHtml with Theme.light produces more elements than Theme.empty (grid lines)`` () =
        let g = Graph.create [ s ] (0.0, 4.0) (0.0, 4.0)
        // Theme.light has GridPen; Theme.empty does not — light should produce a longer SVG
        let svgLight = g |> Graph.withTheme Theme.light |> GraphVG.render
        let svgEmpty = g |> GraphVG.render
        Assert.True(svgLight.Length > svgEmpty.Length)

    [<Fact>]
    let ``toHtml with axes suppressed produces shorter output than with default axes`` () =
        let g = Graph.create [ s ] (0.0, 4.0) (0.0, 4.0)
        let withAxes    = g |> GraphVG.render
        let withoutAxes = g |> Graph.withAxes Axis.none |> GraphVG.render
        Assert.True(withoutAxes.Length < withAxes.Length)

module AxisHideTests =

    // Scale with 0.0 in domain so hideOrigin has a tick to suppress
    let private scale = Scale.linear (-5.0, 5.0) (0.0, 1000.0)

    [<Fact>]
    let ``hideOriginTick sets HideOriginTick only`` () =
        let a = Axis.create Bottom scale |> Axis.hideOriginTick
        Assert.True(a.HideOriginTick)
        Assert.False(a.HideOriginLabel)

    [<Fact>]
    let ``hideOriginLabel sets HideOriginLabel only`` () =
        let a = Axis.create Bottom scale |> Axis.hideOriginLabel
        Assert.False(a.HideOriginTick)
        Assert.True(a.HideOriginLabel)

    [<Fact>]
    let ``hideOrigin sets both HideOriginTick and HideOriginLabel`` () =
        let a = Axis.create Bottom scale |> Axis.hideOrigin
        Assert.True(a.HideOriginTick)
        Assert.True(a.HideOriginLabel)

    [<Fact>]
    let ``hideBoundsTick sets HideBoundsTick only`` () =
        let a = Axis.create Bottom scale |> Axis.hideBoundsTick
        Assert.True(a.HideBoundsTick)
        Assert.False(a.HideBoundsLabel)

    [<Fact>]
    let ``hideBoundsLabel sets HideBoundsLabel only`` () =
        let a = Axis.create Bottom scale |> Axis.hideBoundsLabel
        Assert.False(a.HideBoundsTick)
        Assert.True(a.HideBoundsLabel)

    [<Fact>]
    let ``hideOriginTick removes tick element at origin`` () =
        let full   = Axis.create Bottom scale |> Axis.withTicks 5 |> Axis.toElements Theme.empty
        let hidden = Axis.create Bottom scale |> Axis.withTicks 5 |> Axis.hideOriginTick |> Axis.toElements Theme.empty
        Assert.Equal(full.Length - 1, hidden.Length)

    [<Fact>]
    let ``hideOriginLabel removes label element at origin`` () =
        let full   = Axis.create Bottom scale |> Axis.withTicks 5 |> Axis.toElements Theme.empty
        let hidden = Axis.create Bottom scale |> Axis.withTicks 5 |> Axis.hideOriginLabel |> Axis.toElements Theme.empty
        Assert.Equal(full.Length - 1, hidden.Length)

    [<Fact>]
    let ``hideOrigin removes both tick and label at origin`` () =
        let full   = Axis.create Bottom scale |> Axis.withTicks 5 |> Axis.toElements Theme.empty
        let hidden = Axis.create Bottom scale |> Axis.withTicks 5 |> Axis.hideOrigin |> Axis.toElements Theme.empty
        Assert.Equal(full.Length - 2, hidden.Length)

    [<Fact>]
    let ``hideOrigin on axis with no tick at origin leaves count unchanged`` () =
        let noZeroScale = Scale.linear (1.0, 10.0) (0.0, 1000.0)
        let full   = Axis.create Bottom noZeroScale |> Axis.withTicks 5 |> Axis.toElements Theme.empty
        let hidden = Axis.create Bottom noZeroScale |> Axis.withTicks 5 |> Axis.hideOrigin |> Axis.toElements Theme.empty
        Assert.Equal(full.Length, hidden.Length)
