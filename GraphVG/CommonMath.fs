namespace GraphVG

module CommonMath =

    let canvasSize = 1000.0

    type GraphPadding =
        {
            Top : float
            Right : float
            Bottom : float
            Left : float
        }

    type LayoutSpacing =
        {
            OuterMargin : float
            TitlePadding : float
            AxisLabelPadding : float
            TickLabelPadding : float
        }

    module LayoutSpacing =

        let create outerMargin titlePadding axisLabelPadding tickLabelPadding =
            {
                OuterMargin = outerMargin
                TitlePadding = titlePadding
                AxisLabelPadding = axisLabelPadding
                TickLabelPadding = tickLabelPadding
            }

        let defaults =
            create 20.0 16.0 4.0 4.0

    let estimatedTextWidth fontSize (text : string) =
        float text.Length * fontSize * 0.6

    let epsilon : float = 1e-10

    let isNear (expected : float) (actual : float) =
        abs (actual - expected) < epsilon

    let clamp (lower : float) (upper : float) (value : float) = max lower (min upper value)

    let padRange (percent : float) (minimum : float, maximum : float) =
        let span = maximum - minimum
        minimum - span * percent, maximum + span * percent

    // ── Unit shapes (centered at origin, radius 1) ─────────────────────────────

    let squareUnit = [ -1.0, -1.0; 1.0, -1.0; 1.0, 1.0; -1.0, 1.0 ]
    let diamondUnit = [ 0.0, -1.0; 1.0, 0.0; 0.0, 1.0; -1.0, 0.0 ]
    let triangleUnit = [ 0.0, -1.0; 1.0, 1.0; -1.0, 1.0 ]
    let crossUnit = [ (-1.0, 0.0), (1.0, 0.0); (0.0, -1.0), (0.0, 1.0) ]

    // ── Generic centering ───────────────────────────────────────────────────────

    let scaleAndTranslate (cx : float, cy : float) (r : float) (dx, dy) =
        cx + dx * r, cy + dy * r

    let centerPolygon center r = List.map (scaleAndTranslate center r)

    let centerLines center r =
        List.map (fun (p1, p2) -> scaleAndTranslate center r p1, scaleAndTranslate center r p2)

    // ── Kernel density estimation ────────────────────────────────────────────────

    /// Silverman's rule-of-thumb bandwidth for a Gaussian KDE.
    let silvermanBandwidth (values : float list) =
        let n = values.Length
        if n < 2 then 1.0
        else
            let m = List.sum values / float n
            let sigma = values |> List.sumBy (fun v -> (v - m) ** 2.0) |> fun s -> sqrt (s / float n)
            let sorted = values |> List.sort |> Array.ofList
            let lerp p =
                let idx = p * float (n - 1)
                let lo = int (floor idx)
                let hi = min (n - 1) (int (ceil idx))
                sorted.[lo] + (idx - float lo) * (float sorted.[hi] - float sorted.[lo])
            let iqr = lerp 0.75 - lerp 0.25
            let s = if iqr > 0.0 then min sigma (iqr / 1.34) else sigma
            let s' = if s < epsilon then epsilon else s
            0.9 * s' * (float n ** -0.2)

    /// Evaluate a Gaussian KDE at point x given pre-computed bandwidth and data values.
    let gaussianKde (bandwidth : float) (values : float list) (x : float) =
        let n = float values.Length
        let h = if bandwidth < epsilon then epsilon else bandwidth
        let twoHSq = 2.0 * h * h
        values
        |> List.sumBy (fun xi -> let d = x - xi in exp (-d * d / twoHSq))
        |> fun total -> total / (n * h * sqrt (2.0 * System.Math.PI))
