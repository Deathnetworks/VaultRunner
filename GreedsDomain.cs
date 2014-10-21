﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Zeta.Bot;
using Zeta.Bot.Navigation;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.Game;

namespace VaultRunner
{
    public class GreedsDomain : IPlugin
    {
        private static GreedEvents eventHandler;

        public string Author
        {
            get { return "SmurfX"; }
        }

        public string Description
        {
            get { return "Greed's Domain"; }
        }

        public string Name
        {
            get { return "Vault Runner"; }
        }

        public void OnDisabled()
        {
            Logger.Log("Plugin - Disabled");
            eventHandler = null;
            GameEvents.OnGameJoined -= GameEvents_OnGameJoined;
        }

        public void OnEnabled()
        {
            eventHandler = new GreedEvents();
            Logger.Log("Plugin - Enabled");
            GameEvents.OnGameJoined += GameEvents_OnGameJoined;            
        }

        void GameEvents_OnGameJoined(object sender, EventArgs e)
        {
            eventHandler = new GreedEvents();
        }

        public void OnInitialize() { }

        public void OnPulse()
        {
            if (ZetaDia.Me == null)
                return;

            if (!ZetaDia.IsInGame || !ZetaDia.Me.IsValid || ZetaDia.IsLoadingWorld || ZetaDia.IsPlayingCutscene || ZetaDia.WorldType != Act.OpenWorld)
                return;

            if (eventHandler == null)
                eventHandler = new GreedEvents();
            
            if (eventHandler.state == GreedState.LookingForPortal)
            {
                eventHandler.FindPortal();
            }

            return;
        }

        public void OnShutdown()
        {
            eventHandler = null;
            GameEvents.OnGameJoined -= GameEvents_OnGameJoined;
        }

        Version IPlugin.Version
        {
            get { return new Version(1, 0, 3); }
        }

        public bool Equals(IPlugin other)
        {
            return other.Name == Name;
        }

        public Window DisplayWindow
        {
            get { return null; }
        }
    }
}
