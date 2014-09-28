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

namespace GreedsDomain
{
    public class GreedEvents
    {
        private static readonly int _GreedPortalSNO = 393030;
        private static readonly int _GreedChestSNO = 403683;
        private static string _GreedProfile = System.Environment.CurrentDirectory + "\\Plugins\\GreedsDomain\\profile.xml";
        private Profile _currentProfile;
        private Stopwatch _greedChestTimer = null;
        private List<DiaGizmo> _destructables = new List<DiaGizmo>();
        private Vector3 _previousPosition;
        private bool _positionSet = false;
        private DefaultNavigationProvider satNav = Navigator.GetNavigationProviderAs<DefaultNavigationProvider>();

        public GreedState state = GreedState.LookingForPortal;

        public GreedEvents()
        {
            state = GreedState.LookingForPortal;
            _destructables = new List<DiaGizmo>();
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
            if (ProfileManager.CurrentProfile.Path != _GreedProfile)
            {
                Logger.Log("Loading Greed Profile - " + DateTime.Now.ToString());

                _currentProfile = ProfileManager.CurrentProfile;
                LoadProfile(_GreedProfile, true, 1);
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

            if (!_positionSet)
                _previousPosition = ZetaDia.Me.Position;

            if (_destructables.Count == 0 || ZetaDia.Me.Position.Distance2D(_previousPosition) > 35)
            {
                List<DiaGizmo> currentList = ZetaDia.Actors.RActorList.OfType<DiaGizmo>()
                     .Where(x => x.IsDestructibleObject) //Fuck it - Destroy anything and everything in here!
                     .ToList<DiaGizmo>();

                foreach (DiaGizmo obj in currentList)
                {
                    if (!_destructables.Contains(obj))
                        _destructables.Add(obj);
                }

                //re-order based on distance away

                _destructables.OrderBy(x => ZetaDia.Me.Position.Distance2D(x.Position));

                //Reset current position
                if (ZetaDia.Me.Position.Distance2D(_previousPosition) > 35)
                    _previousPosition = ZetaDia.Me.Position;
            }                       

            if (!ZetaDia.Me.IsInCombat && _destructables.Count > 0)
            {
                bool allDestroyed = true;
                for (int i = 0; i < _destructables.Count; i++)
                {
                    if (ZetaDia.Actors.RActorList.OfType<DiaGizmo>().Contains(_destructables[i]))
                    {
                        allDestroyed = false;
                        while (!(ZetaDia.Me.Position.Distance2D(_destructables[i].Position) < DefaultWeaponDistance) && !_destructables[i].InLineOfSight 
                            && !ZetaDia.Me.IsInCombat)
                        {
                            satNav.MoveTo(_destructables[i].Position, null, true);

                            PauseBot(0, 500);
                        }

                        if ((ZetaDia.Me.Position.Distance2D(_destructables[i].Position) < DefaultWeaponDistance) && _destructables[i].InLineOfSight)
                        {
                            ZetaDia.Me.UsePower(DefaultWeaponPower, _destructables[i].Position, -1, _destructables[i].ACDGuid);
                            PauseBot(0, 300);
                        }

                        break;
                    }
                }

                if (allDestroyed)
                    _destructables.Clear();
            }

            return ConfirmWorld();
        }

        public GreedState InsideBossArea()
        {
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

        #region OldCode

        //public void PortalCheck()
        //{
        //    if (!_portalFound)
        //    {
        //        DiaObject portalObject = ZetaDia.Actors.RActorList.OfType<DiaObject>().FirstOrDefault(r => r.ActorSNO == _GreedPortalSNO);

        //        if (portalObject != null)
        //        {
        //            Navigator.MoveTo(portalObject.Position);

        //            _portalFound = true;

        //            Logger.Log("Found Goblin Portal - " + DateTime.Now.ToString());

        //            Logger.Log("Loading Greed Profile - " + DateTime.Now.ToString());

        //            PauseBot(5);

        //            _currentProfile = ProfileManager.CurrentProfile;

        //            ProfileManager.Load(_GreedProfile);

        //            _greedloaded = true;

        //        }
        //    }

        //    if (_greedloaded && !_greedcomplete && ZetaDia.CurrentWorldId == 380753)
        //    {
        //        DiaObject chestObject = ZetaDia.Actors.RActorList.OfType<DiaObject>().FirstOrDefault(r => r.ActorSNO == _GreedChestSNO);

        //        if (chestObject != null)
        //        {
        //            if (_greedChestTimer == null)
        //            {
        //                _greedChestTimer = new Stopwatch();
        //                _greedChestTimer.Start();
        //            }
        //            else if (_greedChestTimer.Elapsed.Seconds > 20)
        //            {
        //                ProfileManager.Load(_currentProfile.Path);
        //                PauseBot(15);

        //                _greedcomplete = true;

        //                _greedChestTimer.Stop();
        //                _greedChestTimer = null;
        //            }
        //        }
        //    }
        //}

        #endregion OldCode
    }

    public static class Logger
    {
        private static readonly log4net.ILog Logging = Zeta.Common.Logger.GetLoggerInstanceForType();

        public static void Log(string message, params object[] args)
        {
            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            Logging.InfoFormat("[Greeds Domain] => " + string.Format(message, args), type.Name);
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