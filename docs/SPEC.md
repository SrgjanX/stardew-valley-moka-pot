# Moka Pot mod: spec (v0.1)

A portable moka pot for Stardew Valley 1.6. You carry it like any item, load it with coffee beans
anywhere (farm, mines, beach), and after a short wait it gives you a normal Coffee.

## Decisions

| Choice | Pick | Notes |
|---|---|---|
| Balance | 2 coffee beans per brew | A keg uses 5 beans for 1 coffee. Configurable (`BeansPerBrew`). |
| Brew style | Timed, 30 in-game minutes | A keg takes about 2 hours. Configurable (`BrewMinutes`). |
| How you get it | Crafting recipe: 2 Copper Bar + 1 Refined Quartz, or buy it from Pierre for 300g | Recipe known from the start; taught on save load for existing saves. Always in stock at Pierre's, no limit. |
| Art | Placeholder 16×16 sprites (lid closed, lid open) | `MokaPot/assets/moka-pot.png`. Swap in real art any time. |

## The item

- Added to `Data/Objects` from C# under ID `Srgjan.MokaPot_MokaPot` (qualified `(O)Srgjan.MokaPot_MokaPot`).
- Type `Basic`, category 0, sells for 150g, not edible.
- Sprite loaded from the asset `Mods/Srgjan.MokaPot/MokaPot`.
- Added to Pierre's General Store (`Data/Shops` entry `SeedShop`) for 300g. The mod also adds
  Coffee Beans (`(O)433`) there for 20g each. Both entries skip the shop's price doubling
  (`IgnoreShopPriceModifiers`).

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

## Ready: lid opens

The sprite sheet holds two 16×16 frames: closed lid (index 0) and open lid with coffee showing
(index 1). The open-lid pot is a second item, `Srgjan.MokaPot_MokaPotReady`, because 1.6 draws an
object from its data entry's sprite index rather than per item.

When time passes, a new day starts, or the inventory changes, the mod checks the player's
inventory. A closed pot whose brew has finished is swapped for the open-lid pot (keeping its
`modData`), with a sound and "Your moka pot is ready!". Collecting the coffee swaps it back to the
closed pot. A pot that finishes inside a chest opens when you take it out.

## Drinking coffee without the prompt

With `SkipCoffeePrompt` on (the default), using any Coffee drinks it straight away instead of asking
"Drink Coffee?". The mod doesn't take over the button press, so gifting coffee or putting it in a
machine still works as usual. When the game opens its eat question (`lastQuestionKey` `Eat`)
within a few ticks of you pressing the action button with Coffee in hand, the mod answers "Yes"
for you and closes the box. Other food and drinks still ask.

## Translations

All 8 texts the mod shows (item name, two descriptions, five messages) come from `MokaPot/i18n/`:
`default.json` (English) plus `de`, `es`, `fr`, `hu`, `it`, `ja`, `ko`, `pt`, `ru`, `tr` and `zh`, one
for each language the game ships. Counts use the `{{beans}}` and `{{minutes}}` tokens. When the
game language changes, the mod reloads `Data/Objects` and `Data/CraftingRecipes` so the pot's name
and descriptions switch too. Coffee, Coffee Bean and the "Drink Coffee?" question are the game's
own text and are already translated.

## Not in v0.1 (ideas for later)

- Showing the brew time left in the item's tooltip.
- Multiplayer has not been tested; it should work per player since each pot holds its own state.
