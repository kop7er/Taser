using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Taser", "Kopter", "1.0.3")]
    [Description("Transforms a Semi-Automatic Pistol into a Taser")]

    public class Taser : RustPlugin
    {
        #region Variables

        ItemDefinition AmmoType;

        Timer ScreamSoundTimer;

        List<ulong> WoundedPlayers = new List<ulong>();

        private const string UseTaser = "taser.use";
        private const string TaserAffect = "taser.affect";
        private const string LootTasedPlayer = "taser.loot";
        private const string ReviveTasedPlayer = "taser.revive";

        private string ScreamSound = "assets/bundled/prefabs/fx/player/beartrap_scream.prefab";

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            AmmoType = ItemManager.FindItemDefinition("ammo.pistol.hv");

            permission.RegisterPermission(UseTaser, this);
            permission.RegisterPermission(TaserAffect, this);
            permission.RegisterPermission(LootTasedPlayer, this);
            permission.RegisterPermission(ReviveTasedPlayer, this);

            WoundedPlayers.Clear();
        }

        private object OnReloadWeapon(BasePlayer Player, BaseProjectile Projectile)
        {
            if (permission.UserHasPermission(Player.UserIDString, UseTaser) && Projectile.ShortPrefabName == "pistol_semiauto.entity")
            {
                if (Projectile.primaryMagazine.ammoType == AmmoType) Projectile.primaryMagazine.capacity = 1;

                else Projectile.primaryMagazine.capacity = Projectile.primaryMagazine.definition.builtInSize;
            }

            return null;
        }

        private object OnEntityTakeDamage(BaseCombatEntity Entity, HitInfo HitInfo)
        {
            if (Entity == null || !(Entity as BasePlayer) || HitInfo == null) return null;

            var Attacker = HitInfo.InitiatorPlayer;
            var Victim = HitInfo.HitEntity as BasePlayer;

            if (Attacker == null || Victim == null || Victim.IsNpc) return null;

            if (WoundedPlayers.Contains(Victim.userID)) return false;

            if (!permission.UserHasPermission(Attacker.UserIDString, UseTaser)) return null;
            if (!permission.UserHasPermission(Victim.UserIDString, TaserAffect) && config.AffectPermissionNeeded) return null;

            var WeaponName = HitInfo.WeaponPrefab.ShortPrefabName;
            var WeaponAmmo = HitInfo.Weapon as BaseProjectile;

            if (WeaponName == null || WeaponAmmo == null) return null;

            if (WeaponName != "pistol_semiauto.entity" || WeaponAmmo.primaryMagazine.ammoType != AmmoType) return null;

            if (config.MaxDistance > 0 && (!HitInfo.IsProjectile() ? (int)Vector3.Distance(HitInfo.PointStart, HitInfo.HitPositionWorld) : (int)HitInfo.ProjectileDistance) > config.MaxDistance) return null;

            Victim.BecomeWounded();
            WoundedPlayers.Add(Victim.userID);

            if (config.PlayScream)
            {
                Effect.server.Run(ScreamSound, Victim.transform.position);

                ScreamSoundTimer = timer.Every(3f, () =>
                {
                    if (Victim == null) ScreamSoundTimer.Destroy();

                    else Effect.server.Run(ScreamSound, Victim.transform.position);
                });
            }

            timer.Once(config.WoundedTime, () =>
            {
                if (Victim != null) Victim.StopWounded();
                WoundedPlayers.Remove(Victim.userID);
                if (ScreamSoundTimer != null) ScreamSoundTimer.Destroy();
            });

            return false;
        }

        private object CanLootPlayer(BasePlayer Target, BasePlayer Looter)
        {
            if (WoundedPlayers.Contains(Target.userID))
            {
                if (config.LootPermissionNeeded && !permission.UserHasPermission(Looter.UserIDString, LootTasedPlayer)) return false;
            }

            return null;
        }

        private object OnPlayerAssist(BasePlayer Target, BasePlayer Player)
        {
            if (WoundedPlayers.Contains(Target.userID))
            {
                if (config.RevivePermissionNeeded && !permission.UserHasPermission(Player.UserIDString, LootTasedPlayer)) return false;
            }

            return null;
        }

        #endregion

        #region Config

        private ConfigData config = new ConfigData();

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Wounded Time (In Seconds)")]
            public float WoundedTime = 10;

            [JsonProperty(PropertyName = "Max Distance where the Taser will work (If 0, a max distance will no be applied)")]
            public int MaxDistance = 0;

            [JsonProperty(PropertyName = "Play a Scream Sound while the player is down")]
            public bool PlayScream = false;

            [JsonProperty(PropertyName = "Requires a permission to be affected by the taser")]
            public bool AffectPermissionNeeded = false;

            [JsonProperty(PropertyName = "Requires permission to loot a player affected by the taser")]
            public bool LootPermissionNeeded = true;

            [JsonProperty(PropertyName = "Requires permission to revive a player affected by the taser")]
            public bool RevivePermissionNeeded = true;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();

                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }

            catch
            {
                PrintError("Configuration file is corrupt, check your config file at https://jsonlint.com/!");
                LoadDefaultConfig();
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigData();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion
    }
}