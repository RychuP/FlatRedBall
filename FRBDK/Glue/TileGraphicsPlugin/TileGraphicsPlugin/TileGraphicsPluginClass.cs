using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins;
using System.Reflection;
using FlatRedBall.Glue.Elements;
using System.IO;
using FlatRedBall.IO;
using EditorObjects.SaveClasses;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using System.Windows.Forms;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.VSHelpers;
using TileGraphicsPlugin.Controllers;
using TileGraphicsPlugin.Managers;
using FlatRedBall.Content.Instructions;
using TMXGlueLib.DataTypes;
using ProjectManager = FlatRedBall.Glue.ProjectManager;
using TMXGlueLib;
using FlatRedBall.Glue.Parsing;
using TileGraphicsPlugin.CodeGeneration;
using TileGraphicsPlugin.Views;
using TileGraphicsPlugin.ViewModels;
using FlatRedBall.Glue.IO;
using TileGraphicsPlugin.Logic;
using System.ComponentModel;
using TiledPluginCore.Controls;
using TiledPluginCore.CodeGeneration;
using TiledPluginCore.Controllers;
using TiledPluginCore.Managers;
using TiledPluginCore.Views;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.MVVM;
using TiledPluginCore.ViewModels;

namespace TileGraphicsPlugin;

[Export(typeof(PluginBase))]
public class TileGraphicsPluginClass : PluginBase
{
    #region Fields

    string mLastFile;

    PluginTab collisionTab;
    PluginTab nodeNetworkTab;

    TiledObjectTypeCreator tiledObjectTypeCreator;
    TiledToolbar tiledToolbar;

    #endregion

    #region Properties

