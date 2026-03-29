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
