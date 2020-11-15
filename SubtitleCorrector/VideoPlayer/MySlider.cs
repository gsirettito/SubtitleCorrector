using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace SiretT.Controls {

    [TemplatePart(Name = "PART_Thumb", Type = typeof(Thumb))]
    [TemplatePart(Name = "PART_SelectionRange", Type = typeof(UIElement))]
    public class MySlider : Control {
        private Thumb thumb;
        private UIElement selectRange;
        public event EventHandler<RoutedPropertyChangedEventArgs<double>> ValueChanged;
        public MySlider() : base() {
            thumb = new Thumb();
            selectRange = new UIElement();
        }

        public static DependencyProperty MinimumProperty = DependencyProperty.Register(
            "Minimim", typeof(double), typeof(MySlider), new PropertyMetadata(0d, PropertyChanged));

        public static DependencyProperty MaximumProperty = DependencyProperty.Register(
            "Maximum", typeof(double), typeof(MySlider), new PropertyMetadata(100d, PropertyChanged));

        public static DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(double), typeof(MySlider), new PropertyMetadata(0d, PropertyChanged));
        private bool isDragging;
        private Binding valueBinding;

        private static void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            (d as MySlider).OnPropertyChanged(d, e);
        }

        protected virtual void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (e.Property == MySlider.ValueProperty) {
                if ((double)e.NewValue < Minimum || (double)e.NewValue > Maximum)
                    Value = (double)e.OldValue;
                else if (!isDragging) {
                    Canvas.SetLeft(this.thumb, ((this.Value / this.Maximum) * (this.ActualWidth)) - thumb.ActualWidth / 2d);
                }
            } else if (e.Property == MySlider.MinimumProperty) {
                Canvas.SetLeft(this.thumb, ((this.Value / this.Maximum) * (this.ActualWidth)) - thumb.ActualWidth / 2d);
            } else if (e.Property == MySlider.MaximumProperty) {
                Canvas.SetLeft(this.thumb, ((this.Value / this.Maximum) * (this.ActualWidth)) - thumb.ActualWidth / 2d);
            }
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            this.thumb = GetPart<Thumb>("PART_Thumb");
            if (this.thumb != null) {
                this.thumb.DragStarted += Thumb_DragStarted;
                this.thumb.DragDelta += Thumb_DragDelta;
                this.thumb.DragCompleted += Thumb_DragCompleted;
                Canvas.SetLeft(this.thumb, ((this.Value / this.Maximum) * (this.ActualWidth)) - thumb.ActualWidth / 2d);
            }

            this.selectRange = GetPart<UIElement>("PART_SelectionRange");
            if (this.selectRange != null) {

            }
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            Canvas.SetLeft(this.thumb, ((this.Value / this.Maximum) * (this.ActualWidth)) - thumb.ActualWidth / 2d);
        }

        private void Thumb_DragStarted(object sender, DragStartedEventArgs e) {
            isDragging = true;
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e) {
            var left = Canvas.GetLeft(thumb);
            if (left + e.HorizontalChange < -thumb.ActualWidth / 2 || left + e.HorizontalChange > this.ActualWidth - thumb.ActualWidth / 2)
                return;
            Canvas.SetLeft(this.thumb, Canvas.GetLeft(this.thumb) + e.HorizontalChange);
            //this.Value += e.HorizontalChange;// * this.Maximum * 0.01;

            if (CalculateOnDrag) {
                var newValue = this.Value + (e.HorizontalChange * (1 / this.ActualWidth) * Maximum);
                if (newValue < 0) newValue = 0;
                if (newValue > Maximum) newValue = Maximum;
                if (ValueChanged != null) {
                    ValueChanged(this, new RoutedPropertyChangedEventArgs<double>(this.Value, newValue));
                }
                this.Value = newValue;
            }
        }

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e) {
            var newValue = this.Value + (e.HorizontalChange * (1 / this.ActualWidth) * Maximum);
            if (newValue < 0) newValue = 0;
            if (newValue > Maximum) newValue = Maximum;
            if (ValueChanged != null) {
                ValueChanged(this, new RoutedPropertyChangedEventArgs<double>(this.Value, newValue));
            }
            this.Value = newValue;
            isDragging = false;
        }

        /// <summary>
        /// Gets the template child with the given name.
        /// </summary>
        /// <typeparam name="T">The interface type inheirted from DependencyObject.</typeparam>
        /// <param name="name">The name of the template child.</param>
        internal T GetPart<T>(string name)
            where T : DependencyObject {
            return this.GetTemplateChild(name) as T;
        }

        public double Minimum {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public double Maximum {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public double Value {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public bool CalculateOnDrag { get; set; } = false;
    }
}
