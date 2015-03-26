/*
//    BoB autonomous navigation program. Version 2.1
//      This program makes BoB walk forward until it observes an obstacle within 
//      5 inces at which point it will turn right until the way is clear and then
//      start  walking forward again (repeat).
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
#include <Arduino.h>
#include <Servo.h>

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Ultrasonic Sensor Pin definitions
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//#define USE_PING                           // enable this define when using PING or SRF05 in single Pin mode
#define USE_SR04                           // enable this define when using SRF04, HC-SR04 or SRF05 in dual Pin mode
#define USE_LED_MTX

#ifdef  USE_LED_MTX
#include <Wire.h>
#include "Adafruit_LEDBackpack.h"
#include "Adafruit_GFX.h" 

Adafruit_8x8matrix matrix = Adafruit_8x8matrix(); 
int ledMtxMode = 0;

static uint8_t PROGMEM
  smile_bmp[] =
  { B00011100,
    B00111000,
    B01101000,
    B01001000,
    B01001000,
    B01101000,
    B00111000,
    B00011100 },
  neutral_bmp[] =
  { B00011000,
    B00111000,
    B00011000,
    B00011000,
    B00011000,
    B00011000,
    B00111000,
    B00011000 },
  frown_bmp[] =
  { B01110000,
    B00111000,
    B00101100,
    B00100100,
    B00100100,
    B00101100,
    B00111000,
    B01110000 }; 
#endif

#ifdef USE_PING
#define pingPin    3                        //digital pin number on the arduino board that has the ping data line plugged into it
#endif
#ifdef USE_SR04
#define pingPin    4                        //digital pin number on the arduino board that has the ping data line plugged into it
#define triggerPin 3                        //digital pin number on the arduino board that has the ping data line plugged into it
#endif


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
const int  maximumServoPosition       = 2000;   // the maximum pulse duration for the servo shield (2ms pulse)
const int  minimumServoPosition       = 1000;   // the minimum pulse duration for the servo shield (1ms pulse)
const int  centerServoPosition        = 1500;   // the minimum pulse duration for the servo shield (1ms pulse)

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class Action
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  //  The Action class collects information necessary to perform a sequence of movements.  It allows for intermediate 
  //  frames (robot positions) to be generated between key frames on the fly (keeping memory usage low).   
  /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////  
  
  /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  //Members
  /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  public:
    int numberOfFrames;      //total number of frames in the action
    int numberOfKeyFrames;   //number of key frames (this must match the first dimension of the frames array)
    int frameDelay;          //number of milliseconds to wait inbetween each frame during playback
    int frameNumber;         //current frame number during playback
    int* frames;             //pointer to a two dimensional array containing the individual frames

  /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  //Methods
  /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  
  /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  Action(int setNumberOfKeyFrames, int setNumberOfFrames, int setFrameDelay, void* setFrames)
  /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  {
    frames            = (int*)setFrames;
    numberOfKeyFrames = setNumberOfKeyFrames;
    numberOfFrames    = setNumberOfFrames;
    frameDelay        = setFrameDelay;
    frameNumber       = 0;
  }  
  
  /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  void NextFrame()
  /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  {
    frameNumber++;
    if(frameNumber >= numberOfFrames)
      frameNumber = 0;
      
    delay(frameDelay);    
  }

  /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  void GetCurrentFrame(int* frame)
  /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  {
     GetFrame(frame, frameNumber);
  }
   
  /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  void GetFrame(int* frame, int targetFrameNumber)
  /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  {
    //Compute the frame based on the target frame number.  Remember that this can either be a key frame or an intermediate
    //frame between two key frames.
    
    //compute the number of intermediate frames between each key frame
    int numberOfIntermediateFrames = numberOfFrames/numberOfKeyFrames;
    
    //compute the closest key frame before the target frame number
    int sourceKeyFrame = targetFrameNumber/numberOfIntermediateFrames;
    
    //get the key frame after the source key frame 
    int destinationKeyFrame = sourceKeyFrame + 1;
    //wrap around if this is the last frame
    if(destinationKeyFrame >= numberOfKeyFrames)
      destinationKeyFrame = 0;
      
    //compute mixing percentage between the source and destination key frame
    float percent = (float)(targetFrameNumber - sourceKeyFrame*numberOfIntermediateFrames)/(float)numberOfIntermediateFrames;
    
    //mix the source and destination key frame to produce the target frame
    GetIntermediateFrame(frames + sourceKeyFrame*numberOfServos, frames + destinationKeyFrame*numberOfServos, percent, frame);    
  }
  
  private:
  
  //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  void GetIntermediateFrame(int* fromState, int* toState, float percent, int* outState)
  //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  {
      //update each servo
      for (int servo = 0; servo < numberOfServos; servo++)
      {   
        //compute the angles from both the from state and the to state for this servo.
        //remember that the states are stored as 100*angle ... so they need to be converted back into angles
        int source = ((float)fromState[servo]);      
        int target = ((float)toState[servo]);    
        
        //the servo angle used is the linear interpolation between the two 
        outState[servo] = percent*(target - source) + source;
      }
  }
  
};
 
 
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Action Frames
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Joints positions are in degrees * 100 (home position is 60 degrees)
/////////////////////////////////////////////
//JOINT INDEXES
// 0 right ankle
// 1 right hip
// 2 left ankle
// 3 left hip
/////////////////////////////////////////////
int walkFrames[8][4] =
{
                      { 6000,  6000,  6000,  6000 },
                      { 9442,  6000, 10740,  6000 },
                      { 9542,  3580, 10740,  3679 },
                      { 6274,  3704,  6362,  3679 },
                      { 6274,  3704,  6362,  3679 },
                      {  960,  3704,  2718,  3679 },
                      { 3006,  6349,  2718,  6997 },
                      { 5476,  6698,  5961,  6523 }
  
};

int turnFrames[8][4] = {
                      { 6000,  6000,  6000,  6000 },
                      { 8800,  9000,  8000,  8000 },
                      { 6900, 10600,  6800,  9800 },
                      { 4553, 10577,  5491,  9355 },
                      { 5200,  9000,  2700,  7700 },
                      { 5298,  6558,  5325,  5579 },
                      { 6000,  6000,  6000,  6000 },
                      { 6000,  6000,  6000,  6000 }
};


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Calibration
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
const int calibration[4] = {0, -1400, 500, 800 };

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Variables
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
int         distance         = 100;  //distance measured on the ping
int         lastDistance     = 100;  //previous distance measured on the ping

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Actions
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Action turn(7, 100, 25, turnFrames);   //the turn right action
Action walk(8, 100, 25, walkFrames);   //the walk forward action
Action* action;                        //pointer to the current action

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void setup()
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  //set the initial action
  action = &walk;
  
  //wait for a second to begin (keeps the communication line open in case a new program is being downloaded)
  delay(1000);    
  
  //start up the communication
  Serial.begin(9600);  
  
  //initialize the servos
  initializeServos();

#ifdef USE_LED_MTX
  // initialize LED matrix
  matrix.begin(0x70);  // pass in the address 
  distance = 10;
  UpdateLEDMtx();
  delay(2000);
  distance = 5;
  UpdateLEDMtx();
  delay(2000);
  distance = 1;
  UpdateLEDMtx();
  delay(2000);
  distance=100;
#endif  


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
bool ObstacleInPath()
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  //initialize the result
  boolean foundObstacle = true;
 
  //measure the distance 
  distance = MeasureDistance();
  
  //ignore error readings 
  if(distance == 0)
    distance = lastDistance;

  //make sure the object was observed at least twice in a row (high frequency filter)
  if(distance > 5 && lastDistance > 5)    
    foundObstacle = false;
        
  //remember the distance 
  lastDistance = distance;
    
  //return the result
  return foundObstacle;
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void TransitionTo(int* fromState, int* toState, int transitionFrames, int wait)
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  //the idea is to start at the fromState and move to the toState with some intermediate states (transitionFrames) with a delay at each transition state
  int frame[numberOfServos];
  
  //walk through each transition frame (skipping the first one because this is the fromState which the robot should already be at  
  for(int f = 1; f < transitionFrames; f++)
  {
    //set the servo positions
    //SetIntermediateFrame(fromState, toState, (float)f/(float)transitionFrames);
    GetIntermediateFrame(fromState, toState, (float)f/(float)transitionFrames, frame);
    SetServoPositions(frame);
    
    //wait for the specified number of milliseconds
    delay(wait);
  }
}


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void GetIntermediateFrame(const int* fromState, const int* toState, float percent, int* outState)
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
    //update each servo
    for (int servo = 0; servo < numberOfServos; servo++)
    {   
      //compute the angles from both the from state and the to state for this servo.
      //remember that the states are stored as 100*angle ... so they need to be converted back into angles
      int source = ((float)fromState[servo]);      
      int target = ((float)toState[servo]);    
      
      //the servo angle used is the linear interpolation between the two 
      outState[servo] = percent*(target - source) + source;
    }
}


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void UpdateAction()
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  //check to see if the robot is walking
  if(action == &walk)
  {
    //look for obstacles in the way ... but only respond if the robot is in a frame (either left or right) where it can transition to turn
    if( ObstacleInPath() && (action->frameNumber == 0 || action->frameNumber == 49))
    {
      //transition to the start of the turning action
      int sourceFrame[8];
      walk.GetCurrentFrame(sourceFrame);
      int destinationFrame[8];
      turn.GetFrame(destinationFrame, 0);
      TransitionTo(sourceFrame, destinationFrame, 10, 40);
      
      //next time the robot starts walking it will start with its weight on the right foot.
      //starting at frame 0 makes the robot turn left a bit and it might run into the wall it was try to avoid
      action->frameNumber = 0;
      
      //make turning the current action
      action = &turn;
    }
    else
    {
      //no obstacle (or can't transition to a turn) so just move to the next frame in the walking sequence
      action->NextFrame();    
    }
  }
  //check to see if the robot is turning
  else if (action == &turn)
  {
    //update for turning action
    
    //check to see if this is the last frame in the turning action ... if so then look to see if the way is clear
    if(action->frameNumber == action->numberOfFrames -1 && !ObstacleInPath())
    {
      int sourceFrame[8];
      turn.GetFrame(sourceFrame, 0);
      int destinationFrame[8];
      walk.GetFrame(destinationFrame, 0);
      
      //no obstacle so start walking again
      TransitionTo(sourceFrame, destinationFrame, 10, 40);            
            
      //next time the turning action is started begin at frame 0
      action->frameNumber = 0;
      
      //change the action to walk
      action = &walk;      
    }
    else
    {
      //go to the next frame in the turning action
      action->NextFrame();
    }
  }
}

#ifdef USE_LED_MTX
void UpdateLEDMtx()
{
  if (distance >= 10 && ledMtxMode != 1)
  {
    matrix.clear();
    matrix.drawBitmap(0, 0, smile_bmp, 8, 8, LED_ON);
    matrix.writeDisplay();
    ledMtxMode = 1;
  }
  else if (distance < 10 && distance >= 5 && ledMtxMode !=2)
  {
    matrix.clear();
    matrix.drawBitmap(0, 0, neutral_bmp, 8, 8, LED_ON);
    matrix.writeDisplay();
    ledMtxMode = 2;
  }
  else if (distance < 5 && ledMtxMode != 3)
  {
    matrix.clear();
    matrix.drawBitmap(0, 0, frown_bmp, 8, 8, LED_ON);
    matrix.writeDisplay();
    ledMtxMode = 3;
  }
}
#endif

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void loop()
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  //this is the main update loop for the microcontroller
  
  //update the current action
  UpdateAction();
  
  //get the frame from the current action  
  int frame[numberOfServos];
  action->GetCurrentFrame(frame);
  
  //set the servo positions for this frame number
  SetServoPositions(frame);                    

#ifdef USE_LED_MTX
  UpdateLEDMtx();
#endif
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void SetServoPositions(int* frame)
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  //update each servo
  for (int servo = 0; servo < numberOfServos; servo++)
  {   
    //the servo angle used is the linear interpolation between the two 
    float servoAngle = (frame[servo] + calibration[servo])/100.0f;
     
    // the servo control shield commands the servos via pulse width modulations, not angles
    // the PWMs range from 1000 (equal to 0 degrees) to 2000 (equal to 120 degrees)
    // so the servo angle needs to be converted to the corresponding PWM range.
    int pwm = (int)(servoAngle/120.0* (float)(maximumServoPosition - minimumServoPosition)) + minimumServoPosition;
      
    //Set PWM for the servo shield to send out to the servo
    ServosSetPosition(servo, pwm);       
  }


}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// some of the code below was found on http://arduino.cc/en/Tutorial/Ping?from=Tutorial.UltrasoundSensor.  Thanks Arduino guys!
//  credit:
//    by David A. Mellis
//    modified 30 Aug 2011
//    by Tom Igoe
//    modified 04.07.2013
//    by RobotFreak for SR04/HC-SR04
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
long MeasureDistance()
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  // establish variables for duration of the ping, 
  // and the distance result in inches and centimeters:
  long duration, inches, cm;
#ifdef USE_SR04
  pinMode(triggerPin, OUTPUT);
  digitalWrite(triggerPin, LOW);
  delayMicroseconds(2);
  digitalWrite(triggerPin, HIGH);
  delayMicroseconds(5);
  digitalWrite(triggerPin, LOW);
#endif
#ifdef USE_PING
  // The PING))) is triggered by a HIGH pulse of 2 or more microseconds.
  // Give a short LOW pulse beforehand to ensure a clean HIGH pulse:
  pinMode(pingPin, OUTPUT);
  digitalWrite(pingPin, LOW);
  delayMicroseconds(2);
  digitalWrite(pingPin, HIGH);
  delayMicroseconds(10);
  digitalWrite(pingPin, LOW);

  // The same pin is used to read the signal from the PING))): a HIGH
  // pulse whose duration is the time (in microseconds) from the sending
  // of the ping to the reception of its echo off of an object.
#endif
  pinMode(pingPin, INPUT);
  duration = pulseIn(pingPin, HIGH);

  // convert the time into a distance
  inches = microsecondsToInches(duration);
  cm = microsecondsToCentimeters(duration);

  return inches;
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
long microsecondsToInches(long microseconds)
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  // According to Parallax's datasheet for the PING))), there are
  // 73.746 microseconds per inch (i.e. sound travels at 1130 feet per
  // second).  This gives the distance travelled by the ping, outbound
  // and return, so we divide by 2 to get the distance of the obstacle.
  // See: http://www.parallax.com/dl/docs/prod/acc/28015-PING-v1.3.pdf
  return microseconds / 74 / 2;
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
long microsecondsToCentimeters(long microseconds)
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  // The speed of sound is 340 m/s or 29 microseconds per centimeter.
  // The ping travels out and back, so to find the distance of the
  // object we take half of the distance travelled.
  return microseconds / 29 / 2;
}

