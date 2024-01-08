<img src="https://gcdn.thunderstore.io/live/repository/icons/Frogger-GroundReset-2.4.2.png.128x128_q95.png" align="right" alt="Project Logo" style="border-radius: 10px;">

# GroundReset

![Version Badge](https://img.shields.io/badge/version-2.4.2-green.svg)

Automatically and gradually resets the terrain across the map, taking into account player's wards.<br>
Supports wards from vanilla and the WardIsLove mod.

### Installation

Required only on the server side. Players do not need to install the mod.

### Configuration

- TheTriggerTime - time in real minutes between reset _**steps**_.
- Divider - the divider for the terrain restoration. Current ground height will be `value` closer to original
  height it was before any player's modifications.
- Compatible with protections from vanilla and the WardIsLove mod.
- Created by order and for VitByr and his server.

Configuration values can be changed in-game.<br>
Default config file `GroundReset.cfg` ⬇:
<details>
  <summary>Click to expand!</summary>

```markdown
[DO NOT TOUCH]

## [Synced with Server]

# Setting type: Single

# Default value: 0

time has passed since the last trigger = 0.3177821

[General]

## Locks client config file so it can't be modified [Synced with Server]

# Setting type: Boolean

# Default value: true

ServerConfigLock = true

## Time in minutes before reset [Synced with Server]

# Setting type: Single

# Default value: 4320

TheTriggerTime = 1

## The divider for the terrain restoration. Current value will be divided by this value. [Synced with Server]

# Setting type: Single

# Default value: 1.7

Divider = 5

## If the height is lower than this value, the terrain will be reset instantly. [Synced with Server]

# Setting type: Single

# Default value: 0.2

Min Height To Stepped Reset = 0.4

## How often elapsed time will be saved to config file. [Synced with Server]

# Setting type: Single

# Default value: 120

SavedTime Update Interval (seconds) = 120
```

</details>

### How it works & help

Before any player's modifications with hoe or pickaxe, what I call ghound height delta is 0. <br>
If the height delta is lower than zero, it means that there's a hole in the terrain.<br>
Positive height delta means there is a hill.<br>
The zero delta of height, as already mentioned, means there is a flat ground.
<br><br>
When the timer is triggered, reset step will be performed. The mod looks at the entire map - every single point of the
earth that has been raised or lowered by the player using a hoe or pickaxe. Points located in the area of the active
ward or the territory of the makretplace are not taken into account by the algorithm. The mod brings each such point
closer to its original value by `Divider` times.

### Compatibility

<a href="https://valheim.thunderstore.io/package/KGvalheim/Marketplace_And_Server_NPCs_Revamped/">
<img alt="Marketplace Logo" src="https://gcdn.thunderstore.io/live/repository/icons/KGvalheim-Marketplace_And_Server_NPCs_Revamped-9.2.2.png.128x128_q95.jpg" width="35"/> 
Marketplace And Server NPCs</a><br>
<a href="https://valheim.thunderstore.io/package/Azumatt/WardIsLove/">
<img alt="WardIsLove Logo" src="https://gcdn.thunderstore.io/live/repository/icons/Azumatt-WardIsLove-3.4.5.png.128x128_q95.png" width="35"/> 
WardIsLove</a><br>

### Contact

Discord - <img alt="GitHub Logo" src="https://freelogopng.com/images/all_img/1691730813discord-icon-png.png" width="16"/>
`justafrogger`<br>
Me on Thunderstore - <a href="https://valheim.thunderstore.io/package/Frogger/">
<img alt="Thunderstore Logo" src="https://gcdn.thunderstore.io/live/community/valheim/PNG_color_logo_only_1_transparent.png" width="14"/>
Frrogger Mods</a><br>
Mod page - <a href="https://valheim.thunderstore.io/package/Frogger/GroundReset/">
<img alt="Thunderstore Logo" src="https://gcdn.thunderstore.io/live/community/valheim/PNG_color_logo_only_1_transparent.png" width="14"/>
GroundReset</a><br>
GitHub - <a href="https://github.com/FroggerHH/GroundReset">
<img alt="GitHub Logo" src="https://github.githubassets.com/assets/pinned-octocat-093da3e6fa40.svg" width="16"/>
GroundReset</a><br>