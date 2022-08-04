using Newtonsoft.Json;

using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Taser", "Kopter", "2.0.1")]
    [Description("Transforms a Semi-Automatic Pistol into a Taser")]

    public class Taser : RustPlugin
    {
        #region Variables

        private ItemDefinition ammoType;

        private const string useTaserPermission = "taser.use";

        private const string taserAffectPermission = "taser.affect";

        private const string lootPlayerPermission = "taser.loot";

        private const string revivePlayerPermission = "taser.revive";

        private const string semiAutoPistolShortname = "pistol_semiauto.entity";

        private const string screamSound = "assets/bundled/prefabs/fx/player/beartrap_scream.prefab";

        private const string shockEffect = "assets/prefabs/locks/keypad/effects/lock.code.shock.prefab";

        private List<ulong> woundedPlayers = new List<ulong> { };

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            ammoType = ItemManager.FindItemDefinition("ammo.pistol.hv");

            permission.RegisterPermission(useTaserPermission, this);
            permission.RegisterPermission(taserAffectPermission, this);
            permission.RegisterPermission(lootPlayerPermission, this);
            permission.RegisterPermission(revivePlayerPermission, this);

            woundedPlayers.Clear();
        }

        private void OnWeaponReload(BaseProjectile projectile, BasePlayer player)
        {
            if (player == null || !permission.UserHasPermission(player.UserIDString, useTaserPermission) || projectile == null || projectile.ShortPrefabName != semiAutoPistolShortname)
                return;

            if (projectile.primaryMagazine.ammoType == ammoType) projectile.primaryMagazine.capacity = config.numberOfRounds;

            else projectile.primaryMagazine.capacity = projectile.primaryMagazine.definition.builtInSize;
        }

        private object OnEntityTakeDamage(BasePlayer victim, HitInfo hitInfo)
        {
            if (victim == null || victim.IsNpc || (config.affectPermissionNeeded && permission.UserHasPermission(victim.UserIDString, taserAffectPermission)) || hitInfo == null) 
                return null;

            if (woundedPlayers.Contains(victim.userID)) 
                return false;

            var weaponName = hitInfo.WeaponPrefab?.ShortPrefabName;

            if (weaponName == null || weaponName != semiAutoPistolShortname)
                return null;

            var weaponAmmo = hitInfo.Weapon as BaseProjectile;

            if (weaponAmmo == null || weaponAmmo.primaryMagazine.ammoType != ammoType)
                return null;

            var attacker = victim.lastAttacker as BasePlayer ?? hitInfo.InitiatorPlayer;

            if (attacker == null || !permission.UserHasPermission(attacker.UserIDString, useTaserPermission))
                return null;

            if (config.maxDistance > 0 && (int)hitInfo.ProjectileDistance > config.maxDistance)
                return false;

            victim.BecomeWounded();

            woundedPlayers.Add(victim.userID);

            Effect.server.Run(shockEffect, hitInfo.HitPositionWorld);

            Timer screamSoundTimer = null;

            if (config.playScream)
            {
                Effect.server.Run(screamSound, victim.transform.position);

                screamSoundTimer = timer.Every(4f, () =>
                {
                    if (victim == null || !woundedPlayers.Contains(victim.userID)) screamSoundTimer?.Destroy();

                    else Effect.server.Run(screamSound, victim.transform.position);
                });
            }

            timer.Once(config.woundedTime, () =>
            {
                victim?.StopWounded();

                woundedPlayers.Remove(victim.userID);

                screamSoundTimer?.Destroy();
            });

            return false;
        }

        private object CanLootPlayer(BasePlayer target, BasePlayer looter)
        {
            if (config.lootPermissionNeeded && woundedPlayers.Contains(target.userID) && !permission.UserHasPermission(looter.UserIDString, lootPlayerPermission)) 
                return false;

            return null;
        }

        private object OnPlayerAssist(BasePlayer target, BasePlayer reviver)
        {
            if (config.revivePermissionNeeded && woundedPlayers.Contains(target.userID) && !permission.UserHasPermission(reviver.UserIDString, revivePlayerPermission)) 
                return false;

            return null;
        }

        private void Unload()
        {
            ammoType = null;

            foreach (var player in BasePlayer.activePlayerList)
                if (woundedPlayers.Contains(player.userID) && player.IsWounded())
                    player.StopWounded();

            woundedPlayers.Clear();
        }

        #endregion

        #region Config

        private ConfigData config = new ConfigData();

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Number Of Taser Rounds")]
            public int numberOfRounds = 1;

            [JsonProperty(PropertyName = "Wounded Time (In Seconds)")]
            public float woundedTime = 10;

            [JsonProperty(PropertyName = "Max Distance where the Taser will work (If 0, a max distance will not be applied)")]
            public int maxDistance = 0;

            [JsonProperty(PropertyName = "Play a Scream Sound while the player is tased")]
            public bool playScream = false;

            [JsonProperty(PropertyName = "Requires a permission to be affected by the taser")]
            public bool affectPermissionNeeded = false;

            [JsonProperty(PropertyName = "Requires permission to loot a player affected by the taser")]
            public bool lootPermissionNeeded = true;

            [JsonProperty(PropertyName = "Requires permission to revive a player affected by the taser")]
            public bool revivePermissionNeeded = true;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();

                if (config == null) LoadDefaultConfig();
            }

            catch
            {
                PrintError("Configuration file is corrupt, check your config file at https://jsonlint.com/!");
                LoadDefaultConfig();
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = new ConfigData();

        protected override void SaveConfig() => Config.WriteObject(config);
        
        #endregion
    }
}