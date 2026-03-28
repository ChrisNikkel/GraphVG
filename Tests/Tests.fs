module Tests

open Xunit
open GraphVG

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
