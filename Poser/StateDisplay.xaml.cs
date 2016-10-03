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
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Interaction logic for StateDisplay.xaml
    /// </summary>
    public partial class StateDisplay : UserControl
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Members
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            Frame frame;
            string[] variableNames;

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Properties
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            public String Description { set { descriptionLabel.Content = value; } get { return (string)descriptionLabel.Content; } }
            public Frame Frame { get { return frame; } }
            

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public StateDisplay(Frame setFrame, string[] setVaraibleNames)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            InitializeComponent();

            variableNames = setVaraibleNames;
            frame = setFrame;


            frame.ValueChanged += new IntDelegate(frame_ValueChanged);

            double[] stateVector = frame.State;

            for (int i = 0; i < stateVector.Length; i++)
            {
                ColumnDefinition column = new ColumnDefinition();
                column.MinWidth = 40;
                grid.ColumnDefinitions.Add(column);

                StateValue stateValue = new StateValue();
                stateValue.VariableName = variableNames[i];
                Grid.SetColumn(stateValue, grid.ColumnDefinitions.Count-1);
                grid.Children.Add(stateValue);

                stateValue.Value = (float)stateVector[i];

                if (stateVector[i] == 0)
                    stateValue.Active = false;

                stateValue.textBox.TextChanged += new TextChangedEventHandler(textBox_TextChanged);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void frame_ValueChanged(int value)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            int index = value;
            for (int v = 0; v < grid.Children.Count; v++)
                if (grid.Children[v] is StateValue && Grid.GetColumn(grid.Children[v]) == index)
                    ((StateValue)grid.Children[v]).Value = (float)frame.State[index];
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void textBox_TextChanged(object sender, TextChangedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //get the display
            StateValue stateValue = (StateValue)((Grid)((TextBox)sender).Parent).Parent;

            //set the value
            frame.SetValue(Grid.GetColumn(stateValue), (byte)int.Parse(stateValue.textBox.Text));
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public double[] GetStateVector()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            /*
            byte[] stateVector = new byte[grid.Children.Count];

            for (int i = 0; i < grid.Children.Count; i++)
                stateVector[i] = (byte)((StateValue)grid.Children[i]).Value;
            return stateVector;            
            */

            return frame.State;
        }
    }
}
