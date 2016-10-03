/*
//    FOBO Poser application
//      This program allows FOBO to be posed into different positions.
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
using System.IO.Ports;
using System.Threading;
using System.Windows.Threading;

namespace ServoControl
{
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class CommunicationManager
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Members
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            SerialPort                  serialPort;
            Mutex                       mutex;
            List<int>                   bytesBuffer;
            bool                        initialized;
            List<Servo>                 servos;
            public double[]             calibration;
            bool                        validResponseReceived;


        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Properties
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            public string Port { get { if (serialPort != null) return serialPort.PortName; return ""; } }
            public bool IsConnected { get { return serialPort != null && serialPort.IsOpen;  } }



        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public CommunicationManager()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //create members
            mutex                   = new Mutex();
            bytesBuffer             = new List<int>();
            initialized             = false;
            validResponseReceived   = false;
            servos                  = new List<Servo>();

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public bool Start(string portName)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            if (serialPort == null)
                return InitializeSerialPort(portName);

            return false;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void Stop()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            if (serialPort != null)
            {
                if (mutex.WaitOne())
                {

                    if (serialPort.IsOpen)
                    {
                        serialPort.DataReceived -= new SerialDataReceivedEventHandler(serialPort_DataReceived);
                        serialPort.Close();
                    }

                    serialPort = null;

                    mutex.ReleaseMutex();
                }
            }

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public List<string> FindPorts()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            SerialPort port = new SerialPort();
            List<string> portNames = new List<string>();


            for(int p = 0; p < 33; p++)
            {
                port.PortName = "COM" + p;

                try
                {
                    port.Open();
                }
                catch (Exception e) 
                { 
                    continue;
                }
                if (port.IsOpen)
                {
                    portNames.Add(port.PortName);
                    port.Close();
                }
            }

            return portNames;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        bool InitializeSerialPort(string portName)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            bool success = false;

            serialPort = new SerialPort();

            serialPort.PortName = portName;
            serialPort.BaudRate = 38400;

            //reset the response flag
            validResponseReceived = false;

            //open the port
            try
            {
                serialPort.Open();
            }
            catch (Exception e) { }


            if (serialPort.IsOpen)
            {
                //wait for enough time to accumulate at least 1 id from the serial port (id are broadcast every 100ms)
                System.Threading.Thread.Sleep(200);

                //look to see who is on the line
                string data = serialPort.ReadExisting();

                //make sure it is someone who we want to talk to 
                if (data.Contains("FOBO"))
                {
                    //send an ack
                    serialPort.Write("hi");

                    //make sure FOBO wants to talk
                    System.Threading.Thread.Sleep(200);
                    string response = serialPort.ReadExisting();
                    if (response.Contains("connected"))
                    {
                        //setup the data event listener (this is the normal that data is handled)
                        serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);

                        //write the initial position
                        WriteServoPosition();

                        //signal success
                        success = true;
                    }
                }
            }

            //close the port if the connection failed
            if (!success)
            {
                try
                {
                    serialPort.Close();
                }
                catch (Exception e) { }

                serialPort = null;
            }

            return success;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public int NumberOfServos()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            return servos.Count;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void CalibrateFromFrame(Frame calibrationFrame)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //initialize the calibration
            if (calibration == null)
                calibration = new double[GetStateVetorAngle().Length];            

            //set the calibration
            for (int s = 0; s < calibration.Length; s++)
                calibration[s] = calibrationFrame.State[s] - 60;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            if (mutex.WaitOne())
            {
                //read the bytes
                int numberOfBytesToRead = serialPort.BytesToRead;
                //int[] bytes = new int[numberOfBytesToRead];
                for (int b = 0; b < numberOfBytesToRead; b++)
                {
                    int value = serialPort.ReadByte();
                    bytesBuffer.Add(value);
                }


                //make sure the data ends with "done"
                if (bytesBuffer.Count > 4 && bytesBuffer[bytesBuffer.Count - 4] == 100 && bytesBuffer[bytesBuffer.Count - 3] == 111 && bytesBuffer[bytesBuffer.Count - 2] == 110 && bytesBuffer[bytesBuffer.Count - 1] == 101) //100 111 110 101
                {
                    validResponseReceived = true;

                    // no position feeback for FOBO ... but this is where the servo sensors would be updated with potentiometer feedback from the message

                    //write the target servo positions in response
                    WriteServoPosition();

                    //clear the buffer
                    bytesBuffer.Clear();
                }

                mutex.ReleaseMutex();

            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public string[] GetStateVariableNames()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            string[] variableNames = new string[servos.Count()];

            //write the servos names
            for (int s = 0; s < servos.Count(); s++)
                variableNames[s] = servos[s].Name;

            return variableNames;

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public double[] GetStateVetorAngle()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            double[] angleStateVector = new double[servos.Count()];

            for (int s = 0; s < servos.Count(); s++)
                angleStateVector[s] = servos[s].Target;

            return angleStateVector;
        }


        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void SetState(double[] stateVector)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //for(int i = 0; i < setStateVector.Length; i++)
            for (int s = 0; s < servos.Count(); s++)
                servos[s].MoveToAngle(stateVector[s]);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void WriteServoPosition()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //check to see if all servos are initialized
            if (!initialized)
            {
                
                //get the current
                double[] homeState = GetStateVetorAngle();

               //create the calibration state
               for (int s = 0; s < homeState.Length; s++)
                   homeState[s] = 60;

               //go to the calibration state
               SetState(homeState);

               //initialize the calibration
               calibration = new double[homeState.Length];            
                
               //remember that initialization has now happened
               initialized = true;
 
            }

            if (initialized)
            {
                //get the state vector
                double[] stateVector = GetStateVetorAngle();

                //create the write buffer
                byte[] writeBuffer = new byte[3 + stateVector.Length * 2];

                //set the command 
                writeBuffer[0] = (byte)'c';
                writeBuffer[1] = (byte)'m';
                writeBuffer[2] = (byte)'d';

                

                //convert the angle into a word (which is 100*the angle)
                for (int i = 0; i < stateVector.Length; i++)
                {
                    Int16 value = (Int16)((stateVector[i] + calibration[i]) * 100.0);
                    writeBuffer[3 + i * 2 + 0] = BitConverter.GetBytes(value)[0];
                    writeBuffer[3 + i * 2 + 1] = BitConverter.GetBytes(value)[1];

                }

                serialPort.Write(writeBuffer, 0, writeBuffer.Length);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void ConnectServo(Servo servo, int setServoIndex, int setSensorIndex)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            servos.Add(servo);
        }

    }

}
