﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OfficialPlugins.QuickActionPlugin.Views
{
    /// <summary>
    /// Interaction logic for QuickActionButton.xaml
    /// </summary>
    public partial class TitleImageButton : UserControl
    {
        public event RoutedEventHandler Clicked;

        public string Title
        {
            get => TitleTextBlock.Text;
            set => TitleTextBlock.Text = value;
        }

        public string Details
        {
            get => DetailsTextBlock.Text;
            set 
            {
                DetailsTextBlock.Text = value;
                DetailsTooltipTextBlock.Text = value;
            }
        }

        public ImageSource Image
        {
            get => ImageInstance.Source;
            set => ImageInstance.Source = value;
        }

        public double ImageWidthRatio
        {
            get => ImageColumn.Width.Value;
            set => ImageColumn.Width = new GridLength(value, GridUnitType.Star);
        }

        public static readonly DependencyProperty DescribeInTooltipProperty = DependencyProperty.Register(
            nameof(DescribeInToolTip),
            typeof(bool),
            typeof(TitleImageButton),
            new PropertyMetadata(false));

        public bool DescribeInToolTip 
        {
            get => (bool)GetValue(DescribeInTooltipProperty);
            set => SetValue(DescribeInTooltipProperty, value);
        }

        public TitleImageButton()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Clicked?.Invoke(this, e);
        }

        private void Button_MouseEnter_1(object sender, MouseEventArgs e)
        {

        }
    }
}
