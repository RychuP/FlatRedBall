﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Utilities;
using WpfDataUi;
using WpfDataUi.DataTypes;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Elements;
using GlueFormsCore.Controls;
using OfficialPlugins.PropertyGrid.Managers;
using WpfDataUi.Controls;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using System.Threading.Tasks;
using FlatRedBall.Glue.Controls;
using OfficialPlugins.PropertyGrid;

namespace OfficialPlugins.VariableDisplay
{
    class ElementVariableShowingLogic
    {

        private static void CreateCategoriesAndVariables(GlueElement element)
        {
            List<VariableDefinition> variableDefinitions = new List<VariableDefinition>();

            if (element is ScreenSave)
            {
                var screenAti = AvailableAssetTypes.CommonAtis.Screen;
                variableDefinitions.AddRange(screenAti.VariableDefinitions);
            }

            variableDefinitions.AddRange(PluginManager.GetVariableDefinitionsFor(element));


            foreach (var variableDefinition in variableDefinitions)
            {
                var customVariable = element.GetCustomVariable(variableDefinition.Name);

                if (customVariable == null)
                {
                    customVariable = new CustomVariable();
                    customVariable.DefaultValue = variableDefinition.DefaultValue;
                    customVariable.Name = variableDefinition.Name;
                    customVariable.Type = variableDefinition.Type;
                    if (variableDefinition.CustomGetForcedOptionFunc != null)
                    {
                        customVariable.CustomGetForcedOptionsFunc = element => variableDefinition.CustomGetForcedOptionFunc(element, null, null);

                    }
                    // category?
                    customVariable.Category = variableDefinition.Category;

                    element.CustomVariables.Add(customVariable);

                    var throwaway = GlueCommands.Self.GluxCommands.SaveElementAsync(element);
                }
            }
        }

        private static void CreateInstanceMembersForVariables(GlueElement element, List<MemberCategory> categoryListToFill)
        {
            CreateCategoriesAndVariables(element);

            foreach (CustomVariable variable in element.CustomVariables)
            {
                DataGridItem instanceMember = CreateInstanceMemberForVariable(element, variable);

                var categoryName = !string.IsNullOrWhiteSpace(variable.Category) ?
                    variable.Category : "Variables";

                var category = categoryListToFill.FirstOrDefault(item => item.Name == categoryName);

                if (category == null)
                {
                    category = CreateAndAddDefaultCategory(categoryListToFill, categoryName);
                }

                category.Members.Add(instanceMember);
            }
        }


        public static void UpdateShownVariables(DataUiGrid grid, GlueElement element)
        {
            var collapsed = grid.Categories.Where(item => item.IsExpanded == false).Select(item => item.Name).ToArray();
            grid.Categories.Clear();

            List<MemberCategory> categories = new List<MemberCategory>();

            CreateInstanceMembersForVariables(element, categories);

            foreach(var collapsedCategory in collapsed)
            {
                var category = categories.FirstOrDefault(item => item.Name == collapsedCategory);
                if (category != null)
                {
                    category.IsExpanded = false;
                }
            }

            AddAlternatingColors(grid, categories);

            grid.Refresh();

        }


