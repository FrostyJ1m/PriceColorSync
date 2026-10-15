# Changelog

## 1.0.8 - 2026-10-15
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