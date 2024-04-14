﻿using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TiledPluginCore.Models;
using TiledPluginCore.Views;
using TileGraphicsPlugin;
using TMXGlueLib;

namespace TiledPluginCore.Managers
{
    public class TmxCreationManager : Singleton<TmxCreationManager>
    {
        //internal bool HandleNewTmxCreation(AssetTypeInfo assetTypeInfo, object extraData, string directory, string name, out string resultingName)
        //{
        //    var canHandle = assetTypeInfo == AssetTypeInfoAdder.Self.TmxAssetTypeInfo;

        //    //////////////Early Out/////////////////
        //    if(canHandle == false)
        //    {
        //        resultingName = null;
        //        return false;
        //    }
        //    /////////////End Early Out//////////////

        //    resultingName = name;

        //    return true;
        //}
        internal void HandleNewTmx(ReferencedFileSave newFile)
        {
            var creationOptions = newFile.GetProperty<string>("CreationOptions");

            if (!string.IsNullOrWhiteSpace(creationOptions))
            {
                var viewModel = JsonConvert.DeserializeObject<NewTmxViewModel>(creationOptions);

                if(viewModel != null)
                {
                    if(viewModel.IncludeDefaultTileset)
                    {
                        IncludeDefaultTilesetOn(newFile);
                    }
                    if (viewModel.IncludeGameplayLayer)
                    {
                        IncludeGameplayLayerOn(newFile);
                    }
                    if(viewModel.ShouldAddCollisionBorder)
                    {
                        AddCollisionBorderOn(newFile);
                    }

                }
            }
        }

        public void IncludeGameplayLayerOn(ReferencedFileSave newFile)
        {
            var old = Tileset.ShouldLoadValuesFromSource;
            Tileset.ShouldLoadValuesFromSource = false;
            var fullTmxFile = new FilePath(GlueCommands.Self.FileCommands.GetFullFileName(newFile));
            var tileMapSave = TMXGlueLib.TiledMapSave.FromFile(fullTmxFile.FullPath);



            var layer = new MapLayer();
            layer.Name = "GameplayLayer";

            var existingLayer = tileMapSave.Layers.First();
            layer.width = existingLayer.width;
            layer.height = existingLayer.height;
            layer.data = existingLayer.data;

            // remove existing layers, replace it with this layer so the user doesn't accidentally place tiles on the wrong layer
            tileMapSave.MapLayers.Clear();
            tileMapSave.MapLayers.Add(layer);

            tileMapSave.Save(fullTmxFile.FullPath);
            Tileset.ShouldLoadValuesFromSource = old;
        }

        public void IncludeDefaultTilesetOn(ReferencedFileSave newFile)
        {
            bool old;
            FilePath fullTmxFile, existingDefaultTilesetFile;
            TiledMapSave tileMapSave;
            SaveTilesetFilesToDisk(out existingDefaultTilesetFile);
            old = Tileset.ShouldLoadValuesFromSource;
            Tileset.ShouldLoadValuesFromSource = false;
            fullTmxFile = new FilePath(GlueCommands.Self.FileCommands.GetFullFileName(newFile));

            tileMapSave = TMXGlueLib.TiledMapSave.FromFile(fullTmxFile.FullPath);

            var standardTileset = new Tileset();

            var tmxDirectory = fullTmxFile.GetDirectoryContainingThis();
            standardTileset.Source = FileManager.MakeRelative(existingDefaultTilesetFile.FullPath, tmxDirectory.FullPath);


            tileMapSave.Tilesets.Add(standardTileset);

            tileMapSave.Save(fullTmxFile.FullPath);
            Tileset.ShouldLoadValuesFromSource = old;

        }

        public void SaveTilesetFilesToDisk()
        {
            SaveTilesetFilesToDisk(out FilePath ____);
        }

        private void SaveTilesetFilesToDisk(out FilePath existingDefaultTilesetFile)
        {
            existingDefaultTilesetFile = null;
            if (existingDefaultTilesetFile == null)
            {
                var folder = new FilePath(
                    GlueState.Self.ContentDirectory);

                var assembly =
                    this.GetType().Assembly;

                var destinationTsx = new FilePath(folder + "StandardTileset.tsx");
                var destinationPng = new FilePath(folder + "StandardTilesetIcons.png");

                if (destinationTsx.Exists() == false)
                {
                    GlueCommands.Self.TryMultipleTimes(() =>
                    {
                        // save the tsx
                        FileManager.SaveEmbeddedResource(
                            assembly,
                            "TiledPluginCore.Content.Tilesets.StandardTileset.tsx",
                            destinationTsx.FullPath
                            );

                    });
                }
                else
                {
                    GlueCommands.Self.PrintOutput($"Did not save {destinationTsx}, file already exists.");
                }

                existingDefaultTilesetFile = destinationTsx;

                if (destinationPng.Exists() == false)
                {
                    GlueCommands.Self.TryMultipleTimes(() =>
                    {
                        // save the png
                        FileManager.SaveEmbeddedResource(
                        assembly,
                        "TiledPluginCore.Content.Tilesets.StandardTilesetIcons.png",
                        destinationPng.FullPath);
                    });
                }
                else
                {
                    GlueCommands.Self.PrintOutput($"Did not save {destinationPng}, file already exists.");
                }
            }
        }

        public void AddCollisionBorderOn(ReferencedFileSave newFile)
        {
            var old = Tileset.ShouldLoadValuesFromSource;
            Tileset.ShouldLoadValuesFromSource = false;

            var fullTmxFile = new FilePath(GlueCommands.Self.FileCommands.GetFullFileName(newFile));
            var tileMapSave = TMXGlueLib.TiledMapSave.FromFile(fullTmxFile.FullPath);

            var tiles = tileMapSave.Layers[0].data[0].tiles;

            for(int y = 0; y < 32; y++)
            {
                for(int x = 0; x < 32; x++)
                {
                    if(x == 0 || y == 0 || x == 31 || y == 31)
                    {
                        var absoluteValue = x + 32 * y;

                        tiles[absoluteValue] = 1;
                    }
                }
            }

            // let's just cheat:
            // to do this we'd have to do all the data to gzip this. Worry about it later
            //tileMapSave.Layers[0].data[0].Value =
            //    "\n   H4sIAAAAAAAACu3NsQ0AAAzCMPj/6X5RFkfK7Cbp+FV8Pp/P5/P5fD6fz+d/+ssPQZZTxQAQAAA=\n  ";

            tileMapSave.Layers[0].data[0].SetTileData(tiles, "base64", "gzip");

            tileMapSave.Tilesets[0].Firstgid = 1;

            tileMapSave.Save(fullTmxFile.FullPath);
            Tileset.ShouldLoadValuesFromSource = old;
        }

        public void HandleAddNewFileOptions(CustomizableNewFileWindow newFileWindow)
        {
            var view = new NewTmxOptionsView();
            var viewModel = new NewTmxViewModel();
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
                view.Visibility = IsTmx(ati).ToVisibility();
            };

            newFileWindow.GetCreationOption += () =>
            {
                var ati = newFileWindow.SelectedItem;
                return IsTmx(ati) ?
                    viewModel :
                    null;
            };
        }

        public bool IsTmx(AssetTypeInfo ati) =>
            ati?.Extension == "tmx";
    }
}
