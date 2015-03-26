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
#ifndef ServoShield_h
#define ServoShield_h

#define ServoShieldVersion 1.5


class ServoShield
{
private:
	
public:
	ServoShield();
	int setposition(int servo, int position);
	int setbounds(int servo, int minposition, int maxposition);
	int getposition(int servo);	
	int start();
	int stop();
};

#endif
