# GraphVG

F# library for generating SVG-based graphs using SharpVG.

## Project Structure

```fsharp
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

- All data coordinates transform to a fixed 1000×1000 SVG canvas via `Graph.toScaledSvgCoordinates`. The constant lives in `Canvas.canvasSize`.
- `Graph.createWithSeries` auto-calculates domain/range with 10% padding.
- `Graph.addSeries` recalculates bounds across all series.
- Y-axis is inverted during coordinate transform (SVG origin is top-left).
- `Axis.fs` compiles before `Graph.fs`; `Canvas.fs` compiles before both. This ordering makes it possible for `Graph` to embed `Axis option` without a circular dependency.
