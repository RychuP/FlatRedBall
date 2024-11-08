﻿using System;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using System.IO;
using System.Collections.Generic;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.IO;
using System.Linq;
using OfficialPlugins.FrbSourcePlugin.Views;
using OfficialPlugins.FrbSourcePlugin.ViewModels;
using FlatRedBall.Glue.MVVM;
using GeneralResponse = ToolsUtilities.GeneralResponse;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.FrbSourcePlugin.Managers;
using System.Threading.Tasks;

namespace PluginTestbed.GlobalContentManagerPlugins
{
    #region FrbOrGum enum

    public enum FrbOrGum
    {
        Frb,
        Gum
    }

    #endregion

    #region ProjectReference Class

    public class ProjectReference
    {
        public FrbOrGum ProjectRootType;
        public string RelativeProjectFilePath;
        public Guid ProjectTypeId;
        public Guid ProjectId;
        public string ProjectName;
        public List<VSSolution.SharedProject> SharedProjects;
        public List<string> ProjectConfigurations;
        public List<string> SolutionConfigurations;

        public override string ToString()
        {
            return ProjectName;
        }
    }

    #endregion

    [Export(typeof(PluginBase))]
    public class FrbSourcePlugin : PluginBase
    {
        #region Fields/Properties

        private PluginTab Tab;
        private AddFrbSourceView control;
        private AddFrbSourceViewModel ViewModel;

        private ToolStripMenuItem miLinkSource;

        public override string FriendlyName => "FRB Source";

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            miLinkSource.Owner.Items.Remove(miLinkSource);

            this.ReactToLoadedGlux -= HandleGluxLoaded;
            this.ReactToUnloadedGlux -= HandleGluxUnloaded;

            return true;
        }

        public override void StartUp()
        {
            miLinkSource = this.AddMenuItemTo(
                Localization.Texts.LinkGameToFrbSource, 
                Localization.MenuIds.LinkGameToFrbSourceId, 
                ShowGameToGlueSourceTab, 
                Localization.MenuIds.ProjectId);

            miLinkSource.Enabled = false;

            this.ReactToLoadedGlux += HandleGluxLoaded;
            this.ReactToUnloadedGlux += HandleGluxUnloaded;
        }

        private void HandleGluxUnloaded()
        {
            miLinkSource.Enabled = false;
        }

        private void HandleGluxLoaded()
        {
            var mainProject = GlueState.Self.CurrentMainProject;
            if (mainProject is MonoGameDesktopGlBaseProject 
                or FnaDesktopProject or AndroidProject 
                or IosMonogameProject or Xna4Project or AndroidMonoGameNet8Project 
                or IosMonoGameNet8Project or KniWebProject)
            {
                miLinkSource.Enabled = true;
            }
        }

        private void ShowGameToGlueSourceTab()
        {
            CreateTabIfNecessary();

            // Github for desktop has a standard folder for source files, so let's default to that if it exists

            if (System.IO.Directory.Exists(AddSourceManager.DefaultFrbFilePath))
            {
                ViewModel.FrbRootFolder = AddSourceManager.DefaultFrbFilePath;
            }
            if (System.IO.Directory.Exists(AddSourceManager.DefaultGumFilePath))
            {
                ViewModel.GumRootFolder = AddSourceManager.DefaultGumFilePath;
            }

            var alreadyLinked = GlueState.Self.CurrentMainProject.IsFrbSourceLinked();
            ViewModel.AlreadyLinkedMessageVisibility = alreadyLinked.ToVisibility();

            Tab.Show();
            Tab.Focus();

        }

        private void CreateTabIfNecessary()
        {
            if (Tab != null) 
                return;

            ViewModel = new AddFrbSourceViewModel();
            
            control = new AddFrbSourceView();
            control.DataContext = ViewModel;
            control.LinkToSourceClicked += async () =>
            {
                GlueCommands.Self.DialogCommands.ShowToast("Adding Source...", TimeSpan.FromSeconds(999));
                await AddSourceManager.HandleLinkToSourceClicked(ViewModel);
                Tab.Hide();
                GlueCommands.Self.DialogCommands.HideToast();

            };
            Tab = CreateTab(control, "Add FRB Source");
        }

        public bool HasFrbAndGumReposInDefaultLocation() => 
            System.IO.Directory.Exists(AddSourceManager.DefaultFrbFilePath) &&
            System.IO.Directory.Exists(AddSourceManager.DefaultGumFilePath);

        public async Task AddFrbSourceToDefaultLocation(VisualStudioProject visualStudioProject)
        {
            await AddSourceManager.LinkToSourceUsingDefaults(visualStudioProject);
        }
    }
}
