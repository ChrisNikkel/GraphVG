module Tests

open GraphVG

open Xunit

module ScaleTests =

    // Linear – apply

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
