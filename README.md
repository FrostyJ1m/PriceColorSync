# Price Color Sync 1.1.4

Price Color Sync is a player-client plugin for **SPT 4.1.6 + Show Me The Money 3.0.2**. It makes inventory background tiers and the main SMTM tooltip use the same item valuation, with trader/flea comparison and optional ammo stats.

## Choose your pricing rules — built-in configuration

**Price Color Sync includes its own configuration. The defaults are only one way to use it: these settings change which price drives item coloring and which prices appear in the tooltip.**

After the first launch, close the game and edit **`BepInEx/config/local.sptdiagnostics.pricecolorsync.cfg`**, then restart the player client. Pricing and tooltip options are read at startup.

| Setting in `[Pricing]` | Default | What it does |
| --- | --- | --- |
| `Use flea estimates for FIR items` | `true` | For found-in-raid items, show both available trader/flea prices and use the **higher valid value** for valuation and coloring. Set `false` for trader-only pricing and display. |
| `Use flea estimates for non-FIR items` | `false` | By default, non-FIR items use and show the best trader price. Set `true` to show both available prices while using the **flea estimate** for valuation and coloring, even if the trader offer is higher. |
| `Use trader prices for flea-banned items` | `true` | Overrides both rules above: flea-banned items use and show trader prices. Set `false` to allow cached flea estimates when the applicable FIR/non-FIR flea option is also enabled. Estimates may be unavailable. |

**Common setups:**

- **Default:** FIR items use the higher trader/flea value; non-FIR and flea-banned items use trader prices.
- **Show flea estimates on non-FIR items:** enable `Use flea estimates for non-FIR items`. Both available prices appear; the non-FIR color follows flea. FIR items still follow their separate higher-value rule.
- **Trader-only for everything:** disable both `Use flea estimates for FIR items` and `Use flea estimates for non-FIR items`.

These are estimates for display. Enabling a flea estimate does **not** make an item sellable or change transaction prices. In dual-price tooltips, SMTM highlights the higher offer; in optional non-FIR flea mode that highlight can differ from the flea value selected for coloring. Missing/invalid offers are omitted; equal offers retain SMTM's tie behavior.

## More configuration options

| Section / setting | Default | Effect |
| --- | --- | --- |
| `[Tooltip] Show ammo damage and penetration` | `true` | Adds labeled base damage and penetration above hover prices for ammo and populated cartridge packs. Set `false` to hide those numeric stats. Independent of penetration coloring; Item Valuation is not required. |
| `[Background palette] Tier 0` through `Tier 5` | Six dark RGB colors | Customize inventory backgrounds from poor to legendary without changing SMTM's tooltip text palette. |
| `[Background palette] Flea banned` | `#660415` | Customize the red background marker for flea-banned templates. This marker is independent of price tiers; ammo and cartridge packs are exempt. |
| `[Performance] Maximum items per frame` | `12` | Caps advance quote preparation per frame; range 1–64. |
| `[Performance] Background work budget milliseconds` | `2` | Time budget for advance preparation, checked between items; range 0.25–10 ms. One expensive item may exceed it, and cold first-display quotes run outside this budget. |

**Ammo colors are controlled by Show Me The Money:** enable its `UseCaliberPenetrationPower` setting for penetration tiers; disable it for price tiers. SMTM's own config supplies shared tier thresholds, tooltip text colors, trader exclusions and currency/tax handling. Price Color Sync's pricing rules govern quote selection and comparison display; SMTM's own dual-price / Always show flea price options do not override them. Restart after editing Price Color Sync settings.

## Features

- Version 1.1.4 displays both available trader and flea offers in flea-enabled modes, including non-FIR items. SMTM's formatter highlights the better offer. The user confirmed non-FIR mode works with the option both enabled and disabled.
- Filled storage cases are valued as empty cases, excluding contents; tooltip rows are labeled `(case only)`. The live case and its contents are not changed.
- Ammo and populated cartridge packs can show `Damage: 35 | Penetration: 53` (example values). Damage is base per-projectile damage. Empty packs are skipped; matching name stats are not duplicated. SMTM's normal tooltip visibility requirements still apply.
- Exact per-item quotes are prepared before inventories open and reused when views reopen. Actual item changes and price refreshes schedule new work. Simulated inventory moves/swaps retain cached quotes, avoiding repeated repricing during drag previews.
- Startup and post-raid main-menu refreshes use SMTM's existing price updater.

## Requirements and installation

Requires **SPT 4.1.6** and **Show Me The Money 3.0.2** with its documented dependencies. Designed alongside Item Valuation 2.1.0; Item Valuation is not required for this plugin's ammo stats. Other SPT/SMTM versions and Fika multiplayer compatibility are unverified.

1. Close the player game client and back up the old plugin/configuration.
2. Extract the release ZIP into the SPT game folder. The DLL must be at `BepInEx/plugins/SPT.Diagnostics.PriceColorSync.dll`.
3. Keep one copy of this plugin. Install SMTM separately according to its instructions.
4. Start the player client; configuration is generated automatically. Check `BepInEx/LogOutput.log` for loading and initialization errors.
5. Choose your settings in the configuration file above with the game closed, then restart.

**Install only on player clients. No Price Color Sync backend or Fika headless installation is required.** To uninstall, close the client and remove this plugin DLL.

## Scope, refresh and validation

Automatic refresh calls SMTM's existing `/showMeTheMoney/getFleaPrices` endpoint on your configured SPT backend. No separate external server or telemetry is added. Backend caching may return unchanged prices. There is no separate toggle for the added refresh triggers; removing this plugin stops them.

SMTM optional weapon/armor breakdowns and Item Valuation static descriptions retain their original calculations. Actual transactions, saves and sale eligibility are unchanged. Missing trader/market data may prevent an offer. Cold/stale first-display pricing may still cause frame spikes; other mods that mutate items without observed notifications can leave quotes stale until refresh.

On October 16, 2026, the user confirmed FIR dual-price display and tested non-FIR flea pricing both enabled and disabled successfully. The optimized build passes 166 existing assertions. This is not exhaustive mod-interaction, performance or Fika runtime coverage.

MIT licensed. Credits to Show Me The Money, Item Valuation, SPT, BepInEx and Harmony. No game or third-party assemblies are bundled. Developed and documented with OpenAI Codex assistance. [Source and support](https://github.com/GPTSol6-1/PriceColorSync).


## Build, troubleshooting and source

Check `BepInEx/LogOutput.log` for `Loading [GPTSol6-1-PriceColorSync 1.1.4]` and initialization errors. Report exact game, SMTM and plugin versions with relevant log excerpts; redact server addresses, session identifiers and credentials. Confirm the intended pricing options after restarting, and compare FIR/non-FIR, banned items and filled cases.

The companion source archive contains original C# code, standalone tests and a Windows build script. See BUILD.md. No game or third-party assemblies are included. Development and documentation used OpenAI Codex assistance; earlier model IDs were not recorded. Release preparation used OpenAI Codex (GPT-6). No generated art/audio is included. License: MIT for original plugin code/documentation only; dependency and game licenses remain separate.