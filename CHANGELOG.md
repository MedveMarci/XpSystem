# [1.1.0]

## Added

- XP info panel in the Server-specific settings tab: rank, level, XP progress bar, XP left until the next rank, XP
  gained this round and leaderboard position. It refreshes whenever the tab is opened and on every XP change while it is
  open. Can be turned off with `info_menu_enabled`; its setting id is `info_menu_setting_id`.
- Players with a visible global badge get their rank as custom info under their name, since the global badge replaces
  the rank badge (`global_badge_rank_info`). Custom info set by other plugins is never overwritten.
- `0Harmony.dll` is now a required dependency.

## Changed

- The badge system was rewritten.
- A missing XP data file is created right away when the plugin starts.

## Fixed

- The rank badge disappeared for everybody after hiding or showing a global badge, especially after `.gtag`.
- `showtag` after a global badge logged `TAG FAIL 1` / `TAG FAIL 2` in the server console. The leftover global badge
  text and color are now cleared right away.
- Players with a hidden badge showed nothing to RemoteAdmin users who aren't allowed to see hidden badges.
- Rank names with `[`, `]`, `<` or `>` made the badge disappear on the clients; those characters are removed.
- The dedicated server got XP (round win) and showed up on the leaderboard.
- A disguised SCP-3114 showed no rank as custom info with one of the starting disguises; those show its own rank now, as
  they carry its own name. The rank of a disguise whose player has left is still shown.