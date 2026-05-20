using CommunityContracts.Core;
using CommunityContracts.Core.NPC;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using SObject = StardewValley.Object;

public class ModEntry : Mod
{
    public static ModEntry Instance { get; private set; }

    public static bool IsSelectingTile = false;

    internal static ModConfig Config;

    public static CollectionServiceManager Services;
    public static IMonitor ModMonitor { get; private set; }

    public static Vector2? HighlightedDropTile;

    public static Dictionary<string, int> npcCooldowns = new();

    public static bool ForceShowDropLocationHighlight = false;

    public static string T(string key, object tokens = null)
    {
        var translation = Instance.Helper.Translation.Get(key);
        if (tokens != null)
            translation = translation.Tokens(tokens);
        return translation.ToString();
    }
    public static Dictionary<string, object> NPCProfiles = new()
    {
        { "Abigail", new AbigailProfile() },
        { "Alex", new AlexProfile() },
        { "Caroline", new CarolineProfile() },
        { "Demetrius", new DemetriusProfile() },
        { "Elliott", new ElliottProfile() },
        { "Emily", new EmilyProfile() },
        { "Evelyn", new EvelynProfile() },
        { "George", new GeorgeProfile() },
        { "Haley", new HaleyProfile() },
        { "Jas", new JasProfile() },
        { "Jodi", new JodiProfile() },
        { "Leah", new LeahProfile() },
        { "Leo", new LeoProfile() },
        { "Linus", new LinusProfile() },
        { "Maru", new MaruProfile() },
        { "Pam", new PamProfile() },
        { "Penny", new PennyProfile() },
        { "Sam", new SamProfile() },
        { "Sandy", new SandyProfile() },
        { "Sebastian", new SebastianProfile() },
        { "Shane", new ShaneProfile() },
        { "Vincent", new VincentProfile() },
        { "Wizard", new WizardProfile() },
    };

