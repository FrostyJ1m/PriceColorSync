using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using UnityEngine;
using SwiftXP.SPT.ShowMeTheMoney.Client.Configuration;
using SwiftXP.SPT.ShowMeTheMoney.Client.Contexts.Holders;
using SwiftXP.SPT.ShowMeTheMoney.Client.Data;
using SwiftXP.SPT.ShowMeTheMoney.Client.Data.Holders;
using SwiftXP.SPT.ShowMeTheMoney.Client.Patches;
using SwiftXP.SPT.ShowMeTheMoney.Client.Services;
using SwiftXP.SPT.ShowMeTheMoney.Client.Utilities;
using SwiftXP.SPT.Common.Sessions;

namespace SptDiagnostics.PriceColorSync
{
    [BepInPlugin("local.sptdiagnostics.pricecolorsync", "GPTSol6-1-PriceColorSync", "1.1.0")]
    [BepInDependency("com.dark.spt.showmethemoney", "3.0.2")]
    [BepInIncompatibility("com.fika.headless")]
    public sealed class PriceColorSyncPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private static PriceColorSyncPlugin instance;
        private readonly MenuRefreshPolicy menuRefresh = new MenuRefreshPolicy();
        private static readonly FieldInfo PriceUpdater = AccessTools.Field(typeof(SwiftXP.SPT.ShowMeTheMoney.Client.Plugin), "_fleaPriceUpdaterService");
        private bool awaitingMarketRefresh;
        private object requestedMarketSnapshot;
        private float nextRefreshAttempt;
        private Harmony harmony;
        private object lastMarketSnapshot;
        private Action<Item> processBackground;
        private Func<bool> withinBackgroundBudget;
        private readonly System.Diagnostics.Stopwatch backgroundTimer = new System.Diagnostics.Stopwatch();
        private int maximumBackgroundItems;
        private float backgroundBudgetMs;
        private static float nextWarning;
        private static bool fleaForFir;
        private static bool fleaForNonFir;
        private static bool traderForBanned;
        [ThreadStatic] private static int pricingDepth;
        private static volatile bool captureItems;
        private static int valuationEpoch;
        private static long preparedCount;
        private static long coldViewCount;
        private static readonly Dictionary<Item, HashSet<ItemView>> ItemViews = new Dictionary<Item, HashSet<ItemView>>();
        private static readonly ConditionalWeakTable<ItemView, ViewState> ViewStates = new ConditionalWeakTable<ItemView, ViewState>();
        private static readonly ItemPreparationQueue<Item> PendingBackgrounds = new ItemPreparationQueue<Item>();
        private static readonly ConditionalWeakTable<Item, Quote> Quotes = new ConditionalWeakTable<Item, Quote>();
        private static readonly FieldInfo Background = AccessTools.Field(typeof(ItemView), "BackgroundColor");
        private static readonly FieldInfo OriginalBackground = AccessTools.Field(typeof(ItemView), "OriginalBackgroundColor");
        private static readonly FieldInfo AnimatorBackground = AccessTools.Field(typeof(EFT.UI.ItemViewAnimation), "_defaultBackgroundColor");
        private static readonly MethodInfo ShowPrice = AccessTools.Method(typeof(SimpleTooltipShowPatch), "ShowPrice");
        private static readonly MethodInfo WeaponPrice = AccessTools.Method(typeof(FleaPriceUtility), "GetWeaponPrice");
        private static readonly MethodInfo SetFleaPrice = AccessTools.Method(typeof(FleaPriceUtility), "SetFleaPriceOfTradeItem");
        private static readonly string[] DefaultBackgrounds = { "#404040", "#a3a3a3", "#0c3b08", "#08083b", "#590b5e", "#5e470b" };
        private static readonly Color[] Backgrounds = new Color[6];
        private static Color fleaBannedBackground;
        private static readonly double[] PriceBoundaries = new double[5];
        private static readonly double[] PenBoundaries = new double[5];
        private static readonly HashSet<string> LoggedCaseTemplates = new HashSet<string>();

