# Changelog

## 1.1.4 - 2026-10-16

- Restore trader/flea comparison in SMTM hover tooltips: show each valid offer using SMTM's original formatting, better-offer highlighting and price colors. Includes the previously local-only 1.1.3 tooltip update.
- Fix the missing trader row when `Use flea estimates for non-FIR items` is enabled. Both available prices now appear, while non-FIR valuation/coloring still follow the configured flea estimate. FIR flea mode continues to select the higher valid offer.
- Keep default non-FIR and flea-banned behavior trader-only. Preserve case-only valuation, ammo damage/penetration, refresh scheduling and inventory drag-preview improvements.
- Expand the main description and included README with prominent pricing settings, defaults, mode examples, flea-ban precedence, ammo/color options and performance controls. Restart the client after changing Price Color Sync settings.
- User confirmed FIR dual-price tooltips and tested non-FIR flea pricing enabled and disabled successfully. Optimized build passes all 166 existing assertions; exhaustive mod-interaction and Fika coverage is not claimed.


## 1.1.2 - 2026-10-16

- Add labeled base damage and penetration above SMTM hover prices for ammunition and populated cartridge packs. No Item Valuation installation is required for these stats.
- Add a default-on `[Tooltip] Show ammo damage and penetration` setting in the Price Color Sync config. Numeric stats work independently of SMTM's penetration-color setting. Restart the player client after changing it.
- Skip empty packs and non-ammo items. Avoid duplicating matching `[damage/penetration]` name suffixes; outdated suffixes do not hide current stats. Repeated price refreshes replace the stats block without accumulating lines.
- Retain 1.1.1 drag-preview improvements and existing pricing, background, case and flea-ban policies. SMTM's existing tooltip visibility requirements still apply.
- User confirms the installed 1.1.2 works; supplied screenshot shows 4.6x30mm AP SX with Damage: 35, Penetration: 53 and the existing trader price. Optimized build passes 166 assertions, including 1,000 simulated tooltip refreshes. No exhaustive mod-interaction, cartridge-pack or Fika runtime coverage is claimed.


## 1.1.1 - 2026-10-15

- Fix repeated price invalidation during simulated inventory moves, swaps and drag previews, avoiding unnecessary repricing of unchanged assembled weapons and armor.
- Preserve immediate price/color updates for actual item changes and affected assembly parents.
- Restore preview suppression after nested operations and exceptions; temporary preview items do not enter the preparation queue.
- Retain 1.1.0 advance preparation and existing FIR, case-only, ammunition and flea-ban pricing/color policies.
- User confirms complex-item drag lag is gone and actual changes still update price/color immediately. Optimized build passes 154 assertions; standalone Harmony checks resolve 33 game targets and verify preview suppression, real mutations and exception recovery. No independent profiler benchmark, exhaustive mod-interaction coverage or fresh-install test of 1.1.1 is claimed.


## 1.1.0 - 2026-10-15

- Prepare exact per-item valuations before inventories open, retaining condition, attachments and FIR status.
- Reuse valid cached quotes when views reopen; creation, movement, refresh and FIR changes schedule preparation. Market snapshots and startup/post-raid refreshes reschedule known items.
- Price an examined cold/stale item before its first color draw so it does not briefly display a provisional color.
- Keep advance preparation on the main thread with existing item/time limits; weak tracking allows unused items to be collected and excludes pricing clones, trader stock and virtual roots.
- Preserve higher-of-flea/trader FIR selection, case-only valuation, ammo penetration and flea-ban backgrounds.
- User reports items are already colored when an inventory opens and no noticeable frame loss. Optimized build passes 140 assertions. This is user-reported testing, not a profiler benchmark or proof of every mutation/Fika scenario.


## 1.0.8 - 2026-10-15
- Update plugin display-name prefix to GPTSol6-1 following the author profile rename; GUID/configuration path unchanged.
- FIR items using flea estimates now compare against the best trader sell-to quote and use the higher valid SMTM rouble-per-slot value, including configured currency/tax handling. Ties retain flea.
- Fall back to the valid quote when the other source is unavailable; keep trader-winning quotes responsive to replacement flea snapshots.
- Main tooltip, selected source and price-based background tier follow the winning quote. Existing non-FIR, flea-ban, ammo and case-only rules remain.
- User confirmed successful runtime testing of 1.0.8. Optimized release build and 113 policy assertions pass. The release restores the Forge-required plugin display name; pricing code matches the tested local version.


## 1.0.7 - 2026-10-14
- Use Forge-compliant plugin display name FrostyJim-PriceColorSync; GUID/configuration path unchanged.
- Request an SMTM price refresh on first main-menu entry and return from each observed raid.
- Retry when the updater is not ready; recolor from replacement snapshots using the bounded queue.
- User reports successful testing on two clients sharing a server and on a fresh SPT installation with required dependencies; exact tested hash and individual test coverage were not independently recorded.
- Document the SMTM UseCaliberPenetrationPower requirement for penetration-based ammunition colors.

## 1.0.6 - 2026-10-14
- Raise default queue item cap to 12/frame and add a 2 ms elapsed-time budget.
- Runtime performance verification pending.

## 1.0.5 - 2026-10-13
- Exempt ammo and cartridge packs from the red flea-ban background marker.
- User reported the delivered behavior working; no independent profiler result recorded.

## 1.0.4 - 2026-10-13
- Add configurable red backgrounds for flea-banned templates.

## 1.0.3 - 2026-10-13
- Add configurable FIR/non-FIR sources and trader override for flea-banned items.

## 1.0.2 - 2026-10-13
- Price storage cases through detached empty instances and allow case-only tooltips.

## 1.0.1 - 2026-10-13
- Replace inventory polling with bounded background work and preserve independent text/background palettes.

## 1.0.0 - 2026-10-13
- Initial local compatibility plugin for shared inventory/tooltip valuation.