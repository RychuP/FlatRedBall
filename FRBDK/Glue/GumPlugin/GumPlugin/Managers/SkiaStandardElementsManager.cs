﻿using Gum.DataTypes.Variables;
using Gum.DataTypes;
using Gum.Managers;
using SkiaGum.Renderables;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RenderingLibrary.Graphics;

namespace GumPlugin.Managers
{
    internal class SkiaStandardElementsManager
    {
        public static void AddSkiaStandards()
        {
            {
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //                                                        SVG                                                         //
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                var svgState = new StateSave();
                svgState.Name = "Default";
                AddVisibleVariable(svgState);
                StandardElementsManager.AddPositioningVariables(svgState);
                StandardElementsManager.AddDimensionsVariables(svgState, 100, 100,
                    Gum.Managers.StandardElementsManager.DimensionVariableAction.AllowFileOptions);
                StandardElementsManager.AddColorVariables(svgState);

                foreach (var variableSave in svgState.Variables.Where(item => item.Type == typeof(DimensionUnitType).Name))
                {
                    variableSave.Value = DimensionUnitType.Absolute;
                    variableSave.ExcludedValuesForEnum.Add(DimensionUnitType.PercentageOfSourceFile);
                    //variableSave.ExcludedValuesForEnum.Add(DimensionUnitType.MaintainFileAspectRatio);

                }

                svgState.Variables.Add(new VariableSave { SetsValue = true, Type = "string", Value = "", Name = "SourceFile", IsFile = true });

                svgState.Variables.Add(new VariableSave { Type = "float", Value = 0.0f, Category = "Flip and Rotation", Name = "Rotation" });

                AddVariableReferenceList(svgState);

                Gum.Managers.StandardElementsManager.Self.DefaultStates.Add("Svg", svgState);

            }
            {
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //                                                   ColoredCircle                                                    //
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                var filledCircleState = new StateSave();
                filledCircleState.Name = "Default";
                AddVisibleVariable(filledCircleState);

                StandardElementsManager.AddPositioningVariables(filledCircleState);
                StandardElementsManager.AddDimensionsVariables(filledCircleState, 64, 64,
                    StandardElementsManager.DimensionVariableAction.ExcludeFileOptions);
                StandardElementsManager.AddColorVariables(filledCircleState);

                AddGradientVariables(filledCircleState);

                AddDropshadowVariables(filledCircleState);

                AddStrokeAndFilledVariables(filledCircleState);

                filledCircleState.Variables.Add(new VariableSave { Type = "float", Value = 0.0f, Category = "Flip and Rotation", Name = "Rotation" });

                AddVariableReferenceList(filledCircleState);

                Gum.Managers.StandardElementsManager.Self.DefaultStates.Add("ColoredCircle", filledCircleState);

            }
            {
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //                                                  LottieAnimation                                                   //
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                var lottieAnimationState = new StateSave();
                lottieAnimationState.Name = "Default";
                AddVisibleVariable(lottieAnimationState);
                StandardElementsManager.AddPositioningVariables(lottieAnimationState);
                StandardElementsManager.AddDimensionsVariables(lottieAnimationState, 100, 100,
                    Gum.Managers.StandardElementsManager.DimensionVariableAction.AllowFileOptions);

                // Do we support colors?
                //StandardElementsManager.AddColorVariables(lottieAnimationState);

                lottieAnimationState.Variables.Add(new VariableSave { SetsValue = true, Type = "string", Value = "", Name = "SourceFile", IsFile = true });

                AddVariableReferenceList(lottieAnimationState);

                Gum.Managers.StandardElementsManager.Self.DefaultStates.Add("LottieAnimation", lottieAnimationState);
            }

            {
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //                                                        Arc                                                         //
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                var arcState = new StateSave();
                arcState.Name = "Default";
                arcState.Variables.Add(new VariableSave { Type = "float", Value = 10, Category = "Arc", Name = "Thickness" });
                arcState.Variables.Add(new VariableSave { Type = "float", Value = 0, Category = "Arc", Name = "StartAngle" });
                arcState.Variables.Add(new VariableSave { Type = "float", Value = 90, Category = "Arc", Name = "SweepAngle" });
                arcState.Variables.Add(new VariableSave { Type = "bool", Value = false, Category = "Arc", Name = "IsEndRounded" });

                AddVisibleVariable(arcState);

                StandardElementsManager.AddPositioningVariables(arcState);
                StandardElementsManager.AddDimensionsVariables(arcState, 64, 64,
                    StandardElementsManager.DimensionVariableAction.ExcludeFileOptions);
                StandardElementsManager.AddColorVariables(arcState);

                AddGradientVariables(arcState);
                AddVariableReferenceList(arcState);

                Gum.Managers.StandardElementsManager.Self.DefaultStates.Add("Arc", arcState);
            }

            {
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //                                                  RoundedRectangle                                                  //
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                var roundedRectangleState = new StateSave();
                roundedRectangleState.Name = "Default";
                AddVisibleVariable(roundedRectangleState);

                roundedRectangleState.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 5, Name = "CornerRadius", Category = "Dimensions" });
                roundedRectangleState.Variables.Add(new VariableSave { Type = "float", Value = 0.0f, Category = "Flip and Rotation", Name = "Rotation" });

                StandardElementsManager.AddPositioningVariables(roundedRectangleState);
                StandardElementsManager.AddDimensionsVariables(roundedRectangleState, 64, 64,
                    StandardElementsManager.DimensionVariableAction.ExcludeFileOptions);
                StandardElementsManager.AddColorVariables(roundedRectangleState);

