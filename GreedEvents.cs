using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Zeta.Bot;
using Zeta.Bot.Navigation;
using Zeta.Bot.Profile;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals.Actors;
using Trinity;

namespace VaultRunner
{
    public class GreedEvents
    {
        private static readonly int _GreedPortalSNO = 393030;
        private static readonly int _GreedChestSNO = 403683;
        private static string _GreedProfile = string.Format("{0}\\Plugins\\{1}\\profile.xml", System.Environment.CurrentDirectory, "VaultRunner");
        private static string _GreedProfileBackUp = string.Format("{0}\\Plugins\\{1}\\profile.xml", System.Environment.CurrentDirectory, "GreedsDomain");
        private Profile _currentProfile;
        private Stopwatch _greedChestTimer = null;
        private Trinity.Config.Combat.DestructibleIgnoreOption _previousOption;

        public GreedState state = GreedState.LookingForPortal;

        public GreedEvents()
        {
            state = GreedState.LookingForPortal;
            _greedChestTimer = null;
        }

        public GreedState FindPortal()
        {
            if (state == GreedState.LookingForPortal)
            {
                DiaObject portalObject = ZetaDia.Actors.RActorList.OfType<DiaObject>().FirstOrDefault(r => r.ActorSNO == _GreedPortalSNO);

                if (portalObject != null)
                {
                    Logger.Log("Found Goblin Portal - " + DateTime.Now.ToString());

                    Navigator.MoveTo(portalObject.Position);

                    PauseBot(0, 500);

                    return GreedState.FoundPortal;
                }
            }

            return ConfirmWorld();
        }

        public GreedState FoundPortal()
        {
            if (ProfileManager.CurrentProfile.Path != _GreedProfile || ProfileManager.CurrentProfile.Path != _GreedProfileBackUp)
            {
                Logger.Log("Loading Greed Profile - " + DateTime.Now.ToString());

                _currentProfile = ProfileManager.CurrentProfile;
                LoadProfile(_GreedProfile, true, 1);

                if (ProfileManager.CurrentProfile.Path != _GreedProfile)
                    LoadProfile(_GreedProfileBackUp, true, 1);
            }

            if (ZetaDia.CurrentWorldId != 379962 && ZetaDia.CurrentWorldId != 380753)
            {
                DiaObject portalObject = ZetaDia.Actors.RActorList.OfType<DiaObject>().FirstOrDefault(r => r.ActorSNO == _GreedPortalSNO);

                if (portalObject != null && !ZetaDia.Me.IsInCombat)
                {
                    Logger.Log("Moving to portal - Distance " + (int)ZetaDia.Me.Position.Distance2D(portalObject.Position) + " feet away");
                    Navigator.MoveTo(portalObject.Position);

                    PauseBot(0, 300);

                    portalObject.Interact();

                    PauseBot(0, 300);
                }
            }
            else
                return GreedState.InsidePortal;

            return ConfirmWorld();
        }

        public GreedState InsidePortal()
        {
            if (ZetaDia.CurrentWorldId != 379962)
                return ConfirmWorld();

            if (Trinity.Trinity.Settings.WorldObject.DestructibleOption != Trinity.Config.Combat.DestructibleIgnoreOption.DestroyAll)
            {
                _previousOption = Trinity.Trinity.Settings.WorldObject.DestructibleOption;
                Trinity.Trinity.Settings.WorldObject.DestructibleOption = Trinity.Config.Combat.DestructibleIgnoreOption.DestroyAll;
            }

            return ConfirmWorld();
        }

        public GreedState InsideBossArea()
        {
            if (Trinity.Trinity.Settings.WorldObject.DestructibleOption == Trinity.Config.Combat.DestructibleIgnoreOption.DestroyAll &&
                _previousOption != Trinity.Config.Combat.DestructibleIgnoreOption.DestroyAll)
                Trinity.Trinity.Settings.WorldObject.DestructibleOption = _previousOption;

            if (ZetaDia.CurrentWorldId == 380753)
            {
                DiaObject chestObject = ZetaDia.Actors.RActorList.OfType<DiaObject>().FirstOrDefault(r => r.ActorSNO == _GreedChestSNO);

                if (chestObject != null)
                    return GreedState.BossDead;
            }

            return ConfirmWorld();
        }

