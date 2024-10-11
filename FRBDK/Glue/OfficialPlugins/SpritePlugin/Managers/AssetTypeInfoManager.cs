﻿using FlatRedBall;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ICollidablePlugins;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math;
using GlueFormsCore.ViewModels;
using OfficialPlugins.Common.Controls;
using OfficialPlugins.SpritePlugin.Views;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi.Controls;
using WpfDataUiCore.Controls;

namespace OfficialPlugins.SpritePlugin.Managers
{
    internal class AssetTypeInfoManager
    {
        public static void HandleStartup()
        {
            AddSpriteColorAtiVariables();
            AddTextureCoordinateVariables();
            AddCreateNewAchxButton();
        }

        internal static void HandleGluxLoaded()
        {
            AdjustIgnoreAnimationVariables();

            var shouldHaveAddSetCollisionFromAnimation = 
                GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.SpriteHasSetCollisionFromAnimation;

            if(shouldHaveAddSetCollisionFromAnimation)
            {
                if(!AlreadyHasAddSetCollisionFromAnimation())
                {
                    AddSetCollisionFromAnimation();
                }
            }
            else
            {
                if(AlreadyHasAddSetCollisionFromAnimation())
                {
                    var ati = AvailableAssetTypes.CommonAtis.Sprite;
                    ati.VariableDefinitions.RemoveAll(item => item.Name == GetSetCollisionFromAnimationVariableDefinition().Name);
                }
            }
        }

        #region Color (Hex)

        public static void AddSpriteColorAtiVariables()
        {
            var ati = AvailableAssetTypes.CommonAtis.Sprite;

            var textureVariable = ati.VariableDefinitions.FirstOrDefault(item => item.Name == "Texture");
            if (textureVariable != null)
            {
                textureVariable.PreferredDisplayer = typeof(EditableComboBoxDisplay);
            }

            var redVariableDefinition = ati.VariableDefinitions.Find(item => item.Name == "Red");
            redVariableDefinition.PreferredDisplayer = typeof(SliderDisplay);
            redVariableDefinition.PropertiesToSetOnDisplayer[nameof(SliderDisplay.DisplayedValueMultiplier)] = 255.0;
            redVariableDefinition.PropertiesToSetOnDisplayer[nameof(SliderDisplay.DecimalPointsFromSlider)] = 0;


            var greenVariableDefinition = ati.VariableDefinitions.Find(item => item.Name == "Green");
            greenVariableDefinition.PreferredDisplayer = typeof(SliderDisplay);
            greenVariableDefinition.PropertiesToSetOnDisplayer[nameof(SliderDisplay.DisplayedValueMultiplier)] = 255.0;
            greenVariableDefinition.PropertiesToSetOnDisplayer[nameof(SliderDisplay.DecimalPointsFromSlider)] = 0;

            var blueVariableDefinition = ati.VariableDefinitions.Find(item => item.Name == "Blue");
            blueVariableDefinition.PreferredDisplayer = typeof(SliderDisplay);
            blueVariableDefinition.PropertiesToSetOnDisplayer[nameof(SliderDisplay.DisplayedValueMultiplier)] = 255.0;
            blueVariableDefinition.PropertiesToSetOnDisplayer[nameof(SliderDisplay.DecimalPointsFromSlider)] = 0;

            var blueIndex = ati.VariableDefinitions.IndexOf(blueVariableDefinition);

            var colorHexValueDefinition = new VariableDefinition();
            colorHexValueDefinition.Name = "ColorHex";
            colorHexValueDefinition.Category = "Appearance";
            colorHexValueDefinition.DefaultValue = null;
            colorHexValueDefinition.Type = "string";
            colorHexValueDefinition.UsesCustomCodeGeneration = true;
            colorHexValueDefinition.PreferredDisplayer = typeof(ColorHexTextBox);
            colorHexValueDefinition.CustomVariableGet = ColorHexVariableGet;
            colorHexValueDefinition.CustomVariableSet = (element, nos, variableName, newValue) => _=ColorHexVariableSet(element, nos, variableName, newValue);
            ati.VariableDefinitions.Insert(blueIndex + 1, colorHexValueDefinition);

        }

