﻿using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using OfficialPlugins.CollisionPlugin.Managers;
using OfficialPlugins.CollisionPlugin.ViewModels;
using OfficialPlugins.CollisionPlugin.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.CollisionPlugin.Controllers
{
    public class CollidableNamedObjectController
    {
        static CollidableNamedObjectRelationshipViewModel ViewModel;

        public static void RegisterViewModel(CollidableNamedObjectRelationshipViewModel viewModel)
        {
            ViewModel = viewModel;
            ViewModel.PropertyChanged += HandleViewModelPropertyChanged;
        }

        private static void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(ViewModel.SortAxis))
            {
                var nos = GlueState.Self.CurrentNamedObjectSave;
                if (nos != null && ViewModel.CanBePartitioned)
                {
                    var partitionInfo = AutomatedCollisionSizeLogic.GetAutomaticCollisionWidthHeight(
                        nos);
                    ViewModel.CalculatedParitioningWidthHeight = Math.Abs(partitionInfo.HalfDimension * 2);
                    ViewModel.CalculatedPartitionWidthHeightSource = partitionInfo.Source;
                }
            }
        }

        public static string FirstCollidableIn(NamedObjectSave collisionRelationship)
        {
            return collisionRelationship.Properties.GetValue<string>(
                nameof(CollisionRelationshipViewModel.FirstCollisionName));
        }

        public static string SecondCollidableIn(NamedObjectSave collisionRelationship)
        {
            return collisionRelationship.Properties.GetValue<string>(
                nameof(CollisionRelationshipViewModel.SecondCollisionName));
        }

        public static void RefreshViewModelTo(IElement container,
            NamedObjectSave thisNamedObject,
            CollidableNamedObjectRelationshipViewModel viewModel)
        {
            viewModel.GlueObject = thisNamedObject;
            // Set this before updating from Glue object so that we don't persist values which 
            // don't apply
            viewModel.CanBePartitioned = CollisionCodeGenerator.CanBePartitioned(thisNamedObject);
            viewModel.DefinedByBase = thisNamedObject.DefinedByBase;
            viewModel.UpdateFromGlueObject();
            if(viewModel.CanBePartitioned)
            {
                var partitionInfo = AutomatedCollisionSizeLogic.GetAutomaticCollisionWidthHeight(
                    thisNamedObject);
                viewModel.CalculatedParitioningWidthHeight = Math.Abs( partitionInfo.HalfDimension * 2);
                ViewModel.CalculatedPartitionWidthHeightSource = partitionInfo.Source;
            }

            viewModel.CollisionRelationshipsTitle =
                $"{thisNamedObject.InstanceName} Collision Relationships";

            var isSingleEntity = thisNamedObject.IsList == false && thisNamedObject.SourceType == SourceType.Entity;
            var isTileShapeCollection = thisNamedObject.SourceClassType ==
                "FlatRedBall.TileCollisions.TileShapeCollection" ||
                thisNamedObject.SourceClassType == "TileShapeCollection";
            List<NamedObjectSave> collidables;
            
            if(isTileShapeCollection)
            {
                // only against collidables:
                // See update below on why we don't use AllNamedObjects
                //collidables = container.AllNamedObjects
                collidables = container.NamedObjects
                    .Where(item =>
                    {
                        var entity = CollisionRelationshipViewModelController.GetEntitySaveReferencedBy(item);
                        return entity?.ImplementsICollidable == true;
                    })
                    .ToList();
            }
            else
            {
                // Update April 23, 23
                // We used to use AllNamedObjects
                // but now that instances are directly
                // added to Glue (such as coins in a Mario
                // game), lists can get HUGE and this view becomes
                // pointless and slow. Typically relationships are created
                // between lists and other lists. Soemtimes individual objects
                // can be collided too, but rarely are these inside of lists. Therefore
                // let's speed things up and only use the top-level objects and not All:
                //collidables = container.AllNamedObjects
                collidables = container.NamedObjects
                    .Where(item =>
                    {
                        return CollisionRelationshipViewModelController.GetIfCanBeReferencedByRelationship(item);
                    })
                    .ToList();
            }

            if(isSingleEntity)
            {
                // don't let this be against itself
                if(collidables.Contains(thisNamedObject))
                {
                    collidables.Remove(thisNamedObject);
                }
            }

            // Why do we use "all" here? CollisionRelationships cannot be
            // added to lists, so they are always at the top level. Using all 
            // means we look through all of the items inside of lists which can be 
            // slower for large entities.
            //var relationships = container.AllNamedObjects
            var relationships = container.AllNamedObjects
                .Where(item =>
                {
                    return item.GetAssetTypeInfo() == AssetTypeInfoManager.Self.CollisionRelationshipAti;
                })
                .ToArray();



            viewModel.NamedObjectPairs.Clear();


            var orderedCollidables = collidables.OrderBy(item => item.InstanceName);

            if(thisNamedObject.IsList)
            {
                AddRelationship(thisNamedObject, viewModel, relationships, null);
            }



            foreach (var collidable in orderedCollidables)
            {
                AddRelationship(thisNamedObject, viewModel, relationships, collidable);
            }

        }

        private static void AddRelationship(NamedObjectSave thisNamedObject, CollidableNamedObjectRelationshipViewModel viewModel, 
            NamedObjectSave[] relationships, NamedObjectSave collidable)
        {
            var name1 = thisNamedObject.InstanceName;
            var name2 = collidable?.InstanceName;

            var pairViewModel = new NamedObjectPairRelationshipViewModel();
            pairViewModel.AddObjectClicked += (_, _) => _=HandleAddCollisionRelationshipAddClicked(pairViewModel);
            pairViewModel.OtherObjectName = name2;
            pairViewModel.SelectedNamedObjectName = thisNamedObject.InstanceName;

            var relationshipsForThisPair = relationships
                .Where(item =>
                {
                    return (FirstCollidableIn(item) == name1 && SecondCollidableIn(item) == name2) ||
                        (FirstCollidableIn(item) == name2 && SecondCollidableIn(item) == name1);
                })
                .ToArray();

            foreach (var relationship in relationshipsForThisPair)
            {
                var relationshipViewModel = new RelationshipListCellViewModel();
                relationshipViewModel.OwnerNamedObject = thisNamedObject;
                relationshipViewModel.OtherNamedObject = collidable;
                relationshipViewModel.CollisionRelationshipNamedObject = relationship;

                pairViewModel.Relationships.Add(relationshipViewModel);
            }


            viewModel.NamedObjectPairs.Add(pairViewModel);
        }

        private static async Task HandleAddCollisionRelationshipAddClicked(NamedObjectPairRelationshipViewModel pairViewModel)
        {
            // Vic asks - why is the selected "second"?
            // If I select the player and have it collide against
            // bullets, I would expect a PlayerVsBullets collision...
            //var firstNosName = pairViewModel.OtherObjectName;
            //var secondNosName = pairViewModel.SelectedNamedObjectName;

            var firstNosName = pairViewModel.SelectedNamedObjectName;
            var secondNosName = pairViewModel.OtherObjectName;

            await CreateCollisionRelationshipBetweenObjects(firstNosName, secondNosName, GlueState.Self.CurrentElement);
        }

        public static async Task<NamedObjectSave> CreateCollisionRelationshipBetweenObjects(string firstNosName, string secondNosName, GlueElement container)
        {
            if(container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            NamedObjectSave? newNos = null;
            await TaskManager.Self.AddAsync(async () =>
            {
                var addObjectModel = new AddObjectViewModel();
                addObjectModel.ForcedElementToAddTo = container;

                var firstNos = container.GetNamedObjectRecursively(firstNosName);
                var secondNos = container.GetNamedObjectRecursively(secondNosName);

                if (firstNos == null)
                {
                    throw new InvalidOperationException(
                        $"Could not find an entity with the name {firstNosName} in {container}");
                }

                addObjectModel.SourceType = FlatRedBall.Glue.SaveClasses.SourceType.FlatRedBallType;
                addObjectModel.SelectedAti =
                    AssetTypeInfoManager.Self.CollisionRelationshipAti;
                //"FlatRedBall.Math.Collision.CollisionRelationship";

                addObjectModel.Properties.SetValue(nameof(CollisionRelationshipViewModel.IsAutoNameEnabled), true);

                string effectiveSecondCollisionName;
                NamedObjectSave effectiveFirstNos;
                NamedObjectSave effectiveSecondNos;

                bool needToInvert = firstNos.SourceType != SourceType.Entity &&
                    firstNos.IsList == false;

                if (needToInvert)
                {
                    addObjectModel.Properties.SetValue(nameof(CollisionRelationshipViewModel.FirstCollisionName),
                            secondNosName);
                    addObjectModel.Properties.SetValue(nameof(CollisionRelationshipViewModel.SecondCollisionName),
                            firstNosName);

                    effectiveFirstNos = secondNos;
                    effectiveSecondCollisionName = firstNosName;
                    effectiveSecondNos = firstNos;
                }
                else
                {
                    addObjectModel.Properties.SetValue(nameof(CollisionRelationshipViewModel.FirstCollisionName),
                            firstNosName);
                    addObjectModel.Properties.SetValue(nameof(CollisionRelationshipViewModel.SecondCollisionName),
                            secondNosName);

                    effectiveFirstNos = firstNos;
                    effectiveSecondCollisionName = secondNosName;
                    effectiveSecondNos = secondNos;
                }

                EntitySave? firstEntityType = null;
                if (effectiveFirstNos.SourceType == SourceType.Entity)
                {
                    firstEntityType = ObjectFinder.Self.GetEntitySave(effectiveFirstNos.SourceClassType);
                }
                else if (effectiveFirstNos.IsList)
                {
                    firstEntityType = ObjectFinder.Self.GetEntitySave(effectiveFirstNos.SourceClassGenericType);
                }

                EntitySave? secondEntityType = null;
                if(effectiveSecondNos != null)
                {
                    // This can happen if the user is creating an always-colliding relationship.
                    if(effectiveSecondNos.SourceType == SourceType.Entity)
                    {
                        secondEntityType = ObjectFinder.Self.GetEntitySave(effectiveSecondNos.SourceClassType);
                    }
                    else
                    {
                        secondEntityType = ObjectFinder.Self.GetEntitySave(effectiveSecondNos.SourceClassGenericType);
                    }
                }


                // this used to rely on the name "SolidCollision" but that's not a set standard and there could be multiple
                // TileShapeCollections
                if (secondNos?.GetAssetTypeInfo()?.FriendlyName == "TileShapeCollection")
                {

                    bool isPlatformer = false;
                    if (firstEntityType != null)
                    {
                        isPlatformer = firstEntityType.Properties.GetValue<bool>("IsPlatformer");
                    }

                    if (isPlatformer)
                    {
                        addObjectModel.Properties.SetValue(
                            nameof(CollisionRelationshipViewModel.CollisionType),
                            (int)CollisionType.PlatformerSolidCollision);

                    }
                    else
                    {

                        addObjectModel.Properties.SetValue(
                            nameof(CollisionRelationshipViewModel.CollisionType),
                            (int)CollisionType.BounceCollision);


                        addObjectModel.Properties.SetValue(
                            nameof(CollisionRelationshipViewModel.CollisionElasticity),
                            0.0f);
                    }
                }



                var sourceClassType = AssetTypeInfoManager.GetCollisionRelationshipSourceClassType(container, addObjectModel.Properties);
                addObjectModel.SourceClassType = sourceClassType;

                // setting the SourceClassType sets the ObjectName. Overwrite it...
                addObjectModel.ObjectName = "ToBeRenamed";

                newNos =
                    await GlueCommands.Self.GluxCommands.AddNewNamedObjectToAsync(addObjectModel,
                    container, listToAddTo: null);

                var indexToInsertAfter = GetIndexToInsertAfter(newNos, container);

                if(indexToInsertAfter != null)
                {
                    container.NamedObjects.Remove(newNos);
                    container.NamedObjects.Insert(indexToInsertAfter.Value + 1, newNos);
                }

                // if this is an always-colliding relationship, the user will typically want this
                // to be at the beginning before all other relationships. Therefore, let's remove
                // and re-add it:
                var isAlwaysColliding = secondNos == null;
                if(isAlwaysColliding)
                {
                    container.NamedObjects.Remove(newNos);
                    container.NamedObjects.Insert(0, newNos);
                }

                // this will regenerate and save everything too:
                CollisionRelationshipViewModelController.TryApplyAutoName(
                    container, newNos);

                var isFirstIDamageable = firstEntityType?.GetPropertyValue("ImplementsIDamageable") 
                    as bool? ?? false;
                var isFirstIDamageArea = firstEntityType?.GetPropertyValue("ImplementsIDamageArea")
                    as bool? ?? false;

                var isSecondIDamageable = secondEntityType?.GetPropertyValue("ImplementsIDamageable")
                    as bool? ?? false;
                var isSecondIDamageArea = secondEntityType?.GetPropertyValue("ImplementsIDamageArea")
                    as bool? ?? false;

                // January 10, 2023
                // Why not always check
                // this value? If the objects
                // are not IDamageable/IDamageArea, 
                // then the code generator will ignore
                // these options. If they are, then we want
                // them always enabled. 
                async Task SetProp(string propertyName) =>
                    await GlueCommands.Self.GluxCommands.SetPropertyOnAsync(newNos, propertyName, 
                    value: true, 
                    performSaveAndGenerateCode: false,
                    recordUndo:true);

                await SetProp(nameof(CollisionRelationshipViewModel.IsDealDamageChecked));

                // March 1, 2024
                // NO NO! This used to be true, but we don't want that because it results in the player
                // being destroyed right away. This causes so much confusion that it's best to leave it unchecked.
                //await SetProp(nameof(CollisionRelationshipViewModel.IsDestroyFirstOnDamageChecked));
                //await SetProp(nameof(CollisionRelationshipViewModel.IsDestroySecondOnDamageChecked));


                RefreshViewModelTo(container, firstNos, ViewModel);

                CollisionRelationshipViewModelController.TryFixMassesForTileShapeCollisionRelationship(container, newNos);

                if (GlueState.Self.CurrentElement == container)
                {
                    GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();
                }

                GlueState.Self.CurrentNamedObjectSave = newNos;
                GlueCommands.Self.DialogCommands.FocusTab("Collision");

                CollisionRelationshipViewModelController.RefreshViewModel(newNos);
            }, $"Creating collision relationships between {firstNosName} and {secondNosName}", doOnUiThread:true);

            return newNos;
        }

        private static int? GetIndexToInsertAfter(NamedObjectSave newNos, GlueElement container)
        {
            var newNosFirst = newNos.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.FirstCollisionName));

            for(int i = container.NamedObjects.Count - 1; i >= 0; i--)
            {
                var candidate = container.NamedObjects[i];

                if (candidate == newNos) continue;

                var candidateFirst = candidate.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.FirstCollisionName));

                if(candidateFirst == newNosFirst)
                {
                    return i;
                }
            }

            return null;
        }
    }
}
