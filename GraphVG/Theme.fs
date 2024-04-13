namespace GraphVG

open SharpVG

type Theme = {
    Background: Colors
    Pens: Pen list
}

module Theme =

    let turtle =
        { Background = Black; Pens = [Pen.create (Color.ofName Colors.Green)] }