        private static async Task ColorHexVariableSet(GlueElement element, NamedObjectSave nos, string variableName, object newValue)
        {
            var colorConverter = new ColorConverter();
            var newValueAsString = newValue as string;
            if (!string.IsNullOrEmpty(newValueAsString))
            {
                if (!newValueAsString.StartsWith("#"))
                {
                    newValueAsString = "#" + newValueAsString;
                }
                try
                {
                    string redVariableName, greenVariableName, blueVariableName;
                    GetRedGreenBlueVariableNames(nos, variableName, out redVariableName, out greenVariableName, out blueVariableName);

                    if (!string.IsNullOrEmpty(redVariableName) && !string.IsNullOrEmpty(greenVariableName) &&
                        !string.IsNullOrEmpty(blueVariableName))
                    {
                        var color = (Color)colorConverter.ConvertFromString(newValueAsString);
                        await GlueCommands.Self.GluxCommands.SetVariableOnAsync(nos, redVariableName, color.R / 255.0f, performSaveAndGenerateCode: false, updateUi: false);
                        await GlueCommands.Self.GluxCommands.SetVariableOnAsync(nos, greenVariableName, color.G / 255.0f, performSaveAndGenerateCode: false, updateUi: false);
                        await GlueCommands.Self.GluxCommands.SetVariableOnAsync(nos, blueVariableName, color.B / 255.0f, performSaveAndGenerateCode: true, updateUi: true);
                    }
                }
                catch
                {
                    // do we want to do anything?
                }

            }
        }

        private static object ColorHexVariableGet(GlueElement element, NamedObjectSave nos, string variableName)
        {
            string redVariableName, greenVariableName, blueVariableName;
            GetRedGreenBlueVariableNames(nos, variableName, out redVariableName, out greenVariableName, out blueVariableName);

            if (!string.IsNullOrEmpty(redVariableName) && !string.IsNullOrEmpty(greenVariableName) &&
                !string.IsNullOrEmpty(blueVariableName))
            {
                var red = ((ObjectFinder.Self.GetValueRecursively(nos, element, redVariableName) as float?) ?? 0) * 255;
                var green = ((ObjectFinder.Self.GetValueRecursively(nos, element, greenVariableName) as float?) ?? 0) * 255;
                var blue = ((ObjectFinder.Self.GetValueRecursively(nos, element, blueVariableName) as float?) ?? 0) * 255;

                var redInt = MathFunctions.RoundToInt(red);
                var greenInt = MathFunctions.RoundToInt(green);
                var blueInt = MathFunctions.RoundToInt(blue);

                // source: https://stackoverflow.com/questions/39137486/converting-colour-name-to-hex-in-c-sharp
                var hexValue = $"{redInt:X2}{greenInt:X2}{blueInt:X2}";
                return hexValue;
            }


            return "";
        }

        private static void GetRedGreenBlueVariableNames(NamedObjectSave nos, string variableName, out string redVariableName, out string greenVariableName, out string blueVariableName)
        {
            var nosAti = nos.GetAssetTypeInfo();
            redVariableName = null;
            greenVariableName = null;
            blueVariableName = null;
            if (nosAti == AvailableAssetTypes.CommonAtis.Sprite)
            {
                redVariableName = "Red";
                greenVariableName = "Green";
                blueVariableName = "Blue";
            }
            else if (nos.SourceType == SourceType.Entity && variableName != null)
            {
                var entityType = ObjectFinder.Self.GetElement(nos);
                if (entityType != null)
                {
                    var foundVariable = entityType.CustomVariables.Find(item => item.Name == variableName);

                    if (foundVariable != null)
                    {
                        var objectInEntity = entityType.GetNamedObject(foundVariable.SourceObject);

                        if (objectInEntity?.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Sprite)
                        {
                            redVariableName = entityType.CustomVariables.FirstOrDefault(item => item.SourceObject == objectInEntity.InstanceName && item.SourceObjectProperty == "Red")?.Name;
                            greenVariableName = entityType.CustomVariables.FirstOrDefault(item => item.SourceObject == objectInEntity.InstanceName && item.SourceObjectProperty == "Green")?.Name;
                            blueVariableName = entityType.CustomVariables.FirstOrDefault(item => item.SourceObject == objectInEntity.InstanceName && item.SourceObjectProperty == "Blue")?.Name;

                        }
                    }

                }
            }
        }

        #endregion

        #region Texture Coordinates