        private static DataGridItem CreateInstanceMemberForVariable(GlueElement element, CustomVariable variable)
        {
            AssetTypeInfo elementAti = null;
            if(element is ScreenSave)
            {
                elementAti = AvailableAssetTypes.CommonAtis.Screen;
            }

            Type type = variable.GetRuntimeType();
            if (type == null)
            {
                type = typeof(string);
            }

            string name = variable.Name;

            var instanceMember = new DataGridItem();
            instanceMember.CustomGetTypeEvent += (throwaway) => type;
            string displayName = StringFunctions.InsertSpacesInCamelCaseString(name);

            // Currently this only works on TextBox variables - eventually will expand
            instanceMember.DetailText = variable.Summary;

            instanceMember.DisplayName = displayName;
            instanceMember.UnmodifiedVariableName = name;

            var baseVariable = ObjectFinder.Self.GetBaseCustomVariable(variable, element);
            TypeConverter converter = baseVariable.GetTypeConverter(element);
            instanceMember.TypeConverter = converter;

            VariableDefinition variableDefinition = null;
            NamedObjectSave variableNosOwner = null;
            if (!string.IsNullOrEmpty(baseVariable?.SourceObject))
            {
                variableNosOwner = element.GetNamedObjectRecursively(baseVariable.SourceObject);
                variableDefinition = variableNosOwner?.GetAssetTypeInfo()?.VariableDefinitions
                    .FirstOrDefault(item => item.Name == baseVariable.SourceObjectProperty);
            }

            if(variableDefinition == null)
            {
                variableDefinition = baseVariable.VariableDefinition ?? variable.VariableDefinition;
            }


            if (!string.IsNullOrEmpty(baseVariable.PreferredDisplayerTypeName) &&
                VariableDisplayerTypeManager.TypeNameToTypeAssociations.ContainsKey(baseVariable.PreferredDisplayerTypeName))
            {
                instanceMember.PreferredDisplayer = VariableDisplayerTypeManager.TypeNameToTypeAssociations
                    [baseVariable.PreferredDisplayerTypeName];
            }
            else if (variableDefinition?.PreferredDisplayer != null)
            {
                instanceMember.PreferredDisplayer = variableDefinition.PreferredDisplayer;
                foreach (var property in variableDefinition.PropertiesToSetOnDisplayer)
                {
                    instanceMember.PropertiesToSetOnDisplayer[property.Key] = property.Value;
                }
            }
            else if (string.IsNullOrEmpty(variable.SourceObject) && instanceMember.PreferredDisplayer == null &&
                (variable?.Name == nameof(FlatRedBall.PositionedObject.RotationZ) || variable?.Name == nameof(FlatRedBall.PositionedObject.RotationY) || variable?.Name == nameof(FlatRedBall.PositionedObject.RotationX)) && 
                variable.Type == "float")
            {
                instanceMember.PreferredDisplayer = typeof(AngleSelectorDisplay);
            }
            else if (instanceMember.PreferredDisplayer == null && variableDefinition?.MinValue != null && variableDefinition?.MaxValue != null)
            {
                instanceMember.PreferredDisplayer = typeof(SliderDisplay);
                instanceMember.PropertiesToSetOnDisplayer[nameof(SliderDisplay.MaxValue)] =
                    variableDefinition.MaxValue.Value;
                instanceMember.PropertiesToSetOnDisplayer[nameof(SliderDisplay.MinValue)] =
                    variableDefinition.MinValue.Value;
            }

            if (instanceMember.PreferredDisplayer == typeof(AngleSelectorDisplay))
            {
                instanceMember.PropertiesToSetOnDisplayer[nameof(AngleSelectorDisplay.TypeToPushToInstance)] =
                    AngleType.Radians;

                // this used to be 1, then 5, but 10 is prob enough resolution. Numbers can be typed.
                // 15 is better, gives the user access to 45
                instanceMember.PropertiesToSetOnDisplayer[nameof(AngleSelectorDisplay.SnappingInterval)] =
                    15m;
            }

            if (instanceMember.PreferredDisplayer == typeof(SliderDisplay) && variableDefinition?.MinValue != null && variableDefinition?.MaxValue != null)
            {
                instanceMember.PropertiesToSetOnDisplayer[nameof(SliderDisplay.MaxValue)] =
                    variableDefinition.MaxValue.Value;
                instanceMember.PropertiesToSetOnDisplayer[nameof(SliderDisplay.MinValue)] =
                    variableDefinition.MinValue.Value;
            }

            if(variableDefinition?.UiCreated != null)
            {
                instanceMember.UiCreated += (view) => variableDefinition.UiCreated(view);
            }

            instanceMember.CustomSetPropertyEvent += async (intance, args) =>
            {
                var value = args.Value;
                instanceMember.IsDefault = false;
                //RefreshLogic.IgnoreNextRefresh();

                var saveAndUpdate = args.CommitType == SetPropertyCommitType.Full;

                await GlueCommands.Self.GluxCommands.ElementCommands.HandleSetVariable(variable, value, saveAndUpdate, saveAndUpdate);
            };

            instanceMember.CustomGetEvent += (instance) =>
            {
                if (variableDefinition?.CustomVariableGet != null)
                {
                    return variableDefinition.CustomVariableGet(element, variableNosOwner, null);
                }
                else
                {
                    return element.GetVariableValueRecursively(name);
                }

            };

            instanceMember.IsDefault = element.GetCustomVariable(name)?.DefaultValue == null;

            // Assing the IsDefaultSet event setting IsDefault *after* 
            instanceMember.IsDefaultSet += (owner, args) =>
            {

                element.GetCustomVariableRecursively(name).DefaultValue = null;

                if (variable.SourceObject != null)
                {
                    var nos = element.GetNamedObjectRecursively(variable.SourceObject);
                    var sourceNosVariable = nos.GetCustomVariable(variable.SourceObjectProperty);
                    if (sourceNosVariable != null)
                    {
                        PropertyGridRightClickHelper.SetVariableToDefault(nos, variable.SourceObjectProperty);
                    }
                }


                var throwaway = GlueCommands.Self.GluxCommands.SaveElementAsync(element);
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
                if (element.BaseElement != null)
                {
                    var baseElement = ObjectFinder.Self.GetRootBaseElement(element);
                    if (baseElement != null)
                    {
                        // Generate the root base because it may have definitions for the types that have changed... 
                        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(baseElement);
                    }
                }
                
                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
                GlueCommands.Self.RefreshCommands.RefreshVariables();
            };

            instanceMember.SetValueError = (newValue) =>
            {
                if (newValue is string && string.IsNullOrEmpty(newValue as string))
                {
                    element.GetCustomVariableRecursively(name).DefaultValue = null;

                    var throwaway = GlueCommands.Self.GluxCommands.SaveElementAsync(element);

                    GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                }
            };
            AddContextMenuItems(variable, instanceMember);

            return instanceMember;
        }

