/*
  ServoShieldSequencer.pde - Sequencer sample for Renbotics Servo Shield www.renbotics.com
  Revision 1.0
  Copyright (c) 2009 Adriaan Swanepoel.  All right reserved.

  This sample is free software; you can redistribute it and/or
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
  
  Credits
  -------
  Integer EEPROM routines from A||SySt3msG0 at http://www.arduino.cc/cgi-bin/yabb2/YaBB.pl?num=1218921214/0
  
  EEPROM Storage Format
  ---------------------
  0    Sequence count         byte
  1    length of memory used  byte
  n    Sequence Number        byte
  n+1  Sequence Length        byte                                                           |
  n+2  Sequence Interval      int                                                            |
  n+4  Step Number            byte   |                                                       | Repeats for each sequence
  n+4  Servo Number           byte   |  Repeats for each servo in each step, orderd by step  |
  n+5  Servo Step             int    |                                                       |
  
  Sequencer ICD
  -------------
  New Sequence                NS
  Sequence Interval           SI [interval in ms]
  Sequence Step               SS [step number] [servo number] [position]
  Clear Sequences             CS
  Display Sequences           DS
  Play Sequence               PS [sequence number]              
  Stop sequence Play          SP                    
  Sequence Count              SC                    
  Sequence Number             SN    
  Set servo Bounds            SB [servo number] [minimum position] [maximum position]
  Move Servo                  SM [servo number] [position]

  Examples
  --------
  Example 1:
  Sequence to pan a servo from 0deg to 180 deg stopping at 90deg with 1 second interval between steps 
  
  CS
  NS
  SI 1000
  SS 1 1 1000
  SS 2 1 1500
  SS 3 1 2000  
  SS 4 1 1500
*/
#include <stdio.h>
#include <string.h>
#include <EEPROM.h> 
#include <ServoShield.h>
#include <Ethernet.h>

void setup()
{
  //Sequencer
  SetupSequencer();

  //Web
  SetupWeb();
}

void loop()
{
  ProcessComms();
  ProcessWeb();
  PlaySequence();
}


