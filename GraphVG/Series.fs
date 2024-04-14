namespace GraphVG

open SharpVG

type Series = (float * float) list

module Series =
    let empty =
        [ ]