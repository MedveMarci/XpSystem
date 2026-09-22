# XpSystem ![Downloads](https://img.shields.io/github/downloads/MedveMarci/XpSystem/total)

An SCP: Secret Laboratory LabApi plugin which rewards players with XP for what they do during the round and shows
their rank as a badge.

# Features

- Players earn XP for kills, escapes, generators, SCP-914 upgrades, round wins and more. Every event has its own XP
  amount, on/off switch and cooldown, so nothing has to be farmed.
- Each XP gain shows a hint with the reason, the rank the player just reached and how much XP is left until the next
  one.
- Ranks are shown as a badge. A player who already has a RemoteAdmin badge keeps it - the rank is put in front of it
  (`Veteran - Moderator`) and the group's own color is kept.
- Badges are sent per viewer as fake sync vars, so the player's real group is never touched. Global badges are left
  alone entirely, and a hidden badge stays hidden for everybody with RemoteAdmin access.
- SCP-3114 shows the rank of the player it is impersonating, not its own.
- XP is stored by the plugin itself in a plain JSON file, saved automatically, at the end of every round and on
  shutdown. There is no database and no other plugin to install.
- `Do Not Track` is respected: nothing is stored for those players, and they get their own placeholder badge.
- In-game leaderboard, including offline players - Steam names are resolved through playerdb.co.
- Global XP multiplier that can be changed in-game and is saved to the config.

# Installation

- Download `XpSystem.dll` from the [latest release](https://github.com/MedveMarci/XpSystem/releases/latest).
- Move `XpSystem.dll` to => /SCP Secret Laboratory/LabApi/plugins/global/
- Download `LabApiExtensions.dll` from
  its [latest release](https://github.com/KadavasKingdom/LabApiExtensions/releases/latest) and move it to =>
  /SCP Secret Laboratory/LabApi/dependencies/global/
- Start the server once to generate the config, then edit it and restart.

# Configuration

| Option                        | Default                    | Description                                                                                                 |
|-------------------------------|----------------------------|-------------------------------------------------------------------------------------------------------------|
| `debug`                       | `false`                    | Debug logging. Useful while setting the plugin up.                                                          |
| `data_folder`                 | `XpSystem`                 | Folder, relative to the LabApi configs directory, where the XP data file is stored.                         |
| `data_file_name`              | `xp_data.json`             | Name of the XP data file inside the data folder.                                                            |
| `auto_save_interval_seconds`  | `60`                       | How often the XP data is written to disk. Minimum: 10.                                                      |
| `xp_multiplier`               | `1.0`                      | Applied to every XP gain. Can be changed in-game with `.xp setmultiplier`.                                  |
| `dnt_badge_text`              | `No Data`                  | Badge text shown for players who have Do Not Track enabled.                                                 |
| `dnt_badge_color`             | `nickel`                   | Badge color for those players.                                                                              |
| `hint_duration`               | `5`                        | How long the XP hint stays on screen, in seconds.                                                           |
| `xp_events`                   | see below                  | Per-event `enabled`, `xp` and `reason` settings.                                                            |
| `event_cooldowns`             | see below                  | Per-event cooldown in seconds. `0` means no cooldown. The keys must match the `xp_events` keys.              |
| `ranks`                       | see below                  | The rank list. Each entry has `min_xp`, `badge_name` and `badge_color`.                                     |

## XP events

Every key below exists in both `xp_events` and `event_cooldowns`. Setting `enabled` to `false` or `xp` to `0` turns
the event off.

| Key                  | XP    | Cooldown | Granted for                                                          |
|----------------------|-------|----------|----------------------------------------------------------------------|
| `Kill`               | `40`  | `20`     | Killing another player                                               |
| `ScpKill`            | `80`  | `0`      | Killing an SCP                                                       |
| `ScpKillHuman`       | `40`  | `20`     | Killing a human while playing as an SCP                              |
| `Scp106Kill`         | `80`  | `20`     | A player dying in the Pocket Dimension - granted to every SCP-106    |
| `ZombieKill`         | `40`  | `20`     | Killing an SCP-049-2                                                 |
| `Scp049Resurrection` | `50`  | `0`      | SCP-049 resurrecting a body                                          |
| `Generator`          | `50`  | `60`     | Activating a generator                                               |
| `Escape`             | `100` | `0`      | Escaping the facility                                                |
| `UsedMedicalItem`    | `20`  | `0`      | Using SCP-500, a Medkit, Painkillers or Adrenaline                   |
| `PickedUpSpecialItem`| `25`  | `60`     | Picking up a MicroHID, SCP-127, Jailbird or Particle Disruptor       |
| `RoundWin`           | `100` | `0`      | Being on the leading team when the round ends                        |
| `Scp914Upgrade`      | `15`  | `60`     | Upgrading an item in SCP-914                                         |

Adding a new event takes two steps: put the key into `xp_events` (and `event_cooldowns`) and override the matching
LabApi event in `EventHandlers`, calling `TryGrant(player, "YourKey")` in it.

## Ranks

| `min_xp` | `badge_name`  | `badge_color` |
|----------|---------------|---------------|
| `0`      | `Rookie`      | `default`     |
| `100`    | `Player`      | `lime`        |
| `500`    | `Experienced` | `cyan`        |
| `1000`   | `Veteran`     | `aqua`        |
| `2500`   | `Elite`       | `yellow`      |
| `5000`   | `Legend`      | `orange`      |
| `10000`  | `Master`      | `crimson`     |

List the ranks in ascending `min_xp` order. Available colors: `default`, `pink`, `red`, `brown`, `silver`,
`light_green`, `crimson`, `cyan`, `aqua`, `deep_pink`, `tomato`, `yellow`, `magenta`, `blue_green`, `orange`, `lime`,
`green`, `emerald`, `carmine`, `nickel`, `mint`, `army_green`, `pumpkin`.

# Commands

The `.xp` command and its subcommands are available both in the client console and in RemoteAdmin.

| Command                        | Aliases                 | Permission   | Description                                                |
|--------------------------------|-------------------------|--------------|------------------------------------------------------------|
| `.xp`                          | `.rank`, `.level`       | -            | Shows your own XP, rank and the XP left until the next one |
| `.xp leaderboard`              | `top`, `lb`             | -            | Top 5 players by XP, online and offline alike              |
| `.xp refresh`                  | -                       | `xp.refresh` | Refreshes every badge                                      |
| `.xp toggle`                   | -                       | `xp.toggle`  | Turns XP gaining on and off globally                       |
| `.xp add <target> <amount>`    | -                       | `xp.admin`   | Gives XP to the target                                     |
| `.xp remove <target> <amount>` | `rem`                   | `xp.admin`   | Takes XP away from the target                              |
| `.xp get <target>`             | -                       | `xp.admin`   | Shows the target's XP and rank                             |
| `.xp setmultiplier <value>`    | `setmult`, `multiplier` | `xp.admin`   | Sets the XP multiplier and saves it to the config          |

`<target>` accepts everything the RemoteAdmin commands accept - `*` or `all` for everybody, player IDs, names, and
lists separated by dots (`.xp add 2.5.7 100`).

# Storage

XP is kept in `<LabApi configs>/XpSystem/xp_data.json` as a flat `user id -> XP` map. The file is written atomically
and the previous version is kept next to it as `.bak`. Players with `Do Not Track` enabled are never written to it.

# For Support

<div align="left">
<a href='https://discord.gg/KmpA8cfaSA'><img src='https://www.allkpop.com/upload/2021/01/content/262046/1611711962-discord-button.png' height="100"></a>
</div>
