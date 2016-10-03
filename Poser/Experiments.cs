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
using System.Threading;
using System.Windows;

namespace ServoControl
{
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public class Experiment : ServoInteractive
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Members
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            protected   Servo   servo;
            protected   Thread  workThread;    

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Events
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            public event BlankDelegate  Finished;
            public event DoubleDelegate Progress;
            public event StringDelegate Updates;

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void SetServo(Servo value)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            servo = value;
            servo.Changed += new ServoDelegate(servo_Changed);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void servo_Changed(Servo value)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void Start()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //kill existing thread
            if (workThread != null)
                workThread.Abort();

            workThread = new Thread(new ThreadStart(ExperimentLauncher));
            workThread.Start();
            
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected void Update(string text)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            if (Updates != null)
                Updates(text);

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected void SetProgress(double value)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            if (Progress != null)
                Progress(value);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void ExperimentLauncher()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //make sure the servo has been set
            if (servo == null)
                throw new Exception("Servo isn't set for the experiment");

            //reset the stops
            DataRange stops = new DataRange() { maximum = servo.Stops.maximum, minimum = servo.Stops.minimum };
            servo.Stops = servo.Range;

            //run the experiment
            RunExperiment();

            //restore the stops
            servo.Stops = stops;

            //let everyone know it is done
            if (Finished != null)
                Finished();

        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        virtual protected void RunExperiment()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {


        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected void WaitForServoAngle(double angle)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            //wait until the servo actually gets there
            while (true)
            {
                if (Math.Abs(servo.Angle- angle) < 1.5)
                    break;
                System.Threading.Thread.Sleep(10);
            }

        }


    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public class SpeedExperiment : Experiment
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Members
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            double      rate;
            TimeSpan    span;

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Properties
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            public double Rate { get { return rate; } }
            public TimeSpan Span { get { return span; } }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected override void RunExperiment()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {
            SetProgress(0);

            //move to zero angle
            servo.MoveToAngle(servo.Range.minimum);
            WaitForServoAngle(servo.Range.minimum);

            //mark the time
            DateTime start = DateTime.Now;

            servo.MoveToAngle(servo.Range.maximum);
            WaitForServoAngle(servo.Range.maximum);

            //mark the time
            DateTime stop = DateTime.Now;

            //measure the difference
            span = stop.Subtract(start);

            //speedLabel.Content = 
            rate = (servo.Range.maximum - servo.Range.minimum) / span.TotalSeconds;

            SetProgress(90);


            //move to zero angle
            servo.MoveToAngle(servo.Range.minimum);
            WaitForServoAngle(servo.Range.minimum);

            //update the progress
            SetProgress(100);


        }

    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public class CorrelationExperiment : Experiment
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    {
 
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Events
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            public event SinglePointDelegate ReadingTaken;


        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected override void RunExperiment()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {

            //move to zero angle
            servo.MoveToAngle(servo.Range.minimum);
            WaitForServoAngle(servo.Range.minimum);

            //move through the range
            for (double a = servo.Range.minimum; a < servo.Range.maximum; a++)
            {
                //move the servo
                servo.MoveToAngle(a);

                //wait for a sec
                System.Threading.Thread.Sleep(100);

                //get the reading
                Point reading = new Point(a, servo.Angle);

                //let everyone know
                if (ReadingTaken != null)
                    ReadingTaken(reading);

                //progress
                SetProgress(a / (servo.Range.maximum - servo.Range.minimum) * 100);
            }


            //move to zero angle
            servo.MoveToAngle(servo.Range.minimum);
            WaitForServoAngle(servo.Range.minimum);

            //update the progress
            SetProgress(100);


        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public class AutoCalibration : Experiment
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    {

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected override void RunExperiment()
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        {

/*
            //move to zero angle
            servo.MoveToRaw(servo.Control.raw.minimum);

            Update("starting");

            //wait for a bit (can't know when were there becuse that's what is being calibrated)
            System.Threading.Thread.Sleep(1000);

            //read the sensor value
            double minSensorReading     = 0;
            double maxSensorReading     = 0;
            double minServoRaw          = 0;
            double maxServoRaw          = 0;
            double lastSensorReading    = servo.Raw;
            double noiseThreshold       = 1.5;
            double p;

            Update("looking for mins");

            //find the first response of the sensor
            for (p = servo.Control.raw.minimum; p < servo.Control.raw.maximum; p++)
            {
                //move the servo
                servo.MoveToRaw(p);

                //wait for a sec
                System.Threading.Thread.Sleep(100);

                //check to see if there is any response from the sensor
                if (Math.Abs(servo.Raw - lastSensorReading) > noiseThreshold)
                {
                    minSensorReading = servo.Raw;
                    minServoRaw      = p;
                    break;
                }

                //update the progress
                SetProgress(100*p / (servo.Control.raw.maximum - servo.Control.raw.minimum));

            }

            Update("min sensor: " + (int)minSensorReading + " servo: " + (int)minServoRaw);

            //find the last response of the sensor
            int numberOfSameReadings = 0;
            for (p = minServoRaw + 1; p < servo.Control.raw.maximum; p++)
            {
                //move the servo
                servo.MoveToRaw(p);

                //wait for a sec
                System.Threading.Thread.Sleep(100);

                //check to see if there is any response from the sensor
                if (Math.Abs(servo.Raw - lastSensorReading) < 0.25)
                {
                    numberOfSameReadings++;

                    //remember the readings of the first reading that was the same
                    if (numberOfSameReadings == 1)
                    {
                        maxSensorReading = lastSensorReading;
                        maxServoRaw = p-1;

                    }
                }
                else
                    numberOfSameReadings = 0;

                //exit
                if(numberOfSameReadings > 2)
                    break;

                //remember the last reading
                lastSensorReading = servo.Raw;

                //update the progress
                SetProgress(100 * p / (servo.Control.raw.maximum - servo.Control.raw.minimum));
            }

            Update("max sensor: " + (int)maxSensorReading + " servo: " + (int)maxServoRaw);


            //set the calibration
            servo.Control.bounds    = new DataRange(){minimum = minServoRaw, maximum = maxServoRaw};
            servo.Sensor.bounds     = new DataRange() { minimum = minSensorReading, maximum = maxSensorReading };


            //move to zero angle
            servo.MoveToAngle(servo.Range.minimum);
            WaitForServoAngle(servo.Range.minimum);

            //update the progress
            SetProgress(100);

            Update("done");
*/
        }
    }
}
