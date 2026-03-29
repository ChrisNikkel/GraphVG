namespace GraphVG

open System
open CommonMath

type Scale =
    | Linear of domain:(float * float) * range:(float * float)
    | Log    of domain:(float * float) * range:(float * float) * base':float

module Scale =

    let linear domain range =
        Linear(domain, range)

    let log domain range base' =
        Log(domain, range, base')

    let apply scale value =
        match scale with
        | Linear((domainMin, domainMax), (rangeMin, rangeMax)) ->
            rangeMin + (value - domainMin) / (domainMax - domainMin) * (rangeMax - rangeMin)
        | Log((domainMin, domainMax), (rangeMin, rangeMax), base') ->
            let logBase x = Math.Log(x) / Math.Log(base')
            rangeMin + (logBase value - logBase domainMin) / (logBase domainMax - logBase domainMin) * (rangeMax - rangeMin)

    let invert scale value =
        match scale with
        | Linear((domainMin, domainMax), (rangeMin, rangeMax)) ->
            domainMin + (value - rangeMin) / (rangeMax - rangeMin) * (domainMax - domainMin)
        | Log((domainMin, domainMax), (rangeMin, rangeMax), base') ->
            let logBase x = Math.Log(x) / Math.Log(base')
            let t = (value - rangeMin) / (rangeMax - rangeMin)
            domainMin * Math.Pow(base', t * (logBase domainMax - logBase domainMin))

    let domain scale =
        match scale with
        | Linear((d1, d2), _)    -> d1, d2
        | Log((d1, d2), _, _)    -> d1, d2

    let ticks scale count =
        match scale with
        | Linear((domainMin, domainMax), _) ->
            if count <= 1 then
                [ domainMin ]
            else
                let step = (domainMax - domainMin) / float (count - 1)
                [ for i in 0 .. count - 1 -> domainMin + float i * step ]
        | Log((domainMin, domainMax), _, base') ->
            let logBase x = Math.Log x / Math.Log base'
            let lo = int (Math.Ceiling(logBase domainMin - epsilon))
            let hi = int (Math.Floor(logBase domainMax + epsilon))
            [ for i in lo .. hi -> Math.Pow(base', float i) ]
