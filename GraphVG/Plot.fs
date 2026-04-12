namespace GraphVG

open System
open MathNet.Symbolics
open Evaluate

/// Opaque parsed expression. Obtain values only via Plot.parse.
type PlotExpr = private PlotExpr of Expression

module Plot =

    // ── Internal helpers ──────────────────────────────────────────────────────

    let private xSym = symbol "x"

    let private evalAt (expr : Expression) (x : float) =
        match evaluate (Map.ofList [ "x", Real x ]) expr with
        | Real v -> v
        | _ -> nan

    let private sampleDense (xMin : float) (xMax : float) (n : int) (expr : Expression) =
        let dx = (xMax - xMin) / float (n - 1)
        [ for i in 0 .. n - 1 -> xMin + float i * dx ]
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
        try
            let expr = Infix.parseOrThrow input
            Ok (PlotExpr expr)
        with ex ->
            Error ex.Message

    /// Compute the symbolic derivative d/dx.
    let derivative (PlotExpr expr) : PlotExpr =
        PlotExpr (Calculus.differentiate xSym expr)

    /// Sample the expression across the domain and return a SegmentedLine Series.
    /// Consecutive samples where |Δy| exceeds 1000 × (yMax − yMin) are separated
    /// by a (nan, nan) break so the SVG path lifts the pen at discontinuities.
    let toSeries (domain : float * float) (samples : int) (PlotExpr expr) : Series =
        let xMin, xMax = domain
        let pts = sampleDense xMin xMax (max 2 samples) expr

        // First pass: compute y-span from finite values for the threshold.
        let finite = pts |> List.choose (fun (_, y) -> if Double.IsFinite y then Some y else None)
        let ySpan =
            if finite.Length < 2 then 1.0
            else List.max finite - List.min finite |> max 1.0
        let threshold = 1000.0 * ySpan

        // Second pass: insert (nan, nan) sentinels at discontinuities.
        let withBreaks =
            pts
            |> List.pairwise
            |> List.collect (fun ((x1, y1), (x2, y2)) ->
                let jump = abs (y2 - y1)
                if not (Double.IsFinite y1) || not (Double.IsFinite y2) || jump > threshold then
                    [ x1, y1; nan, nan ]
                else
                    [ x1, y1 ])
            |> fun lst ->
                match pts with
                | [] -> lst
                | _ -> lst @ [ List.last pts ]

        {
            Series.empty with
                Points = withBreaks
                Kind = SegmentedLine
        }

    /// Return a finite (yMin, yMax) range for the expression over the domain,
    /// including critical points, with 10% padding. Safe for expressions with asymptotes.
    let autoRange (domain : float * float) (PlotExpr expr) : float * float =
        let xMin, xMax = domain
        let tol = (xMax - xMin) * 1e-6

        // Dense sample for coverage.
        let pts = sampleDense xMin xMax 1000 expr
        let mutable ys = pts |> List.choose (fun (_, y) -> if Double.IsFinite y then Some y else None)

        // Find critical points via the derivative.
        let deriv = Calculus.differentiate xSym expr
        let derivPts = sampleDense xMin xMax 1000 deriv

        derivPts
        |> List.pairwise
        |> List.iter (fun ((x1, d1), (x2, d2)) ->
            if Double.IsFinite d1 && Double.IsFinite d2 && sign d1 <> sign d2 then
                let root = bisect (evalAt deriv) x1 x2 tol
                let v = evalAt expr root
                if Double.IsFinite v then ys <- v :: ys)

        match ys with
        | [] -> (-1.0, 1.0)
        | _ -> padRange 0.10 (List.min ys, List.max ys)

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
