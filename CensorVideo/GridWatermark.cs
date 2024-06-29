using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace CensorVideo
{
    public class GridWatermark : BindableBase
    {
        public Grid WatermarkGrid { get; private set; }
        public Thumb topLeftThumb { get; private set; }
        public Thumb topRightThumb { get; private set; }
        public Thumb bottomLeftThumb { get; private set; }
        public Thumb bottomRightThumb { get; private set; }

        public TextBlock textWatermark { get; private set; }
        public Image imageWatermark { get; private set; }
        public Button delBtn { get; private set; }
        public Rectangle rectangle { get; private set; }

        public string Type { get; set; } // type = "image" or "text" or "rectangle"
        public bool Deleted;

        private double txtTranslationX = 0;
        private double txtTranslationY = 0;

        private string _text = string.Empty;
        public string Text
        {
            get { return _text; }
            set
            {
                if (value == null) return;
                _text = value;
                OnPropertyChanged("Text");
            }
        }

        private string _font;
        public string Font
        {
            get { return _font; }
            set
            {
                if (value == null) return;
                _font = value;
                OnPropertyChanged("Font");
            }
        }

        private SolidColorBrush _color;
        public SolidColorBrush Color
        {
            get { return _color; }
            set
            {
                if (value == null) return;
                _color = value;
                OnPropertyChanged("Color");
            }
        }

        private BitmapImage _bitmapSource;
        public BitmapImage BitmapSource
        {
            get { return _bitmapSource; }
            set
            {
                if (value == _bitmapSource) return;
                _bitmapSource = value;
                OnPropertyChanged("BitmapSource");
            }
        }

        public GridWatermark(string type)
        {
            this.Type = type;
            this.Deleted = false;

            WatermarkGrid = new Grid()
            {
                Width = 70,
                Height = 70,
                ManipulationMode = ManipulationModes.All,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                BorderBrush = new SolidColorBrush(Windows.UI.Colors.White),
                BorderThickness = new Thickness(1),
            };
            imageWatermark = new Image()
            {
                Width = 50,
                Height = 50,
            };
            textWatermark = new TextBlock()
            {
                Foreground = new SolidColorBrush(Windows.UI.Colors.Red),
                FontSize = 15,
                Width = 50,
                Height = 50,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Text = Text
            };

            delBtn = new Button()
            {
                Content = "X",
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                MinWidth = 15,
                MinHeight = 15,
                Width = 20,
                Height = 20,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = new SolidColorBrush(Windows.UI.Colors.White),
                Background = new SolidColorBrush(Windows.UI.Colors.Red),
            };

            rectangle = new Rectangle() 
            { 
                Width = 70,
                Height= 70,
                
            };

            if (type == "image")
            {
                WatermarkGrid.Children.Add(imageWatermark);
            }
            else if (type == "text")
            {
                WatermarkGrid.Children.Add(textWatermark);
            }
            else if (type == "rect")
            {
                 WatermarkGrid.Children.Add(rectangle);
            }


            topLeftThumb = CreateThumb(HorizontalAlignment.Left, VerticalAlignment.Top);
            topRightThumb = CreateThumb(HorizontalAlignment.Right, VerticalAlignment.Top);
            bottomLeftThumb = CreateThumb(HorizontalAlignment.Left, VerticalAlignment.Bottom);
            bottomRightThumb = CreateThumb(HorizontalAlignment.Right, VerticalAlignment.Bottom);


            WatermarkGrid.Children.Add(topLeftThumb);
            WatermarkGrid.Children.Add(bottomLeftThumb);
            WatermarkGrid.Children.Add(topRightThumb);
            WatermarkGrid.Children.Add(bottomRightThumb);
            WatermarkGrid.Children.Add(delBtn);

            WatermarkGrid.ManipulationDelta += ManipulationDelta;
            delBtn.Click += delBtn_Click;
        }

        private void ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            Grid gb = sender as Grid;
            if (e.Delta.Translation.X != 0 || e.Delta.Translation.Y != 0)
            {
                txtTranslationX += e.Delta.Translation.X;
                txtTranslationY += e.Delta.Translation.Y;
                gb.RenderTransform = new TranslateTransform
                {
                    X = txtTranslationX,
                    Y = txtTranslationY
                };

            }
        }

        private Thumb CreateThumb(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment)
        {
            Thumb thumb = new Thumb
            {
                Width = 10,
                Height = 10,
                Background = new SolidColorBrush(Windows.UI.Colors.Gray),
                HorizontalAlignment = horizontalAlignment,
                VerticalAlignment = verticalAlignment,
                Margin = new Thickness(-4)
            };
            thumb.DragStarted += Thumb_DragStarted;
            thumb.DragDelta += Thumb_DragDelta;
            thumb.DragCompleted += Thumb_DragCompleted;

            return thumb;
        }

        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            WatermarkGrid.ManipulationMode = ManipulationModes.None;
        }

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            WatermarkGrid.ManipulationMode = ManipulationModes.All;
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Thumb thumb = sender as Thumb;

            var parent = thumb.Parent as FrameworkElement;

            double newWidth = parent.Width;
            double newHeight = parent.Height;
            if (thumb == topLeftThumb)
            {
                newWidth = parent.Width - e.HorizontalChange;
                newHeight = parent.Height - e.VerticalChange;
                if (newWidth > 20 && newHeight > 20)
                {
                    parent.Width = newWidth;
                    parent.Height = newHeight;
                    Canvas.SetLeft(parent, Canvas.GetLeft(parent) + e.HorizontalChange);
                    Canvas.SetTop(parent, Canvas.GetTop(parent) + e.VerticalChange);
                }
            }
            else if (thumb == topRightThumb)
            {
                newWidth = parent.Width + e.HorizontalChange;
                newHeight = parent.Height - e.VerticalChange;
                if (newWidth > 20 && newHeight > 20)
                {
                    parent.Width = newWidth;
                    parent.Height = newHeight;
                    Canvas.SetLeft(parent, Canvas.GetLeft(parent));
                    Canvas.SetTop(parent, Canvas.GetTop(parent) + e.VerticalChange);
                }
            }
            else if (thumb == bottomLeftThumb)
            {
                newWidth = parent.Width - e.HorizontalChange;
                newHeight = parent.Height + e.VerticalChange;
                if (newWidth > 20 && newHeight > 20)
                {
                    parent.Width = newWidth;
                    parent.Height = newHeight;
                    Canvas.SetLeft(parent, Canvas.GetLeft(parent) + e.HorizontalChange);
                    Canvas.SetTop(parent, Canvas.GetTop(parent));
                }
            }
            else if (thumb == bottomRightThumb)
            {
                newWidth = parent.Width + e.HorizontalChange;
                newHeight = parent.Height + e.VerticalChange;
                if (newWidth > 20 && newHeight > 20)
                {
                    parent.Width = newWidth;
                    parent.Height = newHeight;
                    Canvas.SetLeft(parent, Canvas.GetLeft(parent));
                    Canvas.SetTop(parent, Canvas.GetTop(parent));
                }
            }

            if (Type == "image")
            {
                imageWatermark.Width = parent.Width - 2 * thumb.Width;
                imageWatermark.Height = parent.Height - 2 * thumb.Height;
            }
            else if (Type == "text")
            {
                WatermarkGrid.Width = parent.Width;
                WatermarkGrid.Height = parent.Height;
                textWatermark.Width = parent.Width - 2 * thumb.Width;
                textWatermark.Height = parent.Height - 2 * thumb.Height;
                if (newWidth >= newHeight)
                {
                    textWatermark.FontSize = (int)newWidth / 50 * 20;
                }
                else
                {
                    textWatermark.FontSize = (int)newHeight / 50 * 20;
                }
            }
            else if (Type == "rect")
            {
                WatermarkGrid.Width = parent.Width;
                WatermarkGrid.Height = parent.Height;
                rectangle.Width = parent.Width;
                rectangle.Height = parent.Height;
            } 
        }

        public Action<GridWatermark> DeleteCallback { get; set; }
        public void delBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Deleted = true;
            var delBtn = sender as Button;
            var grid = delBtn.Parent as Grid;
            var canvas = grid.Parent as Canvas;
            if (grid != null)
            {
                canvas.Children.Remove(grid);
            }
            DeleteCallback?.Invoke(this);
        }

    }
}
