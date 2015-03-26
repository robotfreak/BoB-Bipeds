#include <ServoShield.h>

ServoShield servos;                       //Create a ServoShield object to control up to 16 servos

void setup()
{
  for (int servo = 0; servo < 16; servo++)//Initialize all 16 servos
  {
    servos.setbounds(servo, 1000, 2000);  //Set the minimum and maximum pulse duration of the servo
    servos.setposition(servo, 1500);      //Set the initial position of the servo
  }
  
  servos.start();                         //Start the servo shield
}

void loop()
{
  for(int pos = 1000; pos < 2000; pos++)  //Move the servos from 0 degrees to 180 degrees 
  {                                       //in steps of 1 degree 
    for (int i = 0; i < 16; i++)          //for all 16 servos
      servos.setposition(i, pos);        //Tell servo to go to position in variable 'pos' 
     delay(1);                           //waits 15ms for the servos to reach the position 
  } 
  
  for(int pos = 2000; pos >= 1000; pos--) //Move the servos from 180 degrees to 0 degrees 
  {                                
    for (int i = 0; i < 16; i++)          //all 16 servos
      servos.setposition(i, pos);        //Tell servo to go to position in variable 'pos' 
    delay(1);                            //waits 15ms for the servos to reach the position 
  } 
}
