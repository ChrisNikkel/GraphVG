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
            create (canvasSize * 0.020) (canvasSize * 0.016) (canvasSize * 0.004) (canvasSize * 0.004)

    let estimatedTextWidth fontSize (text : string) =
        float text.Length * fontSize * 0.6

    let epsilon : float = 1e-10

    /// Computes an adaptive internal canvas resolution for a given data span.
    /// The canvas stays at 1000 for spans up to 10× canvasSize; above that it scales
    /// up by one decade per decade of span, ensuring SVG coordinate differences remain
    /// distinguishable at standard floating-point formatting precision.
    let adaptiveCanvasSize (xSpan : float) (ySpan : float) =
        let span = max xSpan ySpan
        if span <= 0.0 || System.Double.IsNaN(span) || System.Double.IsInfinity(span) then
            canvasSize
        else
            let decades = max 0.0 (System.Math.Floor(System.Math.Log10(span / canvasSize)))
            canvasSize * System.Math.Pow(10.0, decades)

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

    // ── Flat-top hexbin ──────────────────────────────────────────────────────────

    let private sqrt3 = sqrt 3.0

    /// Convert a data-space point to flat-top axial hex coordinates (q, r).
    let private flatTopAxial (radius : float) (x : float, y : float) =
        let qf = (2.0 / 3.0 * x) / radius
        let rf = (-1.0 / 3.0 * x + sqrt3 / 3.0 * y) / radius
        let sf = -qf - rf
        let rq = round qf
        let rr = round rf
        let rs = round sf
        let dq = abs (rq - qf)
        let dr = abs (rr - rf)
        let ds = abs (rs - sf)
        if dq > dr && dq > ds then
            int (-rr - rs), int rr
        elif dr > ds then
            int rq, int (-rq - rs)
        else
            int rq, int rr

    /// Data-space center of a flat-top axial hex coordinate (q, r).
    let hexFlatTopCenter (radius : float) (q : int, r : int) : float * float =
        let x = radius * 1.5 * float q
        let y = radius * (sqrt3 / 2.0 * float q + sqrt3 * float r)
        x, y

    /// Flat-top hexagon vertices for a given center and canvas-space radius.
    let hexFlatTopVertices (cx : float, cy : float) (r : float) : (float * float) list =
        [ for i in 0 .. 5 ->
            let angle = System.Math.PI / 3.0 * float i
            cx + r * cos angle, cy + r * sin angle ]

    /// Bin a list of (x, y) points into flat-top hexagonal bins.
    /// Returns (q, r, count) triples for occupied bins only.
    let hexbinBins (radius : float) (points : (float * float) list) : (int * int * int) list =
        points
        |> List.map (flatTopAxial radius)
        |> List.groupBy id
        |> List.map (fun ((q, r), vs) -> q, r, vs.Length)

    // ── Squarified treemap ───────────────────────────────────────────────────────

    type TreemapRect =
        {
            X : float
            Y : float
            W : float
            H : float
            Index : int
        }

    let squarifiedTreemap (x : float) (y : float) (w : float) (h : float) (values : float list) : TreemapRect list =
        let total = List.sum values
        if total <= 0.0 || values.IsEmpty then []
        else
            let normalised = values |> List.mapi (fun i v -> i, v * w * h / total)

            let worst (shortSide : float) (rows : float list) =
                let s = List.sum rows
                let maxV = List.max rows
                let minV = List.min rows
                max (shortSide * shortSide * maxV / (s * s)) (s * s / (shortSide * shortSide * minV))

            let layoutRow (rx : float) (ry : float) (rw : float) (rh : float) (row : (int * float) list) : TreemapRect list =
                let rowArea = row |> List.sumBy snd
                if rw >= rh then
                    let colW = if rw > epsilon then rowArea / rh else 0.0
                    row
                    |> List.fold (fun (curY, acc) (idx, area) ->
                        let cellH = if rowArea > epsilon then area / rowArea * rh else 0.0
                        let rect = { X = rx; Y = curY; W = colW; H = cellH; Index = idx }
                        curY + cellH, rect :: acc) (ry, [])
                    |> snd
                    |> List.rev
                else
                    let rowH = if rh > epsilon then rowArea / rw else 0.0
                    row
                    |> List.fold (fun (curX, acc) (idx, area) ->
                        let cellW = if rowArea > epsilon then area / rowArea * rw else 0.0
                        let rect = { X = curX; Y = ry; W = cellW; H = rowH; Index = idx }
                        curX + cellW, rect :: acc) (rx, [])
                    |> snd
                    |> List.rev

            let rec squarify (items : (int * float) list) (row : (int * float) list) (rx : float) (ry : float) (rw : float) (rh : float) (acc : TreemapRect list) =
                match items with
                | [] ->
                    if row.IsEmpty then acc
                    else acc @ layoutRow rx ry rw rh row
                | item :: rest ->
                    let shortSide = min rw rh
                    let newRow = row @ [ item ]
                    let rowVals = newRow |> List.map snd
                    if row.IsEmpty || worst shortSide (row |> List.map snd) >= worst shortSide rowVals then
                        squarify rest newRow rx ry rw rh acc
                    else
                        let placed = layoutRow rx ry rw rh row
                        let rowArea = row |> List.sumBy snd
                        let rx2, ry2, rw2, rh2 =
                            if rw >= rh then rx + rowArea / rh, ry, rw - rowArea / rh, rh
                            else rx, ry + rowArea / rw, rw, rh - rowArea / rw
                        squarify items [] rx2 ry2 rw2 rh2 (acc @ placed)

            squarify normalised [] x y w h []