        private void Awake()
        {
            Log = Logger;
            instance = this;
            try
            {
                if (Background == null || OriginalBackground == null || AnimatorBackground == null || ShowPrice == null || WeaponPrice == null || SetFleaPrice == null)
                    throw new MissingMemberException("Installed UI/SMTM methods differ from inspected versions");
                fleaForFir = Config.Bind("Pricing", "Use flea estimates for FIR items", true,
                    "Compare flea estimates with best trader sell prices for found-in-raid items and use the higher value. False uses best trader prices only. Restart the client after changing.").Value;
                fleaForNonFir = Config.Bind("Pricing", "Use flea estimates for non-FIR items", false,
                    "Use flea estimates even for non-FIR items. False uses best trader prices. Restart the client after changing.").Value;
                traderForBanned = Config.Bind("Pricing", "Use trader prices for flea-banned items", true,
                    "Override both FIR rules for items that cannot be sold on the flea market. Cases check only the case itself. False permits cached market estimates. Restart the client after changing.").Value;
                maximumBackgroundItems = Config.Bind("Performance", "Maximum items per frame", 12,
                    new ConfigDescription("Maximum item quotes prepared ahead of display per frame. Cold first-display quotes run synchronously outside this limit. Restart the client after changing.",
                        new AcceptableValueRange<int>(1, 64))).Value;
                backgroundBudgetMs = Config.Bind("Performance", "Background work budget milliseconds", 2f,
                    new ConfigDescription("Time budget for advance preparation and recoloring per frame, checked between items. One item may exceed it. Cold first-display quotes are outside this budget. Restart the client after changing.",
                        new AcceptableValueRange<float>(0.25f, 10f))).Value;
                for (int i = 0; i < 6; i++)
                {
                    string value = Config.Bind("Background palette", "Tier " + i, DefaultBackgrounds[i], "Soft item background RGB. Tiers ascend from poor to legendary.").Value;
                    if (!ColorUtility.TryParseHtmlString(value, out Backgrounds[i])) throw new FormatException("Invalid background color " + value);
                }
                string bannedColor = Config.Bind("Background palette", "Flea banned", "#660415",
                    "Background RGB for flea-banned item templates, independent of price/penetration tiers. Tooltip colors keep Show Me The Money's rules. Restart the client after changing.").Value;
                if (!ColorUtility.TryParseHtmlString(bannedColor, out fleaBannedBackground)) throw new FormatException("Invalid flea-banned background color " + bannedColor);
                harmony = new Harmony("local.sptdiagnostics.pricecolorsync");
                processBackground = ProcessBackground;
                withinBackgroundBudget = delegate { return backgroundTimer.Elapsed.TotalMilliseconds < backgroundBudgetMs; };
                harmony.PatchAll(typeof(PriceColorSyncPlugin).Assembly);
                captureItems = true;
                Logger.LogInfo("Pricing: FIR flea=" + fleaForFir + "; non-FIR flea=" + fleaForNonFir
                    + "; flea-banned trader=" + traderForBanned + ". FIR flea mode selects the higher flea/trader sell value. Shared price and tier for tooltip/background.");
                Logger.LogInfo("Item preparation: maximum " + maximumBackgroundItems + " items/frame; " + backgroundBudgetMs + " ms budget. Exact cold quotes are calculated before first display.");
                if (PriceUpdater == null) Logger.LogWarning("Automatic price refresh unavailable: SMTM updater field not found.");
                else Logger.LogInfo("Automatic SMTM refresh: first main menu and main menu after each raid; updater checks every 30 seconds.");
            }
            catch (Exception e)
            {
                captureItems = false;
                if (harmony != null) harmony.UnpatchSelf();
                enabled = false;
                Logger.LogError("Price Color Sync disabled: " + e);
            }
        }

