using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;
using StardewValley.Menus;

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

        /// <summary>The unqualified item ID for a moka pot with its lid open and coffee ready.</summary>
        private string ReadyPotId = null!;

        /// <summary>The qualified item ID for a moka pot with its lid open and coffee ready.</summary>
        private string ReadyPotQualifiedId = null!;

        /// <summary>The asset name for the moka pot sprite.</summary>
        private string TextureAssetName = null!;

        /// <summary>The <c>modData</c> key holding the absolute game minute when the current brew is ready.</summary>
        private string ReadyAtKey = null!;

        /// <summary>The game tick when the player last pressed the action button while holding coffee, or -1.</summary>
        private int CoffeeActionTick = -1;


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
            this.ReadyPotId = $"{modId}_MokaPotReady";
            this.ReadyPotQualifiedId = $"(O){this.ReadyPotId}";
            this.TextureAssetName = $"Mods/{modId}/MokaPot";
            this.ReadyAtKey = $"{modId}/ReadyAt";

            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
            helper.Events.Player.InventoryChanged += this.OnInventoryChanged;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Display.MenuChanged += this.OnMenuChanged;
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

                    // same pot with the lid open, swapped in while coffee is ready
                    data[this.ReadyPotId] = new ObjectData
                    {
                        Name = this.ReadyPotId,
                        DisplayName = "Moka Pot",
                        Description = "The coffee's ready! Use the pot to pour yourself a cup.",
                        Type = "Basic",
                        Category = 0,
                        Price = 150,
                        Texture = this.TextureAssetName,
                        SpriteIndex = 1,
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

        /// <summary>Open the lid on pots that finished brewing overnight.</summary>
        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            this.OpenReadyPots();
        }

        /// <summary>Open the lid on pots that just finished brewing.</summary>
        private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
        {
            this.OpenReadyPots();
        }

        /// <summary>Open the lid on ready pots taken out of a chest or picked up.</summary>
        private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
        {
            if (e.IsLocalPlayer)
                this.OpenReadyPots();
        }

        /// <summary>Swap each finished pot in the player's inventory for the open-lid version, and tell the player.</summary>
        private void OpenReadyPots()
        {
            if (!Context.IsWorldReady)
                return;

            int now = GetAbsoluteMinutes();
            for (int i = 0; i < Game1.player.Items.Count; i++)
            {
                Item? item = Game1.player.Items[i];
                if (item?.QualifiedItemId != this.MokaPotQualifiedId)
                    continue;

                if (this.TryGetReadyAt(item, out int readyAt) && readyAt <= now)
                {
                    Game1.player.Items[i] = CreatePotFrom(item, this.ReadyPotQualifiedId);
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

            // let the game handle coffee as usual (gifts, machines, eating), but skip its "Drink Coffee?" prompt in OnMenuChanged
            if (Game1.player.ActiveObject?.QualifiedItemId == CoffeeQualifiedId)
            {
                this.CoffeeActionTick = Game1.ticks;
                return;
            }

            StardewValley.Object? pot = Game1.player.ActiveObject;
            if (pot is null || !this.IsMokaPot(pot))
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

        /// <summary>Answer "yes" to the game's "Drink Coffee?" prompt as soon as it opens.</summary>
        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            // only the prompt opened by the coffee press we just saw, not some later question
            bool pressedCoffee = this.CoffeeActionTick >= 0 && Game1.ticks - this.CoffeeActionTick <= 10;
            this.CoffeeActionTick = -1;

            if (!this.Config.SkipCoffeePrompt || !pressedCoffee)
                return;

            if (e.NewMenu is not DialogueBox { isQuestion: true } box
                || Game1.currentLocation?.lastQuestionKey != "Eat"
                || Game1.player.ActiveObject?.QualifiedItemId != CoffeeQualifiedId)
                return;

            // same as picking "Yes": the game handles "Eat_Yes" by eating the held object
            Game1.currentLocation.answerDialogue(new Response("Yes", ""));
            box.closeDialogue();
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

            Game1.playSound("bubbles");
            Game1.addHUDMessage(new HUDMessage($"Brewing... ready in {this.Config.BrewMinutes} minutes.", HUDMessage.newQuest_type));
        }

        /// <summary>Give the player a coffee, empty the pot, and close its lid.</summary>
        /// <param name="pot">The moka pot the player is holding.</param>
        private void CollectCoffee(StardewValley.Object pot)
        {
            pot.modData.Remove(this.ReadyAtKey);
            Game1.player.Items[Game1.player.CurrentToolIndex] = CreatePotFrom(pot, this.MokaPotQualifiedId);

            Item coffee = ItemRegistry.Create(CoffeeQualifiedId);
            if (!Game1.player.addItemToInventoryBool(coffee))
                Game1.createItemDebris(coffee, Game1.player.getStandingPosition(), Game1.player.FacingDirection);

            Game1.playSound("coin");
        }

        /// <summary>Get whether an item is a moka pot, with its lid open or closed.</summary>
        /// <param name="item">The item to check.</param>
        private bool IsMokaPot(Item item)
        {
            return item.QualifiedItemId == this.MokaPotQualifiedId || item.QualifiedItemId == this.ReadyPotQualifiedId;
        }

        /// <summary>Create a pot with a different item ID (lid open or closed), keeping the original's stack and brew state.</summary>
        /// <param name="original">The pot to replace.</param>
        /// <param name="qualifiedId">The qualified item ID for the new pot.</param>
        private static Item CreatePotFrom(Item original, string qualifiedId)
        {
            Item pot = ItemRegistry.Create(qualifiedId, original.Stack);
            foreach (var pair in original.modData.Pairs)
                pot.modData[pair.Key] = pair.Value;
            return pot;
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
