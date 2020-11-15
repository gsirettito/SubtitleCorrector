using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SiretT.Controls {

    [TemplatePart(Name = "PART_Thumb", Type = typeof(Thumb))]
    [TemplatePart(Name = "PART_SelectionRange", Type = typeof(UIElement))]
    public class ZSlider : Control {
        private Thumb thumb;
        private UIElement selectRange;
        public ZSlider() : base() {
            thumb = new Thumb();
            selectRange = new UIElement();
        }

        public static DependencyProperty MinimumProperty = DependencyProperty.Register(
            "Minimim", typeof(double), typeof(ZSlider), new PropertyMetadata(0d, PropertyChanged));

        public static DependencyProperty MaximumProperty = DependencyProperty.Register(
            "Maximum", typeof(double), typeof(ZSlider), new PropertyMetadata(100d, PropertyChanged));

        public static DependencyProperty MediumProperty = DependencyProperty.Register(
            "Medium", typeof(double), typeof(ZSlider), new PropertyMetadata(50d, PropertyChanged));

        public static DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(double), typeof(ZSlider), new PropertyMetadata(0d, PropertyChanged));

        private static void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            (d as ZSlider).OnPropertyChanged(d, e);
        }

        protected virtual void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (e.Property == ZSlider.ValueProperty) {
                if ((double)e.NewValue < Minimum || (double)e.NewValue > Maximum)
                    Value = (double)e.OldValue;
                else {
                    if (Value <= Medium)
                        Canvas.SetLeft(this.thumb, ((this.Value / this.Medium) * (this.ActualWidth / 2d)) - thumb.ActualWidth / 2d);
                    else
                        Canvas.SetLeft(this.thumb, ((this.Value + this.Maximum - 2d * this.Medium) / (this.Maximum - this.Medium) * this.ActualWidth / 2d) - thumb.ActualWidth / 2d);
                }
            } else if (e.Property == ZSlider.MinimumProperty) {
                if (Value <= Medium)
                    Canvas.SetLeft(this.thumb, ((this.Value / this.Medium) * (this.ActualWidth / 2d)) - thumb.ActualWidth / 2d);
                else
                    Canvas.SetLeft(this.thumb, ((this.Value + this.Maximum - 2d * this.Medium) / (this.Maximum - this.Medium) * this.ActualWidth / 2d) - thumb.ActualWidth / 2d);
            } else if (e.Property == ZSlider.MediumProperty) {
                if (Value <= Medium)
                    Canvas.SetLeft(this.thumb, ((this.Value / this.Medium) * (this.ActualWidth / 2d)) - thumb.ActualWidth / 2d);
                else
                    Canvas.SetLeft(this.thumb, ((this.Value + this.Maximum - 2d * this.Medium) / (this.Maximum - this.Medium) * this.ActualWidth / 2d) - thumb.ActualWidth / 2d);
            } else if (e.Property == ZSlider.MaximumProperty) {
                if (Value <= Medium)
                    Canvas.SetLeft(this.thumb, ((this.Value / this.Medium) * (this.ActualWidth / 2d)) - thumb.ActualWidth / 2d);
                else
                    Canvas.SetLeft(this.thumb, ((this.Value + this.Maximum - 2d * this.Medium) / (this.Maximum - this.Medium) * this.ActualWidth / 2d) - thumb.ActualWidth / 2d);
            }
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            this.thumb = GetPart<Thumb>("PART_Thumb");
            if (this.thumb != null) {
                this.thumb.DragDelta += Thumb_DragDelta;
                if (Value <= Medium)
                    Canvas.SetLeft(this.thumb, ((this.Value / this.Medium) * (this.ActualWidth / 2d)) - thumb.ActualWidth / 2d);
                else
                    Canvas.SetLeft(this.thumb, ((this.Value + this.Maximum - 2d * this.Medium) / (this.Maximum - this.Medium) * this.ActualWidth / 2d) - thumb.ActualWidth / 2d);
            }

            this.selectRange = GetPart<UIElement>("PART_SelectionRange");
            if (this.selectRange != null) {
                
            }
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            if (Value <= Medium)
                Canvas.SetLeft(this.thumb, ((this.Value / this.Medium) * (this.ActualWidth / 2d)) - thumb.ActualWidth / 2d);
            else
                Canvas.SetLeft(this.thumb, ((this.Value + this.Maximum - 2d * this.Medium) / (this.Maximum - this.Medium) * this.ActualWidth / 2d) - thumb.ActualWidth / 2d);
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e) {
            if (Value <= Medium) {
                this.Value += e.HorizontalChange * this.Medium * 0.01;
                double value = Math.Round(Value + e.HorizontalChange * this.Medium * 0.01, 2);
                if (Math.Abs(value - Medium) <= this.Medium * 0.01)
                    this.Value = 1;
            } else {
                this.Value += e.HorizontalChange * (this.Maximum - this.Medium) * 0.01;
                double value = Math.Round(Value + e.HorizontalChange * (this.Maximum - this.Medium) * 0.01, 2);
                if (Math.Abs(value - Medium) <= (this.Maximum - this.Medium) * 0.01)
                    this.Value = 1;
            }
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

        public double Medium {
            get { return (double)GetValue(MediumProperty); }
            set { SetValue(MediumProperty, value); }
        }

        public double Value {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
    }
}