        private void Update()
        {
            UpdateMenuRefresh();
            object snapshot = FleaPriceDataHolder.Current;
            if (awaitingMarketRefresh && snapshot != null && !ReferenceEquals(snapshot, requestedMarketSnapshot))
            {
                awaitingMarketRefresh = false;
                Logger.LogInfo("Automatic SMTM refresh: replacement price snapshot received.");
            }
            if (!ReferenceEquals(snapshot, lastMarketSnapshot))
            {
                lastMarketSnapshot = snapshot;
                valuationEpoch++;
                PendingBackgrounds.ScheduleAll();
            }
            // Prepare real item instances even when no inventory UI exists. Factory hooks
            // only enqueue; all pricing stays on the main thread after session readiness.
            if (!PricingReady) return;
            if (PendingBackgrounds.Count == 0) return;
            backgroundTimer.Restart();
            try { PendingBackgrounds.Process(maximumBackgroundItems, processBackground, withinBackgroundBudget); }
            finally { backgroundTimer.Stop(); }
        }

        private void OnDestroy()
        {
            captureItems = false;
            instance = null;
            if (harmony != null) harmony.UnpatchSelf();
            ItemViews.Clear();
            PendingBackgrounds.Clear();
            if (Log != null) Log.LogInfo("Preparation totals: " + preparedCount + " background quotes; " + coldViewCount + " cold/stale first-display quotes.");
        }