    public override Version Version
    {
        // 1.0.8 introduced tile instantiation per layer
        // 1.0.8.1 adds a null check around returning files referenced by a .tmx
        // 1.0.9 adds TileNodeNetworkCreator
        // 1.0.10 adds shape property support
        // 1.0.11 Adds automatic instantiation and collision setting from shapes
        // 1.0.11.1 Fixed bug where shape had a name
        // 1.0.11.2 Fixed rectangle polygon creation offset bug
        // 1.0.11.3 
        // - Fixed crash occurring when the Object layer .object property is null
        // - Upped to .NET 4.5.2
        // 1.0.11.4
        // - Shapes now have their Z value set according to the object layer index, which may be used to position entities properly.
        // 1.0.11.5
        // - Fixed a bug where the Name property for tiled sprites was being ignored.
        // 1.1.0 
        // - Introduces a MUCH faster loading system - approximately 4x as fast on large maps (680x1200)
        // 1.1.1.0
        // - Fixed offset issues with instantiating entities from objects
        // - Added support for rotated entities 
        // 1.1.1.1
        // - Fixed animations not setting the right and bottom texture coordinates according to offset values
        // 1.1.1.2
        // - Fixed attempting to set the EntityToCreate property on new entities in TileEntityInstantiator
        // - Name property is set even if property is lower-case "name"
        // 1.1.1.3
        // - Added method to add collision in a TileShapeCollection from a single layer in the MapDrawableBatch
        // 1.1.1.4
        // - Fixed crash thich can occur sometimes when clicking on a TMX file due to a collection changed 
        // 1.1.1.5
        // - Fixed possible threading issue if the project attempts to save on project startup
        // 1.1.1.6
        // - Updated tile entity instantiator so that it supports the latest changes to the factory pattern to support X and Y values
        // 1.1.1.7
        // - Added support for overriding the current TextureFilter when rendering a MapDrawableBatch
        // - Entities are now offset according to their layer's position - useful for instantiating entities inside of another entity
        // 1.2.0
        // - Added/improved xml Type generated file creation/updating
        // - Made the IDs alittle clearer - no more +1 in code 
        // 1.2.1
        // - Added TileShapeCollection.AddCollisionFromTilesWithProperty
        // 1.2.2
        // - Added TileShapeCollection.SetColor
        // 1.2.2.1
        // - Fixed possible crash with an empty object layer
        // 1.2.2.2
        // - Added TileShapeCollection CollideAgainst Polygon
        // 1.2.3
        // - Default properties from the TileSet now get assigned on created entities for object layers
        // 1.2.3.1
        // - Fixed polygon conversion not doing conversions using culture invariant values - fixes games running in Finnish
        // - Fixed tile entity instantiation not doing float conversions using culture invariant values - also Finnish fix.
        // 1.3.0
        // - Added TileShapeCollection + CollisionManager integration.
        // 1.3.1
        // - Removed requirement for XNA HiDef so old machines and VMs will run this plugin better, but the edit window won't show
        // 1.3.2
        //  - Added support for adding rectangles on rotated tiles
        // 1.3.3
        //  - Updated the TileShapeCollection CollisionRelationship classes to return bool on DoCollision to match the base implementation
        // 1.4.0
        //  - Collision from shapes on tileset tiles will now check the name of the shape - if it has no name then it's added to a TileShapeCollection
        //    with the name matching the layer. If the shape does have a name then it is added to a TileShapeCollection with the same name as the shape.
        //    This allows games to easily organize their collision with no code and no custom properties.
        // 1.4.1
        //  - ShapeCollections in LayeredTileMap are now cleared out, so if they're made visible in custom code
        //    they don't have to be made invisible manually.
        // 1.5.0
        //  - TileEntityInstantiator can now assign properties from CSVs if the CSV dictionary is registered
        // 1.5.1
        //  - Added creating ICollidables using EntityToCreate property on rectangles and circles
        // 1.5.2
        //  - Fixed missing using statement in TileEntityInstantiator
        // 1.6.0
        //  - Added support for instantiating tile entities applying properties from states
        // 1.6.2
        //  - Fixed bug where TileNemsWith wouldn't check lower-case "name" property
        // 1.7.0
        //  - Added support for layer Alpha
        //  - Added support for layer Red, Green, and Blue
        // 1.7.1
        //  - Fixed flipped tiles not creating entities
        // 1.8.1
        //  - The new entity window now has checkboxes for supporting creating the entity through Tiled and for 
        //    adding the entity to all screens containing tmx files directly.
        // 2.0.0
        //  - Added generation of adding entities from Tiled file
        // 2.0.1
        // - Added support for setting AnimationChains on an entity through Tiled
        // 2.1.0
        // - Every layer now creates a tile shape collection regardless of whether it contains shapes
        // 2.1.1
        // - Tile rectangle collision can now be offset
        // 2.1.2
        // - Fixed bug where layers weren't resetting their blend op to normal (from additive) which can
        //   result in additive layers.
        // 2.1.3
        // - Made RegisterName property public so it can be called outside of MapDrawableBatch
        // 2.1.4
        // - Added support for objects that are larger than the default tile size (scaled sprites in Tiled)
        // 2.1.4.1 
        // - Fixed copy paste compile error from previous change.
        // 2.1.4.2
        // - Fixed Type not being copied over when specified on object instances
        // 2.2
        // - Huge changes allowing tile shape collections to be generated through glue plugin.
        // 2.2.1
        // - Added support for creating TileNodeNetworks by type
        // 2.2.2
        // - Removed exception occurring when a tile has a type, but no matching entity exists
        // 2.3.0
        // - X and Y now apply offsets to created entities, allowing entities to be centered around different points on a tile
        // 2.4.0
        // - Added line vs TileShapeCollection relationships, including support for closest collision for (really efficient) ray casting
        // 2.4.0.1
        // - Fixed tab text
        // 2.5.0.0
        // - TileShapeCollections will now depend on the type
        // 2.5.1.0
        // - TileShapeCollections now set their visibility prior to creation, speeding up the creation of
        //   large tipe shape collections.
        // 2.5.2.0
        // - Added TileShapeCollection extension method AddMergedCollisionFromTilesWithType
        // - Added IsMerged checkbox to collisions from type
        // 2.5.3.0
        // - TileShapeCollection.Setcolor now sets polygons and rectangles.
        // 2.5.4.0
        // - TileShapeCollections from layes are assigned directly on constructor instead
        //   of after. This allows relationships to use these collision relationships.
        // 2.5.5.0
        // - Tiled plugin makes itself required when adding a TMX
        // 2.6.0
        // - Polygons on tiles can now be rotated and flipped just like the tiles.
        // 2.7.0
        // - Added TileShapeCollection.MergeRectangles
        // 2.7.0.2
        // - Added comments
        // 2.8
        // - CollidableVsTileShapeCollectionData.DoFirstCollisionLineVsShapeCollection is now public 
        // 2.9
        // - Added animated, flipped tile support
        // 2.10
        // - Added ability to restrict instantiation to a bound
        // 2.11
        // - Added ability to prevent objects from being removed when creating objects from tiles
        // 2.12
        // - Object layers now have a Visible property
        // 2.13
        // - TileEntityInstantiator.GetFactory is now public making it easier for other code to instantiate objects by name
        // 2.14
        // - Added new tooltip
        // 2.15
        // - Added TileShapeCollection.AssignAllShapesToRepositionOutward 
        // 2.16 
        // - LayeredTileMap is now a FlatRedBall IPositionedSizedObject
        // 2.17
        // - TileShapeCollection now has RemoveCollisionFrom and RemoveCollisionFromTilesWithType
        // 2.18
        // - LayeredTileMap merge function now works properly on x-sorted maps (y sorted will prob come soon)
        get { return new Version(2, 16, 0, 0); }
    }