        public GreedState BossDead()
        {
            if (_greedChestTimer == null)
            {
                _greedChestTimer = new Stopwatch();
                _greedChestTimer.Start();
            }
            else if (_greedChestTimer.Elapsed.Seconds > 20)
            {
                _greedChestTimer.Stop();
                _greedChestTimer = null;

                LoadProfile(_currentProfile.Path);

                return GreedState.Done;
            }

            return ConfirmWorld();
        }

        private GreedState ConfirmWorld()
        {
            switch (ZetaDia.CurrentWorldId)
            {
                case 379962:
                    return GreedState.InsidePortal;
                case 380753:
                    return GreedState.InBossArea;
                default:
                    return state;
            }
        }

        /// <summary>
        /// Borrowed From Trinity 2, 1, 21
        /// Gets the default weapon power based on the current equipped primary weapon
        /// </summary>
        /// <returns></returns>
        private SNOPower DefaultWeaponPower
        {
            get
            {
                ACDItem lhItem = ZetaDia.Me.Inventory.Equipped.FirstOrDefault(i => i.InventorySlot == InventorySlot.LeftHand);
                if (lhItem == null)
                    return SNOPower.None;

                switch (lhItem.ItemType)
                {
                    default:
                        return SNOPower.Weapon_Melee_Instant;

                    case ItemType.Axe:
                    case ItemType.CeremonialDagger:
                    case ItemType.Dagger:
                    case ItemType.Daibo:
                    case ItemType.FistWeapon:
                    case ItemType.Mace:
                    case ItemType.Polearm:
                    case ItemType.Spear:
                    case ItemType.Staff:
                    case ItemType.Sword:
                    case ItemType.MightyWeapon:
                        return SNOPower.Weapon_Melee_Instant;

                    case ItemType.Wand:
                        return SNOPower.Weapon_Ranged_Wand;

                    case ItemType.Bow:
                    case ItemType.Crossbow:
                    case ItemType.HandCrossbow:
                        return SNOPower.Weapon_Ranged_Projectile;
                }
            }
        }

        /// <summary>
        /// Borrowed From Trinity 2, 1, 21
        /// Gets the default weapon distance based on the current equipped primary weapon
        /// </summary>
        /// <returns></returns>
        private float DefaultWeaponDistance
        {
            get
            {
                switch (DefaultWeaponPower)
                {
                    case SNOPower.Weapon_Ranged_Instant:
                    case SNOPower.Weapon_Ranged_Projectile:
                        return 65f;

                    case SNOPower.Weapon_Ranged_Wand:
                        return 55f;

                    default:
                        return 10f;
                }
            }
        }

        public void LoadProfile(string profilePath, bool pauseAfterLoad = true, double pauseDuration = 10)
        {
            ProfileManager.Load(profilePath);

            if (pauseAfterLoad)
                PauseBot(pauseDuration);
        }

        public void PauseBot(double seconds = 0, double milliseconds = 0)
        {
            if (seconds > 0)
                BotMain.PauseFor(TimeSpan.FromSeconds(seconds));
            else
                BotMain.PauseFor(TimeSpan.FromMilliseconds(milliseconds));
        }
    }

    public static class Logger
    {
        private static readonly log4net.ILog Logging = Zeta.Common.Logger.GetLoggerInstanceForType();

        public static void Log(string message, params object[] args)
        {
            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            Logging.InfoFormat("[VaultRunner - Greeds Domain] => " + string.Format(message, args), type.Name);
        }

        public static void Log(string message)
        {
            Log(message, string.Empty);
        }
    }

    public enum GreedState
    {
        LookingForPortal,
        FoundPortal,
        InsidePortal,
        InBossArea,
        BossDead,
        Done
    }
}