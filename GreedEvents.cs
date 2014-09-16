using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zeta.Bot;
using Zeta.Bot.Navigation;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.Game;
using Zeta.Game.Internals.Actors;
using Zeta.Bot.Profile;
using System.Diagnostics;

namespace GreedsDomain
{
    public class GreedEvents
    {
        private static readonly int _GreedPortalSNO = 393030;
        private static readonly int _GreedChestSNO = 403683;
        private static string _GreedProfile = System.Environment.CurrentDirectory + "\\Plugins\\GreedsDomain\\profile.xml";
        private bool _portalFound = false;
        private bool _greedloaded = false;
        private bool _greedcomplete = false;
        private Profile _currentProfile;
        private Stopwatch _greedChestTimer = null;

        public GreedEvents()
        {
            _portalFound = false;
            _greedloaded = false;
            _greedcomplete = false;
        }

        public void PortalCheck()
        {
            if (!_portalFound)
            {
                DiaObject portalObject = ZetaDia.Actors.RActorList.OfType<DiaObject>().FirstOrDefault(r => r.ActorSNO == _GreedPortalSNO);

                if (portalObject != null)
                {
                    Navigator.MoveTo(portalObject.Position);

                    _portalFound = true;

                    Logger.Log("Found Goblin Portal - " + DateTime.Now.ToString());

                    Logger.Log("Loading Greed Profile - " + DateTime.Now.ToString());

                    PauseBot(5);

                    _currentProfile = ProfileManager.CurrentProfile;

                    ProfileManager.Load(_GreedProfile);

                    _greedloaded = true;


                }
            }

            if (_greedloaded && !_greedcomplete && ZetaDia.CurrentWorldId == 380753)
            {
                DiaObject chestObject = ZetaDia.Actors.RActorList.OfType<DiaObject>().FirstOrDefault(r => r.ActorSNO == _GreedChestSNO);

                if (chestObject != null)
                {
                    if (_greedChestTimer == null)
                    {
                        _greedChestTimer = new Stopwatch();
                        _greedChestTimer.Start();
                    }
                    else if (_greedChestTimer.Elapsed.Seconds > 20)
                    {
                        ProfileManager.Load(_currentProfile.Path);
                        PauseBot(15);

                        _greedcomplete = true;

                        _greedChestTimer.Stop();
                        _greedChestTimer = null;
                    }
                }
            }
        }

        public void PauseBot(double seconds)
        {
            BotMain.PauseFor(TimeSpan.FromSeconds(seconds));
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

            Logging.InfoFormat("[Greeds Domain] => " + string.Format(message, args), type.Name);
        }
        public static void Log(string message)
        {
            Log(message, string.Empty);
        }
    }
}
