# Repository Guidelines

## Project Structure & Module Organization

This is a s&box game project named `globaltesting`. Runtime gameplay code lives in `Code/`, with component scripts such as `Code/KodokanController.cs` and animation helpers under `Code/Animation/`. Editor-only tooling lives in `Editor/` and references the runtime project. Game content is stored in `Assets/`, including scenes in `Assets/scenes/`, prefabs in `Assets/Prefab/`, animation graphs in `Assets/animgraphs/`, audio in `Assets/sound/`, and model/material source files in the asset root and subfolders. Engine and project configuration lives in `ProjectSettings/`, while `globaltesting_work.sbproj` defines package metadata and game settings.

## Build, Test, and Development Commands

- `dotnet build globaltesting.slnx`: builds the runtime and editor C# projects.
- `dotnet build Code/globaltesting.csproj`: builds only gameplay code.
- `dotnet build Editor/globaltesting.editor.csproj`: builds editor extensions after runtime code.
- `sbox-dev.exe -project globaltesting_work.sbproj`: opens the project in s&box; the launch profile in `Code/Properties/launchSettings.json` points to the local Steam install path.

Run build commands from the repository root. The project targets `net10.0` and depends on s&box assemblies under the installed Steam `sbox` directory.

## Coding Style & Naming Conventions

Follow `.editorconfig`: use tabs with width 4 for C# and Razor files, CRLF line endings, and a final newline. Keep C# braces on new lines and prefer explicit braces for control blocks. Component classes use PascalCase names matching their file names, for example `WeaponAnimGraphLink` in `WeaponAnimGraphLink.cs`. Public inspector fields should use `[Property]`; required component references should use `[RequireComponent]` when appropriate. Keep runtime code in `Code/` and editor-only APIs in `Editor/`.

## Testing Guidelines

No automated tests are currently committed. For changes, at minimum run the relevant `dotnet build` command and test behavior in s&box. If tests are introduced, place them in a clearly named test folder such as `Code/unittest/`, name files after the behavior under test, and keep gameplay tests focused on component state, input handling, and animation parameter changes.

## Commit & Pull Request Guidelines

This checkout does not include Git history, so no repository-specific commit convention can be inferred. Use short, imperative commit messages such as `Add weapon animation link`. Pull requests should include a brief summary, test/build results, linked issues when applicable, and screenshots or clips for visible gameplay, animation, scene, or asset changes.

## Asset & Configuration Tips

Avoid editing generated files in `obj/` or compiled asset outputs such as `*_c` unless the s&box toolchain regenerates them. Prefer changing source assets like `.scene`, `.prefab`, `.vmdl`, `.vmat`, `.fbx`, `.png`, `.wav`, and `.sound`. Keep project metadata in `globaltesting_work.sbproj` consistent with scenes and player-count settings.