        private static void AddContextMenuItems(CustomVariable variable, DataGridItem instanceMember)
        {
            instanceMember.ContextMenuEvents.Add("Variable Properties", (_,_) => 
                GlueState.Self.CurrentCustomVariable = variable);

            instanceMember.ContextMenuEvents.Add("Set Variable Category", (_, _) => ShowVariableCategoryTextInputWindow(variable));
        }

        private static void ShowVariableCategoryTextInputWindow(CustomVariable variable)
        {
            var tiw = new CustomizableTextInputWindow();
            tiw.Message = $"Enter the desired category for {variable.Name}";
            tiw.Result = variable.Category;

            var dialogResult = tiw.ShowDialog();

            if(dialogResult == true)
            {
                variable.Category = tiw.Result;
                GlueCommands.Self.RefreshCommands.RefreshVariables();
                var parent = ObjectFinder.Self.GetElementContaining(variable);
                if(parent != null)
                {
                    GlueCommands.Self.GluxCommands.SaveElementAsync(parent);
                }
            }
        }


        private static void AddAlternatingColors(DataUiGrid grid, List<MemberCategory> categories)
        {
            var dictionary = MainPanelControl.ResourceDictionary;
            const byte brightness = 227;
            var color = Color.FromRgb(brightness, brightness, brightness);
            if (dictionary.Contains("BlackSelected"))
            {
                color = (Color)MainPanelControl.ResourceDictionary["BlackSelected"];
            }

            foreach (var category in categories)
            {
                category.SetAlternatingColors(
                    new SolidColorBrush(color),
                    Brushes.Transparent);

                grid.Categories.Add(category);
            }
        }

        private static MemberCategory CreateAndAddDefaultCategory(List<MemberCategory> categories, string categoryLabel)
        {
            var defaultCategory = new MemberCategory(categoryLabel);
            defaultCategory.FontSize = 14;
            categories.Add(defaultCategory);
            return defaultCategory;
        }

    }
}
