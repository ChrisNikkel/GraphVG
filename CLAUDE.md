# GraphVG

F# library for generating SVG-based graphs using SharpVG.

## Project Structure

```fsharp
REQUIREMENTS.md    # Track requirements/bugs we plan to build.  Once implemented, remove.
DESIGN.md          # Track the overall architecture / design of the project both current and future.
GraphVG/           # Main library (net8.0)
  Canvas.fs        # Rendering constant: canvasSize
  Scale.fs         # Data-to-pixel mapping (Linear, Log)
  Series.fs        # Data series type (Scatter, Line, Area)
  Theme.fs         # Visual styling (pens, colors, grid)
  Axis.fs          # Axis rendering (ticks, labels, grid lines)
  Graph.fs         # Core graph record and coordinate transforms
  GraphVG.fs       # Top-level API: render, toHtml
Examples/Example/  # Executable usage demo
Tests/             # xUnit test suite
```

## Git

Do not commit changes automatically. Always wait for the user to review and explicitly request a commit.

## Build & Test

```sh
dotnet build
dotnet test
dotnet run --project Examples/Example
```

## Typical Usage

```fsharp
let html =
    Graph.create [ unitCircle; lissajous ] (-1.2, 1.2) (-1.2, 1.2)
    |> Graph.withTheme Theme.light
    |> GraphVG.toHtml
```

## Coding Standards

### SharpVG — source and usage

SharpVG is the only dependency. Source is available in two places:

