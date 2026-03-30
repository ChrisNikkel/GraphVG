namespace GraphVG

open SharpVG
open CommonMath

type SeriesKind =
    | Scatter
    | Line
    | Area
    | Histogram
    | Box

type PointShape = Circle | Square | Diamond | Cross | Triangle

type StrokeDash =
    | Solid
    | Dashed
    | Dotted
    | DashDot

type Series =
    {
        Points : (float * float) list
        Kind : SeriesKind
        Label : string option
        StrokeWidth : Length option
        PointRadius : Length option
        PointShape : PointShape
        StrokeDash : StrokeDash
        Visible : bool
        Opacity : float
        BinWidth : float option
    }

module Series =

    let create kind points =
        {
            Points = points
            Kind = kind
            Label = None
            StrokeWidth = None
            PointRadius = None
            PointShape = Circle
            StrokeDash = Solid
            Visible = true
            Opacity = 1.0
            BinWidth = None
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

    let withPointShape shape (series : Series) =
        { series with PointShape = shape }

    let withStrokeDash dash (series : Series) =
        { series with StrokeDash = dash }

    let withVisible visible (series : Series) =
        { series with Visible = visible }

    let withOpacity opacity (series : Series) =
        { series with Opacity = max 0.0 (min 1.0 opacity) }

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

    // ── Histogram ─────────────────────────────────────────────────────────────

    let private defaultBinCount (values : float list) =
        max 5 (int (System.Math.Ceiling(1.0 + System.Math.Log(float values.Length, 2.0))))

    let histogramWithBins (binCount : int) (values : float list) =
        let sorted = List.sort values
        let minimum = List.head sorted
        let maximum = List.last sorted
        let binWidth = (maximum - minimum) / float binCount
        let bins =
            [ 0 .. binCount - 1 ]
            |> List.map (fun i ->
                let left = minimum + float i * binWidth
                let right = if i = binCount - 1 then maximum + epsilon else left + binWidth
                let count = values |> List.filter (fun v -> v >= left && v < right) |> List.length
                left, float count)
        { create Histogram bins with BinWidth = Some binWidth }

    let histogram (values : float list) =
        histogramWithBins (defaultBinCount values) values

    // ── Box plot ──────────────────────────────────────────────────────────────

    let boxAt (position : float) (values : float list) =
        let sorted = values |> List.sort |> Array.ofList
        let n = Array.length sorted
        let percentile p =
            let idx = p * float (n - 1)
            let lower = int (floor idx)
            let upper = min (n - 1) (int (ceil idx))
            sorted.[lower] + (idx - float lower) * (float sorted.[upper] - float sorted.[lower])
        let points =
            [ position, sorted.[0]
              position, percentile 0.25
              position, percentile 0.50
              position, percentile 0.75
              position, sorted.[n - 1] ]
        create Box points

    let box (values : float list) =
        boxAt 0.5 values

    // ── Bounds ────────────────────────────────────────────────────────────────

    /// Data-aware bounds for auto-scaling. Histogram extends x domain to cover
    /// the right edge of the last bin; Box gives a unit x span around the position.
    let bounds (series : Series) =
        match series.Kind with
        | Histogram ->
            let xs, ys = series.Points |> List.unzip
            let binWidth = series.BinWidth |> Option.defaultValue 0.0
            (List.min xs, List.max xs + binWidth), (0.0, List.fold max 0.0 ys)
        | Box ->
            let xs, ys = series.Points |> List.unzip
            let position = List.min xs
            (position - 0.5, position + 0.5), (List.min ys, List.max ys)
        | _ ->
            let xs, ys = series.Points |> List.unzip
            (List.min xs, List.max xs), (List.min ys, List.max ys)
