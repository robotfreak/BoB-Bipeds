﻿/*
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
using System.Windows.Controls.Primitives;

namespace ServoControl
{
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Interaction logic for ReferenceSlider.xaml
    /// </summary>
    public partial class ReferenceSlider : UserControl
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Members
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            double reference;

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Properties
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            public double   Reference {set{ SetReference(value);} get{return reference;} }
            public System.Windows.Visibility ReferenceVisibility { set { referenceBox.Visibility = value; } get { return referenceBox.Visibility; } }
            public Slider   Slider { get { return slider; } }


        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public ReferenceSlider()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            InitializeComponent();

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void SetReference(double value)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            reference = value;

            UpdateReference();
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            UpdateReference();            
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public double GetPositionFromValue(double value)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            return  (value - slider.Minimum) / (slider.Maximum - slider.Minimum) * (ActualWidth - 12) + 6;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public double GetValueFromPosition(double position)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            return (position - 6) / (ActualWidth - 12) * (slider.Maximum - slider.Minimum) + slider.Minimum;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void UpdateReference()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            if (double.IsInfinity(reference))
                return;

            referenceBox.Height = ActualHeight;

            double position = GetPositionFromValue(slider.Value);
            double referencePosition = GetPositionFromValue(reference);

            double left = position;
            double right = referencePosition;
            if (left > right)
            {
                left  = referencePosition;
                right = position;
            }
            referenceBox.Width = right-left;

            //Canvas.SetLeft(referenceBox, position);
            Canvas.SetLeft(referenceBox, left);

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            UpdateReference();

        }
    }
}