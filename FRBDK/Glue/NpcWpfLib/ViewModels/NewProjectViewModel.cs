﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media;
using ToolsUtilities;

namespace Npc.ViewModels
{
    public class NewProjectViewModel : ViewModel
    {
        public Window owner;

        public static char[] InvalidNamespaceCharacters = new char[]
        {
                '~', '`', '!', '@', '#', '$', '%', '^', '&', '*',
                '(', ')', '-', '=', '+', ';', '\'', ':', '"', '<',
                ',', '>', '.', '/', '\\', '?', '[', '{', ']', '}',
                '|',
        // Spaces are handled separately
        //    ' ' 
        };

        public bool UseLocalCopy
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(UseLocalCopy))]
        public bool IsOnlineTemplatesChecked => UseLocalCopy == false;

        public bool OpenSlnFolderAfterCreation
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ProjectDestinationLocation
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ProjectName
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsDifferentNamespaceChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string DifferentNamespace
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsCreateProjectDirectoryChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public Visibility SourceCheckboxVisibility
        {
            get => Get<Visibility>();
            set => Set(value);
        }

        public bool IsSourceCheckboxChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsIncludeSourceEffectivelyChecked => IsSourceCheckboxChecked && SourceCheckboxVisibility == Visibility.Visible;

        [DependsOn(nameof(IsDifferentNamespaceChecked))]
        public Visibility DifferentNamespaceTextBoxVisibility
        {
            get => IsDifferentNamespaceChecked ? Visibility.Visible : Visibility.Collapsed;
        }

        [DependsOn(nameof(ProjectDestinationLocation))]
        [DependsOn(nameof(ProjectName))]
        public string CombinedProjectDirectory
        {
            get
            {
                if (!ProjectDestinationLocation.EndsWith("\\") && !ProjectDestinationLocation.EndsWith("/"))
                {
                    return ProjectDestinationLocation + "\\" + ProjectName;
                }
                else
                {
                    return ProjectDestinationLocation + ProjectName;

                }
            }
        }

        [DependsOn(nameof(IsCreateProjectDirectoryChecked))]
        [DependsOn(nameof(CombinedProjectDirectory))]
        [DependsOn(nameof(ProjectDestinationLocation))]
        public string FinalDirectory
        {
            get
            {
                if(IsCreateProjectDirectoryChecked)
                {
                    return CombinedProjectDirectory;
                }
                else
                {
                    return ProjectDestinationLocation;
                }
            }
        }

        [DependsOn(nameof(ValidationResponse))]
        public string ProjectLocationError => ValidationResponse.Message;

        [DependsOn(nameof(FinalDirectory))]
        [DependsOn(nameof(IsDifferentNamespaceChecked))]
        [DependsOn(nameof(DifferentNamespace))]
        public GeneralResponse ValidationResponse
        {
            get
            {
                var whyIsntValid = GetWhyIsntValid();

                if(!string.IsNullOrEmpty(whyIsntValid))
                {
                    return GeneralResponse.UnsuccessfulWith(whyIsntValid);
                }
                else
                {
                    return GeneralResponse.SuccessfulResponse;
                }
            }
        }

        public bool IsCancelButtonVisible { get; set; } = false;

        public ObservableCollection<PlatformProjectInfo> AvailableProjects
        {
            get;
            private set;
        } = new ObservableCollection<PlatformProjectInfo>();

        public PlatformProjectInfo SelectedProject
        {
            get => Get<PlatformProjectInfo>();
            set => Set(value);
        }

        public Visibility NewProjectWizardVisibility
        {
            get => Get<Visibility>();
            set => Set(value);
        }

        public bool IsOpenNewProjectWizardChecked
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsAddGitIgnoreChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        #region Methods


        public NewProjectViewModel()
        {
            ProjectName = "MyProject";
            UseLocalCopy = false;
            IsOpenNewProjectWizardChecked = true;
            NewProjectWizardVisibility = Visibility.Visible;
            IsAddGitIgnoreChecked = true;
        }

        private string GetWhyIsntValid()
        {
            string whyIsntValid = null;
            if (IsDifferentNamespaceChecked)
            {
                if (string.IsNullOrEmpty(DifferentNamespace))
                {
                    whyIsntValid = "You must enter a non-empty namespace if using a different namespace";
                }
                else if (char.IsDigit(DifferentNamespace[0]))
                {
                    whyIsntValid = "Namespace can't start with a number.";
                }
                else if (DifferentNamespace.Contains(" "))
                {
                    whyIsntValid = "The namespace can't have any spaces.";
                }
                else if (DifferentNamespace.IndexOfAny(InvalidNamespaceCharacters) != -1)
                {
                    whyIsntValid = "The namespace can't contain invalid character " + DifferentNamespace[DifferentNamespace.IndexOfAny(InvalidNamespaceCharacters)];
                }
            }

            if (string.IsNullOrEmpty(whyIsntValid))
            {
                whyIsntValid = ProjectCreationHelper.GetWhyProjectNameIsntValid(ProjectName);
            }

            if(string.IsNullOrEmpty(whyIsntValid))
            {
                if (System.IO.Directory.Exists(FinalDirectory))
                {
                    // it's okay if it exists, just make sure it's either empty, or only has a .gitignore file
                    var contentsOfDirectory = FileManager.GetAllFilesInDirectory(FinalDirectory, null, 0);
                    if(contentsOfDirectory.Count > 0)
                    {
                        whyIsntValid = $"The directory {FinalDirectory} already exists and it is not empty";
                    }
                    var csprojFolder = FinalDirectory + "/" + ProjectName;

                    if(System.IO.Directory.Exists(csprojFolder))
                    {
                        whyIsntValid = $"The directory {FinalDirectory} exists and it contains a subfolder {csprojFolder} which would be overwritten";
                    }
                }
            }


            return whyIsntValid;
        }

        #endregion
    }
}
