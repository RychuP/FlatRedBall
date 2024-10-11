﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.IO
{
    #region ChangeInformation class

    public enum FileChangeType
    {
        Modified,
        Deleted,
        Created,
        Renamed
        // todo - expand here
    }

    public class FileChange
    {
        public FilePath FilePath { get; set; }
        public FileChangeType ChangeType { get; set; }

        public override string ToString() => FilePath?.ToString();
    }

    class ChangeInformation
    {
        static TimeSpan mMinimumTimeAfterChangeToReact = new TimeSpan(0, 0, 0, 0, 500);

        List<FileChange> mChangedFiles = new List<FileChange>();

        public bool CanFlush
        {
            get
            {
                return DateTime.Now - LastAdd > mMinimumTimeAfterChangeToReact;
            }
        }

        public DateTime LastAdd
        {
            get;
            private set;
        }

        public ReadOnlyCollection<FileChange> Changes
        {
            get;
            private set;
        }

        public ChangeInformation()
        {
            Changes = new ReadOnlyCollection<FileChange>(mChangedFiles);
        }

        /// <summary>
        /// Attempts to add the change to the internal list of changed files. This only adds if the file is not already added
        /// </summary>
        /// <param name="fileName">The file to add</param>
        /// <param name="changeType">The file change</param>
        /// <returns>Whether the file was added</returns>
        public bool Add(FilePath filePath, FileChangeType changeType)
        {
            var wasAdded = false;
            lock (mChangedFiles)
            {
                var contains = mChangedFiles.Any(item => item.FilePath == filePath);

                if (!contains)
                {

                    mChangedFiles.Add(new FileChange
                    {
                        FilePath = filePath,
                        ChangeType = changeType
                    });
                    wasAdded = true;
                }

                LastAdd = DateTime.Now;
            }
            return wasAdded;
        }

        public void Clear()
        {
            lock (mChangedFiles)
            {
                mChangedFiles.Clear();
            }
        }

        public void Sort(Comparison<FileChange> comparison)
        {
            lock (mChangedFiles)
            {
                mChangedFiles.Sort(comparison);
            }

        }
    }

    #endregion

    public class ChangedFileGroup
    {
        #region Fields

        // Files like 
        // the .glux and
        // .csproj files are
        // saved by Glue, but
        // when they change on
        // disk Glue needs to react
        // to the change.  To react to
        // the change, Glue keeps a file
        // watch on these files.  However
        // when Glue saves these files it kicks
        // of a file change.  Therefore, any time
        // Glue changes one of these files it needs
        // to know to ignore the next file change since
        // it came from itself.  Furthermore, multiple plugins
        // and parts of Glue may kick off multiple saves.  Therefore
        // we can't just keep track of a bool on whether to ignore the
        // next change or not - instead we have to keep track of an int
        // to mark how many changes Glue should ignore.
        ConcurrentDictionary<FilePath, int> mChangesToIgnore;

        ConcurrentDictionary<FilePath, DateTimeOffset> timedChangesToIgnore;

        FileSystemWatcher mFileSystemWatcher;

        static TimeSpan mMinimumTimeAfterChangeToReact = new TimeSpan(0, 0, 1);

        ChangeInformation mChangedFiles;
        ChangeInformation mDeletedFiles;

        DateTime mLastModification;

        #endregion

        #region Properties

        public bool CanFlush
        {
            get
            {
                return mChangedFiles.CanFlush && mDeletedFiles.CanFlush;
            }
        }

        object LockObject;

        public bool Enabled
        {
            get { return mFileSystemWatcher.EnableRaisingEvents; }
            set { mFileSystemWatcher.EnableRaisingEvents = value; }
        }

        public string Path
        {
            get
            {
                return mFileSystemWatcher.Path;
            }
            set
            {
                if (value?.EndsWith("/") == true)
                {
                    mFileSystemWatcher.Path = value.Substring(0, value.Length - 1);
                }
                else
                {
                    mFileSystemWatcher.Path = value;
                }
            }
        }

        public ReadOnlyCollection<FileChange> DeletedFiles => mDeletedFiles.Changes;

        public ReadOnlyCollection<FileChange> ChangedFiles
        {
            get { return mChangedFiles.Changes; }
        }

        public IEnumerable<FileChange> AllFiles
        {
            get
            {
                List<FileChange> toReturn = new List<FileChange>();
                lock (LockObject)
                {
                    toReturn.AddRange(mDeletedFiles.Changes);
                    toReturn.AddRange(mChangedFiles.Changes);
                }
                return toReturn;
            }
        }

        #endregion

        public Comparison<FileChange> SortDelegate;

        public ChangedFileGroup()
        {
            mChangesToIgnore = new ConcurrentDictionary<FilePath, int>();
            timedChangesToIgnore = new ConcurrentDictionary<FilePath, DateTimeOffset>();

            LockObject = new object();
            mFileSystemWatcher = new FileSystemWatcher();

            mChangedFiles = new ChangeInformation();
            mDeletedFiles = new ChangeInformation();

            mFileSystemWatcher.Filter = "*.*";
            mFileSystemWatcher.IncludeSubdirectories = true;
            mFileSystemWatcher.NotifyFilter =
                NotifyFilters.LastWrite |
                // tiled seems to save the file with a temp name like
                // MyFile.tmx.D1234
                // then it renames it to
                // MyFile.tmx
                // We need to handle the filename changing or else Glue isn't notified of the change.
                // Update - only do this for TMX (see below).
                // Update 2 - We need to handle renames incase
                //            the user renamed files to solve missing
                //            file errors. Just don't handle the .glux
                //            so we don't get double-loads.
                NotifyFilters.FileName |
                NotifyFilters.DirectoryName;


            mFileSystemWatcher.Deleted += HandleFileSystemDelete;
            mFileSystemWatcher.Changed += HandleFileSystemChange;
            // May 6, 2022
            mFileSystemWatcher.Created += HandleFileSystemCreated;
            mFileSystemWatcher.Renamed += HandleRename;
        }

        string[] extensionsToIgnoreRenames_CreatesAndDeletes = new string[]
        {
            "glux",
            "gluj",
            SaveClasses.GlueProjectSave.ScreenExtension,
            SaveClasses.GlueProjectSave.EntityExtension
        };



        public void ClearIgnores()
        {
            mChangesToIgnore.Clear();
        }

        public int NumberOfTimesToIgnore(string file)
        {

            if (FileManager.IsRelative(file))
            {
                throw new Exception("File name should be absolute");
            }
            string standardized = FileManager.Standardize(file, null, false).ToLowerInvariant();

            if (mChangesToIgnore.TryGetValue(standardized, out var change))
            {
                return change;
            }
            else
            {
                return 0;
            }
        }

        void HandleFileSystemDelete(object sender, FileSystemEventArgs e)
        {
            string fileName = e.FullPath;
            ChangeInformation toAddTo = mDeletedFiles;

            var extension = FileManager.GetExtension(fileName);
            bool shouldProcess = !extensionsToIgnoreRenames_CreatesAndDeletes.Contains(extension) &&
                !IsSkippedBasedOnTypeOrLocation(e.Name);


            if (shouldProcess)
            {
                TryAddChangedFileTo(fileName, FileChangeType.Deleted, toAddTo);
            }

        }

        void HandleFileSystemChange(object sender, FileSystemEventArgs e)
        {
            string fileName = e.FullPath;

            bool shouldProcess = !IsSkippedBasedOnTypeOrLocation(e.Name);


            if (e.ChangeType == WatcherChangeTypes.Renamed)
            {
                var extension = FileManager.GetExtension(fileName);
                shouldProcess = !extensionsToIgnoreRenames_CreatesAndDeletes.Contains(extension);
            }

            if (shouldProcess)
            {
                ChangeInformation toAddTo = mChangedFiles;

                TryAddChangedFileTo(fileName, FileChangeType.Modified, toAddTo);
            }
        }

        private void HandleFileSystemCreated(object sender, FileSystemEventArgs e)
        {
            string fileName = e.FullPath;

            var extension = FileManager.GetExtension(fileName);
            bool shouldProcess = !extensionsToIgnoreRenames_CreatesAndDeletes.Contains(extension) &&
                !IsSkippedBasedOnTypeOrLocation(e.Name);


            if (shouldProcess)
            {
                ChangeInformation toAddTo = mChangedFiles;

                TryAddChangedFileTo(fileName, FileChangeType.Created, toAddTo);
            }
        }

        private void HandleRename(object sender, RenamedEventArgs e)
        {
            var extension = FileManager.GetExtension(e.Name);

            ///////////////early out////////////////
            if(string.IsNullOrEmpty(e.Name))
            {
                // early out
                return;
            }
            ////////////end early out///////////////
            var shouldProcess = extensionsToIgnoreRenames_CreatesAndDeletes.Contains(extension) == false &&
                !IsSkippedBasedOnTypeOrLocation(e.Name);

            if (shouldProcess)
            {
                // Process both the old and the new just in case someone depended on the old
                TryAddChangedFileTo(e.OldFullPath, FileChangeType.Renamed, mChangedFiles);
                TryAddChangedFileTo(e.FullPath, FileChangeType.Renamed, mChangedFiles);
            }
        }

        bool IsSkippedBasedOnTypeOrLocation(FilePath filePath) =>
            filePath.Standardized.Contains("/.vs/")
            || filePath.Standardized.StartsWith(".vs")
            || filePath.Standardized.Contains("/bin/Debug/", StringComparison.InvariantCultureIgnoreCase)
            || filePath.Standardized.EndsWith(".generated.cs", StringComparison.InvariantCultureIgnoreCase);

        private void TryAddChangedFileTo(FilePath fileName, FileChangeType fileChangeType, ChangeInformation toAddTo)
        {
            bool wasAdded = false;

            bool wasIgnored = false;

            // When a file changes, it's deleted and then added. Therefore, if we make a change (delete + create), but we ignore
            // one change, that means that the delete will get ignored, but the create won't. Therefore, we should not check ignores
            // on deletes:
            if (fileChangeType != FileChangeType.Deleted)
            {
                wasIgnored = TryIgnoreFileChange(fileName);
            }

            if(!wasIgnored)
            {
                if(timedChangesToIgnore.TryGetValue(fileName, out DateTimeOffset expiration))
                {
                    wasIgnored = expiration > DateTimeOffset.Now;
                }
            }

            if (wasIgnored)
            {
                if (FileWatchManager.IsPrintingDiagnosticOutput)
                {
                    GlueCommands.Self.PrintOutput($"Ignoring {fileChangeType} on {fileName}");
                }
            }
            else // !wasIgnored
            {
                lock (LockObject)
                {
                    wasAdded = toAddTo.Add(fileName, fileChangeType);
                    if (SortDelegate != null)
                    {
                        toAddTo.Sort(SortDelegate);
                    }
                }
                if (wasAdded && FileWatchManager.IsPrintingDiagnosticOutput)
                {
                    GlueCommands.Self.PrintOutput($"Storing change {fileChangeType} on {fileName} for flushing");
                }
            }
            mLastModification = DateTime.Now;
        }

        bool TryIgnoreFileChange(FilePath fileName)
        {
            int changesToIgnore;

            if(mChangesToIgnore.TryGetValue(fileName, out changesToIgnore))
            {
                mChangesToIgnore[fileName] = System.Math.Max(0, changesToIgnore - 1);
            }
            else
            {
                changesToIgnore = 0;
            }
            return changesToIgnore > 0;
        }


        public void IgnoreNextChangeOn(string fileName)
        {
            if (FileManager.IsRelative(fileName))
            {
                throw new Exception("File name should be absolute");
            }
            string standardized = FileManager.Standardize(fileName, null, false).ToLowerInvariant();
            if (mChangesToIgnore.ContainsKey(standardized))
            {
                mChangesToIgnore[standardized] = 1 + mChangesToIgnore[standardized];
            }
            else
            {
                mChangesToIgnore[standardized] = 1;
            }
        }

        public void IgnoreChangeOnFileUntil(FilePath filePath, DateTimeOffset expiration)
        {
            if (timedChangesToIgnore.TryGetValue(filePath, out DateTimeOffset existing))
            {
                timedChangesToIgnore[filePath] = existing > expiration ? expiration : expiration;
            }
            else
            {
                timedChangesToIgnore[filePath] = expiration;
            }
        }

        public void Clear()
        {
            mDeletedFiles.Clear();
            mChangedFiles.Clear();
        }
    }
}
