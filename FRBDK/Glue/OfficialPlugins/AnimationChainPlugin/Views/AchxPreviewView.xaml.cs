﻿using FlatRedBall.Content.AnimationChain;
using FlatRedBall.IO;
using OfficialPlugins.AnimationChainPlugin.Managers;
using OfficialPlugins.AnimationChainPlugin.ViewModels;
using OfficialPlugins.SpritePlugin.Managers;
using PropertyTools.Wpf;
using SkiaGum.GueDeriving;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OfficialPlugins.ContentPreview.Views
{
    /// <summary>
    /// Interaction logic for AchxPreviewView.xaml
    /// </summary>
    public partial class AchxPreviewView : UserControl
    {
        #region Fields/Properties

        AchxViewModel ViewModel => DataContext as AchxViewModel;

        FilePath textureFilePath;
        FilePath TextureFilePath
        {
            get => textureFilePath;
            set
            {
                if (value != textureFilePath)
                {
                    ForceRefreshMainSpriteTexture(value);
                }
            }
        }

        FilePath achxFilePath;
        public FilePath AchxFilePath
        {
            get => achxFilePath;
            set
            {
                if(value != achxFilePath)
                {
                    achxFilePath = value;
                    ForceRefreshAchx(value);
                }
            }
        }

        public SKBitmap Texture => MainSprite?.Texture;

        SpriteRuntime MainSprite;
        List<PolygonRuntime> Outlines = new List<PolygonRuntime>();

        SolidRectangleRuntime GumBackground { get; set; }

        CameraLogic CameraLogic;

        #endregion

        public AchxPreviewView()
        {
            InitializeComponent();

            this.Loaded += HandleLoaded;

            this.DataContextChanged += HandleDataContextChanged;
            //MemberCategoryManager.SetMemberCategories(PropertyGrid);
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged += HandleViewModelPropertyChanged;
            }
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(ViewModel.SelectedAnimationChain))
            {
                RefreshOutlines();

                GumCanvas.InvalidateVisual();
            }
        }

        public void ForceRefreshAchx(FilePath achxFilePath = null, bool preserveSelection = false)
        {
            var previouslySelected = ViewModel.SelectedAnimationChain;

            achxFilePath = achxFilePath ?? this.AchxFilePath;
            foreach (var outline in Outlines)
            {
                GumCanvas.Children.Remove(outline);
            }

            AnimationChainListSave animationChain = null;
            if (achxFilePath?.Exists() == true)
            {
                animationChain = AnimationChainListSave.FromFile(achxFilePath.FullPath);
            }

            RefreshTexture(achxFilePath, animationChain);

            RefreshOutlines();

            RefreshTreeView(animationChain);

            if(preserveSelection && previouslySelected != null)
            {
                ViewModel.SelectedAnimationChain = ViewModel.VisibleRoot
                    .FirstOrDefault(item => item.Name == previouslySelected.Name);
            }

            GumCanvas.InvalidateVisual();
        }

        private void RefreshTreeView(AnimationChainListSave animationChain)
        {
            ViewModel.VisibleRoot.Clear();

            if (animationChain == null) return;

            foreach(var animation in animationChain.AnimationChains)
            {
                var animationViewModel = new AnimationChainViewModel();
                animationViewModel.SetFrom(animation, ViewModel.ResolutionWidth, ViewModel.ResolutionHeight);
                ViewModel.VisibleRoot.Add(animationViewModel);
            }
        }

        private void RefreshTexture(FilePath value, AnimationChainListSave animationChain)
        {
            if (animationChain == null)
            {
                ForceRefreshMainSpriteTexture(null);
            }
            else
            {

                var firstAnimation = animationChain.AnimationChains.FirstOrDefault(item => item.Frames.Count > 0);
                if (firstAnimation != null)
                {
                    var firstFrame = firstAnimation.Frames.FirstOrDefault();

                    var textureName = firstFrame.TextureName;

                    var textureAbsolute = value.GetDirectoryContainingThis() + textureName;

                    ForceRefreshMainSpriteTexture(textureAbsolute);
                }
            }
        }

        private void RefreshOutlines()
        {
            foreach(var outline in Outlines)
            {
                GumCanvas.Children.Remove(outline);
            }
            Outlines.Clear();
            var texture = MainSprite.Texture;
            if (texture != null && ViewModel != null)
            {
                if(ViewModel.SelectedAnimationFrame != null)
                {
                    CreatePolygonFor(ViewModel.SelectedAnimationFrame.BackingModel);
                }
                else if(ViewModel.SelectedAnimationChain != null)
                {
                    CreatePolygonsFor(ViewModel.SelectedAnimationChain.BackingModel);
                }
                else //if(ViewModel.SelectedAnimationChain == null)
                {
                    foreach (var animationVm in ViewModel.VisibleRoot)
                    {
                        CreatePolygonsFor(animationVm.BackingModel);
                    }
                }
            }

            void CreatePolygonsFor(AnimationChainSave animation)
            {
                foreach (var frame in animation.Frames)
                {
                    CreatePolygonFor(frame);
                }
            }
        }

        private void CreatePolygonFor(AnimationFrameSave frame)
        {
            PolygonRuntime outline = CreateOutlinePolygon(frame);
            Outlines.Add(outline);
            GumCanvas.Children.Add(outline);
        }

        private static PolygonRuntime CreateOutlinePolygon(AnimationFrameSave frame)
        {
            var left = frame.LeftCoordinate;
            var right = frame.RightCoordinate;
            var top = frame.TopCoordinate;
            var bottom = frame.BottomCoordinate;

            var outline = new PolygonRuntime();
            outline.Color = SKColors.White;

            outline.IsFilled = false;
            outline.Points = new List<SKPoint>
                            {
                                new SKPoint(left, top),
                                new SKPoint(right, top),
                                new SKPoint(right, bottom),
                                new SKPoint(left, bottom),
                                new SKPoint(left, top),
                            };
            return outline;
        }

        public void ForceRefreshMainSpriteTexture(FilePath value)
        {
            if (value == null || value.Exists() == false)
            {
                MainSprite.Texture = null;
                GumCanvas.InvalidateVisual();
            }
            else
            {
                try
                {
                    using (var stream = System.IO.File.OpenRead(value.FullPath))
                    {
                        // cache?
                        MainSprite.Texture = SKBitmap.Decode(stream);
                        GumCanvas.InvalidateVisual();

                    }
                }
                catch
                {
                    // do we do anything?
                }
            }

            textureFilePath = value;
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            FillSpriteToView();
        }

        public void Initialize(CameraLogic cameraLogic)
        {
            this.CameraLogic = cameraLogic;

            CreateBackground();
            CreateMainSprite();

            // do this after creating the background so that it can be passed here:
            CameraLogic.Initialize(this, this.GumCanvas, this.GumBackground);

        }


        private void CreateMainSprite()
        {
            MainSprite = new SpriteRuntime();
            MainSprite.Width = 100;
            MainSprite.Height = 100;
            MainSprite.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            MainSprite.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            this.GumCanvas.Children.Add(MainSprite);
        }

        private void GumCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CameraLogic.HandleMousePush(e);
            //MouseEditingLogic.HandleMousePush(e);

            // This allows the canvas to receive focus:
            // Source: https://social.msdn.microsoft.com/Forums/vstudio/en-US/ed6caee6-2cae-4db8-a2df-eafad44dbe37/mouse-focus-versus-keyboard-focus?forum=wpf#:~:text=In%20WPF%2C%20some%20elements%20will%20get%20keyboard%20focus,trick%3A%20userControl.MouseLeftButtonDown%20%2B%3D%20delegate%20%7B%20userControl.Focusable%20%3D%20true%3B
            GumCanvas.Focusable = true;
            IInputElement element = Keyboard.Focus(GumCanvas);
        }

        private void GumCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            CameraLogic.HandleMouseMove(e);
            //MouseEditingLogic.HandleMouseMove(e);
        }

        private void GumCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            CameraLogic.HandleMouseWheel(e);
        }

        private void CreateBackground()
        {
            GumBackground = new SolidRectangleRuntime();
            GumBackground.Color = new SKColor(68, 34, 136);
            GumBackground.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            GumBackground.Width = 100;
            GumBackground.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            GumBackground.Height = 100;
            this.GumCanvas.Children.Add(GumBackground);
        }

        internal void ResetCamera()
        {
            GumCanvas.SystemManagers.Renderer.Camera.X = 0;
            GumCanvas.SystemManagers.Renderer.Camera.Y = 0;
            GumBackground.X = 0;
            GumBackground.Y = 0;

            FillSpriteToView();

        }


        private void FillSpriteToView()
        {
            if (MainSprite.Texture == null || GumCanvas.ActualWidth == 0 || GumCanvas.ActualHeight == 0)
            {
                ViewModel.CurrentZoomPercent = 100;
            }
            else
            {
                var zoomToFitWidth = GumCanvas.ActualWidth / MainSprite.Texture.Width;
                var zoomToFitHeight = GumCanvas.ActualHeight / MainSprite.Texture.Height;

                var minZoom = Math.Min(zoomToFitWidth, zoomToFitHeight);

                ViewModel.CurrentZoomPercent = (float)minZoom * 100;
            }



            CameraLogic.RefreshCameraZoomToViewModel();
        }

        internal void ShowInPropertyGrid(AnimationChainViewModel selectedAnimationChain)
        {
            PropertyGrid.Instance = selectedAnimationChain;
            MemberCategoryManager.SetMemberCategories(PropertyGrid, selectedAnimationChain);
            PropertyGrid.Refresh();
        }

        internal void ShowInPropertyGrid(AnimationFrameViewModel selectedAnimationFrame)
        {
            PropertyGrid.Instance = selectedAnimationFrame;
            MemberCategoryManager.SetMemberCategories(PropertyGrid, selectedAnimationFrame);
            PropertyGrid.Refresh();
        }

        private void TreeListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject originalSource = e.OriginalSource as DependencyObject;
            var treeViewItem = GetTreeViewItemFromOriginalSource(originalSource);

            if (treeViewItem != null)
            {
                if(treeViewItem.DataContext is AnimationChainViewModel animationChainVm)
                {
                    FocusAnimation(animationChainVm.BackingModel);
                }
                else if(treeViewItem.DataContext is AnimationFrameViewModel animationFrameVm)
                {
                    FocusFrame(animationFrameVm.BackingModel);
                }
            }
        }

        private void FocusAnimation(AnimationChainSave backingModel)
        {
            if(backingModel.Frames.Count > 0)
            {
                var firstFrame = backingModel.Frames[0];
                FocusFrame(firstFrame);
            }
        }

        private void FocusFrame(AnimationFrameSave animationFrame)
        {
            var centerX = (animationFrame.LeftCoordinate + animationFrame.RightCoordinate) / 2.0f;
            var centerY = (animationFrame.TopCoordinate + animationFrame.BottomCoordinate) / 2.0f;

            var camera = GumCanvas.SystemManagers.Renderer.Camera;

            // If already zoomed in, stay zoomed in...
            if(ViewModel.CurrentZoomPercent < 100)
            {
                ViewModel.CurrentZoomPercent = 100;
            }
            camera.X = centerX - (GumCanvas.CanvasSize.Width / 2f) / ViewModel.CurrentZoomScale;
            camera.Y = centerY - (GumCanvas.CanvasSize.Height / 2f) / ViewModel.CurrentZoomScale;

            CameraLogic.RefreshCameraZoomToViewModel();
        }

        private TreeListBoxItem GetTreeViewItemFromOriginalSource(DependencyObject originalSource)
        {
            while (originalSource != null && !(originalSource is TreeListBoxItem) && !(originalSource is TreeView))
            {
                originalSource = VisualTreeHelper.GetParent(originalSource);
            }

            return originalSource as TreeListBoxItem;
        }
    }
}