                AddGradientVariables(roundedRectangleState);

                AddDropshadowVariables(roundedRectangleState);

                AddStrokeAndFilledVariables(roundedRectangleState);

                AddVariableReferenceList(roundedRectangleState);
                Gum.Managers.StandardElementsManager.Self.DefaultStates.Add("RoundedRectangle", roundedRectangleState);
            }


            {
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //                                                       Canvas                                                       //
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                var canvasState = new StateSave();
                canvasState.Name = "Default";
                AddVisibleVariable(canvasState);

                StandardElementsManager.AddPositioningVariables(canvasState);
                StandardElementsManager.AddDimensionsVariables(canvasState, 100, 100,
                    StandardElementsManager.DimensionVariableAction.ExcludeFileOptions);

                AddVariableReferenceList(canvasState);

                Gum.Managers.StandardElementsManager.Self.DefaultStates.Add("Canvas", canvasState);
            }

        }


        private static void AddVisibleVariable(StateSave state)
        {
            state.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "Visible" });
        }

        private static void AddVariableReferenceList(StateSave stateSave)
        {
            stateSave.VariableLists.Add(new VariableListSave<string> { Type = "string", Value = new List<string>(), Category = "References", Name = "VariableReferences" });
        }


        private static void AddGradientVariables(StateSave state)
        {
            List<object> xUnitsExclusions = new List<object>();
            xUnitsExclusions.Add(PositionUnitType.PixelsFromTop);
            xUnitsExclusions.Add(PositionUnitType.PercentageHeight);
            xUnitsExclusions.Add(PositionUnitType.PixelsFromBottom);
            xUnitsExclusions.Add(PositionUnitType.PixelsFromCenterY);
            xUnitsExclusions.Add(PositionUnitType.PixelsFromCenterYInverted);
            xUnitsExclusions.Add(PositionUnitType.PixelsFromBaseline);

            List<object> yUnitsExclusions = new List<object>();
            yUnitsExclusions.Add(PositionUnitType.PixelsFromLeft);
            yUnitsExclusions.Add(PositionUnitType.PixelsFromCenterX);
            yUnitsExclusions.Add(PositionUnitType.PercentageWidth);
            yUnitsExclusions.Add(PositionUnitType.PixelsFromRight);


            state.Variables.Add(new VariableSave { Type = "bool", Value = false, Category = "Rendering", Name = "UseGradient" });

            state.Variables.Add(new VariableSave
            {
                SetsValue = true,
                Type = typeof(GradientType).Name,
                Value = GradientType.Linear,
                Name = "GradientType",
                Category = "Rendering",
                CustomTypeConverter = new EnumConverter(typeof(GradientType))
            });


            state.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0, Category = "Rendering", Name = "GradientX1" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(PositionUnitType).Name, Value = PositionUnitType.PixelsFromLeft, Name = "GradientX1Units", Category = "Rendering", ExcludedValuesForEnum = xUnitsExclusions });


            state.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0, Category = "Rendering", Name = "GradientY1" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(PositionUnitType).Name, Value = PositionUnitType.PixelsFromTop, Name = "GradientY1Units", Category = "Rendering", ExcludedValuesForEnum = yUnitsExclusions });

            state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Red1", Category = "Rendering" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Green1", Category = "Rendering" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Blue1", Category = "Rendering" });

            state.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 100, Category = "Rendering", Name = "GradientX2" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(PositionUnitType).Name, Value = PositionUnitType.PixelsFromLeft, Name = "GradientX2Units", Category = "Rendering", ExcludedValuesForEnum = xUnitsExclusions });

            state.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 100, Category = "Rendering", Name = "GradientY2" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(PositionUnitType).Name, Value = PositionUnitType.PixelsFromTop, Name = "GradientY2Units", Category = "Rendering", ExcludedValuesForEnum = yUnitsExclusions });

            state.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 50, Category = "Rendering", Name = "GradientInnerRadius" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(DimensionUnitType).Name, Value = DimensionUnitType.Absolute, Name = "GradientInnerRadiusUnits", Category = "Rendering" });


            state.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 100, Category = "Rendering", Name = "GradientOuterRadius" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(DimensionUnitType).Name, Value = DimensionUnitType.Absolute, Name = "GradientOuterRadiusUnits", Category = "Rendering" });

            state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Red2", Category = "Rendering" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Green2", Category = "Rendering" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "Blue2", Category = "Rendering" });
        }

        static void AddDropshadowVariables(StateSave stateSave)
        {
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Name = "HasDropshadow", Category = "Dropshadow" });

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0, Name = "DropshadowOffsetX", Category = "Dropshadow" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 3, Name = "DropshadowOffsetY", Category = "Dropshadow" });

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0, Name = "DropshadowBlurX", Category = "Dropshadow" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 3, Name = "DropshadowBlurY", Category = "Dropshadow" });

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "DropshadowAlpha", Category = "Dropshadow" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "DropshadowRed", Category = "Dropshadow" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "DropshadowGreen", Category = "Dropshadow" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "DropshadowBlue", Category = "Dropshadow" });
        }

        private static void AddStrokeAndFilledVariables(StateSave stateSave)
        {
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "IsFilled", Category = "Stroke and Fill" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 2.0f, Name = "StrokeWidth", Category = "Stroke and Fill" });

        }


    }
}