    [Import("GlueProjectSave")]
    public GlueProjectSave GlueProjectSave
    {
        get;
        set;
    }
	
    public override string FriendlyName
    {
        get { return "Tiled Plugin"; }
    }

    static TileGraphicsPluginClass mSelf;
    public static TileGraphicsPluginClass Self
    {
        get { return mSelf; }
    }


    #endregion

    #region Constructor/Startup

    public TileGraphicsPluginClass()
    {
        mSelf = this;
    }

    public override void StartUp()
    {
        CodeItemAdderManager.Self.AddFilesToCodeBuildItemAdder();

        AddEvents();

        this.AddErrorReporter(new ErrorReporter());

        CreateToolbar();

        SaveTemplateTmx();

        AddCodeGenerators();

        // This used to be on project load, but now we do it on startup
        AssetTypeInfoAdder.Self.UpdateAtiPresence();
    }

    private void AddEvents()
    {
        tiledObjectTypeCreator = new TiledObjectTypeCreator();

        this.TryHandleCopyFile += HandleCopyFile;

        this.ReactToLoadedGluxEarly += HandleGluxLoadEarly;

        this.ReactToUnloadedGlux += HandleGluxUnload;

        this.ReactToLoadedGlux += async () =>
        {
            HandleGluxLoad();
            await tiledObjectTypeCreator.RefreshFile();
        };

        // Adds all objects contained within a file (like TMX)
        this.TryAddContainedObjects += HandleTryAddContainedObjects;

        this.AdjustDisplayedReferencedFile += HandleAdjustDisplayedReferencedFile;

        this.ReactToItemSelectHandler += HandleItemSelect;
        this.ReactToFileChangeHandler += HandleFileChange;

        this.FillWithReferencedFiles += FileReferenceManager.Self.HandleGetFilesReferencedBy;
        this.CanFileReferenceContent += HandleCanFileReferenceContent;

        //TilesetController.Self.EntityAssociationsChanged +=
        //    EntityListManager.Self.OnEntityAssociationsChanged;

        //TilesetController.Self.GetTsxDirectoryRelativeToTmx = () => "../Tilesets/";

        this.ReactToChangedPropertyHandler += async (changedMember, oldalue, glueElement) =>
        {
            if(GlueState.Self.CurrentCustomVariable != null)
            {
                if (changedMember == nameof(CustomVariable.Name))
                {
                    await tiledObjectTypeCreator.RefreshFile();
                }

            }
            else if(glueElement != null)
            {
                if (changedMember == nameof(EntitySave.CreatedByOtherEntities) || changedMember == nameof(EntitySave.Name))
                {
                    await tiledObjectTypeCreator.RefreshFile();
                }
            }
        };

        this.ReactToElementVariableChange += async (element, variable) =>
        {
            if ((element as EntitySave)?.CreatedByOtherEntities == true)
            {
                await tiledObjectTypeCreator.RefreshFile();
            }
        };

        this.ReactToVariableAdded += async (newVariable) =>
        {
            var element = EditorObjects.IoC.Container.Get<IGlueState>().CurrentElement;
            if ((element as EntitySave)?.CreatedByOtherEntities == true)
            {
                await tiledObjectTypeCreator.RefreshFile();
            }
        };

        this.ReactToVariableRemoved += async (removedVariable) =>
        {
            var element = EditorObjects.IoC.Container.Get<IGlueState>().CurrentElement;
            if ((element as EntitySave)?.CreatedByOtherEntities == true)
            {
                await tiledObjectTypeCreator.RefreshFile();
            }
        };

        this.NewEntityCreated += async (newEntityCreated) =>
        {
            await tiledObjectTypeCreator.RefreshFile();
        };

        //this.ModifyAddEntityWindow += ModifyAddEntityWindowLogic.HandleModifyAddEntityWindow;

        this.ReactToNewObjectHandler += NewObjectLogic.HandleNewObject;

        this.NewEntityCreatedWithUi += NewEntityCreatedReactionLogic.ReactToNewEntityCreated;

        this.ReactToNewFileHandler = ReactToNewFile;

        this.AddNewFileOptionsHandler += HandleAddNewFileOptions;

        //this.CreateNewFileHandler += TmxCreationManager.Self.HandleNewTmxCreation;
    }

