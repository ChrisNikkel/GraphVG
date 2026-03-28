namespace GraphVG

type SeriesKind =
    | Scatter
    | Line
    | Area

type Series = {
    Points : (float * float) list
    Kind   : SeriesKind
    Label  : string option
}

module Series =

    let create kind points =
        { Points = points; Kind = kind; Label = None }

    let scatter points =
        create Scatter points

    let line points =
        create Line points

    let area points =
        create Area points

    let withLabel label series =
        { series with Label = Some label }
