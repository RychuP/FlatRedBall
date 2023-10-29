﻿using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.CodeGeneration.Game1;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Gum.Wireframe;
using GumPlugin.Managers;
using SkiaGum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace GumPluginCore.CodeGeneration
{
    internal class GumGame1CodeGeneratorEarly : Game1CodeGenerator
    {
        bool hasSkia => GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.HasGumSkiaElements &&
            AppState.Self.HasAddedGumSkiaElements;

        public GumGame1CodeGeneratorEarly() => CodeLocation = FlatRedBall.Glue.Plugins.Interfaces.CodeLocation.BeforeStandardGenerated;

        public override void GenerateInitializeEarly(ICodeBlock codeBlock)
        {
            if(hasSkia && GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.HasGame1GenerateEarly)
            {
                codeBlock.Line("SkiaMonoGameRendering.SkiaGlManager.Initialize(GraphicsDevice);");
            }
        }

        public override void GenerateInitialize(ICodeBlock codeBlock)
        {
            if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.GumCommonCodeReferencing)
            {
                codeBlock.Line("global::GumRuntime.ElementSaveExtensions.CustomCreateGraphicalComponentFunc = Gum.Wireframe.RuntimeObjectCreator.TryHandleAsBaseType;");

                codeBlock.Line("global::Gum.Wireframe.GraphicalUiElement.SetPropertyOnRenderable = global::Gum.Wireframe.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;");
                codeBlock.Line("global::Gum.Wireframe.GraphicalUiElement.UpdateFontFromProperties = global::Gum.Wireframe.CustomSetPropertyOnRenderable.UpdateToFontValues;");
                codeBlock.Line("global::Gum.Wireframe.GraphicalUiElement.ThrowExceptionsForMissingFiles = global::Gum.Wireframe.CustomSetPropertyOnRenderable.ThrowExceptionsForMissingFiles;");
                codeBlock.Line("global::Gum.Wireframe.GraphicalUiElement.AddRenderableToManagers = global::Gum.Wireframe.CustomSetPropertyOnRenderable.AddRenderableToManagers;");
                codeBlock.Line("global::Gum.Wireframe.GraphicalUiElement.RemoveRenderableFromManagers = global::Gum.Wireframe.CustomSetPropertyOnRenderable.RemoveRenderableFromManagers;");

            }
            else
            {
                if (hasSkia)
                {
                    codeBlock.Line("GumRuntime.InstanceSaveExtensionMethods.CustomObjectCreation = GetSkiaType;");
                }
            }

        }

        public override void GenerateDrawEarly(ICodeBlock codeBlock)
        {
            if (hasSkia && GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.HasGame1GenerateEarly)
            {
                codeBlock.Line("SkiaMonoGameRendering.SkiaRenderer.Draw();");
            }
        }
    }

    internal class GumGame1CodeGenerator : Game1CodeGenerator
    {
        bool hasSkia => GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.HasGumSkiaElements &&
            AppState.Self.HasAddedGumSkiaElements;



        public override void GenerateInitialize(ICodeBlock codeBlock)
        {
            // handled above in GumGame1CodeGeneratorEarly
            //var fileVersion = GlueState.Self.CurrentGlueProject.FileVersion;
            //if (fileVersion >= (int)GluxVersions.HasFrameworkElementManager)
            //{
            //    codeBlock.Line("FlatRedBall.FlatRedBallServices.AddManager(FlatRedBall.Forms.Managers.FrameworkElementManager.Self);");
            //}



            //if(hasSkia)
            //{
            //    codeBlock.Line("GumRuntime.InstanceSaveExtensionMethods.CustomObjectCreation = GetSkiaType;");
            //}
        }

        public override void GenerateClassScope(ICodeBlock codeBlock)
        {
            if(hasSkia && GlueState.Self.CurrentGlueProject.FileVersion < (int)GluxVersions.GumCommonCodeReferencing)
            {
                var function = codeBlock.Function("RenderingLibrary.Graphics.IRenderable", "GetSkiaType", "string name");
                var switchStatement = function.Switch("name");
                switchStatement.CaseNoBreak("\"Arc\"").Line("return new SkiaGum.Renderables.RenderableArc();");
                switchStatement.CaseNoBreak("\"ColoredCircle\"").Line("return new SkiaGum.Renderables.RenderableCircle();");
                switchStatement.CaseNoBreak("\"RoundedRectangle\"").Line("return new SkiaGum.Renderables.RenderableRoundedRectangle();");
                function.Line("return null;");
            }
        }
    }
}
