﻿using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TiledPluginCore.Models;
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
                var model = JsonConvert.DeserializeObject<NewTmxViewModel>(creationOptions);

                if(model != null)
                {


                    if(model.IncludeDefaultTileset)
                    {
                        IncludeDefaultTilesetOn(newFile);


                    }
                    if (model.IncludeGameplayLayer)
                    {
                        IncludeGameplayLayerOn(newFile);
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
            var old = Tileset.ShouldLoadValuesFromSource;
            Tileset.ShouldLoadValuesFromSource = false;
            var fullTmxFile = new FilePath(GlueCommands.Self.FileCommands.GetFullFileName(newFile));
            var tileMapSave = TMXGlueLib.TiledMapSave.FromFile(fullTmxFile.FullPath);

            FilePath existingDefaultTilesetFile = null;

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
                    // save the tsx
                    FileManager.SaveEmbeddedResource(
                        assembly,
                        "TiledPluginCore.Content.Tilesets.StandardTileset.tsx",
                        destinationTsx.FullPath
                        );
                }
                else
                {
                    GlueCommands.Self.PrintOutput($"Did not save {destinationTsx}, file already exists.");
                }

                existingDefaultTilesetFile = destinationTsx;

                if (destinationPng.Exists() == false)
                {
                    // save the png
                    FileManager.SaveEmbeddedResource(
                        assembly,
                        "TiledPluginCore.Content.Tilesets.StandardTilesetIcons.png",
                        destinationPng.FullPath);
                }
                else
                {
                    GlueCommands.Self.PrintOutput($"Did not save {destinationPng}, file already exists.");
                }
            }


            var standardTileset = new Tileset();

            var tmxDirectory = fullTmxFile.GetDirectoryContainingThis();
            standardTileset.Source = FileManager.MakeRelative( existingDefaultTilesetFile.FullPath, tmxDirectory.FullPath) ;

            tileMapSave.Tilesets.Add(standardTileset);

            tileMapSave.Save(fullTmxFile.FullPath);
            Tileset.ShouldLoadValuesFromSource = old;

        }
    }
}