    private void HandleGluxUnload()
    {
        base.RemoveFromToolbar(tiledToolbar, "Tools");
    }

    private void CreateToolbar()
    {
        tiledToolbar = new TiledToolbar();
        tiledToolbar.Opened += HandleToolbarOpened;
    }

    private void HandleToolbarOpened(object sender, EventArgs e)
    {
        var availableTmxFiles = ObjectFinder.Self.GetAllReferencedFiles()
            .Where(item => item.Name?.ToLowerInvariant()?.EndsWith(".tmx") == true)
            .ToList();

        tiledToolbar.FillDropdown(availableTmxFiles);

        tiledToolbar.HighlightFirstItem();
    }

    private void ReactToNewFile(ReferencedFileSave newFile, AssetTypeInfo assetTypeInfo)
    {
        var isTmx = FileManager.GetExtension(newFile.Name) == "tmx";

        if(isTmx)
        {
            var isRequired = GlueCommands.Self.GluxCommands.GetPluginRequirement(this);

            if(!isRequired)
            {
                GlueCommands.Self.GluxCommands.SetPluginRequirement(this, true);
                GlueCommands.Self.PrintOutput("Added Tiled Plugin as a required plugin because TMX's are used");
                GlueCommands.Self.GluxCommands.SaveProjectAndElements();

            }

            TmxCreationManager.Self.HandleNewTmx(newFile);
        }

    }

    private static void SaveTemplateTmx()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string whatToSave = "TiledPluginCore.Content.Levels.TiledMap.tmx";
        //C:\Users\Victor\AppData\Roaming\Glue\FilesForAddNewFile\