        public static void AddTextureCoordinateVariables()
        {
            var ati = AvailableAssetTypes.CommonAtis.Sprite;

            var mapSpriteTextureVariable = new VariableDefinition();
            mapSpriteTextureVariable.PreferredDisplayer = typeof(MapTextureButtonContainer);
            mapSpriteTextureVariable.UsesCustomCodeGeneration = true;
            mapSpriteTextureVariable.Type = "string"; // not used
            mapSpriteTextureVariable.Name = "MapSpriteTexturePlaceholder";
            mapSpriteTextureVariable.Category = "Texture";
            var variableToAddAfter = ati.VariableDefinitions.FirstOrDefault(item => item.Name == nameof(Sprite.Texture));
            var index = ati.VariableDefinitions.IndexOf(variableToAddAfter);
            ati.VariableDefinitions.Insert(index + 1, mapSpriteTextureVariable);
        }

        #endregion

        #region SetCollisionFromAnimation

        public static void AddSetCollisionFromAnimation()
        {
            var ati = AvailableAssetTypes.CommonAtis.Sprite;

            var variableDefinition =
                GetSetCollisionFromAnimationVariableDefinition();

            var variableToAddAfter = ati.VariableDefinitions.FirstOrDefault(item => item.Name == nameof(Sprite.CurrentChainName));
            var index = ati.VariableDefinitions.IndexOf(variableToAddAfter);
            ati.VariableDefinitions.Insert(index + 1, variableDefinition);

            var createNewShapes =
                GetCreateMissingShapesDefinition();

            ati.VariableDefinitions.Insert(index+2, createNewShapes);

        }

        static VariableDefinition setCollisionFromAnimationVariableDefinition;
        const string SetCollisionFromAnimationVariableName = nameof(Sprite.SetCollisionFromAnimation);
        public static VariableDefinition GetSetCollisionFromAnimationVariableDefinition()
        {
            if(setCollisionFromAnimationVariableDefinition == null)
            {
                setCollisionFromAnimationVariableDefinition = new VariableDefinition();
                setCollisionFromAnimationVariableDefinition.Name = SetCollisionFromAnimationVariableName;
                setCollisionFromAnimationVariableDefinition.Category = "Animation";
                setCollisionFromAnimationVariableDefinition.DefaultValue = "false";
                setCollisionFromAnimationVariableDefinition.Type = "bool";
                setCollisionFromAnimationVariableDefinition.UsesCustomCodeGeneration = true;
                setCollisionFromAnimationVariableDefinition.SubtextFunc = (element, nos) =>
                {
                    if(element is EntitySave && element.IsICollidableRecursive() == false)
                    {
                        return $"{element.GetStrippedName()} must be ICollidable or must inherit from an ICollidable entity for animation collisions to be applied automatically.";
                    }
                    return string.Empty;
                };
            }
            return setCollisionFromAnimationVariableDefinition;
        }


        static VariableDefinition createMissingShapesDefinition;
        public static VariableDefinition GetCreateMissingShapesDefinition()
        {
            if(createMissingShapesDefinition == null)
            {
                createMissingShapesDefinition = new VariableDefinition();
                createMissingShapesDefinition.Name = "CreateMissingAnimationShapes";
                createMissingShapesDefinition.Category = "Animation";
                createMissingShapesDefinition.DefaultValue = "false";
                createMissingShapesDefinition.Type = "bool";
                createMissingShapesDefinition.UsesCustomCodeGeneration = true;
                createMissingShapesDefinition.IsVariableVisibleInEditor = (element, nos) =>
                {
                    // Only show this if the NOS has SetCollisionFromAnimation set to true
                    var foundVariable = nos.GetCustomVariable(SetCollisionFromAnimationVariableName);

                    return foundVariable != null && foundVariable.Value as bool? == true;
                };
            }

            return createMissingShapesDefinition;
        }

        static bool AlreadyHasAddSetCollisionFromAnimation()
        {
            var ati = AvailableAssetTypes.CommonAtis.Sprite;
            return ati.VariableDefinitions.Any(item => item.Name == GetSetCollisionFromAnimationVariableDefinition().Name);
        }

        #endregion

        #region Create new ACHX

