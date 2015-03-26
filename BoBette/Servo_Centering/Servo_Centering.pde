/*
//    BoB servo centering program.
//      This program moves all servos connected to their center. The center
//      of a servo is half way between the maximum and minimum rotation.
//
//    Copyright (C) 2012  Jonathan Dowdall, Project Biped (www.projectbiped.com)
//                        adopted to BoB Biped by RobotFreak
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

#include <Servo.h>


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Servo Pin definitions
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#define RAservoPin 6                       // right ankle servo pin
#define RHservoPin 7                       // right hip servo pin
#define LAservoPin 8                       // left ankle servo pin
#define LHservoPin 9                       // left hip servo pin

#define RAservoIdx 0                       // right ankle servo index in comm buffer
#define RHservoIdx 1                       // right hip servo index in comm buffer
#define LAservoIdx 2                       // left ankle servo index in comm buffer
#define LHservoIdx 3                       // left hip servo index in comm buffer

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Variables
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Servo RAservo;                              // right ankleservo object
Servo RHservo;                              // right hip servo object
Servo LAservo;                              // left ankleservo object
Servo LHservo;                              // left hip servo object

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Constants
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
const int  numberOfServos             = 4;      // the communication with the control program is expecting 8 servos (only 4 needed for BoB!) 
const int  messageLength              = 19;     // the number of bytes in a message from the control program  
const int  maximumServodPosition      = 2000;   // the maximum pulse duration for the servo shield (2ms pulse)
const int  minimumServoPosition       = 1000;   // the minimum pulse duration for the servo shield (1ms pulse)
const int  centerServoPosition        = 1500;   // the minimum pulse duration for the servo shield (1ms pulse)
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void setup()
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  //initialize the servos
  initializeServos();
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void initializeServos()
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{  
  LHservo.attach(LHservoPin);
  RHservo.attach(RHservoPin);
  LAservo.attach(LAservoPin);
  RAservo.attach(RAservoPin);

  ServosSetPosition(LHservoIdx, centerServoPosition);
  ServosSetPosition(RHservoIdx, centerServoPosition);
  ServosSetPosition(LAservoIdx, centerServoPosition);
  ServosSetPosition(RAservoIdx, centerServoPosition);
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void  ServosSetPosition(int servo, int pwm)
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  switch(servo)
  {
    case LHservoIdx:
      LHservo.write(pwm);
    break;
    case RHservoIdx:
      RHservo.write(pwm);
    break;
    case LAservoIdx:
      LAservo.write(pwm);
    break;
    case RAservoIdx:
      RAservo.write(pwm);
    break;
  }
}       

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void loop()
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
}


