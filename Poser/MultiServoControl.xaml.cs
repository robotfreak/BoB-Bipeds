/*
//    BoB Poser application
//      This program allows BoB to be posed into different positions.
//
//    Copyright (C) 2012  Jonathan Dowdall, Project Biped (www.projectbiped.com)
//                  ported to BoB biped by RobotFreak
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
using System.Windows.Shapes;
using System.Threading;
using System.Xml.Serialization;
using System.IO;
using Microsoft.Win32;
using System.Windows.Interop;

namespace ServoControl
{
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Interaction logic for MultiServoControl.xaml
    /// </summary>
    public partial class MultiServoControl : Window
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Members
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            CommunicationManager        communicationManager;
            List<Action>                actions;
            ActionPlayer                player;

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public MultiServoControl()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //initialize the GUI
            InitializeComponent();

            //create the link to the hardware
            communicationManager    = new CommunicationManager();
            actions                 = new List<Action>();

            //add the servos
/*
            // FOBO
            AddRightServo("Right Ankle Roll", 0, 0);
            AddRightServo("Right Ankle", 1, 1);
            AddRightServo("Right Upper Leg", 2, 2);
            AddRightServo("Right Hip", 3, 3);

            AddLeftServo("Left Ankle Roll", 4, 4);
            AddLeftServo("Left Ankle", 5, 5);
            AddLeftServo("Left Upper Leg", 6, 6);
            AddLeftServo("Left Hip", 7, 7);
*/
            // BoB
            AddRightServo("Right Ankle", 0, 0);
            AddRightServo("Right Hip", 1, 1);

            AddLeftServo("Left Ankle", 2, 2);
            AddLeftServo("Left Hip", 3, 3);

            //find the serial ports where FOBO will be waiting
            FindPorts();

            //set the focus to the connect button
            connectButton.Focus();
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void FindPorts()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //find open COM ports
            List<string> ports = communicationManager.FindPorts();

            portComboBox.Items.Clear();
            foreach (string portName in ports)
                portComboBox.Items.Add(portName);

            if (ports.Count == 0)
            {
                portComboBox.Items.Add("no open COM ports");
                connectButton.IsEnabled = false;
            }

            portComboBox.SelectedIndex = 0;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void AddLeftServo(string fileName, int servoIndex, int sensorIndex)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            AddServo(fileName, servoIndex, sensorIndex, leftLegServoPanel);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void AddRightServo(string fileName, int servoIndex, int sensorIndex)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            AddServo(fileName, servoIndex, sensorIndex, rightLegServoPanel);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void AddServo(string name, int servoIndex, int sensorIndex, StackPanel panel)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //create the servo
            Servo servo = new Servo();            
            //servo.LoadFromXmlFile(fileName);
            servo.Name = name;
            communicationManager.ConnectServo(servo, servoIndex, sensorIndex);

            //create the GUI control
            ServoAngleControl control = new ServoAngleControl() { Height=40};
            control.SetServo(servo);
            //panel.Children.Add(control);
            panel.Children.Insert(0, control);
           
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void addStateButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            AddNewFrameToCurrentAction();
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void AddNewFrameToCurrentAction()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            if (actionsListBox.SelectedItem is ActionControl)
            {
                //get the action
                Action action = ((ActionControl)actionsListBox.SelectedItem).Action;

                //create a new frame
                Frame frame = new Frame() { State = communicationManager.GetStateVetorAngle() };

                if (stateListBox.SelectedIndex >= 0)
                    AddFrame(action, frame, stateListBox.SelectedIndex + 1);
                else
                    AddFrame(action, frame, 0);

            }
            else
                MessageBox.Show("No action to add the frame to.  Create an action first");

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void AddFrame(Action action, Frame frame, int index)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            action.InsertFrame(frame, index);
            AddFrameDisplay(frame, index);

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void AddFrameDisplay(Frame frame, int index)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //stateListBox.Items.Add(new StateDisplay(frame, communicationManager.GetStateVariableNames()) { Description = "Frame " + stateListBox.Items.Count });
            //stateListBox.SelectedIndex = stateListBox.Items.Count - 1;
            StateDisplay display = new StateDisplay(frame, communicationManager.GetStateVariableNames()) { Description = "Frame " + index };
            stateListBox.Items.Insert(index, display);
            stateListBox.SelectedItem = display;


        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void moveToStateButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //get the selected state display
            if (stateListBox.SelectedItem is StateDisplay)
            {
                //communicationManager.SetState(((StateDisplay)stateListBox.SelectedItem).GetStateVector());
                double[] stateVector = ((StateDisplay)stateListBox.SelectedItem).GetStateVector();


                //get the current position
                double[] currentStateVector = communicationManager.GetStateVetorAngle();

                //only set positions for the servos in the state
                for (int i = 0; i < stateVector.Length; i++)
                    if (stateVector[i] == 0)
                        stateVector[i] = currentStateVector[i];


                Thread moveThread = new Thread(new ParameterizedThreadStart(MoveToPosition));
                moveThread.Start(stateVector);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void moveToNextStateButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            if(stateListBox.SelectedIndex == stateListBox.Items.Count -1)
                stateListBox.SelectedIndex = 0;
            else
                stateListBox.SelectedIndex++;

            moveToStateButton_Click(sender, e);
        }


        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void MoveToPosition(object param)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //get the target
            double[] targetStateVector = (double[])param;

            //get the current
            double[] startingStateVector = communicationManager.GetStateVetorAngle();

            //create the transition state
            double[] transitionStateVector = new double[targetStateVector.Length];

            //set the number of steps
            int numberOfSteps = 50;

            //loop
            for (int s = 1; s <= numberOfSteps; s++)
            {
                for (int i = 0; i < transitionStateVector.Length; i++)
                    if (targetStateVector[i] != 0)
                        transitionStateVector[i] = (byte)((double)s / (double)numberOfSteps * (double)(targetStateVector[i] - startingStateVector[i]) + startingStateVector[i]);
                    else
                        transitionStateVector[i] = startingStateVector[i];

                communicationManager.SetState(transitionStateVector);
                System.Threading.Thread.Sleep(10);
            }

        }


        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void addActionButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //create a new action
            AddAction(new Action());
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void AddAction(Action action)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            actions.Add(action);

            if(action.Name == null || string.Compare(action.Name,"") == 0)
                action.Name = "Action." + actions.Count.ToString().PadLeft(3, '0');

            //create an action control to display the action
            ActionControl actionControl = new ActionControl();

            actionsListBox.Items.Add(actionControl);

            actionsListBox.SelectedItem = actionControl;

            //hook up the action and its display
            actionControl.SetAction(action);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void removeActionButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //remove the selected item
            if (actionsListBox.SelectedItem is ActionControl)
            {
                //get the action
                Action action = ((ActionControl)actionsListBox.SelectedItem).Action;

                //remove the action
                RemoveAction(action);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void RemoveAction(Action action)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //make sure the action is in the actions list
            if (actions.Contains(action))
            {
                //find the action display that corresponds to the action
                for(int i = 0; i < actionsListBox.Items.Count; i++)
                    if (actionsListBox.Items[i] is ActionControl && ((ActionControl)actionsListBox.Items[i]).Action == action)
                    {
                        //change selection
                        if(actionsListBox.Items[i] == actionsListBox.SelectedItem)
                        {
                            if (i > 0)
                                actionsListBox.SelectedIndex = i - 1;
                            else if ( actionsListBox.Items.Count > i)
                                actionsListBox.SelectedIndex = i +1;
                        }

                        //remove the item
                        actionsListBox.Items.Remove(actionsListBox.Items[i]);

                        //remove the action
                        actions.Remove(action);

                        break;
                    }                        
            }
            else
                throw new Exception("Attemped to remove action " + action.Name + " but it isn't in the actions list");

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void saveActionsButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //create the save file dialog
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.CheckFileExists = false;
            dialog.AddExtension = true;
            dialog.DefaultExt = ".actions";
            dialog.Filter = "ACTIONS files (*.actions)|*.actions|All files (*.*)|*.*";

            //browse and save
            if (dialog.ShowDialog() == true)
                SaveActionsToXMLFile(dialog.FileName, actions);

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void SaveActionsToXMLFile(string fileName, List<Action> actionList)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {

            // these lines do the actual serialization
            XmlSerializer   serializer = new XmlSerializer(typeof(List<Action>));
            StreamWriter    writer = new StreamWriter(fileName);

            //write to file
            serializer.Serialize(writer, actionList);

            //close
            writer.Close();
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public List<Action> LoadActionsFromXmlFile(string fileName)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //Options
            bool mirrorOnLoad = false;

            //create the serializer object
            XmlSerializer serializer = new XmlSerializer(typeof(List<Action>));

            //open the file stream
            FileStream fileStream = new FileStream(fileName, FileMode.Open);

            //deserialize the file into the servo instance
            List<Action> loadedActions = (List<Action>)serializer.Deserialize(fileStream);

            //close the file stream
            fileStream.Close();


            //convert old frame format to new frame format
            for (int a = 0; a < loadedActions.Count; a++)
            {
                Action action = loadedActions[a];
                if (action.oldFrames != null)
                {
                    for (int f = 0; f < action.oldFrames.Count; f++)
                    {

                        byte[] oldState = action.oldFrames[f].State;
                        double[] newState = new double[ communicationManager.NumberOfServos()];
                        for (int s = 0; s < communicationManager.NumberOfServos(); s++)
                        {
                            newState[s] = (double)oldState[s] / 120.0 * 180 - 30;
                            if (newState[s] < 0)
                                newState[s] = 0;
                            if (newState[s] > 120)
                                newState[s] = 120;
                        }

                        action.AddFrame(new Frame() { State = newState });

                        //action.Frames.Add(new Frame(){}
                    }
                    action.oldFrames = null;
                }
            }

            int servosPerLeg = communicationManager.NumberOfServos()/2;

            //add mirrored frames
            if (mirrorOnLoad)
                foreach (Action action in loadedActions)
                {
                    int framesToAdd = action.Frames.Count;

                    for (int f = 0; f < framesToAdd; f++)
                    {
                        Frame sourceFrame = action.Frames[f];

                        Frame mirroredFrame = new Frame();
                        mirroredFrame.State = new double[sourceFrame.State.Length];

                        //flip
                        for (int i = 0; i < mirroredFrame.State.Length; i++)
                        {
                            //mirroredFrame.State[s] = sourceFrame.State[s];
                            if (i < servosPerLeg)
                                mirroredFrame.State[i] = (double)(60 - ((int)sourceFrame.State[i + servosPerLeg] - 60));
                            else
                                mirroredFrame.State[i] = (double)(60 - ((int)sourceFrame.State[i - servosPerLeg] - 60));

                        }

                        action.AddFrame(mirroredFrame);
                    }
                }


            return loadedActions;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void loadActionsButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //create the open file dialog
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.CheckFileExists = false;
            dialog.AddExtension = true;
            dialog.DefaultExt = ".actions";
            dialog.Filter = "ACTIONS files (*.actions)|*.actions|All files (*.*)|*.*";

            //browse
            if (dialog.ShowDialog() == true)
            {
                //remove current actions
                while (actions.Count > 0)
                    RemoveAction(actions[0]);

                //load the actions list
                List<Action> loadedActions = LoadActionsFromXmlFile(dialog.FileName);



                //add in the new actions
                for (int a = 0; a < loadedActions.Count; a++)
                    AddAction(loadedActions[a]);

                //update the frames control
                UpdateFramesDisplay();
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void actionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            if (actionsListBox.SelectedItem != null)
            {
                ((ActionControl)actionsListBox.SelectedItem).Opacity = 1;

                for (int i = 0; i < actionsListBox.Items.Count; i++)
                    if (actionsListBox.Items[i] != actionsListBox.SelectedItem)
                        ((ActionControl)actionsListBox.Items[i]).Opacity = 0.5;

                //update the frames display
                UpdateFramesDisplay();
            }

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void UpdateFramesDisplay()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //clear the old frames
            stateListBox.Items.Clear();

            if (actionsListBox.SelectedItem is ActionControl)
            {
                //get the action
                Action action = ((ActionControl)actionsListBox.SelectedItem).Action;

                //create a frame display for each frame in the current action
                if(action != null)
                    for(int f = 0; f < action.Frames.Count; f++)
                        AddFrameDisplay(action.Frames[f], f);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void stateListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            if (stateListBox.SelectedItem != null)
            {
                ((StateDisplay)stateListBox.SelectedItem).Opacity = 1;

                for (int i = 0; i < stateListBox.Items.Count; i++)
                    if (stateListBox.Items[i] != stateListBox.SelectedItem)
                        ((StateDisplay)stateListBox.Items[i]).Opacity = 0.5;

            }

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void startActionButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //make sure that the player isn't active
            if (player == null && actionsListBox.SelectedItem != null)
            {
                //create the player
                player = new ActionPlayer(((ActionControl)actionsListBox.SelectedItem).Action, communicationManager);

                //listen to events
                player.Finished += new BlankDelegate(player_Finished);
                player.FrameChanged += new IntDelegate(player_FrameChanged);

                //start the player
                player.Start();
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void player_FrameChanged(int value)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            Dispatcher.BeginInvoke(new IntDelegate(SelectFrame), value);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void SelectFrame(int frameNumber)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            if (frameNumber < stateListBox.Items.Count && frameNumber >= 0)
                stateListBox.SelectedIndex = frameNumber;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void player_Finished()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //clean up 
            player = null;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void exportActionButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {

            //dump the action to arduino code
            if (actionsListBox.SelectedItem != null)
            {
                //create the save file dialog
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.CheckFileExists = false;
                dialog.AddExtension = true;
                dialog.DefaultExt = ".txt";
                dialog.Filter = "Arduino files (*.pde)|*.pde|All files (*.*)|*.*";

                //browse and save
                if (dialog.ShowDialog() == true)
                {
                    //replace any spaces in the file name (Arduino will freak out)
                    string noSpacesName = dialog.SafeFileName.Replace(' ', '_');
                    string newFileName = dialog.FileName.Substring(0, dialog.FileName.Length - dialog.SafeFileName.Length) + noSpacesName;

                    DumpToArduino(((ActionControl)actionsListBox.SelectedItem).Action, newFileName);
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void DumpToArduino(Action action, string fileName)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //get all of the states
            //list of states
            List<double[]> stateList = new List<double[]>();
            int intermediateFrames = 1;

            //play through the frames
            for (int f = 0; f < action.Frames.Count; f++)
            {
                double[] startingStateVector = action.Frames[f].State;
                double[] targetStateVector = action.Frames[(f + 1) % action.Frames.Count].State;
                double[] transitionStateVector = new double[targetStateVector.Length];

                //loop
                for (int s = 0; s < intermediateFrames; s++)
                {
                    for (int i = 0; i < transitionStateVector.Length; i++)
                        if (targetStateVector[i] != 0)
                            transitionStateVector[i] = ((double)s / (double)intermediateFrames * (double)(targetStateVector[i] - startingStateVector[i]) + startingStateVector[i]);
                        else
                            transitionStateVector[i] = startingStateVector[i];

                    communicationManager.SetState(transitionStateVector);
                    System.Threading.Thread.Sleep(10);
                    stateList.Add(communicationManager.GetStateVetorAngle());
                        
                }
            }

            //write the states to the output
            int stateVectorLength = action.Frames[0].State.Length;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("int frames[" + stateList.Count() + "][" + stateVectorLength + "] = {");
            int servosToDump = communicationManager.NumberOfServos();

            for (int s = 0; s < stateList.Count(); s++)
            {
                string line = "                      {";
                for (int v = 0; v < servosToDump; v++)
                {
                    line += ((int)(100*stateList[s][v])).ToString().PadLeft(5, ' ');
                    if (v < servosToDump - 1)
                        line += ", ";
                }

                line += " }";

                if (s < stateList.Count() - 1)
                    line += ",";

                sb.AppendLine(line);
            }
            sb.AppendLine("};");

            //write the calibration
            if (communicationManager.calibration != null)
            {
                string line = "int calibration[" + stateVectorLength + "] = {";

                for (int v = 0; v < stateVectorLength; v++)
                {
                    line += ((int)(100.0 * communicationManager.calibration[v])).ToString();
                    if (v < stateVectorLength - 1)
                        line += ", ";
                }

                line += " };";

                sb.AppendLine(line);

            }

            //creatin the actions object in the output program
            sb.AppendLine("");
            sb.AppendLine("//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////");
            sb.AppendLine("// Actions");
            sb.AppendLine("//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////");
            sb.AppendLine("Action myAction(" + stateList.Count() + ", " + stateList.Count() * 10 + ", " + action.Delay*2 + ", frames); //the action");

            //put together the whole program
            string program = Tools.ArduinoProgramStart() + sb.ToString() + Tools.ArduinoProgramEnd();

            //write the text to file
            File.WriteAllText(fileName, program);

        }

 
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void DumpToBlender(Action action)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //get all of the states
            //list of states
            List<double[]> stateList = new List<double[]>();

            //play through the frames
            for (int f = 0; f < action.Frames.Count; f++)
                stateList.Add(action.Frames[f].State);

            //write the states to file
            int numberOfServos = communicationManager.NumberOfServos();
            int stateVectorLength = action.Frames[0].State.Length;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("static int states[" + stateList.Count() + "][" + numberOfServos + "] = {");

            for (int s = 0; s < stateList.Count(); s++)
            {
                string line = "                      {";
                for (int v = 0; v < numberOfServos; v++)
                {
                    line += ((int)stateList[s][v] - 60).ToString().PadLeft(4, ' ');
                    if (v < numberOfServos - 1)
                        line += ", ";
                }

                line += " }";

                if (s < stateList.Count() - 1)
                    line += ",";
                line += "   \\\\  frame number" + s;

                sb.AppendLine(line);
            }
            sb.AppendLine("};");

            //write the calibration
            if (communicationManager.calibration != null)
            {
                string line = "static int calibration[" + stateVectorLength + "] = {";

                for (int v = 0; v < stateVectorLength; v++)
                {
                    line += communicationManager.calibration[v].ToString();
                    if (v < stateVectorLength - 1)
                        line += ", ";
                }

                line += " };";

                sb.AppendLine(line);

            }
            //string output = sb.ToString();
            File.WriteAllText("c:\\tmp\\blender_action.txt", sb.ToString());
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void stopActionButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            if (player != null)
                player.Stop();

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void removeStateButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            RemoveSelectedFrame();

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void RemoveSelectedFrame()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            if (stateListBox.SelectedItem != null && actionsListBox.SelectedItem != null)
            {
                //get the action
                Action action = ((ActionControl)actionsListBox.SelectedItem).Action;

                //get the frame
                Frame frame = (Frame)((StateDisplay)stateListBox.SelectedItem).Frame;

                RemoveFrame(action, frame);

            }

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void RemoveFrame(Action action, Frame frame)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {

            //remove the frame
            action.RemoveFrame(frame);

            UpdateFramesDisplay();


        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void specialButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void updateSelectedStateButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            if (actionsListBox.SelectedItem is ActionControl)
            {
                //get the action
                Action action = ((ActionControl)actionsListBox.SelectedItem).Action;

                //get the frame
                Frame existingFrame = (Frame)((StateDisplay)stateListBox.SelectedItem).Frame;

                //create a new frame
                Frame frame = new Frame() { State = communicationManager.GetStateVetorAngle() };

                //update the frame
                for (int i = 0; i < frame.State.Length; i++)
                    existingFrame.SetValue(i, frame.State[i]);
            }
            else
                MessageBox.Show("No action to add the frame to.  Create an action first");

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void stateListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //bail if there aren't enough items to cycle
            if (stateListBox.Items.Count < 2)
                return;

            if (stateListBox.SelectedItem is StateDisplay)
            {
                //remove the frame
                if (e.Key == Key.Up || e.Key == Key.Down)
                {

                    //get the action
                    Action action = ((ActionControl)actionsListBox.SelectedItem).Action;

                    //get the frame
                    Frame frame = ((StateDisplay)stateListBox.SelectedItem).Frame;

                    //remember the selected index
                    int index = stateListBox.SelectedIndex;
                    int newIndex = index;

                    //remove it
                    RemoveSelectedFrame();
                    
                    //add it back in 
                    switch (e.Key)
                    {
                        case Key.Up:

                            if (index > 0)
                                newIndex = index - 1;
                            else
                                newIndex = stateListBox.Items.Count;

                            break;

                        case Key.Down:
                            if (index < stateListBox.Items.Count)
                                newIndex = index + 1;
                            else
                                newIndex = 0;
                            break;
                    }

                    AddFrame(action, frame, newIndex);

                }

            }

        }


        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void homeButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //move all of the servos to their home positions (this will be the center position of the motion range)

            //get the current
            double[] homeState = communicationManager.GetStateVetorAngle();

            //create the calibration state
            for (int s = 0; s < homeState.Length; s++)
                homeState[s] = 60;

            //go to the calibration state
            MoveToPosition(homeState);

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void invertStateButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //flip the positions of the legs

            //get the current
            double[] sourceState  = communicationManager.GetStateVetorAngle();
            double[] flippedState = communicationManager.GetStateVetorAngle();

            //flip
            for (int i = 0; i < sourceState.Length; i++)
            {
                if (i < sourceState.Length / 2)
                    flippedState[i] = (double)(60 - ((int)sourceState[i + sourceState.Length / 2] - 60));
                else
                    flippedState[i] = (double)(60 - ((int)sourceState[i - sourceState.Length / 2] - 60));

            }

            //move to the flipped state
            MoveToPosition(flippedState);


        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void addIntermediateStateButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {

            //get the next frame
            if (actionsListBox.SelectedItem is ActionControl)
            {
                //get the action
                Action action = ((ActionControl)actionsListBox.SelectedItem).Action;

                //create a new frame
                Frame sourceFrame = new Frame() { State = communicationManager.GetStateVetorAngle() };
                Frame targetFrame = action.Frames[stateListBox.SelectedIndex + 1];

                //split the difference
                Frame frame = new Frame() { State = communicationManager.GetStateVetorAngle() };

                for(int i = 0; i < sourceFrame.State.Length; i++)
                    frame.State[i] = (byte)(((int)sourceFrame.State[i] + (int)targetFrame.State[i])/2);

                if (stateListBox.SelectedIndex >= 0)
                    AddFrame(action, frame, stateListBox.SelectedIndex + 1);
                else
                    AddFrame(action, frame, 0);

            }
            else
                MessageBox.Show("No action to add the frame to.  Create an action first");



        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void calibrationFileButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //create the open file dialog
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.CheckFileExists = false; 
            dialog.AddExtension = true;
            dialog.DefaultExt = ".actions";
            dialog.Filter = "ACTIONS files (*.actions)|*.actions|All files (*.*)|*.*";

            //browse
            if (dialog.ShowDialog() == true)
            {
                //load the actions list
                List<Action> loadedActions = LoadActionsFromXmlFile(dialog.FileName);

                //the calibration is the first frame of the first action
                communicationManager.CalibrateFromFrame(loadedActions[0].Frames[0]);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected override void OnSourceInitialized(EventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            base.OnSourceInitialized(e);
            Tools.ExtendGlass(this, new Thickness(-1));
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void connectButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            if (!communicationManager.IsConnected)
            {
                if (!communicationManager.Start(portComboBox.SelectedItem.ToString()))
                {
                    MessageBox.Show("Couldn't connect to BoB on port : " + portComboBox.SelectedItem.ToString() + ".\n" +
                                    "Could BoB be on a different COM port?\n" +
                                    "If you are sure that BoB is on this COM port then try the following steps:\n" +
                                    "  1 : Close this application \n" +
                                    "  2 : Disconnect BoB from the computer\n" + 
                                    "  3 : Disconnct BoB from the power supply\n" + 
                                    "  4 : Wait for 5 seconds\n" +
                                    "  5 : Plug the USB cable back into BoB\n" +
                                    "  6 : Wait for 5 seconds\n" +
                                    "  7 : Plug the power supply back into BoB\n" +
                                    "  8 : Restart this application");
                }
                else
                {
                    //change the text on the button to reflect that FOBO is connected
                    connectButton.Content = "Connected to BoB";

                    //change the focus to the calibration button
                    calibrationFileButton.Focus();
                }

            }
            else
            {
                //stop the communication manager
                communicationManager.Stop();

                //change the text on the connect button to reflect that FOBO is no longer connected
                connectButton.Content = "Press to connected to BoB";

            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void mirrorStateButton_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //add mirrored frames
            if (actionsListBox.SelectedItem is ActionControl)
            {
                //get the action
                Action action = ((ActionControl)actionsListBox.SelectedItem).Action;

                //how many new frames need to be added
                int framesToAdd = action.Frames.Count;

                //how many servos are there for each leg
                int servosPerLeg = communicationManager.NumberOfServos()/2;

                //copy mirror paste each frame
                for (int f = 0; f < framesToAdd; f++)
                {
                    Frame sourceFrame = action.Frames[f];

                    Frame mirroredFrame = new Frame();
                    mirroredFrame.State = new double[sourceFrame.State.Length];

                    //flip
                    for (int i = 0; i < mirroredFrame.State.Length; i++)
                    {
                        if (i < servosPerLeg)
                            mirroredFrame.State[i] = (double)(60 - ((int)sourceFrame.State[i + servosPerLeg] - 60));
                        else
                            mirroredFrame.State[i] = (double)(60 - ((int)sourceFrame.State[i - servosPerLeg] - 60));

                    }

                    AddFrame(action, mirroredFrame, stateListBox.SelectedIndex + 1);

                    //action.AddFrame(mirroredFrame);
                }
                
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void aboutMenuItem_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            AboutWindow aboutWindow = new AboutWindow();
            
            aboutWindow.ShowDialog();
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void websiteMenuItem_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            System.Diagnostics.Process.Start("www.projectbiped.com/prototypes/fobo/poser");
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void FOBOMenuItem_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            System.Diagnostics.Process.Start("www.projectbiped.com/prototypes/fobo");
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void projectBipedMenuItem_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            System.Diagnostics.Process.Start("www.projectbiped.com");
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void calibrationHelpMenuItem_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            System.Diagnostics.Process.Start("https://docs.google.com/present/edit?id=0AS_h1KTMNaWNZGhtN2h6ZG5fMzIzY2NmYzd0aGs");
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void setupHelpMenuItem_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            System.Diagnostics.Process.Start("https://docs.google.com/present/edit?id=0AS_h1KTMNaWNZGhtN2h6ZG5fMzA0ZnBtbm40Y3o&hl");
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void arduinoMenuItem_Click(object sender, RoutedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            System.Diagnostics.Process.Start("https://sites.google.com/site/projectbiped/prototypes/fobo/poser/Remote_Control.zip?attredirects=0&d=1");
        }

    }



}
