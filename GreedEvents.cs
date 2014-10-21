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
using QuestTools.Helpers;
using QuestTools.ProfileTags;
using QuestTools.ProfileTags.Movement;
using Zeta.Bot.Profile.Common;
using QuestTools.ProfileTags.Complex;
using Action = Zeta.TreeSharp.Action;
using Zeta.TreeSharp;
using System.Media;

namespace VaultRunner
{
    public class GreedEvents
    {
        private static readonly int _GreedPortalSNO = 393030;
        private static readonly int _GreedChestSNO = 403683;
        private Trinity.Config.Combat.DestructibleIgnoreOption _previousOption;
        public GreedState state = GreedState.LookingForPortal;

        public GreedEvents()
        {
            state = GreedState.LookingForPortal;
        }

        public void FindPortal()
        {
            if (state == GreedState.LookingForPortal)
            {
                DiaObject portalObject = ZetaDia.Actors.RActorList.OfType<DiaObject>().FirstOrDefault(r => r.ActorSNO == _GreedPortalSNO);

                if (portalObject != null)
                {
                    SoundPlayer tada = new SoundPlayer(@"C:\Windows\Media\tada.wav");
                    tada.Play();

                    Logger.Log("Found Goblin Portal - " + DateTime.Now.ToString());

                    Navigator.MoveTo(portalObject.Position);

                    PauseBot(0, 500);

                    Logger.Log("Running Goblin Profile");

                    BotBehaviorQueue.Queue(new List<ProfileBehavior>
                    {
                        new CompositeTag()
                        {
                            //<!--  set destructibles  -->
                            IsDoneWhen = ret => Trinity.Trinity.Settings.WorldObject.DestructibleOption == Trinity.Config.Combat.DestructibleIgnoreOption.DestroyAll,
                            Composite = new Action(ret =>
                            {
                                _previousOption = Trinity.Trinity.Settings.WorldObject.DestructibleOption;
                                Trinity.Trinity.Settings.WorldObject.DestructibleOption = Trinity.Config.Combat.DestructibleIgnoreOption.DestroyAll;
                                return RunStatus.Success;
                            })
                        },
                        new MoveToActorTag 
                        { 
                            //<MoveToActor questId="1" actorId="393030" isPortal="True" destinationWorldId="379962" interactRange="5"/>
                            ActorId = 393030, 
                            IsPortal = true, 
                            DestinationWorldId = 379962,
                            InteractRange = 5
                        },
                        new ExploreDungeonTag
                        {
                            //<ExploreDungeon routeMode="WeightedNearestMinimapVisited" questId="1" stepId="1" until="ExitFound" pathPrecision="60" boxSize="40" boxTolerance="0.01">
                            //  <PriorityScenes>
                            //      <PriorityScene sceneName="Exit" />
                            //  </PriorityScenes>
                            //</ExploreDungeon>
                            RouteMode = QuestTools.Navigation.RouteMode.WeightedNearestMinimapVisited,
                            EndType = ExploreDungeonTag.ExploreEndType.ObjectFound,
                            QuestId = 1,
                            StepId = 1,
                            BoxSize = 40,
                            PathPrecision = 60,
                            BoxTolerance = 0.1f,
                            ActorId = 380766                         
                        },
                        new MoveToActorTag 
                        { 
                            //<MoveToActor questId="1" actorId="380766"/>
                            ActorId = 380766, 
                            IsPortal = true,
                            InteractRange = 5,
                            DestinationWorldId = 380753,
                            Timeout = 10
                        },
                        new MoveToActorTag 
                        { 
                            //<MoveToActor questId="1" actorId="403041"/>
                            ActorId = 403041, 
                            IsPortal = false,
                            InteractRange = 5,
                            Timeout = 10,
                            MaxSearchDistance = 200
                        },
                        new SafeMoveToTag 
                        {
                            //<SafeMoveTo questId="312429" stepId="2" x="89" y="161" z="-82" pathPrecision="5" pathPointLimit="250" statusText=""/>
                            QuestId = 312429,
                            StepId = 2,
                            X = 89,
                            Y = 161,
                            Z = -82,
                            PathPrecision = 5,
                            PathPointLimit = 250
                        },
                        new WaitTimerTag 
                        { 
                            //<WaitTimer questId="1" stepId="1" waitTime="6000"/>
                            WaitTime = 6000, 
                            StepId = 1, 
                            QuestId = 1
                        },
                        new CompositeTag()
                        {
                            //<!--  boss fight  -->
                            //When => ActorExistsAt(403683, ZetaDia.Me.Position.X, ZetaDia.Me.Position.Y, ZetaDia.Me.Position.Z, 200)
                            IsDoneWhen = ret => Zeta.Bot.ConditionParser.ActorExistsAt(403683, ZetaDia.Me.Position.X, ZetaDia.Me.Position.Y, ZetaDia.Me.Position.Z, 200),
                            Composite = new Action(ret =>
                            {
                                return RunStatus.Success;
                            })
                        },
                        new MoveToActorTag 
                        {
                            //<MoveToActor questId="1" InteractRange="100" actorId="403683"/>
                            QuestId = 1,
                            InteractRange = 100,
                            ActorId = 403683
                        },
                        new WaitTimerTag 
                        {
                            //<WaitTimer questId="1" stepId="1" waitTime="5000"/>
                            WaitTime = 5000, 
                            StepId = 1, 
                            QuestId = 1
                        },
                        new TownPortalTag() 
                        { 
                            //<TrinityTownPortal questId="1" stepId="1" waitTime="1500" />
                            QuestId = 1,
                            StepId = 1,
                            WaitTime = 5000
                        },
                        new CompositeTag()
                        {
                            //<!--  set destructibles  -->
                            IsDoneWhen = ret => Trinity.Trinity.Settings.WorldObject.DestructibleOption == _previousOption,
                            Composite = new Action(ret =>
                            {
                                Trinity.Trinity.Settings.WorldObject.DestructibleOption = _previousOption;
                                return RunStatus.Success;
                            })                            
                        },
                        new ReloadProfileTag() { }
                    });

                    state = GreedState.Done;
                }
            }
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
        Done
    }
}