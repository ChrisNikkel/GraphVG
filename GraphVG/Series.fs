namespace GraphVG

open SharpVG

type SeriesKind =
    | Scatter
    | Line
    | Area

type StrokeDash =
    | Solid
    | Dashed
    | Dotted
    | DashDot

type Series =
    {
        Points      : (float * float) list
        Kind        : SeriesKind
        Label       : string option
        StrokeWidth : Length option
        PointRadius : Length option
        StrokeDash  : StrokeDash
    }

module Series =

    let create kind points =
        {
            Points = points
            Kind = kind
            Label = None
            StrokeWidth = None
            PointRadius = None
            StrokeDash = Solid
        }

    let scatter points =
        create Scatter points

    let line points =
        create Line points

    let area points =
        create Area points

    let withLabel label series =
        { series with Label = Some label }

    let withStrokeWidth width (series : Series) =
        { series with StrokeWidth = Some width }

    let withPointRadius radius (series : Series) =
        { series with PointRadius = Some radius }

    let withStrokeDash dash (series : Series) =
        { series with StrokeDash = dash }

    let ofFunction kind (f: float -> float * float) tMin tMax samples =
        let points =
            if samples <= 1 then
                [ f tMin ]
            else
                let step = (tMax - tMin) / float (samples - 1)
                [ for i in 0 .. samples - 1 -> f (tMin + float i * step) ]
        create kind points

    let lineOfFunction f tMin tMax samples = ofFunction Line f tMin tMax samples

    let scatterOfFunction f tMin tMax samples = ofFunction Scatter f tMin tMax samples
