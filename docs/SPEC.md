# Moka Pot mod: spec (v0.1)

A portable moka pot for Stardew Valley 1.6. You carry it like any item, load it with coffee beans
anywhere (farm, mines, beach), and after a short wait it gives you a normal Coffee.

## Decisions

| Choice | Pick | Notes |
|---|---|---|
| Balance | 2 coffee beans per brew | A keg uses 5 beans for 1 coffee. Configurable (`BeansPerBrew`). |
| Brew style | Timed, 30 in-game minutes | A keg takes about 2 hours. Configurable (`BrewMinutes`). |
| How you get it | Crafting recipe: 2 Copper Bar + 1 Refined Quartz | Known from the start; taught on save load for existing saves. |
| Art | Placeholder 16×16 sprite | `MokaPot/assets/moka-pot.png`. Swap in real art any time. |

## The item

- Added to `Data/Objects` from C# under ID `Srgjan.MokaPot_MokaPot` (qualified `(O)Srgjan.MokaPot_MokaPot`).
- Type `Basic`, category 0, sells for 150g, not edible.
- Sprite loaded from the asset `Mods/Srgjan.MokaPot/MokaPot`.

## Using it

With the pot as the active item, pressing the action button (right click / A) is suppressed so the
game doesn't try to place, eat or gift it, and then:

1. **Empty pot.** If you have enough beans, they're removed, and the pot's `modData` gets
   `Srgjan.MokaPot/ReadyAt` = the absolute game minute when it'll be done. Otherwise a red message
   says how many beans you need. A stack of more than one pot refuses to brew, because stacked
   items share `modData`.
2. **Still brewing.** A message shows the minutes left.
3. **Ready.** You get one Coffee (`(O)395`, with its usual speed buff when drunk). If your
   inventory is full it drops at your feet. The pot is emptied.

"Absolute game minute" is `TotalDays × 1440 + minutes since midnight`, so a brew keeps counting
across days and sleeping. Because the state lives in `modData`, it's saved with the game and
moves with the pot (chests, other inventory slots, and so on).

## Ready notification

On every in-game time change, the mod checks the player's inventory. When a pot's brew has finished
it plays a sound and shows "Your moka pot is ready!" once (tracked with `Srgjan.MokaPot/Notified`).

## Not in v0.1 (ideas for later)

- Translations (`i18n/default.json`); text is English only for now.
- Showing the brew state in the item's tooltip, or a different sprite while brewing.
- Brewing other drinks (for example Triple Shot Espresso from more beans).
- A shop entry as an alternative to crafting.
- Multiplayer has not been tested; it should work per player since each pot holds its own state.
