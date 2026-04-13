namespace GraphVG

open System
open MathNet.Symbolics

/// Opaque parsed expression. Obtain values only via Plot.parse.
type PlotExpr = private PlotExpr of Expression

module Plot =

    // ── Internal helpers ──────────────────────────────────────────────────────

    let private xSym = Operators.symbol "x"

    let private evalAt (expr : Expression) (x : float) =
        match Evaluate.evaluate (Map.ofList [ "x", FloatingPoint.Real x ]) expr with
        | FloatingPoint.Real v -> v
        | _ -> nan

    let private sampleDense (xMin : float) (xMax : float) (n : int) (expr : Expression) =
        let dx = (xMax - xMin) / float (n - 1)
        List.init n (fun i -> xMin + float i * dx)
        |> List.map (fun x -> x, evalAt expr x)

    let private bisect (f : float -> float) (lo : float) (hi : float) (tol : float) =
        let rec loop lo hi =
            let mid = (lo + hi) / 2.0
            if hi - lo < tol then mid
            elif sign (f lo) = sign (f mid) then loop mid hi
            else loop lo mid
        loop lo hi

    // ── Public API ────────────────────────────────────────────────────────────

    /// Parse an infix expression string into a PlotExpr.
    /// The only variable is "x". Returns Error with a message for invalid input.
    let parse (input : string) : Result<PlotExpr, string> =
        try Ok (PlotExpr (Infix.parseOrThrow input))
        with ex -> Error ex.Message

    /// Compute the symbolic derivative d/dx.
    let derivative (PlotExpr expr) : PlotExpr =
        PlotExpr (Calculus.differentiate xSym expr)

    /// Sample the expression across the domain and return a SegmentedLine Series.
    /// Consecutive samples where |Δy| exceeds 1000 × (yMax − yMin) are separated
    /// by a (nan, nan) break so the SVG path lifts the pen at discontinuities.
    let toSeries (domain : float * float) (samples : int) (PlotExpr expr) : Series =
        let xMin, xMax = domain
        let pts = sampleDense xMin xMax (max 2 samples) expr

        let finite = pts |> List.choose (fun (_, y) -> if Double.IsFinite y then Some y else None)
        // Use IQR rather than full range so asymptote extremes don't inflate the threshold.
        let threshold =
            match finite |> List.sort with
            | sorted when sorted.Length >= 4 ->
                let n = sorted.Length
                let iqr = max 1.0 (sorted.[n * 3 / 4] - sorted.[n / 4])
                100.0 * iqr
            | _ -> 1000.0

        let withBreaks =
            match pts with
            | [] | [_] -> pts
            | _ ->
                let pairs = List.pairwise pts
                let body =
                    pairs
                    |> List.collect (fun ((x1, y1), (x2, y2)) ->
                        let jump = abs (y2 - y1)
                        if not (Double.IsFinite y1) || not (Double.IsFinite y2) || jump > threshold then
                            [ x1, y1; nan, nan ]
                        else
                            [ x1, y1 ])
                body @ [ snd (List.last pairs) ]

        Series.create SegmentedLine withBreaks

    /// Compute a finite (yMin, yMax) range for the expression over the domain,
    /// including critical points, with 10% padding. Safe for expressions with asymptotes.
    let autoRange (domain : float * float) (PlotExpr expr) : float * float =
        let xMin, xMax = domain
        let tol = (xMax - xMin) * 1e-6
        let deriv = Calculus.differentiate xSym expr

        let densePts = sampleDense xMin xMax 1000 expr
        let derivPts = sampleDense xMin xMax 1000 deriv

        let mutable ys =
            densePts |> List.choose (fun (_, y) -> if Double.IsFinite y then Some y else None)

        derivPts
        |> List.pairwise
        |> List.iter (fun ((x1, d1), (x2, d2)) ->
            if Double.IsFinite d1 && Double.IsFinite d2 && sign d1 <> sign d2 then
                let root = bisect (evalAt deriv) x1 x2 tol
                let v = evalAt expr root
                if Double.IsFinite v then ys <- v :: ys)

        match ys with
        | [] -> -1.0, 1.0
        | _ -> CommonMath.padRange 0.10 (List.min ys, List.max ys)

    /// Return real roots of the expression in the domain found by bisection
    /// on sign-change intervals in a dense sample.
    /// Roots are accurate to within 1e-6 of the domain span.
    let roots (domain : float * float) (PlotExpr expr) : float list =
        let xMin, xMax = domain
        let tol = (xMax - xMin) * 1e-6
        sampleDense xMin xMax 1000 expr
        |> List.pairwise
        |> List.choose (fun ((x1, y1), (x2, y2)) ->
            if Double.IsFinite y1 && Double.IsFinite y2 && sign y1 <> sign y2 then
                Some (bisect (evalAt expr) x1 x2 tol)
            else None)