- **Local checkout**: `~/Code/SharpVG` — read this to understand available types and functions before writing any rendering code
- **GitHub**: [https://github.com/ChrisNikkel/SharpVG](https://github.com/ChrisNikkel/SharpVG)

**Always read SharpVG source before reaching for a raw float or a hand-rolled helper.** SharpVG likely already has what you need:

- Use `Length` (not `float`) for widths, radii, font sizes, and any pixel measurement — `Length.ofFloat`, `Length.px`, etc.
- Use `Color` (not strings or tuples) for all colors — `Color.ofRgb`, `Color.ofName`, etc.
- Use `Pen`, `Style`, `Transform` builder functions rather than constructing SVG attributes manually.
- Use `Element`, `Svg`, `Group` combinators to compose output — avoid hand-rolling SVG strings.

When you encounter a raw `float` or `string` where a SharpVG type would fit, that is a signal to look up the right SharpVG abstraction first.

### Follow SharpVG conventions exactly

GraphVG builds on SharpVG — match its style throughout so the two feel like one library.

- **Types**: PascalCase, no access modifier (implicitly public). Braces on their own lines, fields separated by `;`.

  ```fsharp
  type Pen =
      {
          Color: Color;
          Opacity: float;
          Width: Length;
      }
  ```

- **No alignment padding**: Do not pad anything to align columns — this applies to field names, `=` signs, `|` match arms, function arguments, and pipeline operators (`|>`). Use a single space everywhere. Aligned columns look tidy but create noisy diffs and must be maintained manually.

  ```fsharp
  // wrong
  type Graph =
      {
          Series     : Series list
          XScale     : Scale
          TitleStyle : TitleStyle
      }

  // correct
  type Graph =
      {
          Series : Series list
          XScale : Scale
          TitleStyle : TitleStyle
      }
  ```

- **Modules**: same name as the type they accompany, immediately after the type definition.
- **Functions**: camelCase. Public functions are bare `let`. Internal helpers are `let private`.
- **Builders**: `create`, `with*`, `add*`, `to*` — always pipeline-friendly (subject last).
- **DU serialisation**: `override this.ToString()` on the type itself, not in the module.
- **Module `to*` wrappers**: thin delegations to static type members (`let toString = MyType.ToString`).

- **Do not shadow SharpVG types**: Always resolve type name conflicts (e.g., use fully qualified names for `Point` if needed).

### No new dependencies

Do not add NuGet packages without explicit approval. The only allowed dependency is SharpVG. Prefer stdlib (`List`, `String`, `Math`) over pulling in utility libraries.

### No JavaScript

Do not add JavaScript to any generated HTML. Use plain HTML and CSS only. For interactive behaviour in the example gallery, use native HTML elements (`<a>`, `<details>`, CSS `:target`, etc.).

### Idiomatic F\#

- **Leverage pattern matching**: Use `match ... with` for control flow and data deconstruction instead of if-else chains. Prefer exhaustive matches for safety and clarity.
- **Use `Result<'T, 'TError>` for error handling**: Return `Result` types for expected errors instead of exceptions, making error cases explicit and composable.
- **Apply active patterns**: Define active patterns for reusable, readable matching logic, especially for complex or custom data checks.
- **Favor `Seq<'T>` for laziness**: Use `Seq` for large or potentially infinite data to defer computation and improve performance.
- **Model domains with discriminated unions**: Represent mutually exclusive states or variants with DUs, ensuring type safety and preventing invalid states.
- **Chain with the forward pipe (`|>`)**: Structure data transformations as left-to-right pipelines for readability and functional flow.
- **Use function composition (`>>`, `<<`)**: Compose small functions for reuse, but avoid over-composition that reduces clarity.
Prefer `List` functions (`List.map`, `List.collect`, `List.choose`) over loops.
Use `Option.map` / `Option.defaultWith` / `|> Option.toList` rather than `if x.IsSome then`.
Use record update syntax (`{ x with Field = v }`) — no mutation.
Use discriminated unions for variants; avoid booleans as poor-man's enums.
No computation expressions unless genuinely needed (rare here).

Type annotations only where inference needs help (disambiguating record updates, `(axis : Axis)`). Do not annotate every parameter.

### Naming

Prefer full words over abbreviations: `position` not `pos`, `minimum` not `min` when used as a standalone binding, `opacity` not `op`. Short pipeline bindings like `g`, `s`, `v` are fine as locals in tight transforms.

### Markdown and Mermaid

- Use standard CommonMark markdown. Headings, bullet lists, bold, inline code, and fenced code blocks all render correctly.
- **No `\n` inside mermaid node labels** — it does not render as a line break. Keep node labels short enough to fit on one line. If a label needs more detail, use prose below the diagram instead.
- Prefer `flowchart` over `graph` for mermaid diagrams (cleaner syntax, explicit direction).
- Node label style: use `[text]` for plain boxes, `["text"]` when the label contains spaces or punctuation that would be ambiguous.

### Property-Based Testing

Use [FsCheck](https://fscheck.github.io/FsCheck/) with `[<Property>]` (from `FsCheck.Xunit`) alongside `[<Fact>]` tests. Properties catch edge cases that hand-written examples miss.

#### When to Use `[<Property>]` vs `[<Fact>]`

| Use `[<Property>]` | Use `[<Fact>]` |
| --- | --- |
| Invariants that hold for *any* input (round-trips, monotonicity, count formulas) | Exact values with a known expected output |
| Relationships between functions (apply ∘ invert = id) | Specific regression cases |
| "For all n, count = f(n)" | Behaviour for a single named scenario |

#### Common Patterns

- **Round-trip**: `invert (apply scale x) ≈ x` — verifies encode/decode symmetry
- **Invariant**: `(drawSeries scatter graph).Length = graph.Series.[0].Points.Length`
- **Monotonicity**: `x < y ==> (apply scale x < apply scale y)`
- **Count formula**: `toElements axis |> List.length = 1 + 2 * tickCount`
- **Cycling**: `penForSeries (i + period) = penForSeries i`

#### FsCheck Generator Types

- `FsCheck.PositiveInt` — `n.Get >= 1`, avoids zero-length edge cases
- `FsCheck.NonNegativeInt` — `n.Get >= 0`
- `FsCheck.NormalFloat` — finite float (no NaN/inf); use when arithmetic could overflow
- `float` / `int` — unconstrained; guard with `==>` or clamp if needed

#### Conditional Properties

Use `open FsCheck` (add it to the test module) to get `==>`, which discards inputs that violate a precondition:

```fsharp
[<Property>]
let ``apply is order-preserving`` (x1: NormalFloat) (x2: NormalFloat) =
    x1.Get < x2.Get ==> (Scale.apply scale x1.Get < Scale.apply scale x2.Get)
```

Avoid `==>` when the condition is rarely true (>90% discard rate), since FsCheck will give up. Prefer using `min`/`max` to normalize inputs, or clamp to a valid range:

```fsharp
// Better: normalize so no inputs are discarded
let a = min x1.Get x2.Get
let b = max x1.Get x2.Get
a = b || Scale.apply scale a <= Scale.apply scale b
```

#### Clamp Inputs to Valid Domain

Use this when the property is undefined outside a range:

```fsharp
let clamped = max lo (min hi x.Get)
isNear clamped (Scale.invert scale (Scale.apply scale clamped))
```

#### Avoid Over-Relying on Properties

A property that always holds trivially (e.g., `List.length xs >= 0`) adds no value. Every property should have a plausible failure mode.

### CommonMath

`CommonMath.fs` is the home for all pure float math that has no dependency on SharpVG. Keep it isolated — rendering code belongs in `Graph.fs`/`GraphVG.fs`, coordinate math belongs here.

**When to add something to CommonMath:**
- The function takes and returns plain `float` or `(float * float)` values
- It has no dependency on SharpVG types (`Point`, `Length`, `Element`, etc.)
- The same logic would be useful across more than one rendering module

**Unit-shape + centering pattern** — use this for any geometry that places a fixed pattern at a variable center and scale:

```fsharp
// 1. Define the shape as unit offsets (center = origin, radius = 1)
let diamondUnit = [ 0.0, -1.0; 1.0, 0.0; 0.0, 1.0; -1.0, 0.0 ]

// 2. Use centerPolygon / centerLines to place it
let vertices = centerPolygon (cx, cy) r diamondUnit
```

Adding a new shape is just a new list constant — no new arithmetic function needed. Do not inline the offset arithmetic into rendering code; put the unit shape in CommonMath and call `centerPolygon` or `centerLines`.

### Architecture Notes

- `Graph.createWithSeries` auto-calculates domain/range with 10% padding.
- `Graph.addSeries` recalculates bounds across all series.
- Y-axis is inverted during coordinate transform (SVG origin is top-left).
- `Axis.fs` compiles before `Graph.fs`; `Canvas.fs` compiles before both. This ordering makes it possible for `Graph` to embed `Axis option` without a circular dependency.
