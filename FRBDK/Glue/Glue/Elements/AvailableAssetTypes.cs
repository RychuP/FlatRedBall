﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FlatRedBall.IO.Csv;
using FlatRedBall.IO;


using System.Windows.Forms;
using System.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Graphics;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.Elements
{
    #region GlobalOrProjectSpecific enum

    public enum GlobalOrProjectSpecific
    {
        Global,
        ProjectSpecific
    }

    #endregion

    public class CommonAtis
    {
        public AssetTypeInfo Sprite { get; set; }
        public AssetTypeInfo AxisAlignedRectangle { get; set; }
        public AssetTypeInfo Camera { get; set; }
        public AssetTypeInfo Circle { get; set; }
        public AssetTypeInfo Polygon { get; set; }
        public AssetTypeInfo CapsulePolygon { get; set; }
        public AssetTypeInfo Line { get; set; }
        
        public AssetTypeInfo Text { get; set; }
        public AssetTypeInfo PositionedObjectList { get; set; }
        public AssetTypeInfo Layer { get; set; }

        public AssetTypeInfo ShapeCollection { get; set; }
        public AssetTypeInfo RenderTarget { get; set; }

        public AssetTypeInfo Screen { get; set; }

        /// <summary>
        /// AssetTypeInfo for the FlatRedBall AnimationChainList type
        /// </summary>
        public AssetTypeInfo AnimationChainList { get; set; }
    }

    public class AvailableAssetTypes
    {
        #region Fields

        static AvailableAssetTypes mSelf = new AvailableAssetTypes();

        string mStartupPath;
        string mCoreTypesFileLocation;

        List<AssetTypeInfo> mCoreAssetTypes = new List<AssetTypeInfo>();
        List<AssetTypeInfo> mCustomAssetTypes = new List<AssetTypeInfo>();

        List<AssetTypeInfo> mProjectSpecificAssetTypes = new List<AssetTypeInfo>();

        Dictionary<string, List<AssetTypeInfo>> QualifiedAssetTypesDictionary = new Dictionary<string, List<AssetTypeInfo>>();
        Dictionary<string, List<AssetTypeInfo>> RuntimeTypeAssetTypesDictionary = new Dictionary<string, List<AssetTypeInfo>>();
        Dictionary<string, List<AssetTypeInfo>> ExtensionAssetTypeDictionary = new Dictionary<string, List<AssetTypeInfo>>();
        #endregion

        #region Properties

        public static CommonAtis CommonAtis { get; private set; }

        public string GlobalCustomContentTypesFolder
        {
            get
            {
                return mStartupPath + @"\AdditionalContent\";
            }
        }

        public string ProjectSpecificContentTypesFolder
        {
            get
            {
                string projectFileName = GlueState.Self.GlueProjectFileName.FullPath;
                string directory = FileManager.GetDirectory(projectFileName);

                return directory + @"GlueSettings\";
            }
        }


        public static AvailableAssetTypes Self
        {
            get
            {
                return mSelf;
            }
        }


        /// <summary>
        /// A list of extensions to treat as content. This is reset every time a project is loaded
        /// so plugins should add to this in their Glux load event handlers.
        /// </summary>
        public List<string> AdditionalExtensionsToTreatAsAssets
        {
            get;
            private set;
        }

		public IEnumerable<AssetTypeInfo> AllAssetTypes
		{
			get
			{

                return mCoreAssetTypes.Concat(mCustomAssetTypes).ToList();
                
                // Project-specific ATIs will also
                // appear in the mCustomAssetTypes list,
                // so there's no need to loop through this
                // list.
                //foreach (var item in mProjectSpecificAssetTypes)
                //{
                //    yield return item;
                //}

            }
		}

        #endregion


        public void Initialize(string startupPath)
		{
            
            AdditionalExtensionsToTreatAsAssets = new List<string>();

            string fileName = startupPath + @"Content\ContentTypes.csv";

            Initialize(fileName, startupPath);
        }

        public void Initialize(string contentTypesFileLocation, string startupPath)
        {
            mStartupPath = startupPath;
            mCoreTypesFileLocation = contentTypesFileLocation;

            if (!File.Exists(contentTypesFileLocation))
            {
                throw new Exception("Could not find the file " + contentTypesFileLocation + " when trying to initialize the AvailableAssetTypes");
            }
            try
            {
                CsvFileManager.CsvDeserializeList(typeof(AssetTypeInfo), contentTypesFileLocation, mCoreAssetTypes);

                foreach(var item in mCoreAssetTypes)
                {
                    AddToDictionary(item);
                }
                
            }
            catch (Exception e)
            {

                string message = "Could not load the AssetTypes from\n" +
                    contentTypesFileLocation + "\nThis probably means your ContentTypes.csv is corrupt (the file was found).  You should re-install Glue.";


                throw new Exception(message, e);
            }



            if (Directory.Exists(GlobalCustomContentTypesFolder))
            {
                List<string> extraCsvs = FileManager.GetAllFilesInDirectory(GlobalCustomContentTypesFolder, "csv", 0);

                foreach (string file in extraCsvs)
                {
                    try
                    {
                        AddAssetTypes(file);
                    }
                    catch(Exception e)
                    {
                        MessageBox.Show("Failed to add additional asset types from file " + file + "\n\nError info:\n" + e);
                    }
                }
            }

            CommonAtis = new CommonAtis();

            AssetTypeInfo GetAti(string friendlyName) =>
                AllAssetTypes.First(item => item.FriendlyName == friendlyName);

            CommonAtis.AxisAlignedRectangle = GetAti(nameof(AxisAlignedRectangle));
            CommonAtis.CapsulePolygon = GetAti(nameof(CapsulePolygon));
            CommonAtis.Circle = GetAti(nameof(Circle));
            CommonAtis.Camera = GetAti(nameof(Camera));
            CommonAtis.Polygon = GetAti(nameof(Polygon));
            CommonAtis.Line = GetAti(nameof(Line));
            CommonAtis.Sprite = GetAti(nameof(Sprite));
            CommonAtis.Text = GetAti(nameof(Text));
            CommonAtis.Layer = GetAti(nameof(Layer));

            CommonAtis.ShapeCollection = GetAti("Shape Collection (.shcx)");

            CommonAtis.PositionedObjectList = GetAti("PositionedObjectList (Generic)");

            
            CommonAtis.AnimationChainList = AllAssetTypes
                .First(item => item.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.Graphics.Animation.AnimationChainList");

            CommonAtis.Screen = new AssetTypeInfo()
            {
                QualifiedRuntimeTypeName = new PlatformSpecificType
                {
                    QualifiedType = "FlatRedBall.Screens.Screen",
                },
                FriendlyName = "Screen",

            };

            AvailableAssetTypes.Self.AddAssetType(CommonAtis.Screen);

        }

        public void ReactToProjectLoad(string projectFolder)
        {
            foreach (AssetTypeInfo ati in mProjectSpecificAssetTypes)
            {
                RemoveAssetType(ati);
                PluginManager.ReceiveOutput("Removing known content type: " + ati);
            }

            mProjectSpecificAssetTypes.Clear();

            if(!string.IsNullOrEmpty(projectFolder))
            {

                List<string> allCsvs = FileManager.GetAllFilesInDirectory(projectFolder + "GlueSettings/", ".csv");

                foreach (string fileName in allCsvs)
                {
                    //string fileName = projectFolder + "GlueSettings/ProjectSpecificContent.csv";

                    if (File.Exists(fileName))
                    {
                        bool succeeded = AddAssetTypes(fileName, mProjectSpecificAssetTypes, tolerateFailure: true);

                    }
                }



                if (mProjectSpecificAssetTypes.Count != 0)
                {
                    PluginManager.ReceiveOutput("Adding " + mProjectSpecificAssetTypes.Count + " content types");
                    foreach(var ati in mProjectSpecificAssetTypes)
                    {
                        AddAssetType(ati);
                    }
                }
            }
        }

        public void AddAssetType(AssetTypeInfo assetTypeInfo)
        {
            mCustomAssetTypes.Add(assetTypeInfo);

            AddToDictionary(assetTypeInfo);
        }

        private void AddToDictionary(AssetTypeInfo assetTypeInfo)
        {
            var qualified = assetTypeInfo.QualifiedRuntimeTypeName.QualifiedType;

            // This is going to check presence of a value so that first gets priority. 
            if(!string.IsNullOrEmpty(qualified))
            {
                if (!QualifiedAssetTypesDictionary.ContainsKey(qualified))
                {
                    QualifiedAssetTypesDictionary[qualified] = new List<AssetTypeInfo>();
                }

                // Make sure there's no dupes:
                if (QualifiedAssetTypesDictionary[qualified].Any(item => item.IsMatchTo(assetTypeInfo)) == false)
                {
                    QualifiedAssetTypesDictionary[qualified].Add(assetTypeInfo);
                }
            }

            var runtimeType = assetTypeInfo.RuntimeTypeName;
            if(!string.IsNullOrEmpty(runtimeType))
            {
                if(!RuntimeTypeAssetTypesDictionary.ContainsKey(runtimeType))
                {
                    RuntimeTypeAssetTypesDictionary[runtimeType] = new List<AssetTypeInfo>();
                }

                // Make sure there's no dupes:
                if (RuntimeTypeAssetTypesDictionary[runtimeType].Any(item => item.IsMatchTo(assetTypeInfo)) == false)
                {
                    RuntimeTypeAssetTypesDictionary[runtimeType].Add(assetTypeInfo);
                }
            }

            if(!string.IsNullOrEmpty(assetTypeInfo.Extension))
            {
                if(!ExtensionAssetTypeDictionary.ContainsKey(assetTypeInfo.Extension))
                {
                    ExtensionAssetTypeDictionary[assetTypeInfo.Extension] = new List<AssetTypeInfo>();
                }

                // Make sure there's no dupes:
                if (ExtensionAssetTypeDictionary[assetTypeInfo.Extension].Any(item => item.IsMatchTo(assetTypeInfo)) == false)
                {
                    ExtensionAssetTypeDictionary[assetTypeInfo.Extension].Add(assetTypeInfo);
                }
            }
        }

        /// <summary>
        /// Adds asset types from the argument CSV
        /// </summary>
        /// <param name="fullFileName">The full name of the CSV</param>
        public void AddAssetTypes(string fullFileName)
        {
            var tempList = new List<AssetTypeInfo>();
            if(AddAssetTypes(fullFileName, tempList))
            {
                mCustomAssetTypes.AddRange(tempList);
                foreach(var item in tempList)
                {
                    AddToDictionary(item);
                }
            }

        }

        public bool AddAssetTypes(string fullFileName, List<AssetTypeInfo> listToFill, bool tolerateFailure = false)
        {
            bool succeeded = true;

            List<AssetTypeInfo> newTypes = new List<AssetTypeInfo>();

            try
            {
                CsvFileManager.CsvDeserializeList(typeof(AssetTypeInfo), fullFileName, newTypes);
            }
            catch (Exception e)
            {
                succeeded = false;

                if (tolerateFailure)
                {
                    // let's report it:
                    PluginManager.ReceiveError("Error loading CSV: " + fullFileName + "\n" + e.ToString());
                }
                else
                {
                    throw;
                }
            }
            if (succeeded)
            {
                PluginManager.ReceiveOutput("Loading content types from " + fullFileName + " and found " + newTypes.Count + " types");
                listToFill.AddRange(newTypes);
            }
            return succeeded;
        }

        public void RemoveAssetType(AssetTypeInfo assetTypeInfo)
        {
            mCustomAssetTypes.Remove(assetTypeInfo);

            RemoveFromDictionary(this.QualifiedAssetTypesDictionary);
            RemoveFromDictionary(this.RuntimeTypeAssetTypesDictionary);
            RemoveFromDictionary(this.ExtensionAssetTypeDictionary);

            return;

            void RemoveFromDictionary(Dictionary<string, List<AssetTypeInfo>> dictionary)
            {
                List<string> toRemove = new List<string>();
                foreach (var kvp in dictionary)
                {
                    foreach(var item in kvp.Value)
                    {
                        if(item == assetTypeInfo)
                        {
                            kvp.Value.Remove(item);
                            break;
                        }
                    }
                }
            }
        }

		public AssetTypeInfo GetAssetTypeFromExtension(string extension)
		{
            if(this.ExtensionAssetTypeDictionary.ContainsKey(extension))
            {
                return this.ExtensionAssetTypeDictionary[extension].FirstOrDefault();
            }
            else
            {
                return AllAssetTypes.FirstOrDefault(item => item.Extension == extension);
            }
		}

        public AssetTypeInfo GetAssetTypeFromRuntimeType(string runtimeType, object callingObject, bool? isObject = null)
        {
            if (string.IsNullOrEmpty(runtimeType) || runtimeType.StartsWith("Entities\\")) return null;

            ////////////////(fast) Early Out/////////////////////

            if(this.QualifiedAssetTypesDictionary.ContainsKey(runtimeType))
            {
                return this.QualifiedAssetTypesDictionary[runtimeType].FirstOrDefault();
            }
            else if(this.RuntimeTypeAssetTypesDictionary.ContainsKey(runtimeType))
            {
                return this.RuntimeTypeAssetTypesDictionary[runtimeType].FirstOrDefault();
            }
            ////////////////End Early Out///////////////////////
            else
            {
                var assetsToLoopThrough = AllAssetTypes;

                if(isObject.HasValue)
                {
                    if (isObject == true)
                    {
                        assetsToLoopThrough = assetsToLoopThrough.Where(item => item.CanBeObject);
                    }
                    else
                    {
                        assetsToLoopThrough = assetsToLoopThrough.Where(item => !string.IsNullOrEmpty(item.Extension));
                    }
                }

                bool isQualified = runtimeType.Contains('.') || runtimeType.Contains("\\");
                if (isQualified)
                {
                    foreach (var ati in assetsToLoopThrough)
                    {
                        var effectiveQualified =
                            ati.QualifiedRuntimeTypeName.PlatformFunc?.Invoke(callingObject)
                            ?? ati.QualifiedRuntimeTypeName.QualifiedType;

                        if (effectiveQualified == runtimeType)
                        {
                            return ati;
                        }
                    }
                }
                else
                {

                    foreach (var ati in assetsToLoopThrough)
                    {
                        if (ati.RuntimeTypeName == runtimeType)
                        {
                            return ati;
                        }
                    }
                }
            }
			return null;
        }

        public AssetTypeInfo GetAssetTypeFromExtensionAndQualifiedRuntime(string extension, string qualifiedRuntimeType)
        {
            var fromExtension = ExtensionAssetTypeDictionary.ContainsKey(extension) ? ExtensionAssetTypeDictionary[extension] : null;

            if(fromExtension != null)
            {
                return fromExtension.FirstOrDefault(item => item.QualifiedRuntimeTypeName.QualifiedType == qualifiedRuntimeType);
            }

            return null;

        }

        internal void CreateAdditionalCsvFile(string name, GlobalOrProjectSpecific globalOrProjectSpecific)
        {
            // We're going to load the full 
            // AvailableAssetTypes just to get
            // the headers.  Then we'll
            // create a new RCR, copy over the headers
            // then save off the RCR using the argument
            // fileName.  The new one will be empty except
            // for the headers.
            RuntimeCsvRepresentation rcr = CsvFileManager.CsvDeserializeToRuntime(
                mCoreTypesFileLocation);

            rcr.Records = new List<string[]>();


            string desiredFullPath = null;

            if (globalOrProjectSpecific == GlobalOrProjectSpecific.Global)
            {
                desiredFullPath = GlobalCustomContentTypesFolder + name + ".csv";
            }
            else // project-specific
            {
                desiredFullPath = ProjectSpecificContentTypesFolder + name + ".csv";
            }
            if (File.Exists(desiredFullPath))
            {
                MessageBox.Show("The CSV " + desiredFullPath + " already exists.");
            }
            else
            {
                try
                {
                    CsvFileManager.Serialize(rcr, desiredFullPath);
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Unable to save the CSV file.  You need to run Glue as an administrator");
                }
                catch (Exception e)
                {
                    MessageBox.Show("Unknown error attempting to create the CSV:\n\n" + e.ToString());
                }
            }
                
        }
    }
}
