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
using System.Threading;
using Zeta.Bot.Logic;

namespace VaultRunner
{
    public class GreedEvents
    {
        private static readonly int _GreedPortalSNO = 393030;
        private static readonly int _GreedChestSNO = 403683;
        private static string _GreedProfile = string.Format("{0}\\Plugins\\{1}\\profile.xml", System.Environment.CurrentDirectory, "VaultRunner");
        private static string _GreedProfileBackUp = string.Format("{0}\\Plugins\\{1}\\profile.xml", System.Environment.CurrentDirectory, "GreedsDomain");
        private Profile _currentProfile;
        private Trinity.Config.Combat.DestructibleIgnoreOption _previousOption;

        public GreedState state = GreedState.LookingForPortal;

        public GreedEvents()
        {
            state = GreedState.LookingForPortal;
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
            if (ProfileManager.CurrentProfile.Path != _GreedProfile && ProfileManager.CurrentProfile.Path != _GreedProfileBackUp)
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
                {
                    Logger.Log("Boss is dead");

                    return GreedState.BossDead;
                }
            }

            return ConfirmWorld();
        }

        public GreedState BossDead()
        {
            if (ZetaDia.IsInTown && !BrainBehavior.IsVendoring)
            {
                if (_currentProfile.Path != _GreedProfile && _currentProfile.Path != _GreedProfileBackUp)
                {
                    Logger.Log("Loading previous profile: " + _currentProfile.Name);

                    LoadProfile(_currentProfile.Path);
                    Thread.Sleep(750);
                }
                else
                {
                    Logger.Log("Previous profile cannot be loaded. Stopping DB.");
                    BotMain.CurrentBot.Stop();
                    BotMain.Stop();
                }

                return GreedState.Done;
            }

            return GreedState.BossDead;
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