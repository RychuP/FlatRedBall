﻿using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace GlueFormsCore.ViewModels
{
    public class AddEntityViewModel : ViewModel
    {
        public string Name
        {
            get => Get<string>();
            set
            {
                if (Set(value))
                {
                    var isValid = NameVerifier.IsEntityNameValid(value, null, out string whyIsntValid);

                    if (!isValid)
                    {
                        FailureText = whyIsntValid;
                    }
                    else
                    {
                        FailureText = null;
                    }
                }
            }
        }


        public bool IsSpriteChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsTextChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsCircleChecked
        {
            get => Get<bool>();
            set
            {
                if(Set(value) && value && !hasExplicitlyUncheckedICollidable)
                {
                    IsICollidableChecked = true;
                }
            }
        }

        public bool IsAxisAlignedRectangleChecked
        {
            get => Get<bool>();
            set
            {
                if (Set(value) && value && !hasExplicitlyUncheckedICollidable)
                {
                    IsICollidableChecked = true;
                }
            }
        }

        public bool IsPolygonChecked
        {
            get => Get<bool>();
            set
            {
                if (Set(value) && value && !hasExplicitlyUncheckedICollidable)
                {
                    IsICollidableChecked = true;
                }
            }
        }

        public bool IsIVisibleChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsIClickableChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsIWindowChecked
        {
            get => Get<bool>();
            set
            {
                if(Set(value) && value)
                {
                    IsIVisibleChecked = true;
                }
            }
        }

        public string FailureText
        {
            get => Get<string>();
            set => Set(value);
        }

        [DependsOn(nameof(FailureText))]
        public Visibility FailureTextVisibility => string.IsNullOrWhiteSpace(FailureText) ?
            Visibility.Collapsed : Visibility.Visible;

        bool hasExplicitlyUncheckedICollidable;

        public bool IsICollidableChecked
        {
            get => Get<bool>();
            set
            {
                if (Set(value) && !value)
                {
                    hasExplicitlyUncheckedICollidable = true;
                }
            }
        }
    }
}