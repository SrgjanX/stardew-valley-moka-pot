# Moka Pot (Stardew Valley mod)

A portable moka pot for Stardew Valley 1.6. Craft it, put 2 coffee beans in it anywhere, and
after 30 in-game minutes you get a Coffee. See [docs/SPEC.md](docs/SPEC.md) for the details.

## Requirements

- Stardew Valley 1.6
- [SMAPI](https://smapi.io/) 4.0 or later
- .NET 6 SDK (to build)

## Build

```
dotnet build MokaPot/MokaPot.csproj
```

The `Pathoschild.Stardew.ModBuildConfig` package finds your game folder, references the game
DLLs, and copies the built mod into `Stardew Valley/Mods/MokaPot`. If it can't find the game, set
`GamePath` in the `.csproj` or in a `stardewvalley.targets` file (see the
[ModBuildConfig docs](https://smapi.io/package/readme)).

Then launch the game through SMAPI.

## Try it in game

1. Load a save. The Moka Pot recipe is in the crafting menu (2 Copper Bar + 1 Refined Quartz).
   For quick testing, the SMAPI console can spawn items: `debug item (O)433 10` gives coffee beans
   and `debug item (O)Srgjan.MokaPot_MokaPot` gives the pot.
2. Hold the pot and right click (or press A) to start a brew.
3. Wait 30 in-game minutes, then use the pot again to get a Coffee.

## Config

After the first run, `Mods/MokaPot/config.json` lets you change `BeansPerBrew` and `BrewMinutes`.
