using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.Security.Authentication.Identity.Core;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using System.Collections.ObjectModel;
using Windows.Media.Playback;
using Microsoft.Graphics.Canvas;
using System.ComponentModel;
using Windows.Graphics.Imaging;
using Windows.Media.Transcoding;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI;
using Win2DCustomEffects;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CensorVideo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private MediaComposition composition;
        private MediaStreamSource mediaStreamSource;
        private MediaClip clip;

        private TimeSpan videoDuration;
        private DispatcherTimer timer;

        private GridWatermark gridWatermark { get; set; }

        public ObservableCollection<GridWatermark> overlayElements { get; set; }
        public Dictionary<string, SolidColorBrush> colors;

        public MainPage()
        {
            this.InitializeComponent();
            composition = new MediaComposition();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            // Đăng ký sự kiện khi video kết thúc
            mediaPlayerElement.MediaPlayer.MediaEnded += videoEnded;

            durationSlider.ValueChanged += durationSlider_ValueChanged;
            PlayBtn.IsEnabled = false;

            // add font family list in the combobox
            List<string> systemFonts = FontHelper.GetSystemFonts();
            fontCombo.ItemsSource = systemFonts;
            fontCombo.SelectedValue = null;

            overlayElements = new ObservableCollection<GridWatermark>();
            DataContext = this;
            WatermarkListView.ItemsSource = overlayElements;

            // khai báo màu
            colors = new Dictionary<string, SolidColorBrush>();
            colors.Add("Red", new SolidColorBrush(Windows.UI.Colors.Red));
            colors.Add("Green", new SolidColorBrush(Windows.UI.Colors.Green));
            colors.Add("Blue", new SolidColorBrush(Windows.UI.Colors.Blue));
            colors.Add("Yellow", new SolidColorBrush(Windows.UI.Colors.Yellow));
            colors.Add("Orange", new SolidColorBrush(Windows.UI.Colors.Orange));
            colors.Add("Purple", new SolidColorBrush(Windows.UI.Colors.Purple));

            // assign list color to colorCombo itemSource
            colorCombo.ItemsSource = colors;

            // Specify the ComboBox items text and value
            colorCombo.SelectedValue = null;
        }


        private async void addBtn_Click(object sender, RoutedEventArgs e)
        {
            PlayBtn.Visibility = Visibility.Visible;
            PauseBtn.Visibility = Visibility.Collapsed;
            mediaPlayerElement.MediaPlayer.Pause();
            await UploadVideo();
        }

        private async Task UploadVideo()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add(".mp4");
            picker.FileTypeFilter.Add(".mov");
            Windows.Storage.StorageFile pickedFile = await picker.PickSingleFileAsync();
            if (pickedFile != null)
            {
                MainCanvas.Visibility = Visibility.Visible;
                composition.OverlayLayers.Clear();
                composition.Clips.Clear();
                overlayElements.Clear();
                RemoveOverlay.IsEnabled = false;
                AddOverlay.IsEnabled = false;
                MainCanvas.Children.Clear();

                startTxt.Text = TimeSpan.Zero.ToString(@"hh\:mm\:ss");
                durationSlider.Minimum = 0;

                PlayBtn.IsEnabled = true;
            }
            else
            {
                mediaPlayerElement.MediaPlayer.Pause();
                timer.Stop();
                return;
            }
            // These files could be picked from a location that we won't have access to later
            var storageItemAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            storageItemAccessList.Add(pickedFile);

            clip = await MediaClip.CreateFromFileAsync(pickedFile);
            composition.Clips.Add(clip);

            videoDuration = clip.OriginalDuration;
            double totalDuration = videoDuration.TotalSeconds;
            durationSlider.Maximum = totalDuration;

            endTxt.Text = videoDuration.ToString(@"hh\:mm\:ss");
            TimeSpan start = TimeSpan.Zero;
            UpdateMediaElementSource();
        }

        private void ShowErrorMessage(string v)
        {
            throw new NotImplementedException();
        }

        

        public void UpdateMediaElementSource()
        {

            mediaStreamSource = composition.GeneratePreviewMediaStreamSource(
                (int)mediaPlayerElement.ActualWidth,
                (int)mediaPlayerElement.ActualHeight);

            mediaPlayerElement.Source = MediaSource.CreateFromMediaStreamSource(mediaStreamSource);

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            mediaPlayerElement.Source = null;
            mediaStreamSource = null;
            base.OnNavigatedFrom(e);

        }

        private void playBtn_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayerElement.MediaPlayer.Play();
            PlayBtn.Visibility = Visibility.Collapsed;
            PauseBtn.Visibility = Visibility.Visible;
            timer.Start();
        }

        private void pauseBtn_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayerElement.MediaPlayer.Pause();
            PlayBtn.Visibility = Visibility.Visible;
            PauseBtn.Visibility = Visibility.Collapsed;
            composition.OverlayLayers.Clear();
            timer.Stop();
        }

        public void Timer_Tick(object sender, object e)
        {
            var currentTime = mediaPlayerElement.MediaPlayer.PlaybackSession.Position;

            startTxt.Text = currentTime.ToString(@"hh\:mm\:ss");
            var timeleft = videoDuration - currentTime;
            endTxt.Text = timeleft.ToString(@"hh\:mm\:ss");

            if (mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalDuration != TimeSpan.Zero)
            {
                durationSlider.Value = currentTime.TotalSeconds;
            }
            else
            {
                durationSlider.Value = TimeSpan.Zero.TotalSeconds;
                PlayBtn.Visibility = Visibility.Visible;
                PauseBtn.Visibility = Visibility.Collapsed;
            }

        }

        // event video ended
        public void videoEnded(object sender, object e)
        {
            timer.Stop();

        }

        private void durationSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (Math.Abs(mediaPlayerElement.MediaPlayer.PlaybackSession.Position.TotalSeconds - e.NewValue) > 1)
            {
                mediaPlayerElement.MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(e.NewValue);
            }

        }

        // add text watermark
        private void textWatermarkBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextW.Text) || colorCombo.SelectedValue == null || fontCombo.SelectedValue == null)
            {
                gridWatermark = new GridWatermark("text");
                // add text watermark into list
                gridWatermark.Text = "Watermark";
                gridWatermark.Font = "Arial";
                gridWatermark.Color = new SolidColorBrush(Windows.UI.Colors.White);
                // add grid watermark into main canvas
                gridWatermark.textWatermark.Text = "Watermark";
                gridWatermark.textWatermark.FontFamily = new FontFamily("Arial");
                gridWatermark.textWatermark.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
                gridWatermark.DeleteCallback = DeleteItemCallback;

                MainCanvas.Children.Add(gridWatermark.WatermarkGrid);
                overlayElements.Add(gridWatermark);
            }
            else
            {
                if (colorCombo.SelectedValue != null && fontCombo.SelectedValue != null)
                {
                    composition.OverlayLayers.Clear();
                    MainCanvas.Visibility = Visibility.Visible;
                    // Get the selected SolidColorBrush
                    var selectedBrush = colorCombo.SelectedValue as SolidColorBrush;

                    String text = TextW.Text;
                    String font = fontCombo.SelectedValue.ToString();
                    SolidColorBrush brush = selectedBrush;

                    gridWatermark = new GridWatermark("text");
                    // add text watermark into list
                    gridWatermark.Text = text;
                    gridWatermark.Font = font;
                    gridWatermark.Color = brush;
                    // add grid watermark into main canvas
                    gridWatermark.textWatermark.Text = text;
                    gridWatermark.textWatermark.FontFamily = new FontFamily(font);
                    gridWatermark.textWatermark.Foreground = brush;
                    gridWatermark.DeleteCallback = DeleteItemCallback;

                    MainCanvas.Children.Add(gridWatermark.WatermarkGrid);
                    overlayElements.Add(gridWatermark);
                    TextW.Text = "";
                    fontCombo.SelectedItem = null;
                    colorCombo.SelectedItem = null;


                    if (overlayElements != null)
                    {
                        AddOverlay.IsEnabled = true;
                    }
                }
            }

            AddOverlay.IsEnabled = true;

        }

        private void DeleteItemCallback(GridWatermark watermark)
        {
            overlayElements.Remove(watermark);
        }

        // add image watermark
        private async void imgWatermarkBtn_Click(object sender, RoutedEventArgs e)
        {
            gridWatermark = new GridWatermark("image");
            gridWatermark.DeleteCallback = DeleteItemCallback;

            FileOpenPicker photoPicker = new FileOpenPicker();
            photoPicker.ViewMode = PickerViewMode.Thumbnail;
            photoPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            photoPicker.FileTypeFilter.Add(".jpg");
            photoPicker.FileTypeFilter.Add(".jpeg");
            photoPicker.FileTypeFilter.Add(".png");
            photoPicker.FileTypeFilter.Add(".bmp");

            StorageFile photoFile = await photoPicker.PickSingleFileAsync();
            if (photoFile != null)
            {
                composition.OverlayLayers.Clear();
                MainCanvas.Visibility = Visibility.Visible;

                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(await photoFile.OpenAsync(FileAccessMode.Read));
                gridWatermark.imageWatermark.Source = bitmapImage;
                gridWatermark.BitmapSource = bitmapImage;
                MainCanvas.Children.Add(gridWatermark.WatermarkGrid);
                overlayElements.Add(gridWatermark);

                if (overlayElements != null)
                {
                    AddOverlay.IsEnabled = true;
                }
            }
        }

        private void fontCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
        }

        private async void AddOverlay_Click(object sender, RoutedEventArgs e)
        {
            await CreateOverlay(videoDuration);
            MainCanvas.Visibility = Visibility.Collapsed;
            AddOverlay.IsEnabled = false;
            RemoveOverlay.IsEnabled = true;
            UpdateMediaElementSource();
        }

        private GridWatermark GridRect { get; set; }


        private void CensoringBtn_Click(object sender, RoutedEventArgs e)
        {
            GridRect = new GridWatermark("rect");
            MainCanvas.Children.Add(GridRect.WatermarkGrid);
            AddOverlay.IsEnabled = true;
            AddMask.IsEnabled = true;
        }

        private void AddMask_Click(object sender, RoutedEventArgs e)
        {
            var TrackingPath = new List<TrackingPoint>();
            var MaskList = new List<MaskInfo>();
            var Mask = new MaskInfo(0, 0, 0, 0);
            var videoWidth = clip.GetVideoEncodingProperties().Width;
            var videoHeight = clip.GetVideoEncodingProperties().Height;
            Mask.IsTrackingMask = true;

            var transform = GridRect.WatermarkGrid.RenderTransform as TranslateTransform;
            Mask.PosX = (transform.X + GridRect.WatermarkGrid.Width) / videoWidth;
            Mask.PosY = (transform.Y + GridRect.WatermarkGrid.Height) / videoHeight;
            Mask.UIWidth = GridRect.WatermarkGrid.Width / videoWidth;
            Mask.UIHeight = GridRect.WatermarkGrid.Height / videoHeight;
            Mask.MaskType = 0;  //=0 mask co dinh, =1 tracking
            Mask.MaskShape = 1; //=0 ellipse, =1 rectangle
            Mask.StartTime = mediaPlayerElement.MediaPlayer.PlaybackSession.Position.TotalMilliseconds;
            Mask.StartTime = clip.OriginalDuration.TotalMilliseconds - Mask.StartTime;

            MaskList.Add(Mask);
            var properties = new PropertySet();
            properties.Add("TrackingPath", TrackingPath);
            properties.Add("Points", MaskList);
            var opticalFlowEffect = new VideoEffectDefinition("CustomVideoEffects.OpticalFlowEffect", properties);

            clip.VideoEffectDefinitions.Add(opticalFlowEffect);
            UpdateMediaElementSource();
        }

        private async Task CreateOverlay(TimeSpan duration)
        {
            for (int i = 0; i < overlayElements.Count; i++)
            {
                if (overlayElements[i].Deleted == true)
                {
                    overlayElements.Remove(overlayElements[i]);
                }
            }
            foreach (var element in overlayElements)
            {
                var renderTargetBitmap = new RenderTargetBitmap();
                var overlayLayer = new MediaOverlayLayer();

                if (element.Type == "image")
                {
                    await renderTargetBitmap.RenderAsync(element.imageWatermark);
                    var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

                    // Tạo SoftwareBitmap từ pixel buffer
                    var softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(
                        pixelBuffer,
                        BitmapPixelFormat.Bgra8,
                        renderTargetBitmap.PixelWidth,
                        renderTargetBitmap.PixelHeight,
                        BitmapAlphaMode.Premultiplied);

                    // Tạo VideoFrame từ SoftwareBitmap
                    CanvasBitmap canvas = CanvasBitmap.CreateFromSoftwareBitmap(CanvasDevice.GetSharedDevice(), softwareBitmap);

                    MediaClip m = MediaClip.CreateFromSurface(canvas, duration);

                    var transform = element.WatermarkGrid.RenderTransform as TranslateTransform;

                    // Tạo hình ảnh từ RenderTargetBitmap
                    var overlayImage = new MediaOverlay(m)
                    {
                        Position = new Rect(transform.X + element.topLeftThumb.Width, transform.Y + element.topLeftThumb.Height, element.imageWatermark.RenderSize.Width, element.imageWatermark.RenderSize.Height),
                        Opacity = 1,
                    };
                    overlayLayer.Overlays.Add(overlayImage);
                }

                else if (element.Type == "text")
                {
                    await renderTargetBitmap.RenderAsync(element.textWatermark);
                    var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

                    // Tạo SoftwareBitmap từ pixel buffer
                    var softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(
                        pixelBuffer,
                        BitmapPixelFormat.Bgra8,
                        renderTargetBitmap.PixelWidth,
                        renderTargetBitmap.PixelHeight,
                        BitmapAlphaMode.Premultiplied);

                    // Tạo VideoFrame từ SoftwareBitmap
                    CanvasBitmap canvas = CanvasBitmap.CreateFromSoftwareBitmap(CanvasDevice.GetSharedDevice(), softwareBitmap);

                    MediaClip m = MediaClip.CreateFromSurface(canvas, duration);

                    var transform = element.WatermarkGrid.RenderTransform as TranslateTransform;

                    // Tạo hình ảnh từ RenderTargetBitmap
                    var overlayText = new MediaOverlay(m)
                    {
                        Position = new Rect(transform.X + element.topLeftThumb.Width, transform.Y + element.topLeftThumb.Height, element.textWatermark.RenderSize.Width, element.textWatermark.RenderSize.Height),
                        Opacity = 1,
                    };
                    overlayLayer.Overlays.Add(overlayText);
                }

                // Add MediaOverlayLayer into MediaComposition
                composition.OverlayLayers.Add(overlayLayer);

                UpdateMediaElementSource();
            }

        }


        private void colorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            var selectedBrush = colorCombo.SelectedValue as SolidColorBrush;

        }

        private async void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            await RenderCompositionToFile();
        }

        private async Task RenderCompositionToFile()
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            picker.FileTypeChoices.Add("MP4 files", new List<string>() { ".mp4" });
            picker.SuggestedFileName = "RenderedComposition.mp4";

            Windows.Storage.StorageFile file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                // Call RenderToFileAsync
                var saveOperation = composition.RenderToFileAsync(file, MediaTrimmingPreference.Precise);

                saveOperation.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>(async (info, progress) =>
                {
                    await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
                    {
                        ShowErrorMessage(string.Format("Saving file... Progress: {0:F0}%", progress));
                    }));
                });
                saveOperation.Completed = new AsyncOperationWithProgressCompletedHandler<TranscodeFailureReason, double>(async (info, status) =>
                {
                    await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
                    {
                        try
                        {
                            var results = info.GetResults();
                            if (results != TranscodeFailureReason.None || status != AsyncStatus.Completed)
                            {
                                ShowErrorMessage("Saving was unsuccessful");
                            }
                            else
                            {
                                ShowErrorMessage("Trimmed clip saved to file");
                            }
                        }
                        finally
                        {
                            // Update UI whether the operation succeeded or not
                        }

                    }));
                });
            }
            else
            {
                ShowErrorMessage("User cancelled the file selection");
            }
        }

        GridWatermark _currentItem;

        private void WatermarkListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentItem != null)
            {
                _currentItem.WatermarkGrid.BorderBrush = new SolidColorBrush(Colors.White);
            }
            GridWatermark selectedItem = e.AddedItems[0] as GridWatermark;
            _currentItem = selectedItem;

            if (selectedItem == null)
                return;

            selectedItem.WatermarkGrid.BorderBrush = new SolidColorBrush(Colors.Red);

            if (selectedItem.Type == "image")
            {
                fontCombo.SelectedItem = null;
                colorCombo.SelectedItem = null;
                TextW.Text = "";
            }
            else if (selectedItem.Type == "text")
            {
                TextW.Text = selectedItem.Text;
                fontCombo.SelectedItem = selectedItem.Font;
                colorCombo.SelectedItem = selectedItem.Color.ToString();
            }


        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _inputText;
        public string InputText
        {
            get { return _inputText; }
            set
            {
                if (value == _inputText) return;
                _inputText = value;
                OnPropertyChanged("InputText");

                if (_currentItem == null) return;
                _currentItem.textWatermark.Text = value;
                _currentItem.Text = value;
            }
        }

        private string _newFont;
        public string NewFont
        {
            get { return _newFont; }
            set
            {
                if (value == _newFont) return;
                _newFont = value;
                OnPropertyChanged("NewFont");

                if (_currentItem == null) return;
                _currentItem.textWatermark.FontFamily = new FontFamily(value);
                _currentItem.Font = value;
            }
        }

        private SolidColorBrush _newColor;
        public SolidColorBrush NewColor
        {
            get { return _newColor; }
            set
            {
                if (value == _newColor) return;
                _newColor = value;
                OnPropertyChanged("NewColor");

                if (_currentItem == null) return;
                _currentItem.textWatermark.Foreground = value;
                _currentItem.Color = value;
            }
        }

        private void RemoveOverlay_Click(object sender, RoutedEventArgs e)
        {
            composition.OverlayLayers.Clear();
            MainCanvas.Visibility = Visibility.Visible;
            AddOverlay.IsEnabled = true;
            RemoveOverlay.IsEnabled = false;

            startTxt.Text = TimeSpan.Zero.ToString(@"hh\:mm\:ss");
            durationSlider.Minimum = 0;
            PlayBtn.Visibility = Visibility.Visible;
            PlayBtn.IsEnabled = true;
            UpdateMediaElementSource();
        }

        
    }

}
