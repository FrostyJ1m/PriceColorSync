# Price Color Sync 1.0.7

Make inventory background tiers agree with Show Me The Money's main tooltip valuation. Player-client plugin for SPT 4.1.6 and Show Me The Money 3.0.2.

## Release status

Release candidate, not yet published. Earlier 1.0.5 behavior was user-confirmed working. Versions 1.0.6 and 1.0.7 passed static/policy checks, but their performance and automatic-refresh changes still require in-game verification. No clean-install test has been recorded. Compatibility with other SPT/SMTM versions is not established.

## Requirements and deployment

- SPT 4.1.6 with its supplied BepInEx installation (inspected loader: 5.4.23.5).
- Show Me The Money 3.0.2, installed according to its own client/server instructions. This plugin calls internal SMTM APIs; newer versions are not automatically compatible.
- Designed alongside Item Valuation 2.1.0. Item Valuation is not a declared BepInEx dependency; its backend description text is outside this plugin's scope.
- Install this plugin only on each player's game client. No Price Color Sync server or Fika headless installation is needed. The plugin explicitly rejects the Fika headless plugin. Fika multiplayer compatibility has not been independently certified for this release.

## Install and remove

1. Exit the player game client normally. Back up any older Price Color Sync DLL and configuration before updating.
2. Extract the archive into the SPT game directory containing EscapeFromTarkov.exe. The result must be `BepInEx/plugins/SPT.Diagnostics.PriceColorSync.dll`.
3. Keep only one copy of this plugin in the plugins tree. Start the client normally; configuration is generated automatically.
4. Check `BepInEx/LogOutput.log` for `Loading [FrostyJim-PriceColorSync 1.0.7]` and any Price Color Sync initialization errors.

To uninstall, exit the client, remove this DLL, and restart. Its optional config can also be removed. The archive does not replace SMTM, Item Valuation, game files, saved profiles, or server configuration.

## Pricing and colors

- Found-in-raid (FIR), flea-eligible items use SMTM's cached flea estimate. Non-FIR and flea-banned items use the best available trader price by default.
- Storage cases are priced as empty cases, excluding contents without changing the live inventory. Filled-case tooltips show `(case only)`.
- Background and main tooltip share a cached item valuation and tier. Price-per-slot tiers default to 5,000 / 10,000 / 15,000 / 25,000 / 35,000 roubles through SMTM settings.
- Ammunition and populated cartridge packs use SMTM's penetration tiers when enabled. Disable that SMTM option to use price tiers. Empty cartridge packs use a neutral background when no penetration sample exists.
- Flea-banned templates have a red background, independently of tooltip price tiers. Ammunition and cartridge packs are exempt from this red marker; storage ammo cases are not.
- Background colors are dark; SMTM tooltip colors remain independent. Missing flea estimates receive a neutral background rather than silently switching to trader estimates.
- Recoloring is queued with item/time limits; other unopened items refresh on their next UI update or hover. Selection/highlight effects can temporarily override normal backgrounds.

SMTM optional weapon-mod/armor-plate breakdown lines retain their original calculations. Item Valuation's static backend description strings remain historical template estimates and may differ. This mod does not change actual buy/sell prices or sale eligibility.

## Configuration

Edit `BepInEx/config/local.sptdiagnostics.pricecolorsync.cfg` while the client is closed, then restart. Defaults:

```ini
[Pricing]
Use flea estimates for FIR items = true
Use flea estimates for non-FIR items = false
Use trader prices for flea-banned items = true

[Performance]
Maximum items per frame = 12
Background work budget milliseconds = 2
```

The flea-ban override takes precedence over both FIR options. Set both flea-estimate options false for trader-only pricing. Background tier RGB colors and the flea-ban marker are configurable in this file.

SMTM's `BepInEx/config/com.dark.spt.showmethemoney.cfg` controls shared price/penetration thresholds, penetration mode, tooltip colors, trader exclusions, conversion/tax settings, and normal market refresh behavior. SMTM's dual-price and Always show flea price options do not override this plugin's single selected source.

The work budget is checked between items; one expensive item can exceed it. Item cap accepts 1-64 and budget accepts 0.25-10 ms. These limits cover this queue, not total frame time.

## Automatic refresh and network behavior

Version 1.0.7 requests an SMTM refresh at the first usable main menu after launch and once after each observed raid when returning to the main menu. Ordinary stash/trader/settings navigation does not schedule another request.

This calls SMTM's existing updater, which requests `/showMeTheMoney/getFleaPrices` from your configured SPT backend using SMTM's normal session/network implementation. There is no separate external update server or telemetry implemented by Price Color Sync. The plugin adds no independent network payload; requests use the existing SMTM/SPT protocol and authentication. SMTM's normal updater checks about every 30 seconds, and backend caching can return unchanged values. This does not force economy regeneration. There is currently no separate toggle for the added refresh requests; remove the plugin to stop these added triggers.

## Troubleshooting and validation

Check `BepInEx/LogOutput.log` for initialization errors and Price Color Sync warnings. A disabled plugin often indicates mismatched UI/SMTM internals. Report exact SPT, SMTM and plugin versions with relevant log excerpts; redact server addresses, session identifiers and credentials before sharing.

For 1.0.7, verify `Automatic SMTM refresh requested: client startup` followed by `Automatic SMTM refresh: replacement price snapshot received.` Complete a raid and return to the main menu; expect a `return from raid` request and a replacement snapshot. Allow at least 30 seconds plus request time. A request message alone does not establish a successful fetch. Repeated ordinary menu navigation should not add automatic requests.

Compare FIR/non-FIR ordinary items, banned items, ammo, filled/empty cases, damaged gear and a large stash. Check tooltip/background agreement, frame drops and new errors. A matching tier does not mean identical RGB palettes. Pure policy tests do not establish live compatibility or performance.

## Source, credits and AI disclosure

The companion source archive contains the plugin's original C# source, policy tests and Windows build script. It contains no game or third-party assemblies. Build instructions are in `BUILD.md`.

Thanks to the Show Me The Money, Item Valuation, SPT, BepInEx and Harmony projects. Their code/binaries are not included in this release. Install dependencies from their original publishers.

Development and documentation used OpenAI Codex assistance; earlier model IDs were not recorded in the workspace. Release preparation used OpenAI Codex (GPT-6). No generated art/audio is included.

License: MIT, covering the original Price Color Sync code and documentation only. Dependency and game licenses remain separate.

Support: use the eventual mod listing's comments or source repository issues. Public support and source URLs must be assigned before publication.