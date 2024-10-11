﻿using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.CodeGeneration.Game1;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace GameCommunicationPlugin.GlueControl.CodeGeneration
{
    public class Game1GlueControlGenerator : Game1CodeGenerator
    {
        public bool GenerateConnectionOnlyInDebug { get; set; } = true;
        private void AddIfDebug(ICodeBlock codeBlock)
        {
            if (GenerateConnectionOnlyInDebug)
            {
                codeBlock.Line("#if DEBUG");
            }
        }


        private void EndIfDebug(ICodeBlock codeBlock)
        {
            if (GenerateConnectionOnlyInDebug)
            {
                codeBlock.Line("#endif");
            }
        }

        public bool IsGlueControlManagerGenerationEnabled { get; set; }
        public int PortNumber { get; set; }
        public override void GenerateClassScope(ICodeBlock codeBlock)
        {
            if(IsGlueControlManagerGenerationEnabled)
            {
                AddIfDebug(codeBlock);

                codeBlock.Line("GlueControl.GlueControlManager glueControlManager;");

                EndIfDebug(codeBlock);

            }
        }

        public override void GenerateInitialize(ICodeBlock codeBlock)
        {
            GenerateCameraSetup(codeBlock);

            GenerateGlueControlManagerInitialize(codeBlock);

            GenerateStartScreen(codeBlock);
        }

        private void GenerateCameraSetup(ICodeBlock codeBlock)
        {
            if(HasStartupInGeneratedGame)
            {
                codeBlock.Line("var args = System.Environment.GetCommandLineArgs();");
                codeBlock.Line("bool? changeResize = null;");
                codeBlock.Line("var resizeArgs = args.FirstOrDefault(item => item.StartsWith(\"AllowWindowResizing=\"));");
                codeBlock.Line("if (!string.IsNullOrEmpty(resizeArgs))");
                codeBlock.Line("{");
                codeBlock.Line("    var afterEqual = resizeArgs.Split('=')[1];");
                codeBlock.Line("    changeResize = bool.Parse(afterEqual);");
                codeBlock.Line("}");
                codeBlock.Line("if (changeResize != null)");
                codeBlock.Line("{");
                codeBlock.Line("    CameraSetup.Data.AllowWindowResizing = changeResize.Value;");
                codeBlock.Line("}");

                codeBlock.Line("CameraSetup.SetupCamera(FlatRedBall.Camera.Main, graphics);");

                codeBlock.Line("#if WEB");
                codeBlock.Line("global::FlatRedBall.FlatRedBallServices.ForceClientSizeUpdates();");
                codeBlock.Line("#endif");

                codeBlock.Line("#if GameCanStartInEditMode || REFERENCES_FRB_SOURCE");
                codeBlock.Line("var isInEditMode = args.FirstOrDefault(item => item.StartsWith(\"IsInEditMode=\"));");
                codeBlock.Line("if (!string.IsNullOrEmpty(isInEditMode))");
                codeBlock.Line("{");
                
                codeBlock.Line("    //I started working on this, but decided to drop it because it's more complicated than simply setting");
                codeBlock.Line("    //IsNextScreenInEditMode. The code that handles SetEditMode dto needs to run, which is a bigger refator. ");
                codeBlock.Line("    //I'll keep this code in here for now, and return later when I'm ready to do a bigger refactor. ");
                codeBlock.Line("    //var afterEqual = isInEditMode.Split('=')[1];");
                codeBlock.Line("    //FlatRedBall.Screens.ScreenManager.IsNextScreenInEditMode = bool.Parse(afterEqual);");
                codeBlock.Line("    //this.IsMouseVisible = true;");
                codeBlock.Line("}");
                codeBlock.Line("#endif");


            }
        }

        string GetStartupScreenUnqualified()
        {
            ScreenSave requiredScreen = null;

            var project = GlueState.Self.CurrentGlueProject;
            for (int i = 0; i < project.Screens.Count; i++)
            {
                ScreenSave screenSave = project.Screens[i];

                if (screenSave.IsRequiredAtStartup)
                {
                    requiredScreen = screenSave;
                    break;
                }
            }

            var screenName = requiredScreen?.Name ?? project.StartUpScreen;

            return screenName;
        }

        bool HasStartupInGeneratedGame =>
            GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.StartupInGeneratedGame;

        private void GenerateStartScreen(ICodeBlock codeBlock)
        {
            if(HasStartupInGeneratedGame)
            {
                var project = GlueState.Self.CurrentGlueProject;
                var startUpScreen = GetStartupScreenUnqualified();
                if(!string.IsNullOrEmpty(startUpScreen))
                {
                    startUpScreen = "global::" + GlueState.Self.ProjectNamespace + "." + startUpScreen.Replace("\\", ".");
                    codeBlock.Line($"System.Type startScreenType = typeof({startUpScreen});");

                }
                else
                {
                    codeBlock.Line("System.Type startScreenType = null;");
                }

                codeBlock.Line("var commandLineArgs = System.Environment.GetCommandLineArgs();");
                var ifBlock = codeBlock.If("commandLineArgs.Length > 0");
                {
                    ifBlock.Line("var thisAssembly = this.GetType().Assembly;");
                    ifBlock.Line("// see if any of these are screens:");
                    var foreachBlock = ifBlock.ForEach("var item in commandLineArgs");
                    {
                        foreachBlock.Line("var type = thisAssembly.GetType(item);");

                        var innerIf = foreachBlock.If("type != null");
                        {
                            innerIf.Line("startScreenType = type;");
                            innerIf.Line("break;");
                        }
                    }
                }

                var startScreenIf = codeBlock.If("startScreenType != null");
                {
                    startScreenIf.Line("FlatRedBall.Screens.ScreenManager.Start(startScreenType);");
                }
            }
        }

        private void GenerateGlueControlManagerInitialize(ICodeBlock codeBlock)
        {
            if (IsGlueControlManagerGenerationEnabled)
            {
                AddIfDebug(codeBlock);
                codeBlock.Line($"glueControlManager = new GlueControl.GlueControlManager({PortNumber})");
                codeBlock.Line("{");
                codeBlock.Line("    GameConnectionManager = gameConnectionManager,");
                codeBlock.Line("};");
                //codeBlock.Line("glueControlManager.Start();");
                //codeBlock.Line("this.Exiting += (not, used) => glueControlManager.Kill();");
                codeBlock.Line("FlatRedBall.FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += (not, used) =>");
                var sizeChangedInnerBlock = codeBlock.Block();
                var sizeChangedInnerBlockIf = sizeChangedInnerBlock.If("FlatRedBall.Screens.ScreenManager.IsInEditMode");
                sizeChangedInnerBlockIf.Line("GlueControl.Editing.CameraLogic.UpdateCameraToZoomLevel(zoomAroundCursorPosition: false);");
                sizeChangedInnerBlock.Line("GlueControl.Editing.CameraLogic.PushZoomLevelToEditor();");
                codeBlock.Line(";");




                EndIfDebug(codeBlock);

                if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.HasScreenManagerAfterScreenDestroyed)
                {
                    codeBlock.Line("FlatRedBall.Screens.ScreenManager.AfterScreenDestroyed += (screen) =>");
                    var innerDestroyBlock = codeBlock.Block();
                    innerDestroyBlock.Line("GlueControl.Editing.EditorVisuals.DestroyContainedObjects();");

                    codeBlock.Line(";");
                }
            }


        }
    }
}
