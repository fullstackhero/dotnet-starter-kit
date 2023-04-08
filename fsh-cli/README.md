# Fullstackhero CLI Tool

## Prerequisites

Before creating your first fullstackhero solution, you should ensure that your local machine has:

- **.NET 7** You can find the download [here](https://dotnet.microsoft.com/en-us/download/dotnet/7.0).
- **NodeJS (16+)** You can find the download [here](https://nodejs.org/en/download).

## Installation

After you have installed .NET, you will need to install the `fsh` console tool.

```bash
dotnet tool install --global FSH.CLI
fsh install
```

You are now ready to create your first FSH project!

## FSH .NET WebAPI Boilerplate
Here's how you would create a Solution using the FSH .NET WebAPI Boilerplate.

```bash
fsh api new Demo.Server
```

OR

```bash
fsh api n Demo.Server
```

This will create a new solution for you using the FSH Templates.

## FSH Blazor WASM Boilerplate
Here's how you would create a Solution using the FSH Blazor WASM Boilerplate.

```bash
fsh wasm new Demo.Blazor
```

OR

```bash
fsh wasm n Demo.Blazor
```

This will create a new solution for you using the FSH Templates.

## Update

```bash
dotnet tool update FSH.CLI --global
```

## Uninstall
```bash
dotnet tool uninstall FSH.CLI --global
```

# NuGet Generation
For developers
```bash
dotnet pack
```

# More Features Incoming!