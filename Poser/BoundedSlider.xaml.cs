/*
//    FOBO Poser application
//      This program allows FOBO to be posed into different positions.
//
//    Copyright (C) 2012  Jonathan Dowdall, Project Biped (www.projectbiped.com)
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ServoControl
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Interaction logic for BoundedSlider.xaml
    /// </summary>
    public partial class BoundedSlider : UserControl
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    {

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Members
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            double minBound;
            double maxBound;
            string unitString;

            public double MaxBound { set { SetMaxBound(value); } get { return maxBound; } }
            public double MinBound { set { SetMinBound(value); } get { return minBound; } }
            public string UnitString { set { SetUnitString(value); } get { return unitString; } }
            public Slider Slider    { get{ return slider.slider;}}

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public BoundedSlider()
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            InitializeComponent();
            MaxBound = slider.slider.Maximum - 2;
            MinBound = 0.5;

            //UpdateMaxRect();
            //UpdateMinRect();

            slider.slider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(slider_ValueChanged);

            slider.ReferenceVisibility = System.Windows.Visibility.Hidden;

            UpdateSize();

            this.Loaded += new RoutedEventHandler(BoundedSlider_Loaded);

            unitString = "°";

        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void BoundedSlider_Loaded(object sender, RoutedEventArgs e)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            UpdateSize();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void SetUnitString(string value)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            unitString = value;
            label.Content = (int)slider.slider.Value + unitString;

        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void SetMaxBound(double value)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            maxBound = value;

            if (maxBound < minBound)
                maxBound = minBound;

            if (slider.slider.Value > maxBound)
                slider.slider.Value = maxBound;

            UpdateMaxRect();

        }
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void SetMinBound(double value)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            minBound = value;

            if (maxBound < minBound)
                minBound = maxBound;

            if (slider.slider.Value < minBound)
                slider.slider.Value = minBound;

            UpdateMinRect();

        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void UpdateMaxRect()
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            double position = slider.GetPositionFromValue(maxBound);
            double width = ActualWidth - position - label.Width;

            if (width < 0)
                width = 0;
            maxRect.Width = width;
            Canvas.SetLeft(maxRect, position);

        }
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void UpdateMinRect()
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {

            double position = slider.GetPositionFromValue(minBound);

            if (position < 0)
                position = 0;
            minRect.Width = position;
            Canvas.SetLeft(minRect, 0);

        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            if (slider.slider.Value < minBound)
                slider.slider.Value = minBound;
            if (slider.slider.Value > maxBound)
                slider.slider.Value = maxBound;

            label.Content = (int)slider.slider.Value + unitString;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void UpdateSize()
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            double width = ActualWidth - label.Width;
            if (width < 0)
                width = 0;
            canvas.Width = width;
            canvas.Height = ActualHeight;

            slider.Width = width;
            slider.Height = ActualHeight;

            minRect.Height = ActualHeight;
            maxRect.Height = ActualHeight;


            UpdateMaxRect();
            UpdateMinRect();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            UpdateSize();

        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        double GetPositionFromValue(double value)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {           
            return slider.GetPositionFromValue(value) + 15;
            //return (value - slider.slider.Minimum) / (slider.slider.Maximum - slider.slider.Minimum)*(ActualWidth - 30) + 15;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        double GetValueFromPosition(double position)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            return slider.GetValueFromPosition(position );
            //return (position - 15 ) / (ActualWidth - 30) * (slider.slider.Maximum - slider.slider.Minimum);
        }



    }
}
