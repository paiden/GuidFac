# Copilot Instructions

## Project Details

* GuidFac (`GuidFac64` in the manifest) – a Visual Studio extension (VSIX) that inserts a new GUID into the active text editor in the format you choose. The command is Edit > InsertGuid, bound to Ctrl+E, Ctrl+G; it shows a small dialog at the caret with the GUID as `D`, `N`, single-byte array and multi-byte formats in lower case, upper case or both, and Enter inserts the selected line. The history is in #file:'Source/GuidFac/ReleaseNotes.txt'; #file:'README.md' only lists build prerequisites and is out of date (it names Visual Studio 2013)
* Installs into Visual Studio 2022 and later (`[17.0,)`, `amd64`; see #file:'Source/GuidFac/source.extension.vsixmanifest'); .NET Framework 4.7.2; `LangVersion` is 10.0 (set in the project file). The manifest still declares dependencies of older Visual Studio versions (`Microsoft.Framework.NDP` 4.5, `Microsoft.VisualStudio.MPF.12.0` and a core editor prerequisite from 15.0), and `app.config` (binding redirects for `Microsoft.VisualStudio.Threading` and `Newtonsoft.Json`) is only a `None` item of the project
* Versions disagree: the manifest and `ReleaseNotes.txt` say 2.1.0, `Properties/AssemblyInfo.cs` still says 1.3.0, and Help > About shows `1.0` (the argument of `InstalledProductRegistration`). A release bump changes the manifest and adds an entry to `ReleaseNotes.txt`
* The solution is #file:'GuidFac.slnx', in the repository root on purpose: Visual Studio reads `.mcp.json` from the folder of the solution (see "MCP Servers"). It uses the XML `.slnx` format. It needs the .NET SDK 9.0.200 or later, or the MSBuild of Visual Studio or Build Tools 17.13 or later (`dotnet sln` edits it). It has one old-style (non-SDK) project, #file:'Source/GuidFac/GuidFac.csproj': every `.cs`, XAML page, resource and content file is listed in it by hand (`Compile`, `Page`, `EmbeddedResource`, `Content`, `VSCTCompile`), so a new file is neither compiled nor packaged until it is added there. Package versions are `PackageReference` items in the same file – there is no central package management
* Windows only: building needs **Visual Studio 2022** with the "Visual Studio extension development" workload. Visual Studio 2026 does not build this project: its private assemblies are version 18.0.0.0 while the public SDK packages stay at 17.0.0.0, and the compile stops with `CS1705` (see "Private Visual Studio Assemblies"). `dotnet build` does not build VSIX projects
* The assembly is signed with the key file in the `DEVSNK` environment variable (`SignAssembly` is on in the project file). Point `DEVSNK` to any `.snk` file, or pass `-p:SignAssembly=false` as the command below does
* Build from the repository root in PowerShell. `msbuild` is usually not on `PATH`, so find the one of Visual Studio 2022 with `vswhere`; `DeployExtension=false` keeps the build from installing the extension into the Experimental instance:

  ```powershell
  $msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -version '[17.0,18.0)' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
  & $msbuild GuidFac.slnx -restore -p:Configuration=Debug -p:DeployExtension=false -p:SignAssembly=false -v:m
  ```

* Test: there is no test project. Run the extension in the Visual Studio Experimental instance (F5 starts `devenv.exe /rootsuffix Exp`, see the start options of the project), open a code file, put the caret in it and press Ctrl+E, Ctrl+G. Try an editor in the main window and in an undocked window, and, when SQL Server Data Tools is installed, a SQL editor: the window lookup was changed for the SQL editor (see "Private Visual Studio Assemblies")
* CI: there is no build workflow. `.github/workflows/copilot-setup-steps.yml` only restores the packages for the Copilot coding agent

## Solution Structure

**IMPORTANT: Every new source file must be added to #file:'Source/GuidFac/GuidFac.csproj'**: `<Compile Include>` for code, `<Page Include>` for XAML, `<Content Include>` with `IncludeInVSIX` for files that go into the VSIX. A file that is created outside Visual Studio is not added automatically.

```text
/
├── .claude/                                  – Claude Code settings (the vs-agent-plugins plugin) and helper scripts; `skills` is a local, git-ignored link to `.github/skills`
├── .github/                                  – these instructions, the `vs-extension-expert` agent, skills, prompts and workflows
├── GuidFac.slnx                              – the solution; it lives in the repository root so that Visual Studio finds `.mcp.json`
├── Source/GuidFac/                           – the extension, a single old-style project
│   ├── GuidFacPackage.cs                     – the `AsyncPackage`: reads the settings, registers the command and shows the dialog
│   ├── GuidFacWindow.xaml / .xaml.cs         – the dialog (a `DialogWindow`): the list of formats, the case switches and the keyboard handling
│   ├── GuidSequncesGenerator.cs              – `GuidSequences`, the formats of a GUID (the file name has a typo)
│   ├── GuidFacConfig.cs                      – the settings file `config.ini`, and the `GuidFormat` flags
│   ├── RadioButtonSelector.cs                – steps through the case radio buttons from the keyboard
│   ├── Guids.cs, PkgCmdID.cs                 – the package and command set GUIDs (`GuidList`) and the command id (`PkgCmdIDList`); they must match `GuidFac.vsct`
│   ├── GuidFac.vsct                          – the command table: the Edit menu item and the Ctrl+E, Ctrl+G binding
│   ├── Resources.resx / Resources.Designer.cs – an empty resource file and its generated designer; the texts of the dialog are literals in the XAML
│   ├── VSPackage.resx                        – the product name, description and icon that Help > About shows
│   ├── Resources/                            – the package icon, a bitmap strip that the command table declares but no command uses, and the WPF styles of the dialog
│   ├── ReleaseNotes.txt, PreviewScreen.png   – shown by the extension manager (listed as `Content`)
│   ├── source.extension.vsixmanifest         – the manifest
│   ├── Properties/AssemblyInfo.cs, GlobalSuppressions.cs, app.config
│   └── GuidFac.csproj
├── Directory.Build.targets                   – links `.claude/skills` to `.github/skills`
├── .gitattribute, .gitignore, README.md
```

`.gitattribute` is a UTF-16 file with the content `* -text`. Git only reads `.gitattributes`, so the file has no effect: the CRLF line endings of the repository come from the blobs themselves.

### How It Works

* `GuidFacPackage` is an `AsyncPackage` that loads in the background when a code window appears (`UIContextGuids.CodeWindow`). Its constructor reads `config.ini` through `GuidFacConfig`; `InitializeAsync` registers the command `cmdidInsertGuid` (`0x100` of the command set `guidGuidFacCmdSet`) with the `IMenuCommandService`. `GuidFac.vsct` places the button in the group `MyMenuGroup` of the Edit menu and binds Ctrl+E, Ctrl+G to it.
* `MenuItemCallback` takes the active `IVsTextView` from `IVsTextManager`, does nothing for a selection over several lines, works out the screen position of the caret from the `IWpfTextView`, finds the WPF window that hosts the text view (private assemblies, see below), shows `GuidFacWindow` as a dialog owned by that window and replaces the selection with the chosen text through `IVsTextView.ReplaceTextOnLine`.
* `GuidFacWindow` handles the keys itself: Space makes a new GUID, Up/K and Down/J choose a format, Left/H and Right/L switch between lower case, upper case and both, Shift inverts lower and upper case while it is held (it does nothing in the "both" mode), C toggles the copy to the clipboard, Enter inserts and Esc cancels; a double click on the list inserts too. The dialog itself shows hints for some of the keys (`GuidFacWindow.xaml`) and `ReleaseNotes.txt` lists them, so change them together. `GuidSequences` builds the formats.
* `GuidFacConfig` keeps two settings, the case mode and whether to copy the GUID to the clipboard, in `%APPDATA%\GuidFac 06b6528b-1a8f-43fd-b717-411ea0dbb3de\config.ini` (the folder name contains the package GUID). The key of the case mode is spelled `GuidFaceMode` in the file; renaming it would drop the saved setting of every user.

### Private Visual Studio Assemblies

`MenuItemCallback` has to find the WPF window that hosts the text view, so that the dialog is owned by it and opens next to the caret. Walking up the visual tree did not work in the SQL editor of SQL Server Data Tools, which has no final root window parent (commit `450b2ca`), so the code asks the window frame instead. It uses types of assemblies that are installed with Visual Studio but are not part of the public SDK (no NuGet package, no compatibility promise):

* `Microsoft.VisualStudio.Platform.WindowManagement` – `WindowFrame` and its `RootView`
* `Microsoft.VisualStudio.Shell.ViewManager` – `UtilityMethods.WindowFromView` (namespace `Microsoft.VisualStudio.PlatformUI.Shell`)
* `Microsoft.VisualStudio.Diagnostics.Assert` – referenced as well, although no source file uses its types

The project file references them with a `<HintPath>` into the Visual Studio that builds the extension (`$(MSBuildExtensionsPath)\..\Common7\IDE`, and `PrivateAssemblies` for the last one) and `<Private>False</Private>`. What follows from that:

* A package update does not touch them. They are resolved from the Visual Studio whose MSBuild runs the build.
* They are not a stable API: a Visual Studio update can rename or remove what the extension uses. When the dialog does not open, or the extension fails with a `MissingMethodException` or a `TypeLoadException`, look here first. The callback has no exception handling of its own and does not check the window frame for `null`.
* Their assembly version follows the Visual Studio that builds the extension: Visual Studio 2026 ships them as 18.0.0.0, while the public SDK assemblies from the packages stay at 17.0.0.0. Building with Visual Studio 2026 stops with `CS1705` ("uses `Microsoft.VisualStudio.Utilities, Version=18.0.0.0` which has a higher version than the referenced assembly"). Build with the oldest Visual Studio that must be supported, here 2022: a newer host usually redirects the older references to its own assemblies, an older host cannot satisfy a newer reference.
* Never set `Private` to `True`: the assemblies belong to Visual Studio and must not be shipped in the VSIX. Do not remove a reference, even one that no source file uses, without a build and a run in the Experimental instance. Do not add other private APIs without a clear reason and a fallback.

## Visual Studio Extension Rules

* Threading: use Visual Studio COM objects (`DTE`, `IVs*` services) and WPF elements on the UI thread only. `MenuItemCallback` runs on the UI thread, which is why it can call `GetService` synchronously; new asynchronous code switches with `await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken)` before it touches such an object. Never block with `.Result` or `.Wait()`. The existing code does not follow this everywhere, so do not copy it: the package declares `AllowsBackgroundLoading`, yet its constructor shows a message box on errors and `InitializeAsync` begins with a synchronous `base.Initialize()` and adds the command without switching to the UI thread, and `GuidFacConfig.Write` runs on a thread-pool thread (`Task.Run` in `CloseAndInsert`) and shows a message box in its own `catch`. Keep new initialization in `InitializeAsync`. The VSTHRD analyzers come with `Microsoft.VisualStudio.SDK`: fix what they report, and where a suppression is unavoidable use a narrow `#pragma warning disable` / `restore` pair
* Errors: never swallow a caught exception, and never let one escape from an event handler into Visual Studio. There is no logging helper yet: the settings code shows a message box and `GuidFacWindow.CloseAndInsert` has an empty `catch` around the background write of the settings, which new code must not copy. Log to the activity log (`ActivityLog.LogError`) or to an Output window pane, and see the skill `handling-extension-errors`. `async void` is for event handlers only
* Theming: the dialog follows the Visual Studio theme. `GuidFacWindow.xaml` takes its colors from `VsBrushes` keys (and from `EnvironmentColors` for the status bar) with `DynamicResource`, as do `Resources/Styles.ImageButton.xaml`, `Styles.LinkButton.xaml`, `Styles.ListViewItem.xaml` and `Styles.TextBox.xaml`; the environment font is set on the status bar only. The two Expander styles hard-code colors and a few styles use `SystemColors` keys, which new code must not copy: never hard-code colors or fonts
* Generated files: `Resources.Designer.cs` is generated from `Resources.resx` by `ResXFileCodeGenerator`; edit the `.resx`. `Guids.cs` and `PkgCmdID.cs` are not generated: they are hand-kept copies of the ids in `GuidFac.vsct` (their comments still say "MUST match" `guids.h` and `PkgCmdID.h`, files of the C++ package template that do not exist here)
* Failures that appear only at run time: an id that differs between `GuidFac.vsct` and `Guids.cs` or `PkgCmdID.cs` leaves the command missing or dead; `[ProvideMenuResource("Menus.ctmenu", 1)]` must match the `ResourceName` of the `VSCTCompile` item; `[InstalledProductRegistration("#110", "#112", ..., IconResourceID = 400)]` reads `VSPackage.resx`; a missing `<Content>` item for an icon compiles and breaks the VSIX later. Check such changes in the Experimental instance
* .NET Framework 4.7.2 and C# 10: file-scoped namespaces are used in the hand-written files; C# 11 and later features (raw string literals, `required` members, list patterns) do not compile. The framework has no `Index`/`Range`, default interface members or `IsExternalInit` (`record` and `init` need a polyfill type, which the project does not have). `Span<T>`, `Memory<T>` and `IAsyncEnumerable<T>` compile only because the packages that the project restores bring `System.Memory` and `Microsoft.Bcl.AsyncInterfaces`; do not add a polyfill package for something that already compiles, and check that an API exists in .NET Framework 4.7.2 before using it
* Package versions: the Visual Studio SDK packages are 17.2 (`Microsoft.VisualStudio.SDK` is referenced with `ExcludeAssets="runtime"`). Visual Studio loads its own copy of some libraries, so check the build warnings and run the extension before you raise a version
* Stale declarations: do not take the old dependencies of the manifest or the binding redirects of `app.config` as a requirement of the code

## Libraries in Use

* [Visual Studio SDK](https://learn.microsoft.com/visualstudio/extensibility/) packages (`Microsoft.VisualStudio.SDK`, `Microsoft.VisualStudio.Interop`, `Microsoft.VisualStudio.Shell.15.0`) – the shell, editor and WPF dialog APIs
* `Microsoft.VSSDK.BuildTools` – builds the VSIX (`.vsct`, `.pkgdef`, packaging)
* Windows Forms (`System.Windows.Forms.Screen`) – used by the dialog to place itself on the right monitor

## Agents and Skills

* Use the `vs-extension-expert` agent (#file:'.github/agents/vs-extension-expert.agent.md') for work on the extension code.
* Skills live in `.github/skills`; `Directory.Build.targets` links the folder to the git-ignored `.claude/skills` for Claude Code. They are copied from [`vs-agent-plugins`](https://github.com/madskristensen/vs-agent-plugins) by Mads Kristensen, see #file:'.github/skills/README.md': `handling-async-threading`, `handling-extension-errors`, `adding-commands`, `theming-extension-ui`, `providing-consuming-services` and `showing-message-boxes`. When a task matches the domain of a skill, load it and follow it before you write code.
* `.claude/settings.json` also enables the whole catalogue (40 skills) as the Claude Code plugin `vs-extensibility-skills`; if Claude Code has not offered it, install it with `claude plugin install vs-extensibility-skills@vs-agent-plugins --scope project`.
* The skills describe up to three approaches: VisualStudio.Extensibility (out of process), Community.VisualStudio.Toolkit and VSSDK. This extension is in-process VSSDK (`AsyncPackage`) without the Toolkit, so follow the VSSDK parts and ignore the others.

## MCP Servers

Agents discover servers from #file:'.mcp.json'; these are only hints on when to prefer one.

* Use the `GitHub` server for code search in dependency repositories. In Claude Code it authenticates through `.claude/scripts/github-mcp-headers.ps1`, which reuses the GitHub token stored by Git Credential Manager, because the server does not support the OAuth dynamic client registration Claude Code needs.
* Use the `Microsoft Learn` server for official Microsoft documentation, the Visual Studio extensibility documentation included. Visual Studio bundles this server itself, so `.mcp.json` lists it for Claude Code only.
* Visual Studio reads `.mcp.json` from the folder of the solution, and Claude Code reads it from the root of the repository. The solution is in the repository root so that one file serves both; keep it there.
* When the solution is open in Visual Studio and an MCP server that exposes the IDE (`vs`) is connected, prefer its tools to the shell: build with it instead of resolving MSBuild, read the diagnostics from it instead of parsing the build output, and navigate with its symbol tools instead of text search.
* When adding or changing a server, keep `servers` (Visual Studio / VS Code / GitHub Copilot) and `mcpServers` (Claude Code) in sync and sorted alphabetically by key, except for the servers that Visual Studio already bundles (Microsoft Learn, NuGet): those go only in `mcpServers`.

## Definition of Done

Before reporting a task as finished:

1. Match the style of the surrounding code; the repository has no `.editorconfig`.
2. Build the solution with Visual Studio 2022 and make sure that it succeeds with no errors; do not claim a successful build that you did not run. If only Visual Studio 2026 is installed, say that the build could not be run (it stops with `CS1705`).
3. Compare the warnings with the build before your change: the change must add none.
4. There is no automated test project, so say what you could not verify without the Experimental instance instead of claiming that it works. For the window lookup that also means an undocked editor window and a SQL editor.
5. Update `Source/GuidFac/ReleaseNotes.txt` when a user-visible feature changes, and `README.md` when the build steps change.

## C# Coding Guidelines

* Match the file that you edit; do not reformat code that you do not change. The code is indented with 4 spaces, with braces on their own line for types and methods. The text files are stored with CRLF line endings (`Source/GuidFac/app.config` is the one stored with LF), and the source files, the project file and the solution start with a UTF-8 byte order mark, which you keep (`.gitignore` and `README.md` have none).
* `.gitignore` ignores `*.xml`, `*.dll`, `*.cache`, `bin/`, `obj/`, `.vs/` and `packages/`: a new file of those kinds stays untracked until you add it with `git add -f` or change the rule.
* Types live in the `PrivateDeveloperInc.GuidFac` namespace (the assembly is `GuidFac`), declared file-scoped (`namespace PrivateDeveloperInc.GuidFac;`) with the `using` directives after it. Do not convert existing files.
* Name async methods with the `Async` suffix and pass the `CancellationToken` through async chains, as the last parameter in new methods (the `InitializeAsync` override takes it first because the base class does).
* The ids in `Guids.cs` and `PkgCmdID.cs` keep the camelCase names of the package template (`guidGuidFacPkgString`, `cmdidInsertGuid`); do not rename them.
* XML documentation: document new public and protected members.

## Naming Conventions

* Use PascalCase for types, methods and public members, and camelCase without a prefix for private fields (`config`, `currentGuid`, `buttonSelector`) and PascalCase for private constants (`MakeNiceMargin`).
* Prefix interface names with `I`. Name the files after the type they hold; the exceptions are `GuidSequncesGenerator.cs` (holds `GuidSequences`), `Guids.cs` (`GuidList`), `PkgCmdID.cs` (`PkgCmdIDList`) and `GuidFacConfig.cs` (which also holds the `GuidFormat` enum).

## General

* Make only high-confidence suggestions when reviewing code changes.
* Write code with good maintainability practices, including comments on why certain design decisions were made.
* Handle edge cases and write clear exception handling.
* Never duplicate code unless explicitly allowed.
* All comments, documentation, README files, and markdown files must be written in **English only**.
* Use an en dash (`–`) as a punctuation dash, never an em dash (`—`), and do not use emojis in text, comments or messages.
* In markdown, write each paragraph on one line instead of hard-wrapping it.
* Scripts and one-off automation: PowerShell 7 (`pwsh`, `.ps1`) or F# Interactive (`dotnet fsi`, `.fsx`); do not use Python.
* The default branch is `master`.
* Commit messages follow the recent history of this repository: one short sentence in the past tense (`Migrated to C# 10 and file-scoped namespaces`), no prefix.