    public static bool ShowPlacementOverlay = false;
    public bool SetPlacementMenuOpen = false;
    public class ContractDelivery
    {
        public List<Item> Items { get; set; }
        public string Source { get; set; }
        public long RecipientID { get; set; }
    }
    private readonly List<ContractDelivery> ContractsDeliveries = new();
    public void QueueContractsDelivery(ContractDelivery delivery)
    {
        if (delivery.Items.Count > 0)
        {
            ContractsDeliveries.Add(delivery);
            Monitor.Log(T("QueuedDelivery", new { source = delivery.Source }), LogLevel.Info);
        }
    }
    private void OnRenderedHud(object sender, RenderedHudEventArgs e)
    {

    }
    private void OnRenderingHud(object sender, RenderingHudEventArgs e)
    {
        if (Config.EnableProcessTimeReduction)
        {
            Vector2 cursorTile = Game1.currentCursorTile;

            if (Game1.currentLocation != null &&
                Game1.currentLocation.Objects.TryGetValue(cursorTile, out SObject obj) &&
                obj is not null &&
                obj.bigCraftable.Value &&
                obj.minutesUntilReady.Value > 0)
            {
                int minutesLeft = obj.minutesUntilReady.Value;

                int hours = minutesLeft / 60;
                int minutes = minutesLeft % 60;

                string tooltip = T("DisplayTime", new { DisName = obj.DisplayName, Hrs = hours, Min = minutes });

                IClickableMenu.drawHoverText(
                    e.SpriteBatch,
                    tooltip,
                    Game1.smallFont
                );
            }
        }
    }
    public override void Entry(IModHelper helper)
    {
        Instance = this;

        ModMonitor = this.Monitor;

        Config = helper.ReadConfig<ModConfig>();

        Services = new CollectionServiceManager();

        Helper.Events.Display.RenderingHud += OnRenderingHud;

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;

        helper.Events.GameLoop.DayStarted += OnDayStarted;

        helper.Events.Input.ButtonPressed += OnButtonPressed;

        helper.Events.Display.MenuChanged += OnMenuChanged;

        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        helper.Events.Display.RenderedHud += OnRenderedHud;

        helper.Events.GameLoop.TimeChanged += OnTimeChanged;

        Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;

        Helper.Events.Display.RenderedWorld += OnRenderedWorld;

        helper.Events.GameLoop.SaveLoaded += (_, _) =>
        {
            foreach (var location in Game1.locations)
            {
                foreach (var pair in location.objects.Pairs)
                {
                    if (pair.Value is Chest chest &&
                        chest.modData.TryGetValue("CommunityContracts/DeliveryColor", out var savedColor) &&
                        Config.ChestColors.TryGetValue(savedColor, out var tint))
                    {
                        chest.playerChoiceColor.Value = tint;
                    }
                }
            }
        };

        foreach (var m in typeof(Crop).GetMethods())
        {
            if (m.Name == "harvest")
                Monitor.Log($"HARVEST METHOD: {m}", LogLevel.Warn);
        }

        var QualityHarmony = new Harmony(this.ModManifest.UniqueID);

        Helper.Events.GameLoop.Saving += OnSaving;

        var harmony = new Harmony(ModManifest.UniqueID);

        Helper.Events.GameLoop.DayEnding += (s, e) =>
        {
            var convertedDeliveries = ContractsDeliveries
                .Select(w => new ContractsDelivery
                {
                    Items = w.Items,
                    RecipientID = w.RecipientID
                })
                .ToList();

            DeliverContractsItems(convertedDeliveries, Config);
            ContractsDeliveries.Clear();
        };
    }
    private void OnGameLaunched(object sender, EventArgs e)
    {

    }
    private void OnDayStarted(object sender, DayStartedEventArgs e)
    {
        ApplyProductionTimeReduction(Config, Monitor);
    }
    private void OnSaving(object sender, SavingEventArgs e)
    {

    }
    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {

    }
    private void OnMenuChanged(object sender, MenuChangedEventArgs e)
    {

    }
    private void OnTimeChanged(object sender, TimeChangedEventArgs e)
    {
        foreach (var key in npcCooldowns.Keys.ToList())
        {
            if (npcCooldowns[key] > 0)
                npcCooldowns[key] -= 10;

            if (npcCooldowns[key] <= 0)
                npcCooldowns.Remove(key);
        }
    }
    private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {
        if (Instance.SetPlacementMenuOpen)
        {
            if (Game1.activeClickableMenu is not SetPlacement)
            {
                ShowPlacementOverlay = false;
                Instance.SetPlacementMenuOpen = false;
            }
        }
    }
    private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
    {
        if (ForceShowDropLocationHighlight)
        {
            DrawDeliveryLocationHighlight(
                e.SpriteBatch,
                Config.DropLocationName,   // map name
                Config,
                s => T(s)
            );
        }

        if (ShowPlacementOverlay)
                DrawSquarePlacementOverlay(e.SpriteBatch);
    }
    private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (!Context.IsPlayerFree)
            return;

        if (Config.CheatMenuHotkey != SButton.None && e.Button == Config.CheatMenuHotkey)
        {
            Game1.activeClickableMenu = new CCToolbar(Helper, Monitor, Config);
        }

        if (e.Button == SButton.MouseLeft)
        { 
            Vector2 cursorPos = new Vector2(Game1.getMouseX(), Game1.getMouseY());
        } 

        if (!Context.IsWorldReady || !IsSelectingTile || e.Button != SButton.MouseLeft)
            return;

        var cursorTile = e.Cursor.Tile;
        var locationName = Game1.currentLocation.Name;

        Config.DropLocationName = locationName;
        Config.DropTileX = (int)cursorTile.X;
        Config.DropTileY = (int)cursorTile.Y;
        Helper.WriteConfig(Config);

        Game1.addHUDMessage(new HUDMessage(T("DeliveryLocationSet", new { location = locationName, x = cursorTile.X, y = cursorTile.Y }), HUDMessage.newQuest_type));
        IsSelectingTile = false;
    }
}