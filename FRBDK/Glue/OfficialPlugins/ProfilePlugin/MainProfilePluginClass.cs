﻿using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using OfficialPlugins.ProfilePlugin.Manager;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace OfficialPlugins.ProfilePlugin
{
    [Export(typeof(PluginBase))]
    public class MainProfilePluginClass : PluginBase
    {
        public override string FriendlyName => "Profile Plugin";

        public override Version Version => new Version(1, 0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            AssignEvents();
        }

        private void AssignEvents()
        {
            this.AddMenuItemTo("Add ProfileManager", Localization.MenuIds.AddProfileManagerId, HandleAddProfileManager, Localization.MenuIds.PluginId);
        }

        private void HandleAddProfileManager(object sender, EventArgs e)
        {
            CodeItemAdder.Self.UpdateCodePresenceInProject();

            GlueCommands.Self.ProjectCommands.AddNugetIfNotAdded("Newtonsoft.Json", "13.0.3");

            GlueCommands.Self.ProjectCommands.SaveProjects();
        }
    }
}
