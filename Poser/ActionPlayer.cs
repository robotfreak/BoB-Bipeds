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
using System.Threading;

namespace ServoControl
{
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class ActionPlayer
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Members
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            Action                  action;
            Thread                  playerThread;
            CommunicationManager    communicationManager;

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Events
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            public event BlankDelegate  Finished;
            public event IntDelegate    FrameChanged;

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public ActionPlayer(Action setAction, CommunicationManager setCommunicationManager)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            action                  = setAction;
            communicationManager    = setCommunicationManager;


            playerThread = new Thread(new ThreadStart(Process));
            playerThread.Priority = ThreadPriority.Highest;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void Start()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            playerThread.Start();
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void Stop()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //stop the thread
            try
            {
                playerThread.Abort();
            }
            catch (Exception e)
            {

            }

            //done
            if (Finished != null)
                Finished();

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void Process()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //get into position
            //get the current state
            double[] currentStateVector = communicationManager.GetStateVetorAngle();

            //move to the next state
            MoveToPosition(currentStateVector, action.Frames[0].State, 40);


            //play loop
            while (true)
            {

                //play through the frames
                for (int f = 0; f < action.Frames.Count; f++)
                {
                    //get the current state
                    currentStateVector = communicationManager.GetStateVetorAngle();

                    //move to the next state
                    MoveToPosition(currentStateVector, action.Frames[f].State, action.Delay);

                    if (FrameChanged != null)
                        FrameChanged(f);
                }

                //break if the action doesn't loop
                if (!action.Loop)
                    break;
            }

            //done
            if (Finished != null)
                Finished();
        }


        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void MoveToPosition(double[] startingStateVector, double[] targetStateVector, int delay)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //create the transition state
            double[] transitionStateVector = new double[targetStateVector.Length];

            //set the number of steps
            int numberOfSteps = 25;

            //loop
            for (int s = 1; s <= numberOfSteps; s++)
            {
                for (int i = 0; i < transitionStateVector.Length; i++)
                    if (targetStateVector[i] != 0)
                        transitionStateVector[i] = (byte)((double)s / (double)numberOfSteps * (double)(targetStateVector[i] - startingStateVector[i]) + startingStateVector[i]);
                    else
                        transitionStateVector[i] = startingStateVector[i];

                communicationManager.SetState(transitionStateVector);
                System.Threading.Thread.Sleep(delay);
            }

        }
    }
}
