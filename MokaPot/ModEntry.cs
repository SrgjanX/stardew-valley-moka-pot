using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;

namespace MokaPot
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        /*********
        ** Fields
        *********/
        /// <summary>The qualified item ID for Coffee Bean.</summary>
        private const string CoffeeBeanId = "(O)433";

        /// <summary>The qualified item ID for Coffee.</summary>
        private const string CoffeeQualifiedId = "(O)395";

        /// <summary>The mod settings.</summary>
        private ModConfig Config = null!;

        /// <summary>The unqualified item ID for the moka pot.</summary>
        private string MokaPotId = null!;

        /// <summary>The qualified item ID for the moka pot.</summary>
        private string MokaPotQualifiedId = null!;

        /// <summary>The asset name for the moka pot sprite.</summary>
        private string TextureAssetName = null!;

        /// <summary>The <c>modData</c> key holding the absolute game minute when the current brew is ready.</summary>
        private string ReadyAtKey = null!;

        /// <summary>The <c>modData</c> key set once the player has been told the brew is ready.</summary>
        private string NotifiedKey = null!;


        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public override void Entry(IModHelper helper)
        {
            this.Config = helper.ReadConfig<ModConfig>();

            string modId = this.ModManifest.UniqueID;
            this.MokaPotId = $"{modId}_MokaPot";
            this.MokaPotQualifiedId = $"(O){this.MokaPotId}";
            this.TextureAssetName = $"Mods/{modId}/MokaPot";
            this.ReadyAtKey = $"{modId}/ReadyAt";
            this.NotifiedKey = $"{modId}/Notified";

            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Add the moka pot item, its sprite, its crafting recipe, and its shop entry to the game data.</summary>
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(this.TextureAssetName))
            {
                e.LoadFromModFile<Texture2D>("assets/moka-pot.png", AssetLoadPriority.Exclusive);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;
                    data[this.MokaPotId] = new ObjectData
                    {
                        Name = this.MokaPotId,
                        DisplayName = "Moka Pot",
                        Description = $"A little stovetop coffee maker. Use it with {this.Config.BeansPerBrew} coffee beans to brew a cup anywhere.",
                        Type = "Basic",
                        Category = 0,
                        Price = 150,
                        Texture = this.TextureAssetName,
                        SpriteIndex = 0,
                        Edibility = -300
                    };
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                e.Edit(asset =>
                {
                    // format: ingredients / unused / output / is big craftable / unlock condition / display name
                    var data = asset.AsDictionary<string, string>().Data;
                    data[this.MokaPotId] = $"334 2 338 1/Home/{this.MokaPotId}/false/default/Moka Pot";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
            {
                e.Edit(asset =>
                {
                    // sell the pot and coffee beans at Pierre's General Store; his shop doubles every price, so skip that
                    var data = asset.AsDictionary<string, ShopData>().Data;
                    if (data.TryGetValue("SeedShop", out ShopData? shop))
                    {
                        shop.Items.Add(new ShopItemData
                        {
                            Id = this.MokaPotId,
                            ItemId = this.MokaPotQualifiedId,
                            Price = 300,
                            IgnoreShopPriceModifiers = true
                        });
                        shop.Items.Add(new ShopItemData
                        {
                            Id = $"{this.ModManifest.UniqueID}_CoffeeBean",
                            ItemId = CoffeeBeanId,
                            Price = 20,
                            IgnoreShopPriceModifiers = true
                        });
                    }
                });
            }
        }

        /// <summary>Make sure the player knows the recipe, including on saves created before the mod was installed.</summary>
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            if (!Game1.player.craftingRecipes.ContainsKey(this.MokaPotId))
                Game1.player.craftingRecipes.Add(this.MokaPotId, 0);
        }

        /// <summary>Tell the player when a pot in their inventory finishes brewing.</summary>
        private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
        {
            int now = GetAbsoluteMinutes();
            foreach (Item? item in Game1.player.Items)
            {
                if (item?.QualifiedItemId != this.MokaPotQualifiedId)
                    continue;

                if (this.TryGetReadyAt(item, out int readyAt) && readyAt <= now && !item.modData.ContainsKey(this.NotifiedKey))
                {
                    item.modData[this.NotifiedKey] = "true";
                    Game1.playSound("dwop");
                    Game1.addHUDMessage(new HUDMessage("Your moka pot is ready!", HUDMessage.newQuest_type));
                }
            }
        }

        /// <summary>Brew or collect coffee when the player presses the action button while holding a moka pot.</summary>
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsPlayerFree || !e.Button.IsActionButton())
                return;

            StardewValley.Object? pot = Game1.player.ActiveObject;
            if (pot?.QualifiedItemId != this.MokaPotQualifiedId)
                return;

            // stop the game from trying to place, eat, or gift the pot
            this.Helper.Input.Suppress(e.Button);

            if (!this.TryGetReadyAt(pot, out int readyAt))
                this.StartBrew(pot);
            else if (readyAt > GetAbsoluteMinutes())
                Game1.showRedMessage($"Still brewing... {readyAt - GetAbsoluteMinutes()} more minutes.");
            else
                this.CollectCoffee(pot);
        }

        /// <summary>Use coffee beans from the player's inventory to start a brew.</summary>
        /// <param name="pot">The moka pot the player is holding.</param>
        private void StartBrew(StardewValley.Object pot)
        {
            // pots in a stack share modData, so only brew in a single pot
            if (pot.Stack > 1)
            {
                Game1.showRedMessage("Hold just one moka pot to brew.");
                return;
            }

            int beans = this.Config.BeansPerBrew;
            if (!Game1.player.Items.ContainsId(CoffeeBeanId, beans))
            {
                Game1.showRedMessage($"You need {beans} coffee beans to brew.");
                return;
            }

            Game1.player.Items.ReduceId(CoffeeBeanId, beans);
            pot.modData[this.ReadyAtKey] = (GetAbsoluteMinutes() + this.Config.BrewMinutes).ToString();
            pot.modData.Remove(this.NotifiedKey);

            Game1.playSound("bubbles");
            Game1.addHUDMessage(new HUDMessage($"Brewing... ready in {this.Config.BrewMinutes} minutes.", HUDMessage.newQuest_type));
        }

        /// <summary>Give the player a coffee and empty the pot.</summary>
        /// <param name="pot">The moka pot the player is holding.</param>
        private void CollectCoffee(StardewValley.Object pot)
        {
            pot.modData.Remove(this.ReadyAtKey);
            pot.modData.Remove(this.NotifiedKey);

            Item coffee = ItemRegistry.Create(CoffeeQualifiedId);
            if (!Game1.player.addItemToInventoryBool(coffee))
                Game1.createItemDebris(coffee, Game1.player.getStandingPosition(), Game1.player.FacingDirection);

            Game1.playSound("coin");
        }

        /// <summary>Get the absolute game minute when the pot's current brew is ready.</summary>
        /// <param name="pot">The moka pot to check.</param>
        /// <param name="readyAt">The absolute game minute, if the pot is brewing.</param>
        /// <returns>Returns whether the pot has a brew in progress or ready to collect.</returns>
        private bool TryGetReadyAt(Item pot, out int readyAt)
        {
            readyAt = 0;
            return pot.modData.TryGetValue(this.ReadyAtKey, out string? raw) && int.TryParse(raw, out readyAt);
        }

        /// <summary>Get the number of in-game minutes since the start of the save, so brews carry across days.</summary>
        private static int GetAbsoluteMinutes()
        {
            return Game1.Date.TotalDays * 24 * 60 + Utility.ConvertTimeToMinutes(Game1.timeOfDay);
        }
    }
}
