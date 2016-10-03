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
using System.Windows.Markup;
using System.Collections.ObjectModel;

namespace ServoControl
{
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Interaction logic for GroupDisplayPanel.xaml
    /// </summary>
    [ContentProperty("Children")]
    public partial class GroupDisplayPanel : UserControl
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Members
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            Color color;

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Properties
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            public string Title { set { titleLabel.Content = value; } get { return (string)titleLabel.Content; } }
            public Color Color { set { SetColor(value); } get { return color; } }
            public ObservableCollection<UIElement> Children { get; private set; }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public GroupDisplayPanel()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            InitializeComponent();

            color = Colors.Blue;

            //set up the children
            Children = new ObservableCollection<UIElement>();
            Children.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(Children_CollectionChanged);

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void SetColor(Color setColor)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            color = setColor;

            ((LinearGradientBrush)border.Background).GradientStops[0].Color = color; 
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void Children_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (UIElement elem in e.NewItems)
                    {
                        childGrid.Children.Add(elem);
                    }

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:

                    foreach (UIElement elem in e.OldItems)
                    {
                        childGrid.Children.Remove(elem);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    break;
            }
        }
    }
}
