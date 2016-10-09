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

void EstablistConnection();
void InitializeServos();
void ServosSetPosition(int servo, int pwm);
bool ReadMessage();
void SendResponse();

#define obstaclePin 12
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Servo Pin definitions
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#define RAservoPin 4                       // right ankle servo pin
#define RHservoPin 5                       // right hip servo pin
#define LAservoPin 14                      // left ankle servo pin
#define LHservoPin 16                      // left hip servo pin

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
                      {  960,  3704,  2718,  3679 },
                      { 3006,  6349,  2718,  6997 },
                      { 5476,  6698,  5961,  6523 },
                      { 6000,  6000,  6000,  6000 }
  
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
int calibration[4] = {956, -458, 498, -298 };
//int calibration[4] = {518, -79, -1235, 358 };
//int calibration[4] = {-1036, -816, 996, -199 };

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Variables
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
int         distance         = 100;  //distance measured on the ping
int         lastDistance     = 100;  //previous distance measured on the ping
int         Connected        = false;


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Actions
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Action turn(7, 100, 25, turnFrames);   //the turn right action
Action walk(7, 100, 25, walkFrames);   //the walk forward action
Action* action;                        //pointer to the current action

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void setup()
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  //set the initial action
  action = &walk;
  //initialize the servos
  InitializeServos();  
  
  //establist a connection with the remote controller (the FOBO poser application)
  EstablistConnection();
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void EstablistConnection()
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  int timeout = 100;  /* 10 seconds timeout */
  //wait for a second to begin (keeps the communication line open in case a new program is being downloaded)
  delay(1000);  
  
  //start up the communication
  Serial.begin(38400);
  Serial.print("\r\n+INQ=1\r\n"); // This is for Seeedstudio master/slave unit (change as needed for your model)   

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
            Connected = true;
            break;
      }
    }
    if (timeout) 
    {
      timeout--;    
    }
    else  /* timeout counter expired */
    {
      Connected = false;
      break;
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
long MeasureDistance()
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{
  long ret = 0;
  int val1, val2;
  
  pinMode(obstaclePin, INPUT);
  val1 = digitalRead(obstaclePin);
  val2 = digitalRead(obstaclePin);
  if (val1 == LOW && val2 == LOW)
     ret = 5;
   else
     ret = 10;
  return ret;
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

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void loop()
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{  
  if (Connected == true)
  {
    // check to see if there are enough bytes on the serial line for a message
    if (Serial.available() >= messageLength) 
      // read the incoming message
      if( ReadMessage() )    
        // respond to the computer that is controlling FOBO
        SendResponse();      
  }
  else
  {
    //update the current action
    UpdateAction();
  
    //get the frame from the current action  
    int frame[numberOfServos];
    action->GetCurrentFrame(frame);
  
    //set the servo positions for this frame number
    SetServoPositions(frame);                    
  }
  
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

