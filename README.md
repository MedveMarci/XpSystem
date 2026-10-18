# XpSystem

[![Version](https://img.shields.io/github/v/release/MedveMarci/XpSystem?label=Version&color=d500ff)](https://github.com/MedveMarci/XpSystem/releases/latest) [![LabAPI Version](https://img.shields.io/badge/LabAPI_Version-1.1.7-51f4ff)](https://github.com/northwood-studios/LabAPI/releases/tag/1.1.7) [![SCP:SL Version](https://img.shields.io/badge/SCP:SL_Version-14.2.8-e5b200)](https://store.steampowered.com/app/700330/SCP_Secret_Laboratory/) [![Downloads](https://img.shields.io/github/downloads/MedveMarci/XpSystem/total?label=Downloads&color=ffbf00)](https://github.com/MedveMarci/XpSystem/releases) [![License](https://img.shields.io/github/license/MedveMarci/XpSystem?label=License&color=2ea44f)](LICENSE)

An SCP: Secret Laboratory [LabAPI](https://github.com/northwood-studios/LabAPI) plugin which rewards players with XP for
what they do during the round and shows their rank as a badge.

---

## Features

- XP for kills, escapes, generators, SCP-914 upgrades, round wins and more, each with its own amount and cooldown.
- A hint on every XP gain with the reason and the XP left until the next rank.
- The rank is shown as a badge. RemoteAdmin badges are kept: `Veteran - Moderator`.
- Hidden badges stay hidden, staff who can see hidden badges still see them.
- Players with a global badge get their rank under their name instead.
- A disguised SCP-3114 shows the rank of the player it's disguised as.
- XP info panel in the Server-specific settings tab.
- Leaderboard for players.
- XP multiplier that can be changed in-game.
- XP is saved to a JSON file, no database needed.
- `Do Not Track` players get a placeholder badge and nothing is saved for them.

---

## Installation

1. Download `XpSystem.dll` and `0Harmony.dll` from
   the [latest release](https://github.com/MedveMarci/XpSystem/releases/latest).
2. Move `XpSystem.dll` to:
    - Windows: `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\`
    - Linux: `~/.config/SCP Secret Laboratory/LabAPI/plugins/global/`
3. Move `0Harmony.dll` to:
    - Windows: `%AppData%\SCP Secret Laboratory\LabAPI\dependencies\global\`
    - Linux: `~/.config/SCP Secret Laboratory/LabAPI/dependencies/global/`
4. Start the server once to generate the config, then edit it and restart.

---

### XP events

Every key below exists in both `xp_events` and `event_cooldowns`. Setting `enabled` to `false` or `xp` to `0` turns the
event off.

| Key                   | XP    | Cooldown | Granted for                                                       |
|-----------------------|-------|----------|-------------------------------------------------------------------|
| `Kill`                | `40`  | `20`     | Killing another player                                            |
| `ScpKill`             | `80`  | `0`      | Killing an SCP                                                    |
| `ScpKillHuman`        | `40`  | `20`     | Killing a human while playing as an SCP                           |
| `Scp106Kill`          | `80`  | `20`     | A player dying in the Pocket Dimension - granted to every SCP-106 |
| `ZombieKill`          | `40`  | `20`     | Killing an SCP-049-2                                              |
| `Scp049Resurrection`  | `50`  | `0`      | SCP-049 resurrecting a body                                       |
| `Generator`           | `50`  | `60`     | Activating a generator                                            |
| `Escape`              | `100` | `0`      | Escaping the facility                                             |
| `UsedMedicalItem`     | `20`  | `0`      | Using SCP-500, a Medkit, Painkillers or Adrenaline                |
| `PickedUpSpecialItem` | `25`  | `60`     | Picking up a MicroHID, SCP-127, Jailbird or Particle Disruptor    |
| `RoundWin`            | `100` | `0`      | Being on the leading team when the round ends                     |
| `Scp914Upgrade`       | `15`  | `60`     | Upgrading an item in SCP-914                                      |

Adding a new event takes two steps: put the key into `xp_events` (and `event_cooldowns`) and override the matching
LabApi event in `EventHandlers`, calling `TryGrant(player, "YourKey")` in it.

### Ranks

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

---

## Commands

The `xp` command and its subcommands are RemoteAdmin commands. Players see their own rank and progress in the info panel
of the Server-specific settings tab.

| Command                       | Aliases                 | Permission   | Description                                                |
|-------------------------------|-------------------------|--------------|------------------------------------------------------------|
| `xp leaderboard`              | `top`, `lb`             | -            | Top 5 players by XP, online and offline alike              |
| `xp refresh`                  | -                       | `xp.refresh` | Sends every badge again                                    |
| `xp toggle`                   | -                       | `xp.toggle`  | Turns XP gaining on and off globally                       |
| `xp add <target> <amount>`    | -                       | `xp.admin`   | Gives XP to the target                                     |
| `xp remove <target> <amount>` | `rem`                   | `xp.admin`   | Takes XP away from the target                              |
| `xp get <target>`             | -                       | `xp.admin`   | Shows the target's XP and rank                             |
| `xp setmultiplier <value>`    | `setmult`, `multiplier` | `xp.admin`   | Sets the XP multiplier and saves it to the config          |

`<target>` accepts everything the RemoteAdmin commands accept - `*` or `all` for everybody, player IDs, names, and lists
separated by dots (`xp add 2.5.7 100`).

---

## Storage

- Windows: `%AppData%\SCP Secret Laboratory\LabAPI\configs\XpSystem\xp_data.json`
- Linux: `~/.config/SCP Secret Laboratory/LabAPI/configs/XpSystem/xp_data.json`

---

## Support

<a href="https://discord.gg/KmpA8cfaSA"><img src="https://www.allkpop.com/upload/2021/01/content/262046/1611711962-discord-button.png" height="80"></a>

---

## Credits

- Made by [MedveMarci](https://github.com/MedveMarci)