        private static void AddCreateNewAchxButton()
        {
            var ati = AvailableAssetTypes.CommonAtis.Sprite;

            var createNewAchxVariable = new VariableDefinition();

            createNewAchxVariable.PreferredDisplayer = typeof(CreateNewAchxButton);
            createNewAchxVariable.UsesCustomCodeGeneration = true;
            createNewAchxVariable.Type = "string"; // not used
            createNewAchxVariable.Name = "CreateNewAchxButtonPlaceholder";
            createNewAchxVariable.Category = "Animation";
            createNewAchxVariable.IsVariableVisibleInEditor = (element, nos) =>
            {
                // does this or any derived element have an .achx file?

                var alreadyHasAchx = element.GetAllReferencedFileSavesRecursively()
                    .Any(item => item.Name.ToLower().EndsWith(".achx"));

                return !alreadyHasAchx;
            };

            var variableToAddBefore = ati.VariableDefinitions.FirstOrDefault(item => item.Name == nameof(Sprite.AnimationChains));
            var index = ati.VariableDefinitions.IndexOf(variableToAddBefore);
            ati.VariableDefinitions.Insert(index, createNewAchxVariable);



        }

        #endregion

        private static void AdjustIgnoreAnimationVariables()
        {
            var shouldHaveUseAnimationTextureFlip =
                GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.SpriteHasUseAnimationTextureFlip;

            var ati = AvailableAssetTypes.CommonAtis.Sprite;

            var existingUseAnimationTextureVariableDefinition = ati.VariableDefinitions
                .FirstOrDefault(item => item.Name == nameof(FlatRedBall.Sprite.UseAnimationTextureFlip));
#pragma warning disable CS0618 // using as nameof, that's okay
            var existingIgnoreAnimationTextureFlipVariableDefinition = ati.VariableDefinitions
                .FirstOrDefault(item => item.Name == nameof(FlatRedBall.Sprite.IgnoreAnimationChainTextureFlip));
#pragma warning restore CS0618 // Type or member is obsolete

            var doesAtiAlreadyHaveUseAnimationTextureFlip = existingUseAnimationTextureVariableDefinition != null;

            // Update the presence of the UseAnimationTextureFlip variable definition
            if (shouldHaveUseAnimationTextureFlip && !doesAtiAlreadyHaveUseAnimationTextureFlip)
            {
                var useAnimationTextureFlipVariableDefinition = new VariableDefinition();
                useAnimationTextureFlipVariableDefinition.Type = "bool";
                useAnimationTextureFlipVariableDefinition.Name = nameof(FlatRedBall.Sprite.UseAnimationTextureFlip);
                useAnimationTextureFlipVariableDefinition.Category = "Animation";
                useAnimationTextureFlipVariableDefinition.DefaultValue = "true";

                var useAnimationRelativePositionVariableDefinition = ati.VariableDefinitions.FirstOrDefault(item =>
                    item.Name == nameof(FlatRedBall.Sprite.UseAnimationRelativePosition));
                if (useAnimationRelativePositionVariableDefinition != null)
                {
                    var indexOf = ati.VariableDefinitions.IndexOf(useAnimationRelativePositionVariableDefinition);

                    ati.VariableDefinitions.Insert(indexOf + 1, useAnimationTextureFlipVariableDefinition);

                }
                else
                {
                    ati.VariableDefinitions.Add(useAnimationTextureFlipVariableDefinition);
                }
            }
            else if (!shouldHaveUseAnimationTextureFlip && doesAtiAlreadyHaveUseAnimationTextureFlip)
            {
                ati.VariableDefinitions.Remove(existingUseAnimationTextureVariableDefinition);
            }

            if (shouldHaveUseAnimationTextureFlip && existingIgnoreAnimationTextureFlipVariableDefinition != null)
            {
                ati.VariableDefinitions.Remove(existingIgnoreAnimationTextureFlipVariableDefinition);
            }
            if (!shouldHaveUseAnimationTextureFlip && existingIgnoreAnimationTextureFlipVariableDefinition == null)
            {
                var ignoreAnimationTextureFlipVariableDefinition = new VariableDefinition();
                ignoreAnimationTextureFlipVariableDefinition.Type = "bool";
#pragma warning disable CS0618 // Type or member is obsolete
                ignoreAnimationTextureFlipVariableDefinition.Name = nameof(FlatRedBall.Sprite.IgnoreAnimationChainTextureFlip);
#pragma warning restore CS0618 // Type or member is obsolete
                ignoreAnimationTextureFlipVariableDefinition.Category = "Animation";
                ignoreAnimationTextureFlipVariableDefinition.DefaultValue = "false";

                ati.VariableDefinitions.Add(ignoreAnimationTextureFlipVariableDefinition);
            }
        }

    }
}
