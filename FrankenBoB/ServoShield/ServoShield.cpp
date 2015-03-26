/*
  ServoShield.h - Decade counter driven Servo library for Arduino using one 8 bit timer and 4 DIO to control up to 16 servos
  Revision 1.1
  Copyright (c) 2009 Adriaan Swanepoel.  All right reserved.

  This library is free software; you can redistribute it and/or
  modify it under the terms of the GNU Lesser General Public
  License as published by the Free Software Foundation; either
  version 2.1 of the License, or (at your option) any later version.

  This library is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
  Lesser General Public License for more details.

  You should have received a copy of the GNU Lesser General Public
  License along with this library; if not, write to the Free Software
  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/
#include "ServoShield.h"
#include <Servo.h> 

#define servoNum 8

volatile uint16_t servopositions[servoNum];
uint16_t servosmax[servoNum];
uint16_t servosmin[servoNum];

int outputmap[] = {6, 7, 8, 9, 2, 3, 4, 5};

Servo myServo[servoNum];


ServoShield::ServoShield()
{
        //Set all servos to default center
	for (int servo = 0; servo < servoNum; servo++) 
	{
                myServo[servo].attach(outmap[servo]);
                servopositions[servo] = 1500;
		servosmax[servo] = 2000;
		servosmin[servo] = 1000;
	}
	
}

int ServoShield::setposition(int servo, int position)
{
  if (servo < servoNum)
    myServo[servo].write(position);

}

int ServoShield::getposition(int servo)
{
  if (servo < servoNum)
    return servopositions[outputmap[servo]];
  else	
    return -1;
}

int ServoShield::setbounds(int servo, int minposition, int maxposition)
{
	if (servo < servoNum)
	{
		servosmax[servo] = maxposition;
		servosmin[servo] = minposition;
		return 0;
	}
	
	return 1;
}

int ServoShield::start()
{
}

int ServoShield::stop()
{
}