        private void UpdateMenuRefresh()
        {
            bool inRaid = SwiftXP.SPT.Common.EFT.EFTHelper.IsInRaid;
            menuRefresh.ObserveRaid(inRaid);
            if (PriceUpdater == null || Time.unscaledTime < nextRefreshAttempt) return;
            string reason = menuRefresh.PendingReason(inRaid, SptSession.Session != null && SptSession.Session.Profile != null);
            if (reason == null) return;
            nextRefreshAttempt = Time.unscaledTime + 5f;
            try
            {
                BepInEx.PluginInfo info;
                if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("com.dark.spt.showmethemoney", out info) || info.Instance == null) return;
                var updater = PriceUpdater.GetValue(info.Instance) as FleaPriceUpdaterService;
                if (updater == null) return;
                requestedMarketSnapshot = FleaPriceDataHolder.Current;
                updater.ForceUpdate();
                valuationEpoch++;
                PendingBackgrounds.ScheduleAll();
                awaitingMarketRefresh = true;
                menuRefresh.Requested();
                Logger.LogInfo("Automatic SMTM refresh requested: " + reason + ". Waiting for SMTM updater (up to about 30 seconds plus request time).");
            }
            catch (Exception e)
            {
                Logger.LogWarning("Automatic SMTM refresh request failed; retrying in 5 seconds: " + e);
            }
        }

        [HarmonyPatch]
        private static class MainMenuShownPatch
        {
            private static MethodBase TargetMethod()
            {
                return typeof(EFT.UI.MenuScreen).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Single(m => m.Name == "Show" && m.GetParameters().Length == 3);
            }

            private static void Postfix()
            {
                if (instance != null && instance.enabled) instance.menuRefresh.MenuShown();
            }
        }

        internal static void Warn(Exception e)
        {
            if (Time.unscaledTime < nextWarning) return;
            nextWarning = Time.unscaledTime + 30f;
            if (Log != null) Log.LogWarning("Price Color Sync could not calculate value: " + e);
        }

        private sealed class Quote
        {
            public Quote() { }
            public TradeItem Trade;
            public TradePrice Price;
            public double? Compare;
            public float Expires;
            public bool Fir;
            public bool UsesFlea;
            public bool DependsOnFlea;
            public bool FleaEligible;
            public object MarketSnapshot;
            public bool Dirty;
            public int Generation;
            public int Epoch;
            public long Revision;
        }

        private sealed class ViewState
        {
            public ViewState() { }
            public Item Item;
            public int PaintedGeneration = -1;
        }

        private static bool IsAmmo(Item item) { return item is Ammo || item is AmmoBox; }
        private static bool IsStorageCase(Item item) { return item is SimpleContainer; }

        private static Item CreateCasePricingShell(Item original)
        {
            // GetDiff contains this item's own components/state, not its contained grids.
            // CreateItem constructs a detached empty instance; CloneItem would recursively
            // copy the inventory tree first. Never clear or modify the live case.
            var factory = Singleton<EFT.ItemFactory>.Instance;
            Item shell = factory.CreateItem(EFT.MongoID.Generate(true), original.TemplateId, factory.GetDiff(original));
            shell.StackObjectsCount = original.StackObjectsCount;
            return shell;
        }

        private static ViewState RegisterView(ItemView view)
        {
            ViewState state = ViewStates.GetOrCreateValue(view);
            if (ReferenceEquals(state.Item, view.Item)) return state;
            UnregisterView(view);
            state.Item = view.Item;
            state.PaintedGeneration = -1;
            if (state.Item != null)
            {
                HashSet<ItemView> views;
                if (!ItemViews.TryGetValue(state.Item, out views)) ItemViews[state.Item] = views = new HashSet<ItemView>();
                views.Add(view);
            }
            return state;
        }

        private static void UnregisterView(ItemView view)
        {
            ViewState state;
            HashSet<ItemView> views;
            if (!ViewStates.TryGetValue(view, out state) || state.Item == null) return;
            if (ItemViews.TryGetValue(state.Item, out views))
            {
                views.Remove(view);
                if (views.Count == 0) ItemViews.Remove(state.Item);
            }
            state.Item = null;
            state.PaintedGeneration = -1;
        }

        private static bool NeedsQuote(Item item, Quote quote, bool interactive)
        {
            return quote.Trade == null || quote.Dirty || quote.Fir != item.MarkedAsSpawnedInSession
                || quote.Epoch != valuationEpoch || quote.Revision != PendingBackgrounds.Revision(item)
                || (quote.DependsOnFlea && !ReferenceEquals(quote.MarketSnapshot, FleaPriceDataHolder.Current))
                || (interactive && quote.Expires <= Time.unscaledTime);
        }

        private static void ProcessBackground(Item item)
        {
            // Skip temporary trader stock and detached clones. Real instances become
            // owned after deserialization; a view can still quote an unowned item later.
            if (item is Stash || item is InventoryEquipment || item is LootContainer
                || item is MobContainer || item is Pockets || item is HideoutAreaContainer
                || item is StationaryContainer) return;
            if (item.Owner == null || item.Owner.OwnerType == EOwnerType.Trader) return;
            if (IsAmmo(item) && Settings.UseCaliberPenetrationPower.Value) return;
            Quote previous = Quotes.GetOrCreateValue(item);
            bool needed = NeedsQuote(item, previous, false);
            Quote quote = GetQuote(item, false);
            if (needed) preparedCount++;
            RefreshItemViews(item, quote);
        }

        private static bool PricingReady
        {
            get { return Settings != null && Settings.EnablePlugin.Value && SptSession.Session != null
                && SptSession.Session.Profile != null && SptSession.Session.Traders != null
                && SptSession.Session.Traders.Any(); }
        }

        private static void ItemChanged(Item item)
        {
            if (pricingDepth != 0 || !captureItems || item == null) return;
            PendingBackgrounds.Schedule(item, true);
            // Refresh weapon/armor parents when parts move or change condition. Storage
            // cases intentionally retain their shell-only quote when contents change.
            // onlyMerged=true stops at inventory grids, avoiding pricing the entire
            // stash/backpack when one contained item changes.
            try
            {
                foreach (Item parent in item.GetAllParentItems(true))
                    if (!IsStorageCase(parent)) PendingBackgrounds.Schedule(parent, true);
            }
            catch (Exception e)
            {
                // A malformed/transitional address must not interrupt the game's event.
                // Hooks may run off-thread, so avoid Unity Time/Component APIs here.
                if (Log != null) Log.LogWarning("Item preparation could not invalidate parent quotes: " + e.GetType().Name);
            }
        }

        private static void PrepareFirstDisplay(ItemView view)
        {
            if (view == null || view.Item == null || !view.Examined || !PricingReady) return;
            if (IsAmmo(view.Item) && Settings.UseCaliberPenetrationPower.Value) return;
            Quote quote = Quotes.GetOrCreateValue(view.Item);
            if (!NeedsQuote(view.Item, quote, false)) return;
            // Rare cold/changed items must be priced before UpdateStaticInfo calls
            // UpdateColor. Never substitute a template average for the actual item.
            coldViewCount++;
            GetQuote(view.Item, false);
        }

        private static void RefreshItemViews(Item item, Quote quote)
        {
            HashSet<ItemView> views;
            if (!ItemViews.TryGetValue(item, out views)) return;
            foreach (ItemView view in views)
            {
                if (view == null || !view.isActiveAndEnabled) continue;
                ViewState state = ViewStates.GetOrCreateValue(view);
                if (state.PaintedGeneration != quote.Generation) view.UpdateColor();
            }
        }

        private static PluginConfiguration Settings
        {
            get { return PluginContextHolder.Current == null ? null : PluginContextHolder.Current.Configuration; }
        }

        private static Quote GetQuote(Item item, bool interactive)
        {
            Quote result = Quotes.GetOrCreateValue(item);
            object snapshot = FleaPriceDataHolder.Current;
            if (!NeedsQuote(item, result, interactive)) return result;
            result.Trade = null;
            result.Price = null;
            result.Compare = null;
            result.Fir = item.MarkedAsSpawnedInSession;
            result.MarketSnapshot = snapshot;
            result.Expires = Time.unscaledTime + 1f;
            result.Dirty = false;
            result.Generation++;
            result.Epoch = valuationEpoch;
            result.Revision = PendingBackgrounds.Revision(item);
            if (Settings == null) return result;
            pricingDepth++;
            try
            {
                Item pricingItem = IsStorageCase(item) ? CreateCasePricingShell(item) : item;
                result.FleaEligible = pricingItem.CanSellOnRagfair;
                result.UsesFlea = TierPolicy.PriceSource(result.Fir, result.FleaEligible,
                    fleaForFir, fleaForNonFir, traderForBanned) == "Flea";
                // A trader-winning FIR quote must still be reconsidered after market refresh.
                result.DependsOnFlea = result.UsesFlea;
                result.Trade = new TradeItem(pricingItem, null, null);
                if (result.UsesFlea)
                {
                    bool includedParts;
                    FleaPriceUtility.GetFleaPrice(result.Trade, Settings.IncludeFleaTax, out includedParts);
                    // Optional legacy estimate mode affects presentation only.
                    if (result.Trade.FleaPrice == null && !result.FleaEligible)
                        EstimateBannedFleaItem(result.Trade);
                    result.Price = result.Trade.FleaPrice;
                    if (result.Fir)
                    {
                        TraderPriceService.Instance.GetBestTraderPrice(result.Trade);
                        if (TierPolicy.PreferTrader(ComparePrice(result.Price), ComparePrice(result.Trade.TraderPrice)))
                        {
                            result.Price = result.Trade.TraderPrice;
                            result.UsesFlea = false;
                        }
                    }
                }
                else
                {
                    TraderPriceService.Instance.GetBestTraderPrice(result.Trade);
                    result.Price = result.Trade.TraderPrice;
                }
                result.Compare = ComparePrice(result.Price);
                if (IsStorageCase(item) && LoggedCaseTemplates.Add(item.TemplateId))
                    Log.LogInfo("Case valuation: template=" + item.TemplateId + "; source=" + (result.UsesFlea ? "Flea" : "Trader")
                        + "; shell-only price/slot=" + (result.Compare.HasValue ? result.Compare.Value.ToString() : "unavailable"));
            }
            catch (Exception e) { result.Dirty = true; Warn(e); }
            finally { pricingDepth--; }
            return result;
        }

        private static double? ComparePrice(TradePrice price)
        {
            double? value = price == null ? (double?)null : price.GetComparePriceInRouble();
            return TierPolicy.IsValidPrice(value) ? value : null;
        }

        private static void EstimateBannedFleaItem(TradeItem trade)
        {
            var market = FleaPriceDataHolder.Current;
            double price;
            if (market == null || !market.Prices.TryGetValue(trade.Item.TemplateId, out price)) return;
            Weapon weapon = trade.Item as Weapon;
            if (weapon != null) price = (double)WeaponPrice.Invoke(null, new object[] { weapon, price });
            else
            {
                var partial = PartialRagfairConfigHolder.Current;
                double multiplier;
                if (partial != null && partial.ItemPriceMultiplier != null && partial.ItemPriceMultiplier.TryGetValue(trade.Item.TemplateId, out multiplier)) price *= multiplier;
            }
            double quality = ItemQualityUtility.GetItemQualityModifier(trade.Item);
            price = SellChangeUtility.GetPriceForDesiredSellChange(price * quality, quality, -1);
            if (price > 0) SetFleaPrice.Invoke(null, new object[] { trade, price, Settings.IncludeFleaTax });
        }

        private static int GetTier(Item item, Quote quote)
        {
            PluginConfiguration config = Settings;
            if (config == null) return -1;
            Ammo ammo = item as Ammo;
            AmmoBox box = item as AmmoBox;
            if (box != null) ammo = box.Cartridges.Items.OfType<Ammo>().FirstOrDefault();
            if (ammo != null && config.UseCaliberPenetrationPower.Value)
            {
                PenBoundaries[0] = (double)config.PoorPenetrationValue.Value; PenBoundaries[1] = (double)config.CommonPenetrationValue.Value;
                PenBoundaries[2] = (double)config.UncommonPenetrationValue.Value; PenBoundaries[3] = (double)config.RarePenetrationValue.Value; PenBoundaries[4] = (double)config.EpicPenetrationValue.Value;
                return TierPolicy.Tier(ammo.PenetrationPower, PenBoundaries);
            }
            if ((box != null && ammo == null && config.UseCaliberPenetrationPower.Value) || quote == null || !quote.Compare.HasValue) return -1;
            PriceBoundaries[0] = (double)config.PoorValue.Value; PriceBoundaries[1] = (double)config.CommonValue.Value;
            PriceBoundaries[2] = (double)config.UncommonValue.Value; PriceBoundaries[3] = (double)config.RareValue.Value; PriceBoundaries[4] = (double)config.EpicValue.Value;
            return TierPolicy.Tier(quote.Compare.Value, PriceBoundaries);
        }

        internal static void ApplyBackground(ItemView view)
        {
            if (view == null || view.Item == null || !view.Examined || Settings == null || !Settings.EnablePlugin.Value) return;
            try
            {
                ViewState state = RegisterView(view);
                Quote quote = Quotes.GetOrCreateValue(view.Item);
                // UI repaint events never perform price calculations. Ammo backgrounds
                // need only penetration; other missing/stale quotes go to the limited queue.
                bool pending = (!IsAmmo(view.Item) || !Settings.UseCaliberPenetrationPower.Value) && NeedsQuote(view.Item, quote, false);
                if (pending) PendingBackgrounds.Schedule(view.Item, false);
                int tier = GetTier(view.Item, quote);
                // Item Valuation's ban marker is template-based. This also works before
                // quotes settle without calculating prices or traversing contained items
                // on a repaint. Ammo/AmmoBox retain penetration or price tiers even if banned.
                // Tooltip tiers stay independent.
                Color color = !IsAmmo(view.Item) && !view.Item.Template.CanSellOnRagfair ? fleaBannedBackground : Backgrounds[tier < 0 ? 0 : tier];
                color.a = ((Color)Background.GetValue(view)).a;
                if (!((Color)Background.GetValue(view)).Equals(color)) Background.SetValue(view, color);
                if (!((Color)OriginalBackground.GetValue(view)).Equals(color)) OriginalBackground.SetValue(view, color);
                if (view.Animator != null && !((Color)AnimatorBackground.GetValue(view.Animator)).Equals(color)) AnimatorBackground.SetValue(view.Animator, color);
                if (!pending) state.PaintedGeneration = quote.Generation;
            }
            catch (Exception e) { Warn(e); }
        }

        [HarmonyPatch(typeof(ItemView), "UpdateStaticInfo")]
        private static class ViewInitPatch
        {
            private static void Prefix(ItemView __instance)
            {
                try { PrepareFirstDisplay(__instance); }
                catch (Exception e) { Warn(e); }
            }

            private static void Postfix(ItemView __instance)
            {
                RegisterView(__instance);
                __instance.UpdateColor();
            }
        }

        [HarmonyPatch(typeof(EFT.ItemFactory), "CreateItem")]
        private static class ItemCreatedPatch
        {
            private static void Postfix(Item __result)
            {
                if (pricingDepth == 0 && captureItems)
                    PendingBackgrounds.Schedule(__result, true);
            }
        }

        [HarmonyPatch(typeof(Item), "RaiseRefreshEvent")]
        private static class ItemRefreshedPatch
        {
            private static void Prefix(Item __instance) { ItemChanged(__instance); }
        }

        [HarmonyPatch(typeof(Item), "set_CurrentAddress")]
        private static class ItemMovedPatch
        {
            private static void Prefix(Item __instance) { ItemChanged(__instance); }
            private static void Postfix(Item __instance) { ItemChanged(__instance); }
        }

        [HarmonyPatch(typeof(Item), "set_SpawnedInSession")]
        private static class ItemFirPatch
        {
            private static void Postfix(Item __instance) { ItemChanged(__instance); }
        }

        [HarmonyPatch(typeof(EFT.GameWorld), "OnGameStarted")]
        private static class RaidPreparedPatch
        {
            private static void Postfix()
            {
                PendingBackgrounds.ScheduleAll();
                if (Log != null) Log.LogInfo("Raid item preparation scheduled: " + PendingBackgrounds.Count + " known instances.");
            }
        }

        [HarmonyPatch(typeof(ItemView), "UpdateColor")]
        private static class ViewColorPatch
        {
            private static void Prefix(ItemView __instance) { ApplyBackground(__instance); }
        }

        [HarmonyPatch(typeof(ItemView), "Kill")]
        private static class ViewKillPatch
        {
            private static void Postfix(ItemView __instance) { UnregisterView(__instance); }
        }

        [HarmonyPatch(typeof(SimpleTooltipShowPatch), "ItemMeetsRequirements")]
        private static class CaseTooltipRequirementsPatch
        {
            private static bool Prefix(Item __0, ref bool __result)
            {
                if (!IsStorageCase(__0)) return true;
                // SMTM normally rejects filled containers. We quote only the empty case
                // shell, so keep examination/ownership checks but skip the contents scan.
                try
                {
                    var session = SptSession.Session;
                    var owner = __0.Owner;
                    __result = session != null && session.Profile != null && session.Profile.Examined(__0)
                        && owner != null && owner.OwnerType != EOwnerType.Trader;
                }
                catch (Exception e) { Warn(e); __result = false; }
                return false;
            }
        }

        [HarmonyPatch(typeof(SimpleTooltipShowPatch), "ShowTradeAndFleaPriceInformation")]
        private static class TooltipPricePatch
        {
            private static bool Prefix(Item __0, ref StringBuilder __1, ref double? __2)
            {
                __2 = null;
                try
                {
                    Quote quote = GetQuote(__0, true);
                    __2 = quote.Compare;
                    if (quote.Price == null || !quote.Compare.HasValue)
                    __1.Append("<br>").Append(quote.UsesFlea ? "Flea estimate unavailable" : "Trader sell price unavailable");
                    else
                    {
                        ShowPrice.Invoke(null, new object[] { quote.Trade, quote.Price, null, __1, quote.UsesFlea && __0 is Weapon });
                        if (!quote.FleaEligible) __1.Append(quote.UsesFlea ? " (estimate; flea banned)" : " (flea banned; trader price)");
                        if (IsStorageCase(__0)) __1.Append(" (case only)");
                    }
                    RefreshItemViews(__0, quote);
                }
                catch (Exception e) { Warn(e); __1.Append("<br>Value unavailable"); }
                return false;
            }
        }
    }
}
