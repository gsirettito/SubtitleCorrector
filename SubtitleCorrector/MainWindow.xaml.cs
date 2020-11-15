using Microsoft.Win32;
using SiretT;
using SiretT.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SubtitleCorrector {
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private TimeSpan SecondsStep = TimeSpan.FromSeconds(5);
        private TimeSpan SubtitleOffset = TimeSpan.FromSeconds(.5);
        private double VolumeStep = 0.05;
        private TimeSpan Zero = TimeSpan.FromSeconds(0);
        private bool isDragging;
        private Binding valueBinding;
        private Binding volumeBinding;
        private IniFile ini;
        private string extensions = "*.3g2;*.3gp;*.3gp2;*.3gpp;*.amr;*.amv;*.asf;*.avi;*.bdmv;*.bik;*.d2v;*.divx;*.drc;*.dsa;*.dsm;*.dss;*.dsv;*.evo;*.f4v;*.flc;*.fli;*.flic;*.flv;*.hdmov;*.ifo;*.ivf;*.m1v;*.m2p;*.m2t;*.m2ts;*.m2v;*.m4b;*.m4p;*.m4v;*.mkv;*.mp2v;*.mp4;*.mp4v;*.mpe;*.mpeg;*.mpg;*.mpls;*.mpv2;*.mpv4;*.mov;*.mts;*.ogm;*.ogv;*.pss;*.pva;*.qt;*.ram;*.ratdvd;*.rm;*.rmm;*.rmvb;*.roq;*.rpm;*.smil;*.smk;*.swf;*.tp;*.tpr;*.ts;*.vob;*.vp6;*.webm;*.wm;*.wmp;*.wmv";
        private string srtfile;
        private string filename;

        public double PanelWidth { get; }

        public MainWindow() {
            InitializeComponent();
            video.SubtitleClick += Video_SubtitleClick;
            valueBinding = BindingOperations.GetBinding(mslider, MySlider.ValueProperty);
            volumeBinding = BindingOperations.GetBinding(vol_slider, MySlider.ValueProperty);
            var localpath = System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            ini = new IniFile(localpath + "\\config.ini");
            var loc = (Point)ini.GetValue("Main\\Location", new Point(this.Left, this.Top));
            this.Left = loc.X;
            this.Top = loc.Y;
            var size = (Point)ini.GetValue("Main\\Size", new Point(this.Width, this.Height));
            this.Width = size.X;
            this.Height = size.Y;
            this.WindowState = (WindowState)Enum.Parse(typeof(WindowState), (ini.GetValue("Main\\State", (object)"Normal").ToString()));
            video.Foreground = new SolidColorBrush(ColorFromHtmlFormat(ini.GetValue("Subtitle\\Foreground", (object)"#FF000000").ToString()));
            video.FontFamily = new FontFamily(ini.GetValue("Subtitle\\FontFamily", (object)"Console").ToString());
            video.FontSize = double.Parse(ini.GetValue("Subtitle\\FontSize", 12.0).ToString());
            video.FontWeight = (ini.GetValue("Subtitle\\FontWeight", (object)"Normal").ToString()) == "Bold" ? FontWeights.Bold : FontWeights.Normal;
            video.FontStyle = (ini.GetValue("Subtitle\\FontStyle", (object)"Normal").ToString()) == "Italic" ? FontStyles.Italic : FontStyles.Normal;
            video.Volume = double.Parse(ini.GetValue("Player\\Volume", 1.0).ToString());
            PanelWidth = double.Parse(ini.GetValue("Main\\SubtitlePanelWidth", 250).ToString());
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            ini.AddOrUpdate("Main\\Location", new Point(this.Left, this.Top));
            ini.AddOrUpdate("Main\\Size", new Point(this.Width, this.Height));
            ini.AddOrUpdate("Main\\State", this.WindowState == WindowState.Minimized ? WindowState.Normal : WindowState);
            ini.AddOrUpdate("Main\\SubtitlePanelWidth", column.ActualWidth == 0 ? PanelWidth : column.ActualWidth);

            ini.AddOrUpdate("Subtitle\\Foreground", ColorToHtmlFormat((video.Foreground as SolidColorBrush).Color));
            ini.AddOrUpdate("Subtitle\\FontFamily", video.FontFamily.ToString());
            ini.AddOrUpdate("Subtitle\\FontSize", video.FontSize);
            ini.AddOrUpdate("Subtitle\\FontWeight", video.FontWeight == FontWeights.Normal ? "Normal" : "Bold");
            ini.AddOrUpdate("Subtitle\\FontStyle", video.FontStyle == FontStyles.Normal ? "Normal" : "Italic");
            ini.AddOrUpdate("Player\\Volume", video.Volume);
            ini.Save();
        }

        private string ColorToHtmlFormat(Color color) {
            return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", color.A, color.R, color.G, color.B);
        }

        private Color ColorFromHtmlFormat(string htmlFormat) {
            var hex = htmlFormat[0] == '#' ? htmlFormat.Substring(1).ToUpper() : htmlFormat;
            var int32 = Convert.ToInt32(hex, 16);
            var color = Color.FromArgb(
                    (byte)((int32 & 0xFF000000) >> 24),
                    (byte)((int32 & 0xFF0000) >> 16),
                    (byte)((int32 & 0xFF00) >> 8),
                    (byte)(int32 & 0xFF));
            return color;
        }

        private void Video_SubtitleClick(object sender, EventArgs e) {
            sublist.SelectedIndex = video.SrtIndex;
            sublist.ScrollIntoView(sublist.SelectedItem);
        }

        private void OpenClick(object sender, RoutedEventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Video|" + extensions;
            if ((bool)ofd.ShowDialog()) {
                srtfile = "";
                filename = ofd.FileName;
                Open(ofd.FileName);
            }
        }

        private void Open(string filename) {
            Title = "SubtitleCorrector - " + System.IO.Path.GetFileName(filename);
            video.Source = new Uri(filename);
            sublist.ItemsSource = null;
            if (video.HasSubtitles)
                sublist.ItemsSource = video.Subtitle.Texts;
            bPlay.Checked -= bPlay_Checked;
            bPlay.Unchecked -= bPlay_Checked;
            bPlay.IsChecked = true;
            video.Play();
            mslider.IsEnabled = true;
            bPlay.Checked += bPlay_Checked;
            bPlay.Unchecked += bPlay_Checked;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (!video.IsPlaying) return;
            if (e.Key == Key.OemComma) {
                video.Subtitle.Move(+SubtitleOffset);
                sublist.ItemsSource = null;
                sublist.ItemsSource = video.Subtitle.Texts;
                video.Subtitle.Offset += TimeSpan.FromSeconds(0.5);
                info.Text = "subtitle offset: " + video.Subtitle.Offset;
                info.Opacity = 1;
                FadeOut(info, new Duration(TimeSpan.FromSeconds(3)));
            } else if (e.Key == Key.OemPeriod) {
                video.Subtitle.Move(-SubtitleOffset);
                sublist.ItemsSource = null;
                sublist.ItemsSource = video.Subtitle.Texts;
                video.Subtitle.Offset -= TimeSpan.FromSeconds(0.5);
                info.Text = "subtitle offset: " + video.Subtitle.Offset;
                info.Opacity = 1;
                FadeOut(info, new Duration(TimeSpan.FromSeconds(3)));
            } else if (e.Key == Key.Z) {

            } else if (e.Key == Key.X) {

            } else if (e.Key == Key.M) {

            } else if (e.Key == Key.Left) {
                var current = video.player.Clock.CurrentTime.Value;
                video.player.Clock.Controller.Seek(
                    (current - SecondsStep).TotalSeconds < 0 ? Zero : current - SecondsStep, TimeSeekOrigin.BeginTime);
            } else if (e.Key == Key.Right) {
                var current = video.player.Clock.CurrentTime.Value;
                var total = video.player.Clock.NaturalDuration.TimeSpan.TotalSeconds;
                var Total = video.player.Clock.NaturalDuration.TimeSpan;
                video.player.Clock.Controller.Seek(
                    (current + SecondsStep).TotalSeconds > total ? Total : current + SecondsStep, TimeSeekOrigin.BeginTime);
            } else if (e.Key == Key.Up) {
                video.player.Volume += VolumeStep;
                info.Text = string.Format("volume: {0}%", video.player.Volume * 100);
                info.Opacity = 1;
                FadeOut(info, new Duration(TimeSpan.FromSeconds(3)));
            } else if (e.Key == Key.Down) {
                video.player.Volume -= VolumeStep;
                info.Text = string.Format("volume: {0}%", video.player.Volume * 100);
                info.Opacity = 1;
                FadeOut(info, new Duration(TimeSpan.FromSeconds(3)));
            }
        }

        public static void FadeOut(UIElement element, Duration duration) {
            DoubleAnimation anima = new DoubleAnimation {
                From = 1, To = 0,
                Duration = duration,
            };
            Storyboard.SetTarget(anima, element);
            Storyboard.SetTargetProperty(anima, new PropertyPath(UIElement.OpacityProperty));

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(anima);
            storyboard.Begin();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.ClickCount == 2)
                try {
                    var selected = (sender as FrameworkElement).DataContext as SubRipFormat;
                    mslider.Value = selected.StartTime.TotalSeconds;
                    BindingOperations.SetBinding(mslider, MySlider.ValueProperty, valueBinding);
                    bPlay.IsChecked = true;
                    video.player.Clock.Controller.Seek(selected.StartTime, TimeSeekOrigin.BeginTime);
                    video.SrtIndex = selected.Index - 1;
                } catch { }
        }

        private void SaveClick(object sender, RoutedEventArgs e) {
            if (!video.HasSubtitles) return;
            if (string.IsNullOrEmpty(srtfile)) {
                string srtname = System.IO.Path.GetFileNameWithoutExtension(video.Source.OriginalString);
                string srtpath = System.IO.Path.GetDirectoryName(video.Source.OriginalString);
                srtfile = srtpath + "\\" + srtname + ".srt";
            }
            video.Subtitle.Save(srtfile);
            info.Text = string.Format("subtitle saved");
            info.Opacity = 1;
            FadeOut(info, new Duration(TimeSpan.FromSeconds(3)));
        }

        private void MoveFromSelectedIndex(object sender, RoutedEventArgs e) {
            int index = sublist.SelectedIndex;
            if (index == -1) return;
            if ((sender as Button).Name == "fwm")
                video.Subtitle.Move(-SubtitleOffset, index);
            else
                video.Subtitle.Move(+SubtitleOffset, index);
            sublist.ItemsSource = null;
            sublist.ItemsSource = video.Subtitle.Texts;
            sublist.SelectedIndex = index;
            info.Text = string.Format("subtitle offset moved from {0} index: {1}", sublist.SelectedIndex, SubtitleOffset);
            info.Opacity = 1;
            FadeOut(info, new Duration(TimeSpan.FromSeconds(3)));
        }

        private void RemoveFromSelectedIndex(object sender, RoutedEventArgs e) {
            int index = sublist.SelectedIndex;
            if (index == -1) return;
            video.Subtitle.RemoveAt(index);
            sublist.ItemsSource = null;
            sublist.ItemsSource = video.Subtitle.Texts;
            sublist.SelectedIndex = index;
            info.Text = string.Format("subtitle removed from {0} index", sublist.SelectedIndex);
            info.Opacity = 1;
            FadeOut(info, new Duration(TimeSpan.FromSeconds(3)));
        }

        private void ExpandFromSelectedIndex(object sender, RoutedEventArgs e) {
            int index = sublist.SelectedIndex;
            if (index == -1) return;
            if ((sender as Button).Name == "exp")
                video.Subtitle.Expand(index, SubtitleOffset);
            else
                video.Subtitle.Shrink(index, SubtitleOffset);
            sublist.ItemsSource = null;
            sublist.ItemsSource = video.Subtitle.Texts;
            sublist.SelectedIndex = index;
            info.Text = string.Format("subtitle offset moved from {0} index: {1}", sublist.SelectedIndex, SubtitleOffset);
            info.Opacity = 1;
            FadeOut(info, new Duration(TimeSpan.FromSeconds(3)));
        }

        private void EditFromSelectedIndex(object sender, RoutedEventArgs e) {

        }

        private void slider_MouseDown(object sender, MouseButtonEventArgs e) {
            if (isDragging) return;
            var slider = sender as MySlider;
            var left = (e.GetPosition(slider).X - slider.BorderThickness.Left);
            var width = slider.ActualWidth - slider.BorderThickness.Left - slider.BorderThickness.Right;
            var value = left / width * slider.Maximum;
            video.ProgressProperty = TimeSpan.FromSeconds(value);
        }

        private void mslider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e) {
            isDragging = true;
            mslider.Value = mslider.Value;
        }

        private void mslider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e) {
            isDragging = false;
            video.Position = TimeSpan.FromSeconds(mslider.Value);
            BindingOperations.SetBinding(mslider, MySlider.ValueProperty, valueBinding);
        }

        private void bStop_Click(object sender, RoutedEventArgs e) {
            mslider.IsEnabled = false;
            mslider.Value = 0;
            BindingOperations.SetBinding(mslider, MySlider.ValueProperty, valueBinding);
            bPlay.Checked -= bPlay_Checked;
            bPlay.Unchecked -= bPlay_Checked;
            bPlay.IsChecked = false;
            bPlay.Checked += bPlay_Checked;
            bPlay.Unchecked += bPlay_Checked;
        }

        private void bPlay_Checked(object sender, RoutedEventArgs e) {
            if (video.CanPlay) {
                if ((bool)bPlay.IsChecked) {
                    video.Play();
                    mslider.IsEnabled = true;
                } else video.Pause();
            }
        }

        private void vol_slider_MouseDown(object sender, MouseButtonEventArgs e) {
            var slider = sender as MySlider;
            var left = (e.GetPosition(slider).X - slider.BorderThickness.Left);
            var width = slider.ActualWidth - slider.BorderThickness.Left - slider.BorderThickness.Right;
            var value = left / width * slider.Maximum;
            video.Volume = value;
            BindingOperations.SetBinding(vol_slider, MySlider.ValueProperty, volumeBinding);
            info.Text = string.Format("volume: {0}%", (int)(video.Volume * 100));
            info.Opacity = 1;
            FadeOut(info, new Duration(TimeSpan.FromSeconds(3)));
        }

        private void vol_slider_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e) {
            var slider = sender as MySlider;
            video.Volume = slider.Value;
            BindingOperations.SetBinding(vol_slider, MySlider.ValueProperty, volumeBinding);
            info.Text = string.Format("volume: {0}%", (int)(video.Volume * 100));
            info.Opacity = 1;
            FadeOut(info, new Duration(TimeSpan.FromSeconds(3)));
        }

        private void SaveAsClick(object sender, RoutedEventArgs e) {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "SubRip Format|*.srt";
            string srtname = System.IO.Path.GetFileNameWithoutExtension(video.Source.OriginalString);
            string srtfile = srtname + ".srt";
            sfd.FileName = srtfile;
            if ((bool)sfd.ShowDialog()) {
                video.Subtitle.Save(sfd.FileName);
                info.Text = string.Format("subtitle saved");
                info.Opacity = 1;
                FadeOut(info, new Duration(TimeSpan.FromSeconds(3)));
            }
        }

        private void vol_slider_MouseWheel(object sender, MouseWheelEventArgs e) {
            var value = e.Delta / 120 * 0.01 + video.Volume;
            value = (value < 0) ? 0 : (value > 1) ? 1 : value;
            video.Volume = value;
            BindingOperations.SetBinding(vol_slider, MySlider.ValueProperty, volumeBinding);
            info.Text = string.Format("volume: {0}%", (int)(video.Volume * 100));
            info.Opacity = 1;
            FadeOut(info, new Duration(TimeSpan.FromSeconds(3)));
        }

        private void video_DragOver(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
                e.Effects = DragDropEffects.Copy;
        }

        private void video_Drop(object sender, DragEventArgs e) {
            string[] files = (string[])(e.Data.GetData(DataFormats.FileDrop));
            var ext = System.IO.Path.GetExtension(files[0]).ToLower();
            if (ext == ".srt") {
                string srtname = System.IO.Path.GetFileNameWithoutExtension(files[0]);
                string srtpath = System.IO.Path.GetDirectoryName(files[0]);
                if (video.Source != null && video.CanPlay) {
                    video.Subtitle = Subtitle.FromFile(files[0]);
                    srtfile = files[0];
                    sublist.ItemsSource = video.Subtitle.Texts;
                    return;
                } else {
                    var vfiles = System.IO.Directory.GetFiles(srtpath).Where(
                        d => {
                            return System.IO.Path.GetFileNameWithoutExtension(d) == srtname &&
                            extensions.Contains(System.IO.Path.GetExtension(d));
                        });
                    if (vfiles != null) {
                        Open(vfiles.First());
                    }
                }
            }
            if (!extensions.Contains(ext))
                return;
            Open(files[0]);

        }

        private void mslider_MouseWheel(object sender, MouseWheelEventArgs e) {
            if (!mslider.IsEnabled) return;
            var value = e.Delta / 120 * 0.01 * mslider.Maximum + mslider.Value;
            if (value < 0 || value > mslider.Maximum) return;
            video.ProgressProperty = TimeSpan.FromSeconds(value);
            BindingOperations.SetBinding(mslider, MySlider.ValueProperty, valueBinding);
            info.Text = string.Format("move to: {0}", video.Position.ToString("hh':'mm':'ss\\,fff"));
            info.Opacity = 1;
            FadeOut(info, new Duration(TimeSpan.FromSeconds(3)));
        }

        private void FontTypeClick(object sender, RoutedEventArgs e) {
            System.Windows.Forms.FontDialog fd = new System.Windows.Forms.FontDialog();
            var fontstyle = video.FontWeight == FontWeights.Normal ? System.Drawing.FontStyle.Regular : System.Drawing.FontStyle.Bold;
            fontstyle |= video.FontStyle == FontStyles.Italic ? System.Drawing.FontStyle.Italic : System.Drawing.FontStyle.Regular;
            fd.Font = new System.Drawing.Font(
                video.FontFamily.Source,
                (float)(video.FontSize * 72d / 96d),
                fontstyle);

            if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                FontFamilyConverter ffc = new FontFamilyConverter();
                var selectedFont = fd.Font;
                video.FontFamily = (FontFamily)ffc.ConvertFromString(selectedFont.Name);
                video.FontSize = selectedFont.Size * 96.0 / 72.0;
                video.FontWeight = selectedFont.Bold ? FontWeights.Bold : FontWeights.Regular;
                video.FontStyle = selectedFont.Italic ? FontStyles.Italic : FontStyles.Normal;
            }
        }

        private void ColorFontClick(object sender, RoutedEventArgs e) {
            System.Windows.Forms.ColorDialog cd = new System.Windows.Forms.ColorDialog();
            var color = (video.Foreground as SolidColorBrush).Color;
            cd.Color = System.Drawing.Color.FromArgb(255, color.R, color.G, color.B);
            if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                video.Foreground = new SolidColorBrush(Color.FromArgb(255, cd.Color.R, cd.Color.G, cd.Color.B));
            }
        }

        private void AboutClick(object sender, RoutedEventArgs e) {
            MessageBox.Show("Created by: Guillermo Siret Tito\r\n" +
                "e-mail: guillermosiret@gmail.com\r\n" +
                "Copyright ©: 2020\r\n" +
                "Description: Beta application for subtitle",
                "SubtitleCorrector",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void DockPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (!(bool)e.NewValue)
                column.Width = GridLength.Auto;
            else
                column.Width = new GridLength(PanelWidth);
        }
    }
}