        string destination = FileManager.UserApplicationDataForThisApplication + "FilesForAddNewFile/" + 
            "TiledMap.tmx";
        try
        {
            FileManager.SaveEmbeddedResource(assembly, whatToSave, destination);
        }
        catch (Exception e)
        {
            PluginManager.ReceiveError("Error trying to save tmx: " + e.ToString());
        }
    }

    private void AddCodeGenerators()
    {
        // November 5, 2020 - I don't think we do this anymore, it's all handled with the inheritance model
        //this.RegisterCodeGenerator(new LevelCodeGenerator());

        this.RegisterCodeGenerator(new TmxCodeGenerator());

        this.RegisterCodeGenerator(new TileShapeCollectionCodeGenerator());

        this.RegisterCodeGenerator(new TileNodeNetworkCodeGenerator());

        this.RegisterCodeGenerator(new TiledGame1CodeGenerator());
    }

    #endregion

    #region Methods

    public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason reason)
    {
        // Do anything your plugin needs to do to shut down
        // or don't shut down and return false

        if (tiledToolbar != null)
        {
            base.RemoveFromToolbar(tiledToolbar, "Standard");
        }

        return true;
    }

    private bool HandleTryAddContainedObjects(string absoluteFile, List<string> availableObjects)
    {
        var isTmx = new FilePath(absoluteFile).Extension == "tmx";

        if(isTmx && System.IO.File.Exists(absoluteFile))
        {
            try
            {
                TiledMapSave save = TiledMapSave.FromFile(absoluteFile);

                // loop through each layer, adding it:
                foreach(var layer in save.MapLayers)
                {
                    availableObjects.Add($"{layer.Name} (FlatRedBall.TileCollisions.TileShapeCollection)");
                    availableObjects.Add($"{layer.Name} (FlatRedBall.TileGraphics.MapDrawableBatch)");

                }
            }
            catch(Exception e)
            {
                GlueCommands.Self.PrintError(e.ToString());
            }
        }

        return isTmx;
    }

    private void HandleGluxLoadEarly()
    {
        // Add the necessary files for performing the builds to the Libraries/tmx folder
        BuildToolSaver.Self.SaveBuildToolsToDisk();

        // Add Builders so that the user has the option to handle these file types
        BuildToolAssociationManager.Self.UpdateBuildToolAssociations();
    }

    public static void ExecuteFinalGlueCommands(EntitySave entity)
    {
        FlatRedBall.Glue.Plugins.ExportedImplementations.GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();
        FlatRedBall.Glue.Plugins.ExportedImplementations.GlueCommands.Self.GluxCommands.SaveProjectAndElements();
        FlatRedBall.Glue.Plugins.ExportedImplementations.GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(entity);
        FlatRedBall.Glue.Plugins.ExportedImplementations.GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
    }

    private bool HandleCanFileReferenceContent(string fileName)
    {
        var extension = FileManager.GetExtension(fileName);
        return extension == "tilb" || extension == "tsx" || extension == "tmx";

    }

    private bool HandleCopyFile(string sourceFile, string sourceDirectory, string targetFile)
    {
        string extension = FileManager.GetExtension(sourceFile);

        if (extension == "tmx")
        {
            if (System.IO.File.Exists(sourceFile))
            {
                CopyFileManager.Self.CopyTmx(sourceFile, targetFile);
            }
            return true;

        }
        return false;
    }
    
    private void HandleItemSelect(ITreeNode treeNode)
    {
        bool shouldRemove = true;

        ReferencedFileSave rfs = treeNode?.Tag as ReferencedFileSave;

        if (FileReferenceManager.Self.IsTmx(rfs))
        {
            shouldRemove = false;

            //FileReferenceManager.Self.UpdateAvailableTsxFiles();

            ReactToRfsSelected(rfs);
        }


        if (shouldRemove)
        {
            collisionTab?.Hide();
            nodeNetworkTab?.Hide();
        }

        if(TileShapeCollectionsPropertiesController.IsTileShapeCollection(treeNode?.Tag as NamedObjectSave))
        {
            if(collisionTab == null)
            {
                var view = TileShapeCollectionsPropertiesController.Self.GetView();

                collisionTab = base.CreateTab(view, "TileShapeCollection Properties");
            }

            var nos = treeNode?.Tag as NamedObjectSave;
            GlueElement element = GlueState.Self.CurrentElement;
            if(element == null && nos != null)
            {
                element = ObjectFinder.Self.GetElementContaining(nos);
            }

            TileShapeCollectionsPropertiesController.Self.RefreshViewModelTo(nos,element);

            collisionTab.Show();
            GlueCommands.Self.DialogCommands.FocusTab("TileShapeCollection Properties");
        }
        else if(collisionTab != null)
        {
            collisionTab?.Hide();
        }

        if(TileNodeNetworkPropertiesController.Self.IsTileNodeNetwork(treeNode?.Tag as NamedObjectSave))
        {
            if(nodeNetworkTab == null)
            {
                var view = TileNodeNetworkPropertiesController.Self.GetView();

                nodeNetworkTab = base.CreateTab(view, "TileNodeNetwork Properties");
            }

            var nos = treeNode?.Tag as NamedObjectSave;
            GlueElement element = GlueState.Self.CurrentElement;
            if (element == null && nos != null)
            {
                element = ObjectFinder.Self.GetElementContaining(nos);
            }
            TileNodeNetworkPropertiesController.Self.RefreshViewModelTo(nos, element);

            nodeNetworkTab.Show();
            GlueCommands.Self.DialogCommands.FocusTab("TileNodeNetwork Properties");
        }
        else if(nodeNetworkTab != null)
        {
            nodeNetworkTab?.Hide();
        }

        // Levels are dead, long live inherited levels!
        //if(LevelScreenController.Self.GetIfShouldShow())
        //{
        //    if(levelTab == null)
        //    {
        //        var view = LevelScreenController.Self.GetView();

        //        levelTab = base.CreateTab(view, "Levels");
        //    }

        //    LevelScreenController.Self.RefreshViewModelTo(GlueState.Self.CurrentScreenSave);

        //    levelTab.Show();
        //    LevelScreenController.Self.HandleTabShown();
        //    // prob don't focus it, it's rare the user needs to mess with this
        //}
        //else if(levelTab != null)
        //{
        //    levelTab?.Hide();
        //}
    }

    private void ReactToRfsSelected(ReferencedFileSave rfs)
    {
        // These aren't built anymore, so no command line
        //mCommandLineViewModel.ReferencedFileSave = rfs;

        string fileNameToUse = rfs.Name;
        if(FileManager.GetExtension(rfs.SourceFile) == "tmx")
        {
            fileNameToUse = rfs.SourceFile;
        }

        string fullFileName = GlueCommands.Self.GetAbsoluteFileName(fileNameToUse, true);
        mLastFile = fullFileName;

        EntityCreationManager.Self.ReactToRfsSelected(rfs);
    }

    private void HandleFileChange(string fileName)
    {
        string extension = FileManager.GetExtension(fileName);

        var shouldRefreshErrors = false;
        if(extension == "tmx")
        {
            shouldRefreshErrors = true;
        }

        if(extension == "tsx")
        {
            // oh boy, the user changed a shared tile set.  Time to rebuild everything that uses this tileset
            var allReferencedFileSaves = FileReferenceManager.Self.GetReferencedFileSavesReferencingTsx(fileName).ToArray();

            // build em!
            foreach(var file in allReferencedFileSaves)
            {
                file.PerformExternalBuild(runAsync:true);
            }
            shouldRefreshErrors = true;
            if(allReferencedFileSaves.Length > 0)
            {

                GlueCommands.Self.DoOnUiThread(() =>
                {
                    var nos = GlueState.Self.CurrentNamedObjectSave;
                    var element = GlueState.Self.CurrentElement;
                    if (collisionTab?.IsShown == true && nos != null)
                    {
                        TileShapeCollectionsPropertiesController.Self.RefreshViewModelTo(nos, element);
                    }
                    if(nodeNetworkTab?.IsShown == true && nos != null)
                    {
                        TileNodeNetworkPropertiesController.Self.RefreshViewModelTo(nos, element);
                    }
                });

            }

        }

        // If a png changes, it may be resized. Tiled changes IDs of tiles when a PNG resizes if
        // no external tileset is used, so we want to rebuild the .tmx's.
        if (extension == "png")
        {
            var allReferencedFileSaves = FileReferenceManager.Self.GetReferencedFileSavesReferencingPng(fileName);

            var toListForDebug = allReferencedFileSaves.ToList();

            // build em!
            foreach (var file in allReferencedFileSaves)
            {
                file.PerformExternalBuild(runAsync: true);
            }
        }

        if (this.PluginTab != null && this.PluginTab.Parent != null && fileName == mLastFile)
        {
            if (changesToIgnore == 0)
            {
                //mControl?.LoadFile(fileName);
            }
            else
            {
                changesToIgnore--;
            }
        }

        if(shouldRefreshErrors)
        {
            GlueCommands.Self.RefreshCommands.RefreshErrors();
        }
    }

    private void OnClosedByUser(object sender)
    {
        PluginManager.ShutDownPlugin(this);
    }


    bool IsTmx(AssetTypeInfo ati) =>
        ati?.Extension == "tmx";

    private void HandleAddNewFileOptions(CustomizableNewFileWindow newFileWindow)
    {
        var view = new NewTmxOptionsView();
        var viewModel = new NewTmxViewModel();

        var hasPlatformerEntity = GetIfHasPlatformerEntity();
        var hasTopDownEntity = GetIfHasTopDownEntity();

        if((hasPlatformerEntity == false && hasTopDownEntity == false) ||
            (hasPlatformerEntity && hasTopDownEntity))
        {
            // We don't know what kind of game the user is making, so show it all
            viewModel.PlatformerLevelVisibility = System.Windows.Visibility.Visible;
            viewModel.TopDownLevelVisibility = System.Windows.Visibility.Visible;
        }
        else if(hasPlatformerEntity)
        {
            viewModel.PlatformerLevelVisibility = System.Windows.Visibility.Visible;
            viewModel.TopDownLevelVisibility = System.Windows.Visibility.Collapsed;
        }
        else if(hasTopDownEntity)
        {
            viewModel.PlatformerLevelVisibility = System.Windows.Visibility.Collapsed;
            viewModel.TopDownLevelVisibility = System.Windows.Visibility.Visible;
        }

        if(viewModel.PlatformerLevelVisibility == System.Windows.Visibility.Visible)
        {
            viewModel.SelectedLevel = TmxLevels.OverworldPlatformerA;
        }
        else if(viewModel.TopDownLevelVisibility == System.Windows.Visibility.Visible)
        {
            viewModel.SelectedLevel = TmxLevels.OverworldTopDownA;
        }


        viewModel.IncludeDefaultTileset = true;
        viewModel.IncludeGameplayLayer = true;
        // January 23, 2022
        // This is so common,
        // at least according to
        // Vic's usage, that we should
        // just make it default to true.
        viewModel.IsSolidCollisionBorderChecked = true;
        view.DataContext = viewModel;

        newFileWindow.AddCustomUi(view);

        newFileWindow.SelectionChanged += (not, used) =>
        {
            var ati = newFileWindow.SelectedItem;

            if(IsTmx(ati))
            {
                view.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                view.Visibility = System.Windows.Visibility.Collapsed;
            }
        };

        newFileWindow.GetCreationOption += () =>
        {
            var ati = newFileWindow.SelectedItem;
            if (IsTmx(ati))
            {
                return viewModel;
            }
            else
            {
                return null;
            }
        };
    }

    private bool GetIfHasPlatformerEntity()
    {
        return GlueState.Self.CurrentGlueProject.Entities.Any(item => 
            item.Properties.GetValue<bool>("IsPlatformer"));
    }

    private bool GetIfHasTopDownEntity()
    {
        return GlueState.Self.CurrentGlueProject.Entities.Any(item =>
            item.Properties.GetValue<bool>("IsTopDown"));
    }

    int changesToIgnore = 0;
    //public void SaveTiledMapSave(ChangeType changeType)
    //{
    //    if(mControl != null)
    //    {
    //        string fileName = mLastFile;

    //        FlatRedBall.Glue.Managers.TaskManager.Self.AddSync(() =>
    //            {

    //                changesToIgnore++;

    //                bool saveTsxFiles = changeType == ChangeType.Tileset;

    //                mControl.SaveCurrentTileMap(saveTsxFiles);
    //            },
    //            "Saving tile map");
    //    }
    //}

    void HandleAdjustDisplayedReferencedFile(ReferencedFileSave rfs, ReferencedFileSavePropertyGridDisplayer displayer)
    {
        if (rfs.IsCsvOrTreatedAsCsv && !string.IsNullOrEmpty(rfs.SourceFile))
        {

        }
    }

    void HandleGluxLoad()
    {
        base.AddToToolBar(tiledToolbar, "Tools");

        CodeItemAdderManager.Self.RefreshAppendGenerated();

        // Add the .cs files which include the map drawable batch classes
        FlatRedBall.Glue.Managers.TaskManager.Self.Add( CodeItemAdderManager.Self.UpdateCodePresenceInProject,
            "Adding Tiled .cs files to the project");

        // Make sure the TileMapInfo CustomClassSave is there, and make sure it has all the right properties
        TileMapInfoManager.Self.AddAndModifyTileMapInfoClass();
    }

    internal void UpdateTilesetDisplay()
    {
        //mControl?.UpdateTilesetDisplay();
    }

    public void SaveTilesetFilesToDisk()
    {
        TmxCreationManager.Self.SaveTilesetFilesToDisk();
    }

    public void AddStandardTilesetOnCurrentFile()
    {
        var rfs = GlueState.Self.CurrentReferencedFileSave;

        var rfsAti = rfs?.GetAssetTypeInfo();

        if (rfsAti != null && IsTmx(rfsAti))
        {
            TmxCreationManager.Self.IncludeDefaultTilesetOn(rfs);
        }
    }

    public void AddGameplayLayerToCurrentFile()
    {
        var rfs = GlueState.Self.CurrentReferencedFileSave;

        var rfsAti = rfs?.GetAssetTypeInfo();

        if (rfsAti != null && IsTmx(rfsAti))
        {
            TmxCreationManager.Self.IncludeGameplayLayerOn(rfs);
        }
    }

    public void AddCollisionBorderToCurrentFile()
    {
        var rfs = GlueState.Self.CurrentReferencedFileSave;

        var rfsAti = rfs?.GetAssetTypeInfo();

        if (rfsAti != null && IsTmx(rfsAti))
        {
            TmxCreationManager.Self.AddCollisionBorderOn(rfs);
        }
    }

    #endregion

}
