# Taser
Transforms a Semi-Automatic Pistol into a Taser

## How It Works

If a player with permission reloads a `Semi-Automatic Pistol` with `HV Pistol Ammo`, that weapon will be reloaded with only 1 shot and becomes a Taser. If a player gets hit, he will be wounded for an X amount of time defined by the config file (10 seconds by default).

## Permissions

* `taser.use` -- Allows players to use the taser
* `taser.affect` -- Allows players to be affected by the taser (Disabled by default)
* `taser.loot` -- Allows players to loot a player affected by the taser (Enabled by default)
* `taser.revive` -- Allows players to revive a player affected by the taser (Enabled by default)

## Configuration

``` json
{
  "Number Of Taser Rounds": 1,
  "Wounded Time": 10.0,
  "Max Distance where the Taser will work (If 0, a max distance will no be applied)": 0,
  "Play a Scream Sound while the player is tased": false,
  "Requires a permission to be affected by the taser": false,
  "Requires a permission to loot a player affected by the taser": true,
  "Requires a permission to revive a player affected by the taser": true
}
```
