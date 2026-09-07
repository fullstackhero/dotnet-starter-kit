# `fsh` CLI — forks, agents, and framework packages

This is the maintainer guide for three opt-in capabilities of the `fsh` CLI:

1. **Scaffolding from your own fork or branch**, instead of whatever template happens to be installed.
2. **Shipping the `.agents` kit** into generated projects, so AI tools have project context.
3. **Consuming `src/BuildingBlocks` as `FSH.Framework.*` NuGet packages** from a local feed, instead of copying its source into every project.

> **Nothing here changes the default.** `fsh new MyApp` still produces exactly what it always
> did: a fully owned, detached source tree with no framework packages. Every capability below is
> off unless you ask for it.

**Contents** — [Why](#why-framework-packages) · [Setup](#one-time-setup) · [Install `fsh`](#installing-fsh-from-this-source) · [Daily loop](#the-everyday-loop) ·
[Fork scaffolding](#scaffolding-from-your-own-fork) · [Agents](#shipping-the-agents-kit) ·
[Debugging](#debugging-into-buildingblocks) · [Troubleshooting](#troubleshooting) ·
[Reference](#command-reference) · [Swapping modes](#swapping-an-existing-project-between-the-two-modes)

---

## Why framework packages

The starter kit's default distribution model is **source ownership**: you get every BuildingBlock
as source, wired by `ProjectReference`, with nothing to eject later. For a single product that is
the right call, and it stays the default.

It stops scaling when you run *several* products off one kernel. Ten projects means ten copies of
`src/BuildingBlocks`, drifting apart, with every fix applied by hand N times.

Framework packaging inverts that for teams in that position: the kernel is built once, published
to a NuGet feed, and consumed as versioned binaries. You keep owning the modules — only
BuildingBlocks becomes a package.

| | Source ownership (default) | Framework packages (opt-in) |
|---|---|---|
| Kernel lives in | every project | one starter-kit clone |
| Fixing a kernel bug | edit N projects | pack once, bump N versions |
| Editing kernel code in-project | yes | no — edit it in the kit |
| Step-into debugging | trivially | yes, via embedded PDBs |
| Best for | one product | a platform team, several products |

---

## One-time setup

You need a starter-kit clone: `fsh framework` builds packages from BuildingBlocks source, so it
refuses to run anywhere else.

```bash
git clone <your-fork> ~/dev/dotnet-starter-kit
cd ~/dev/dotnet-starter-kit

# Build the 11 FSH.Framework.* packages, publish them to a local feed, and register that
# feed as a NuGet source. The feed directory is created if it does not exist.
dotnet run --project src/Tools/CLI -- framework pack --push --register-source
```

The feed defaults to `~/.fsh/local-nuget`. To put it somewhere else, either pass `--feed` every
time or set it once:

```bash
# ~/.zshrc or ~/.bashrc
export FSH_LOCAL_FEED=~/dev/nuget-local          # where framework packages live
export FSH_TEMPLATE_PATH=~/dev/dotnet-starter-kit # scaffold from your fork, not nuget.org
```

With those exported, later commands need no flags at all.

### Running the CLI

```bash
dotnet run --project src/Tools/CLI -- framework pack   # always matches the source in front of you
dotnet tool restore && dotnet fsh framework pack       # version pinned in .config/dotnet-tools.json
fsh framework pack                                     # globally installed tool
```

The first form is exact but verbose, and it only works from inside the clone. The third is the one
you want day to day — but by default it is whatever `FullStackHero.CLI` is published on nuget.org,
which will not have your fork's changes.

### Installing `fsh` from this source

`fsh self install` closes that gap: it packs the CLI from the current working copy and installs it
as the global tool, so `fsh` *is* your fork.

```bash
dotnet run --project src/Tools/CLI -- self install    # once, to bootstrap
fsh self install                                      # afterwards, to pick up CLI changes
```

```
Repository: /Users/you/dev/dotnet-starter-kit
Version:    10.0.0-local.20260902T030247
  packed FullStackHero.CLI
  installed global tool 'fsh'
```

Every command in this guide then works as written, from any directory:

```bash
fsh new FS.Proxy --output /Users/you/dev/falconsoft/fs-proxy \
  --agents --framework-packages --framework-feed /Users/you/dev/nuget-local
```

And with `FSH_LOCAL_FEED` and `FSH_TEMPLATE_PATH` exported, down to:

```bash
fsh new FS.Proxy --output /Users/you/dev/falconsoft/fs-proxy --agents --framework-packages
```

Notes:

- The build is stamped `10.0.0-local.<timestamp>`, so `fsh --version` tells you whether you are
  running your own build or the published one — and each install is a distinct version, so
  `dotnet tool update` never mistakes a rebuild for "already current".
- **Re-run `fsh self install` after changing CLI source.** The global tool is a snapshot, not a
  live link to the repo; while iterating on the CLI itself, `dotnet run --project src/Tools/CLI --`
  is still the shorter loop.
- `fsh self uninstall` removes it. To return to the published build afterwards:
  `dotnet tool install -g FullStackHero.CLI`.
- The tool lands in `~/.dotnet/tools`. If that is not on your `PATH`, the command says so and
  prints the line to add.

### Creating a project that uses the packages

Running the CLI straight from the kit's source — the form to use while working on a fork, since
it always matches the code in front of you:

```bash
dotnet run --project src/Tools/CLI -- new FS.Proxy \
  --agents --framework-packages \
  --framework-feed /Users/you/dev/nuget-local
```

Or, with the globally installed tool and `FSH_LOCAL_FEED` exported:

```bash
fsh new FS.Proxy --agents --framework-packages
```

That scaffolds without `src/BuildingBlocks`, writes a `NuGet.config` pointing at your feed, and
pins the newest version found there.

> Note the `--` after the project path. It separates `dotnet run`'s own arguments from the ones
> meant for the CLI; without it `dotnet run` tries to interpret them itself.

### Choosing where the project is created

By default the project is created in a folder named after it, **inside the current directory** —
so running the command from a starter-kit clone drops the new project inside the kit. Use
`-o` / `--output` to put it anywhere:

```bash
dotnet run --project src/Tools/CLI -- new FS.Proxy \
  --output /Users/you/dev/falconsoft/fs-proxy \
  --agents --framework-packages
```

The directory is created if it does not exist, and the path is unrelated to the project name — so
`FS.Proxy` can live in `fs-proxy/`, as above. If the target exists and is not empty, `fsh new`
prompts before overwriting, and refuses outright under `--non-interactive`.

---

## The everyday loop

Change the kernel, republish, pick it up downstream:

```bash
# 1. edit something in src/BuildingBlocks, in your kit clone
dotnet run --project src/Tools/CLI -- framework pack --push --clear-cache

#    -> Published 11 package(s) to /Users/you/dev/nuget-local
#    -> Done. Consume with:
#         dotnet build -p:UseFrameworkPackages=true -p:FshFrameworkVersion=10.0.0-local.20260901T194030

# 2. in the consuming project, take the new version
```

Each pack stamps a **new, unique version** (`10.0.0-local.<timestamp>`). That is deliberate: NuGet
caches packages by id *and* version, so republishing one version with different content silently
serves the old bits — the single most common local-feed trap. A fresh version every time makes
that impossible, and `--clear-cache` handles anything already extracted.

To adopt a new version in a project, edit one line in `src/Directory.Packages.props`:

```xml
<FshFrameworkVersion Condition="'$(FshFrameworkVersion)' == ''">10.0.0-local.20260901T194030</FshFrameworkVersion>
```

Or override per build without touching the file:

```bash
dotnet build -p:FshFrameworkVersion=10.0.0-local.20260901T194030
```

To see what the feed currently holds:

```bash
fsh framework list          # newest of each package
fsh framework list --all    # every version
```

### How the reference rewrite works

There are no `PackageReference` lines to maintain. `src/Directory.Build.targets` rewrites any
`ProjectReference` pointing into `BuildingBlocks` into the matching `PackageReference`
(`..\..\BuildingBlocks\Core\Core.csproj` → `FSH.Framework.Core`), so the 33 project files are
identical in both modes.

It switches on automatically when `src/BuildingBlocks` is **absent** — which is exactly the shape
of a project scaffolded with `--framework-packages`. Nothing to remember, and it cannot fall out
of sync with how the project was created. Force it either way with `-p:UseFrameworkPackages=true|false`.

---

## Scaffolding from your own fork

By default `fsh new` uses whatever FSH template is already installed and never upgrades it, so a
fork's fixes never reach your scaffolds. Four options change that:

```bash
# from a working tree (what a contributor wants)
fsh new MyApp --template-path ~/dev/dotnet-starter-kit

# from a locally packed template
dotnet pack templates/FullStackHero.NET.StarterKit.csproj -o ./nupkgs
fsh new MyApp --template-path ./nupkgs        # a directory of .nupkg files works too

# a specific published version, or a private feed
fsh new MyApp --template-version 10.0.1-rc.2
fsh new MyApp --template-source https://my-feed/index.json

# force a re-install of whatever is configured
fsh new MyApp --refresh-template
```

`--template-path` accepts a starter-kit checkout, a `.nupkg`, or a folder containing packed
nupkgs (the newest is used). Any of these also re-installs the template rather than reusing a
stale one, and uninstalls the previous copy first — two template packages sharing the identity
`FullStackHero.NET.StarterKit` make `dotnet new` fail with
`Sequence contains more than one matching element`.

---

## Shipping the `.agents` kit

```bash
fsh new MyApp --agents
```

This writes `.agents/` (43 files of rules, skills and workflows) plus `AGENTS.md`, `CLAUDE.md` and
`GEMINI.md` into the project, so Claude Code, Cursor, Gemini CLI and friends start with real
context instead of guessing.

Without the flag, none of those four are written. That also fixes a long-standing wart: `AGENTS.md`
used to ship on its own, leaving every project with a rules index pointing at files that were never
copied.

**One caveat.** The template engine rewrites tokens throughout the copied files, including these.
That is mostly what you want — `src/Host/FSH.Starter.Api` correctly becomes `src/Host/MyApp.Api`,
and namespace examples follow the rename. The cosmetic cost is that brand prose is rewritten too,
so "FullStackHero .NET Starter Kit" reads "MyApp .NET Starter Kit". Worth one skim after scaffolding.

---

## Debugging into BuildingBlocks

Packages built with the default `local` profile carry an **embedded PDB with the sources embedded
inside it**. Stepping into framework code needs no symbol server, no source checkout, and no
network — the source travels inside the DLL.

This is why the local profile does not produce a `.snupkg`: a folder feed does not serve symbol
packages (only the NuGet.org symbol server does), so a `.snupkg` would be dead weight locally.

### You must turn off "Just My Code"

This is the step everyone misses. With it on, the debugger steps *over* framework code and the
embedded sources are never consulted.

| Tool | Where |
|---|---|
| Visual Studio | Tools → Options → Debugging → General → uncheck **Enable Just My Code** |
| Rider | Settings → Build, Execution, Deployment → Debugger → uncheck **Enable Just My Code** |
| VS Code | `"justMyCode": false` in the configuration in `.vscode/launch.json` |

```jsonc
// .vscode/launch.json
{
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "justMyCode": false
    }
  ]
}
```

Then set a breakpoint in a module handler and step into any `FSH.Framework.*` call.

### Publishing publicly instead

If you push to a real NuGet server with a symbol server, use the other profile:

```bash
fsh framework pack --profile public --version 10.1.0
```

That produces a normal DLL plus a `.snupkg` with SourceLink, which is what nuget.org expects.

---

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Kernel changes don't show up in the consuming project | the same version was restored from the NuGet cache | `fsh framework pack --push --clear-cache`, and make sure the project pins the new version |
| `NU1101: Unable to find package FSH.Framework.Core` | feed not registered, or `NuGet.config` points elsewhere | `fsh framework pack --register-source`; check `NuGet.config` in the project root |
| `NU1103: no stable version found` | local packages are prereleases | pin `FshFrameworkVersion` explicitly, or pack with `--profile public` and a stable `--version` |
| F11 steps over framework code | Just My Code is enabled | see [above](#you-must-turn-off-just-my-code) |
| `fsh new` keeps scaffolding the old template | the installed template is sticky and never auto-upgrades | `--refresh-template`, or `--template-path`, or `fsh update` |
| `Sequence contains more than one matching element` | two template packages share the FSH template identity | `dotnet new uninstall FullStackHero.NET.StarterKit`, then re-install (the CLI now does this for you) |
| Project scaffolds with `src/BuildingBlocks` and no `NuGet.config` despite passing the flags | template symbol names (`--frameworkPackages`) were used instead of CLI option names (`--framework-packages`) | use the CLI spelling; unknown options are now a hard error rather than silently ignored |
| `Framework feed not found: ~/.fsh/local-nuget` | no feed configured and packages live elsewhere | pass `--framework-feed <path>` or `export FSH_LOCAL_FEED=<path>` |
| `fsh` runs but lacks the new options | the globally installed tool is the published build, not your fork | `dotnet run --project src/Tools/CLI -- self install` |
| `fsh: command not found` right after installing | `~/.dotnet/tools` is not on `PATH` | add it to your shell profile; `fsh self install` prints the exact line |
| Project was created inside the starter-kit clone | `fsh new` defaults to the current directory | pass `-o /path/to/project` |
| `Not inside a FullStackHero starter-kit repository` | `fsh framework` ran outside a clone | `cd` into a directory that has both `src/BuildingBlocks` and `.template.config` |
| `NU5026: the file ... .pdb is not found` when packing by hand | `--no-build` with an embedded PDB — there is no `.pdb` on disk to pack | drop `--no-build` (`fsh framework pack` already does) |

---

## Command reference

### `fsh framework pack`

Builds the 11 BuildingBlocks projects as `FSH.Framework.*` packages. Must run inside a
starter-kit clone.

| Option | Default | Notes |
|---|---|---|
| `-f, --feed <path>` | `$FSH_LOCAL_FEED`, else `~/.fsh/local-nuget` | created if missing |
| `-v, --version <ver>` | `10.0.0-local.<timestamp>` | unique per run, on purpose |
| `-o, --output <dir>` | `<repo>/artifacts/nupkgs` | where `.nupkg` files are written |
| `-p, --profile <local\|public>` | `local` | `local` = embedded PDB + sources; `public` = `.snupkg` + SourceLink |
| `--push` | off | copy the packages into the feed |
| `--register-source` | off | `dotnet nuget add source` if not already registered |
| `--clear-cache` | off | purge `FSH.Framework.*` from the global-packages cache |
| `--dry-run` | off | print the plan and stop |

### `fsh framework list`

| Option | Default |
|---|---|
| `-f, --feed <path>` | `$FSH_LOCAL_FEED`, else `~/.fsh/local-nuget` |
| `--all` | off — show only the newest version of each package |

### `fsh framework swap`

Converts an existing project between the two modes. Runs inside the **project**, not a
starter-kit clone.

| Option | Default | Notes |
|---|---|---|
| `-t, --to <source\|packages>` | — | required; which side to swap to |
| `--project <path>` | current directory | the project to convert |
| `--from <path>` | `$FSH_TEMPLATE_PATH` | kit/template to regenerate the kernel from (`--to source`) |
| `-f, --feed <path>` | `$FSH_LOCAL_FEED`, else `~/.fsh/local-nuget` | feed to consume from (`--to packages`) |
| `-v, --version <ver>` | newest in the feed | version to pin (`--to packages`) |
| `-y, --yes` | off | skip the confirmation before deleting kernel source |
| `--dry-run` | off | print the plan and stop |

### `fsh framework clean-cache`

| Option | Default |
|---|---|
| `--dry-run` | off — list what would be removed without deleting |

### `fsh self install` / `fsh self uninstall`

Builds the global `fsh` tool from this repository. `install` must run inside a starter-kit clone.

| Option | Default | Notes |
|---|---|---|
| `-v, --version <ver>` | `10.0.0-local.<timestamp>` | stamped on both the package and `fsh --version` |
| `-o, --output <dir>` | `<repo>/artifacts/nupkgs` | where the `.nupkg` is written |
| `--dry-run` | off | print the plan and stop |

### `fsh new` — new options

| Option | Env var | Notes |
|---|---|---|
| `-o, --output <path>` | `./<name>` | where the project is created; created if missing |
| `--template-path <path>` | `FSH_TEMPLATE_PATH` | checkout, `.nupkg`, or folder of nupkgs |
| `--template-version <ver>` | `FSH_TEMPLATE_VERSION` | installs `FullStackHero.NET.StarterKit::<ver>` |
| `--template-source <feed>` | `FSH_TEMPLATE_SOURCE` | extra NuGet source for the install |
| `--refresh-template` | — | re-install even if a template is present |
| `--agents [true\|false]` | `FSH_AGENTS=1` | include `.agents` + `AGENTS.md`/`CLAUDE.md`/`GEMINI.md` |
| `--framework-packages [true\|false]` | — | consume BuildingBlocks as packages |
| `--framework-version <ver>` | — | defaults to the newest in the feed |
| `--framework-feed <path>` | `FSH_LOCAL_FEED` | written into the project's `NuGet.config` |

Every option resolves **flag → environment variable → default**.

`--agents` and `--framework-packages` accept a bare flag or an explicit value, so both
`--agents` and `--agents true` work (and `--agents false` turns it off). Unknown options are a
hard error: these are the `fsh` option names, **not** the `dotnet new` symbol names — see the
next section for those.

### Using it from `dotnet new` directly

The CLI only forwards template symbols, so the template works standalone:

```bash
dotnet new fsh -n MyApp --agents true --frameworkPackages true --frameworkVersion 10.0.0-local.20260901T194030
dotnet nuget add source ~/dev/nuget-local --name fsh-local   # the CLI does this part for you
```

---

## Swapping an existing project between the two modes

Framework packaging is not a one-way door, and you do not have to decide at scaffold time.
`fsh framework swap` converts a project that already exists, in either direction. Unlike the other
`framework` commands it runs **inside the project**, not inside a starter-kit clone.

```bash
cd /Users/you/dev/falconsoft/fs-proxy

fsh framework swap --to source      # bring src/BuildingBlocks back and own it
fsh framework swap --to packages    # drop the source, consume FSH.Framework.* instead
```

Add `--dry-run` to see the plan first, or `--project <path>` to act on a project elsewhere.

### `--to source`

Regenerates `src/BuildingBlocks` (and `src/Tests/Framework.Tests`), adds them to the `.slnx`, and
removes the local feed from `NuGet.config`. `Directory.Build.targets` sees the source on disk and
stops rewriting references, so nothing else changes.

The kernel is **regenerated from the template**, not copied out of a starter-kit clone. That
matters: the template rewrites tokens inside BuildingBlocks, and not all of them are cosmetic —
`MultitenancyConstants.Issuer`, for instance, is derived from the project name. A raw copy would
quietly install the starter kit's own JWT issuer into your project. The command scaffolds a
throwaway copy under your project's own name and takes the kernel from that.

It needs a template to generate from, resolved exactly like `fsh new`:

```bash
export FSH_TEMPLATE_PATH=~/dev/dotnet-starter-kit   # your fork
fsh framework swap --to source

fsh framework swap --to source --from ~/dev/dotnet-starter-kit   # or pass it explicitly
```

Point it at the same kit the project came from, so the kernel you get back matches the packages
you were consuming.

### `--to packages`

Deletes `src/BuildingBlocks` and `src/Tests/Framework.Tests`, removes them from the `.slnx`, writes
`NuGet.config`, and pins `FshFrameworkVersion` to the newest version in the feed (or `--version`).

Deleting the kernel source is the one irreversible step, so it prompts first; pass `--yes` in
scripts. `--to source` can regenerate the source, but any **local edits** you made to
BuildingBlocks are gone — move them into a module, or upstream them into your fork, first.

```bash
fsh framework swap --to packages --feed ~/dev/nuget-local --yes
```

Both directions are idempotent: swapping to the mode a project is already in reports that and
exits successfully.

### Doing it by hand

Nothing about either mode is magic, if you would rather not use the command. To go back to source:
copy in a `src/BuildingBlocks` generated for your project name, add the projects to the `.slnx`, and
drop the local feed from `NuGet.config`. The `FSH.Framework.*` entries left in
`Directory.Packages.props` become inert — central package management ignores versions nothing
references.
