namespace GraphVG

open SharpVG
open CommonMath

type PointShape = Circle | Square | Diamond | Cross | Triangle

type StrokeDash =
    | Solid
    | Dashed
    | Dotted
    | DashDot

type StepMode = Before | After | Mid

type ErrorBar =
    | Symmetric of float list
    | Asymmetric of low: float list * high: float list

type OhlcPoint =
    {
        X : float
        Open : float
        High : float
        Low : float
        Close : float
    }

type SeriesKind =
    | Scatter
    | Line
    | StepLine of StepMode
    | Area
    | StackedArea
    | NormalizedStackedArea
    | Streamgraph
    | Histogram of binWidth: float
    | Box
    | Bar
    | HorizontalBar
    | Bubble of sizes: float list
    | Heatmap of values: float list
    | Band of highs: float list
    | Candlestick of ohlc: OhlcPoint list
    | Ohlc of ohlc: OhlcPoint list

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
        ColorScale : (float -> Color) option
        ErrorBars : ErrorBar option
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
            ColorScale = None
            ErrorBars = None
        }

    let scatter points =
        create Scatter points

    let line points =
        create Line points

    let stepLine points =
        create (StepLine After) points

    let withStepMode mode (series : Series) =
        match series.Kind with
        | StepLine _ -> { series with Kind = StepLine mode }
        | _ -> series

    let area points =
        create Area points

    let stackedArea points =
        create StackedArea points

    let normalizedStackedArea points =
        create NormalizedStackedArea points

    let streamgraph points =
        create Streamgraph points

    let bar points =
        create Bar points

    let horizontalBar points =
        create HorizontalBar points

    let bubble (triples : (float * float * float) list) =
        let points = triples |> List.map (fun (x, y, _) -> x, y)
        let sizes = triples |> List.map (fun (_, _, s) -> s)
        create (Bubble sizes) points

    let withBubbleSizes (sizes : float list) (series : Series) =
        { series with Kind = Bubble sizes }

    let heatmap (triples : (float * float * float) list) =
        let points = triples |> List.map (fun (col, row, _) -> col, row)
        let values = triples |> List.map (fun (_, _, v) -> v)
        create (Heatmap values) points

    let withColorScale (colorScale : float -> Color) (series : Series) =
        { series with ColorScale = Some colorScale }

    let band (triples : (float * float * float) list) =
        let points = triples |> List.map (fun (x, yLow, _) -> x, yLow)
        let highs = triples |> List.map (fun (_, _, yHigh) -> yHigh)
        create (Band highs) points

    let candlestick (bars : OhlcPoint list) =
        let points = bars |> List.map (fun b -> b.X, b.Close)
        create (Candlestick bars) points

    let ohlc (bars : OhlcPoint list) =
        let points = bars |> List.map (fun b -> b.X, b.Close)
        create (Ohlc bars) points

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

    let withErrorBars (errorBar : ErrorBar) (series : Series) : Result<Series, string> =
        let n = series.Points.Length
        match errorBar with
        | Symmetric errors when errors.Length <> n ->
            Error (sprintf "Symmetric error list length %d does not match point count %d" errors.Length n)
        | Asymmetric (lows, _) when lows.Length <> n ->
            Error (sprintf "Asymmetric low list length %d does not match point count %d" lows.Length n)
        | Asymmetric (_, highs) when highs.Length <> n ->
            Error (sprintf "Asymmetric high list length %d does not match point count %d" highs.Length n)
        | _ ->
            Ok { series with ErrorBars = Some errorBar }

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
        create (Histogram binWidth) bins

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
        | Histogram binWidth ->
            let xs, ys = series.Points |> List.unzip
            (List.min xs, List.max xs + binWidth), (0.0, List.fold max 0.0 ys)
        | Box ->
            let xs, ys = series.Points |> List.unzip
            let position = List.min xs
            (position - 0.5, position + 0.5), (List.min ys, List.max ys)
        | Bar ->
            let xs, ys = series.Points |> List.unzip
            (List.min xs - 0.5, List.max xs + 0.5), (min 0.0 (List.min ys), List.max ys)
        | HorizontalBar ->
            let xs, ys = series.Points |> List.unzip
            (min 0.0 (List.min xs), List.max xs), (List.min ys - 0.5, List.max ys + 0.5)
        | Heatmap _ ->
            let xs, ys = series.Points |> List.unzip
            let halfSpan values =
                let sorted = values |> List.sort |> List.distinct
                match sorted |> List.pairwise |> List.map (fun (a, b) -> b - a) with
                | spacings when not spacings.IsEmpty -> List.min spacings / 2.0
                | _ -> 0.5
            (List.min xs - halfSpan xs, List.max xs + halfSpan xs),
            (List.min ys - halfSpan ys, List.max ys + halfSpan ys)
        | Band highs ->
            let xs, yLows = series.Points |> List.unzip
            (List.min xs, List.max xs), (List.min yLows, List.max highs)
        | Candlestick bars | Ohlc bars ->
            let xs = bars |> List.map (fun b -> b.X)
            let yMin = bars |> List.map (fun b -> b.Low) |> List.min
            let yMax = bars |> List.map (fun b -> b.High) |> List.max
            (List.min xs, List.max xs), (yMin, yMax)
        | _ ->
            let xs, ys = series.Points |> List.unzip
            let yMin, yMax =
                match series.ErrorBars with
                | None ->
                    List.min ys, List.max ys
                | Some (Symmetric errors) ->
                    List.map2 (fun y e -> y - e) ys errors |> List.min,
                    List.map2 (fun y e -> y + e) ys errors |> List.max
                | Some (Asymmetric (lows, highs)) ->
                    List.map2 (fun y lo -> y - lo) ys lows |> List.min,
                    List.map2 (fun y hi -> y + hi) ys highs |> List.max
            (List.min xs, List.max xs), (yMin, yMax)
