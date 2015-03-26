/*
//    BoB remote control program. Version 1.2
//      This program allows BoB to be actuated remotely (via USB cable) by a windows desktop application.
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
const int  messageLength              = 11;     // the number of bytes in a message from the control program  
const int  maximumServoPosition       = 2000;   // the maximum pulse duration for the servo shield (2ms pulse)
const int  minimumServoPosition       = 1000;   // the minimum pulse duration for the servo shield (1ms pulse)
const int  centerServoPosition        = 1500;   // the minimum pulse duration for the servo shield (1ms pulse)

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void setup()
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  //initialize the servos
  InitializeServos();  
  
  //establist a connection with the remote controller (the FOBO poser application)
  EstablistConnection();
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void EstablistConnection()
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  //wait for a second to begin (keeps the communication line open in case a new program is being downloaded)
  delay(1000);  
  
  //start up the communication
  Serial.begin(9600);

  //buffer to hold the incoming message
  int inputBuffer[20];
  
  //broadcast our id until someone responds
  while(true)
  {
    //broadcast id
    Serial.print("FOBO");  
    
    //wait for a bit
    delay(100);  
    
    //look for a response
    if(Serial.available() > 1)
    {
      for(int b = 0; b < 2; b++)
        inputBuffer[b] = Serial.read();
        
      // make sure someone friendly is on the line
      if(inputBuffer[0] == (int)'h' && inputBuffer[1] == (int)'i')
      {
            Serial.print("connected");  
            break;
      }
    }      
  }
  
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void InitializeServos()
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
  // check to see if there are enough bytes on the serial line for a message
  if (Serial.available() >= messageLength) 
    // read the incoming message
    if( ReadMessage() )    
      // respond to the computer that is controlling FOBO
      SendResponse();      
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool ReadMessage()
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  // buffer to hold the incoming message
  int inputBuffer[20];

  // read the message
  int index = 0;
  while(Serial.available() > 0)
  {
    inputBuffer[index] = Serial.read();
    index++;
  }

  // make sure the message starts with "cmd" ... otherwise it isn't a valid message
  if(inputBuffer[0] == (int)'c' && inputBuffer[1] == (int)'m' && inputBuffer[2] == (int)'d')
  {
    //set the servo positions
    for (int servo = 0; servo < numberOfServos; servo++)
    {      
      // each servo position is sent as a 2 byte value (high byte, low byte) unsigned integer (from 0 to 65536)
      // this number is encoding the angle of the servo. The number is 100 * the servo angle.  This allows for the
      // storage of 2 significant digits(i.e. the value can be from 0.00 to 120.00 and every value in between).
      // Also remember that the servos FOBO uses have a range of 120 degrees, so the home position is 60 degrees.
      word value = word(inputBuffer[servo*2 + 1 + 3], inputBuffer[servo*2 + 0 + 3]);
      float servoAngle = (float)value/100.00;
      
      // the servo control shield commands the servos via pulse width modulations, not angles
      // the PWMs range from 1000 (equal to 0 degrees) to 2000 (equal to 120 degrees)
      // so the servo angle needs to be converted to the corresponding PWM range.
      int pwm = (int)(servoAngle/120.0* (float)(maximumServoPosition - minimumServoPosition)) + minimumServoPosition;
      
      //Set PWM for the servo shield to send out to the servo
      ServosSetPosition(servo, pwm);       
    }

    // a valid message was received    
    return true;
  }  
  else
    // the message wasn't valid
    return false;  
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void SendResponse()
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  // the program is expecting feedback for each the servo position
  // FOBO doesn't have any position feedback, so just send the center position (128)    
  for(int s = 0; s < numberOfServos; s++)
    Serial.write(128);
  
  // end the response  
  Serial.print("done");  
}

