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
- **GitHub**: https://github.com/ChrisNikkel/SharpVG

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

- **Modules**: same name as the type they accompany, immediately after the type definition.
- **Functions**: camelCase. Public functions are bare `let`. Internal helpers are `let private`.
- **Builders**: `create`, `with*`, `add*`, `to*` — always pipeline-friendly (subject last).
- **DU serialisation**: `override this.ToString()` on the type itself, not in the module.
- **Module `to*` wrappers**: thin delegations to static type members (`let toString = MyType.ToString`).

- **Do not shadow SharpVG types**: Always resolve type name conflicts (e.g., use fully qualified names for `Point` if needed).

### No new dependencies

Do not add NuGet packages without explicit approval. The only allowed dependency is SharpVG. Prefer stdlib (`List`, `String`, `Math`) over pulling in utility libraries.

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

### Architecture Notes

- `Graph.createWithSeries` auto-calculates domain/range with 10% padding.
- `Graph.addSeries` recalculates bounds across all series.
- Y-axis is inverted during coordinate transform (SVG origin is top-left).
- `Axis.fs` compiles before `Graph.fs`; `Canvas.fs` compiles before both. This ordering makes it possible for `Graph` to embed `Axis option` without a circular dependency.
