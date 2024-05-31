﻿using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.WildcardFilePlugin
{
    [Export(typeof(PluginBase))]
    internal class MainWildcardFilePlugin : EmbeddedPlugin
    {
        public override void StartUp()
        {
            this.ReactToFileChange += HandleFileChanged;
        }

        ConcurrentQueue<FilePath> changedExistingFiles = new ConcurrentQueue<FilePath>();

        private async void HandleFileChanged(FilePath changedFile, FileChangeType fileChangeType)
        {
            var project = GlueState.Self.CurrentGlueProject;

            var exists = changedFile.Exists();

            if(exists)
            {
                if(project != null)
                {
                    changedExistingFiles.Enqueue(changedFile);
                    // Sept 26, 2023 - why do we await here? Shouldn't we just fire and forget?
                    //await TaskManager.Self.AddAsync(() =>
                    _ = TaskManager.Self.AddAsync(() =>
                    {
                        var glueState = GlueState.Self;
                        var contentFolder = glueState.ContentDirectory;

                        Dictionary<ReferencedFileSave, List < FilePath >> filesMatchingPattern = new ();

                        // This is the slowest part of the methods here. We can do this up-front once, then re-use it for every 
                        // file that has been queued up for change, speeding up the process when there are a lot of files that have changed:
                        foreach(var wildcardFile in project.GlobalFileWildcards)
                        {
                            var wildcardFilePath = GlueCommands.Self.GetAbsoluteFilePath(wildcardFile);
                            FilePath directoryWithNoWildcard = wildcardFilePath;
                            while (directoryWithNoWildcard.FullPath.Contains("*"))
                            {
                                directoryWithNoWildcard = directoryWithNoWildcard.GetDirectoryContainingThis();
                            }

                            var suffix = wildcardFilePath.RelativeTo(directoryWithNoWildcard);

                            List<FilePath> filesMatchingSuffixFilePattern = null;

                            if (suffix.StartsWith("**") && suffix != "**" && suffix.Contains('/'))
                            {
                                var suffixFilePattern = wildcardFilePath.NoPath;

                                filesMatchingSuffixFilePattern = System.IO.Directory
                                    .GetFiles(directoryWithNoWildcard.FullPath, suffixFilePattern, System.IO.SearchOption.AllDirectories)
                                    .Select(item => new FilePath(item))
                                    .ToList();
                            }

                            filesMatchingPattern[wildcardFile] = filesMatchingSuffixFilePattern;

                        }


                        while (changedExistingFiles.TryDequeue(out FilePath existingFile))
                        {
                            if(existingFile.IsDirectory)
                            {
                                foreach (var wildcardFile in project.GlobalFileWildcards)
                                {
                                    var absoluteFile = new FilePath(contentFolder + wildcardFile.Name);
                                    var files = WildcardReferencedFileSaveLogic.GetFilesForWildcard(absoluteFile);

                                    foreach(var candidate in files)
                                    {
                                        if(candidate.IsRelativeTo(existingFile))
                                        {
                                            TryAddFile(candidate, project, wildcardFile);

                                        }
                                    }
                                }
                            }
                            else
                            {
                                // was it added?
                                foreach (var wildcardPattern in project.GlobalFileWildcards)
                                {
                                    // Note - a file may change 2x really fast (one after another)
                                    // If that happens, the alradyExists may be false both times, and
                                    // the file may get added 2x. We need to instead wrap everything in tasks to prevent this from happening:
                                    if(glueState.CurrentGlueProject != null && IsFileRelativeToWildcard(existingFile, wildcardPattern, filesMatchingPattern[wildcardPattern]))
                                    {
                                        TryAddFile(existingFile, project, wildcardPattern);
                                        break;
                                    }
                                }
                            }

                        }
                    }, $"MainWildcardFilePlugin process files in HandleFileChanged",
                    TaskExecutionPreference.AddOrMoveToEnd);

                }
            }
            else
            {
                // was it removed?
                var wildcardGlobalFiles = project.GlobalFiles.Where(item => item.IsCreatedByWildcard).ToList();
                foreach (var file in wildcardGlobalFiles)
                {
                    var fileForCandidate = GlueCommands.Self.GetAbsoluteFilePath(file);

                    if(fileForCandidate == changedFile || fileForCandidate.IsRelativeTo(changedFile))
                    {
                        await GlueCommands.Self.GluxCommands.RemoveReferencedFileAsync(file, null);
                    }
                }
            }
        }

        private static void TryAddFile(FilePath newFile, GlueProjectSave project, ReferencedFileSave wildcardFile)
        {
            var newRfsName = newFile.RelativeTo(GlueState.Self.ContentDirectory);
            var alreadyExists = project.GlobalFiles.Any(item => item.Name == newRfsName);
            if (!alreadyExists)
            {
                // clone it, add it here
                var clone = wildcardFile.Clone();
                clone.Name = newRfsName;
                clone.IsCreatedByWildcard = true;
                var fireAndForget = GlueCommands.Self.GluxCommands.AddReferencedFileToGlobalContentAsync(clone);
            }
        }

        private bool IsFileRelativeToWildcard(FilePath changedFilePath, SaveClasses.ReferencedFileSave wildcardFile, IEnumerable<FilePath> filesMatchingSuffixFilePattern)
        {
            var changedFileName = changedFilePath.FullPath;

            // This could be faster, but we'll cheat and use some (probably slow) operations:
            var wildcardFilePath = GlueCommands.Self.GetAbsoluteFilePath(wildcardFile);
            FilePath directoryWithNoWildcard = wildcardFilePath;
            while (directoryWithNoWildcard.FullPath.Contains("*"))
            {
                directoryWithNoWildcard = directoryWithNoWildcard.GetDirectoryContainingThis();
            }

            var suffix = wildcardFilePath.RelativeTo(directoryWithNoWildcard);

            if(suffix.StartsWith("**"))
            {
                // we're going to any depth
                if(suffix == "**")
                {
                    // as long as the file is relative to the wildcard path, then return true
                    return directoryWithNoWildcard.IsRootOf(changedFileName);
                }
                else if(suffix.Contains('/'))
                {
                    return filesMatchingSuffixFilePattern.Contains(changedFilePath);
                }
                else
                {
                    // unsupported pattern
                    return false;
                }
            }
            else
            {
                // we're only looking in the current folder:
                var suffixFilePattern = wildcardFilePath.NoPath;

                if(directoryWithNoWildcard.Exists())
                {

                    var allFiles = System.IO.Directory
                        .GetFiles(directoryWithNoWildcard.FullPath, suffixFilePattern, System.IO.SearchOption.TopDirectoryOnly)
                        .Select(item => new FilePath(item));
                    return allFiles.Contains(changedFilePath);
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
