using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace SiretT.Controls {
    public partial class VideoPlayer : UserControl, INotifyPropertyChanged {
        private MediaTimeline timeline;
        private MediaClock clock;
        private DispatcherTimer timer;
        private int srtIndex;
        public event PropertyChangedEventHandler PropertyChanged;
        public VideoPlayer() {
            InitializeComponent();
            this.PlayCommand = new DelegateCommand(Play);
            this.PlayPauseCommand = new DelegateCommand(PlayPause);
            this.StopCommand = new DelegateCommand(Stop);

            timeline = new MediaTimeline();
            timeline.FillBehavior = FillBehavior.Stop;
            timeline.Completed += timeline_Completed;
            player.Clock = clock;
            timer = new DispatcherTimer();
            subtitle.Text = "";
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            if (this.ActualWidth < 430)
                subtitle.FontSize = 16d / 430d * this.ActualWidth;
            else subtitle.FontSize = this.FontSize;
        }

        private void playerLoaded(object sender, RoutedEventArgs e) {
            SetValue(VolumeProperty, player.Volume);
            //Binding positionBinding = new Binding();
            //positionBinding.Source = this;
            //positionBinding.Mode = BindingMode.OneWay;
            //positionBinding.Path = new PropertyPath("ProgressProperty");
            //BindingOperations.SetBinding(this, PositionProperty, positionBinding);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Tick += timer_Tick;
            player.ScrubbingEnabled = true;
        }

        private void timeline_Completed(object sender, EventArgs e) {
            Stop();
        }

        private void player_MediaOpened(object sender, EventArgs e) {
            if (player.Clock != null) {
                DisplayRect = new Rect(.5, .5, player.NaturalVideoWidth, player.NaturalVideoHeight);
                SetValue(PositionProperty, TimeSpan.Zero);
                SetValue(CanPlayProperty, true);
                SetValue(DurationProperty, player.Clock.NaturalDuration.TimeSpan);
                timer.Start();
            }
        }

        void timer_Tick(object sender, EventArgs e) {
            if (clock.CurrentTime == null) return;
            OnPropertyChanged("ProgressProperty");
            if ((player.Source != null) && (player.NaturalDuration.HasTimeSpan)) {
                isLocalChange = true;
                SetValue(PositionProperty, player.Position);
            }

            if (!HasSubtitles) return;
            if (SrtIndex >= Subtitle.Texts.Count) {
                subtitle.Text = "";
                srtIndex = 0;
                return;
            }
            while (clock.CurrentTime.Value < Subtitle[srtIndex].StartTime && srtIndex > 0)
                srtIndex--;
            while (clock.CurrentTime.Value > Subtitle[srtIndex].EndTime && srtIndex < Subtitle.Texts.Count) {
                srtIndex++;
                if (SrtIndex >= Subtitle.Texts.Count) {
                    subtitle.Text = "";
                    return;
                }
            }
            if (clock.CurrentTime.Value < Subtitle[srtIndex].StartTime && subtitle.Text != "")
                subtitle.Text = "";
            if (clock.CurrentTime.Value > Subtitle[srtIndex].EndTime) {//- srt.Offset) {
                subtitle.Text = "";
                srtIndex++;
            } else if (clock.CurrentTime.Value >= Subtitle[srtIndex].StartTime) {//- srt.Offset) {
                var text = Subtitle[SrtIndex].Text;
                if (text.Contains("<i>")) {
                    text = text.Replace("<i>", "");
                    subtitle.FontStyle = FontStyles.Italic;
                } else subtitle.FontStyle = FontStyles.Normal;
                subtitle.Text = text;
            }
        }

        private void play(object sender, RoutedEventArgs e) {
            Play();
        }

        private void pause(object sender, RoutedEventArgs e) {
            Pause();
        }

        private void stop(object sender, RoutedEventArgs e) {
            Stop();
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if ((bool)e.NewValue)
                Play();
            else Stop();
        }

        private void OnPropertyChanged(string name) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            VideoPlayer self = (d as VideoPlayer);
            if (e.Property == SourceProperty) {
                self.timeline.Source = e.NewValue as Uri;
                self.Subtitle = null;
                self.subtitle.Text = "";
                self.SrtIndex = 0;
                string srtname = System.IO.Path.GetFileNameWithoutExtension((e.NewValue as Uri).OriginalString);
                string srtpath = System.IO.Path.GetDirectoryName((e.NewValue as Uri).OriginalString);
                string srtfile = srtpath + "\\" + srtname + ".srt";
                if (System.IO.File.Exists(srtfile)) {
                    self.Subtitle = Subtitle.FromFile(srtfile);
                }
                self.clock = self.timeline.CreateClock();
                self.clock.Controller.Stop();
                self.player.Clock = self.clock;
            } else if (e.Property == RepeatBehaviorProperty) {
                self.timeline.RepeatBehavior = (RepeatBehavior)e.NewValue;
            } else if (e.Property == PositionProperty) {
                var value = (TimeSpan)e.NewValue;
                if (!self.isLocalChange)
                    self.player.Clock.Controller.Seek(value, TimeSeekOrigin.BeginTime);
                self.isLocalChange = false;
            } else if (e.Property == VolumeProperty) {
                var value = (double)e.NewValue;
                if (value < 0 || value > 1) return;
                self.player.Volume = (double)e.NewValue;
            }
        }

        public TimeSpan ProgressProperty {
            get {
                if (clock == null) return TimeSpan.Zero;
                if (clock.CurrentTime != null)
                    return clock.CurrentTime.Value;
                return TimeSpan.Zero;
            }

            set {
                clock.Controller.SeekAlignedToLastTick(value, TimeSeekOrigin.BeginTime);
                OnPropertyChanged("ProgressProperty");
            }
        }

        public Subtitle Subtitle { get { return srt;} set { srt = value; HasSubtitles = (value != null); } }

        public int SrtIndex { get { return srtIndex; } set { srtIndex = value; } }

        public Thickness VideoMargin { get { return rPlayer.Margin; } set { rPlayer.Margin = value; } }

        public Rect DisplayRect { get { return drawg.Rect; } set { drawg.Rect = value; } }

        public bool IsPaused { get { return (bool)player.Clock.IsPaused; } }

        public ClockState CurrentState { get { return clock.CurrentState; } }

        //#region FontSizeProperty
        //public static DependencyProperty HasSubtitlesProperty = DependencyProperty.Register(
        //    "HasSubtitles", typeof(bool), typeof(VideoPlayer), new PropertyMetadata(false));

        //public bool HasSubtitles {
        //    get { return (bool)GetValue(HasSubtitlesProperty); }
        //    private set { SetValue(HasSubtitlesProperty, value); }
        //}
        //#endregion


        #region HasSubtitlesProperty
        public static DependencyProperty HasSubtitlesProperty = DependencyProperty.Register(
            "HasSubtitles", typeof(bool), typeof(VideoPlayer), new PropertyMetadata(false));

        public bool HasSubtitles {
            get { return (bool)GetValue(HasSubtitlesProperty); }
            private set { SetValue(HasSubtitlesProperty, value); }
        }
        #endregion

        #region CanPlayProperty
        public static DependencyProperty CanPlayProperty = DependencyProperty.Register(
            "CanPlay", typeof(bool), typeof(VideoPlayer), new PropertyMetadata(false));

        public bool CanPlay {
            get { return (bool)GetValue(CanPlayProperty); }
            private set { SetValue(CanPlayProperty, value); }
        }
        #endregion

        #region IsPlayingProperty
        public static DependencyProperty IsPlayingProperty = DependencyProperty.Register(
            "IsPlaying", typeof(bool), typeof(VideoPlayer), new PropertyMetadata(false));

        public bool IsPlaying {
            get { return (bool)GetValue(IsPlayingProperty); }
            private set { SetValue(IsPlayingProperty, value); }
        }
        #endregion

        #region IsStoppedProperty
        public static DependencyProperty IsStoppedProperty = DependencyProperty.Register(
            "IsStopped", typeof(bool), typeof(VideoPlayer), new PropertyMetadata(true));

        public bool IsStopped {
            get { return (bool)GetValue(IsStoppedProperty); }
            private set { SetValue(IsStoppedProperty, value); }
        }
        #endregion

        #region PositionProperty
        public static DependencyProperty PositionProperty = DependencyProperty.Register(
            "Position", typeof(TimeSpan), typeof(VideoPlayer), new PropertyMetadata(OnPropertyChanged));

        public TimeSpan Position {
            get { return (TimeSpan)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }
        #endregion

        #region VolumeProperty
        public static DependencyProperty VolumeProperty = DependencyProperty.Register(
            "Volume", typeof(double), typeof(VideoPlayer), new PropertyMetadata(OnPropertyChanged));

        public double Volume {
            get { return (double)GetValue(VolumeProperty); }
            set { SetValue(VolumeProperty, value); }
        }
        #endregion

        #region Duration
        public static DependencyProperty DurationProperty = DependencyProperty.Register(
            "Duration", typeof(TimeSpan), typeof(VideoPlayer), new PropertyMetadata());

        public TimeSpan Duration {
            get { return (TimeSpan)GetValue(DurationProperty); }
            private set { SetValue(DurationProperty, value); }
        }
        #endregion

        #region SourceProperty
        public static DependencyProperty SourceProperty = DependencyProperty.Register(
            "Source", typeof(Uri),
            typeof(VideoPlayer),
            new PropertyMetadata(OnPropertyChanged));

        public Uri Source {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }
        #endregion

        #region RepeatBehaviorProperty
        public static DependencyProperty RepeatBehaviorProperty = DependencyProperty.Register(
            "RepeatBehavior", typeof(RepeatBehavior),
            typeof(VideoPlayer),
            new PropertyMetadata(new RepeatBehavior(), OnPropertyChanged));
        private bool isDragging;
        private bool isLocalChange;
        private Subtitle srt;

        public RepeatBehavior RepeatBehavior {
            get { return (RepeatBehavior)GetValue(RepeatBehaviorProperty); }
            set { SetValue(RepeatBehaviorProperty, value); }
        }
        #endregion

        #region Internal
        internal void Play() {
            if (clock == null) return;
            if (!clock.IsPaused)
                clock.Controller.Begin();
            clock.Controller.Resume();
            timer.Start();
            IsPlaying = true;
        }

        internal void PlayPause() {
            if (IsPaused || CurrentState == ClockState.Stopped) Play();
            else Pause();
        }

        internal void Stop() {
            if (clock == null) return;
            //clock.Controller.SeekAlignedToLastTick(new TimeSpan(0), TimeSeekOrigin.BeginTime);
            timer.Stop();
            SetValue(PositionProperty, TimeSpan.Zero);
            clock.Controller.Begin();
            clock.Controller.Resume();
            clock.Controller.Stop();
            SetValue(IsStoppedProperty, true);
            srtIndex = 0;
            subtitle.Text = "";
        }

        internal void Pause() {
            if (clock == null) return;
            clock.Controller.Pause();
            timer.Stop();
            IsPlaying = false;
        }

        internal void Close() {
            timer.Stop();
            player.Close();
            SetValue(CanPlayProperty, false);
            SetValue(IsStoppedProperty, true);
            SetValue(IsPlayingProperty, false);
        }
        #endregion

        #region Commands

        public ICommand PlayCommand { protected set; get; }

        public ICommand PlayPauseCommand { protected set; get; }

        public ICommand PauseCommand { protected set; get; }

        public ICommand StopCommand { protected set; get; }


        #endregion

        public new event EventHandler<EventArgs> SubtitleClick;

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e) {
            if (SubtitleClick != null) {
                SubtitleClick(this, new EventArgs());
            }
        }
    }
}
