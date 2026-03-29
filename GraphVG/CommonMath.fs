namespace GraphVG

module CommonMath =

    let epsilon : float = 1e-10

    let isNear (expected : float) (actual : float) =
        abs (actual - expected) < epsilon

    let padRange (percent : float) (minimum : float, maximum : float) =
        let span = maximum - minimum
        minimum - span * percent, maximum + span * percent
