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

ServoShield servoshield;
char command[80];

int sequencecount;
int length;
int sequencestepcount;
int sequencestepaddress;

boolean playing;
int playstart;
int playsequence;
int playstep;
int playlength;
int playaddress;
int playinterval;

//EEPROMWriteInt(int p_address, int p_value)
//Writes a 2 byte integer to the eeprom at the specified address and address + 1
//Cycle time approx 6.66ms
void EEPROMWriteInt(int p_address, int p_value)
{
  byte lowByte = ((p_value >> 0) & 0xFF);
  byte highByte = ((p_value >> 8) & 0xFF);

  EEPROM.write(p_address, lowByte);
  EEPROM.write(p_address + 1, highByte);
}

//EEPROMReadInt(int p_address)
//Returns a 2 byte integer from the eeprom at the specified address and address + 1
unsigned int EEPROMReadInt(int p_address)
{
  byte lowByte = EEPROM.read(p_address);
  byte highByte = EEPROM.read(p_address + 1);

  return ((lowByte << 0) & 0xFF) + ((highByte << 8) & 0xFF00);
}

//NewSequence()
//Creates a new sequence and sets eeprom pointers
void NewSequence()
{
  sequencecount++;
  EEPROM.write(0, sequencecount);                           //Increment swquence count
  EEPROM.write(length++, sequencecount);                    //Set new sequence number

  //Set defaults to try and prevent EEPROM structure corrucption in case of an error
  sequencestepcount = 0;
  sequencestepaddress = length++;
  EEPROM.write(sequencestepaddress, sequencestepcount);     //Default sequence steps
  EEPROMWriteInt(length++, 0);                              //Default sequence interval
  length++;                                                 //Int reqires two step inc
  EEPROM.write(1, length);
}

//SetSequenceStepCount()
//Increments the current sequence step count
void IncSequenceStepCount()
{
  EEPROM.write(sequencestepaddress, sequencestepcount);
}

//SetSequenceInterval(int interval)
//Sets the current sequence step interval
void SetSequenceInterval(int interval)
{
  EEPROMWriteInt(length - 2, interval);
}

//AddSequenceStep(byte stepnumber, byte servonumber, int position)
//Adds a new step to the current sequence
void AddSequenceStep(byte stepnumber, byte servonumber, int position)
{
  sequencestepcount++;
  EEPROM.write(length++, stepnumber);
  EEPROM.write(length++, servonumber);
  EEPROMWriteInt(length++, position);
  length++;
  EEPROM.write(1, length);
  IncSequenceStepCount();
}

//ClearSequences()
//Clears the current sequences from the eeprom
void ClearSequences()
{
  sequencecount = 0;
  length = 2;
  EEPROM.write(0, sequencecount);
  EEPROM.write(1, length);
}

//StartSequence(int sequencenumber)
//Starts the selected sequence
void StartSequence(int sequencenumber)
{
  playstart = 6;
  
  while (EEPROM.read(playstart - 4) != sequencenumber)
    playstart += (EEPROM.read(playstart - 3) * 4) + 4; 

  playinterval = EEPROMReadInt(playstart - 2);
  playlength = EEPROM.read(playstart - 3);
  playstep = 1;
  playaddress = playstart;
  playing = true;
}

//PlaySequence()
//Steps through current sequence
void PlaySequence()
{
  int servonumber;
  int position;
  
  if (playing)
  {
      while ((EEPROM.read(playaddress) == playstep) && (((playaddress - playstart) / 4) < playlength))
      {
        servonumber = EEPROM.read(playaddress + 1);
        position = EEPROMReadInt(playaddress + 2);
  
        Serial.print("SS ");
        Serial.print(playstep);
        Serial.print(" ");
        Serial.print(servonumber);
        Serial.print(" ");
        Serial.println(position);
        
        servoshield.setposition(servonumber, position);
        
        playaddress += 4;  //Move to next step        
      }
        
      if (((playaddress - playstart) / 4) == playlength)    //Reset to start
      {
        playaddress = playstart;
        playstep = 0;
      }

      playstep++;
      delay(playinterval);
  }
  
  else
  
    delay(10);
}

//DisplaySequences()
//Display list of current sequences
void DisplaySequences()
{
  int curaddress;
  int cursequence;
  int sequences;
  int steps;
  int curstep;
  
  sequences = EEPROM.read(0);
  Serial.print("SC ");
  Serial.println(sequences);
  
  curaddress = 2;
  cursequence = 1;
  while (cursequence <= sequences)
  {
    //Display sequence number
    Serial.print("SN ");
    Serial.println((int)EEPROM.read(curaddress++));
    
    //Get step count
    steps = EEPROM.read(curaddress++);
    
    //Display intervale
    Serial.print("SI ");
    Serial.println(EEPROMReadInt(curaddress++));
    curaddress++;
    
    //Display steps    
    curstep = 1;
    while (curstep <= steps)
    {
      Serial.print("SS ");
      Serial.print((int)EEPROM.read(curaddress++));
      Serial.print(" ");
      Serial.print((int)EEPROM.read(curaddress++));
      Serial.print(" ");
      Serial.println(EEPROMReadInt(curaddress++));
      curaddress++;
      curstep++;
    }
    
    cursequence++;
  }
}

//ProcessCommand(char process[80])
//Processed incomming commands
void ProcessCommand(char process[80])
{
  char cmd[5];
  int param1, param2, param3;
  sscanf(process, "%s", cmd);
  
  if (strstr(cmd, "NS") != 0)
  {
    NewSequence();
    Serial.println("NS OK");
  }
  
  if (strstr(cmd, "SI") != 0)
  {
    sscanf(process, "%s %d", cmd, &param1);
    SetSequenceInterval(param1);
    Serial.print("SI ");
    Serial.print(param1);
    Serial.println(" OK");
  }
  
  if (strstr(cmd, "SS") != 0)
  {
    sscanf(process, "%s %d %d %d", cmd, &param1, &param2, &param3);
    AddSequenceStep(param1, param2, param3);    
    Serial.print("SS ");
    Serial.print(param1);
    Serial.print(" ");
    Serial.print(param2);
    Serial.print(" ");
    Serial.print(param3);
    Serial.println(" OK");
  }
 
  if (strstr(cmd, "CS") != 0)
  {
    ClearSequences();
    Serial.println("CS OK");
  }
  
  if (strstr(cmd, "DS") != 0)
  {
     DisplaySequences();
     Serial.println("DS OK");
  }
   
  if (strstr(cmd, "PS") != 0)
  {
    sscanf(process, "%s %d", cmd, &param1);
    StartSequence(param1);
    Serial.print("PS ");
    Serial.print(param1);
    Serial.println(" OK");
  }
  
  if (strstr(cmd, "SP") != 0)
  {
    playing = false;
    Serial.println("SP OK");
  }
  
  if (strstr(cmd, "SB") != 0)
  {
    sscanf(process, "%s %d %d %d", cmd, &param1, &param2, &param3);
    servoshield.setbounds(param1, param2, param3);
    Serial.print("SB ");
    Serial.print(param1);
    Serial.print(" ");
    Serial.print(param2);
    Serial.print(" ");
    Serial.print(param3);
    Serial.println(" OK");
  }
  
  if (strstr(cmd, "MS") != 0)
  {
    sscanf(process, "%s %d %d", cmd, &param1, &param2);
    servoshield.setposition(param1, param2);
    Serial.print("MS ");
    Serial.print(param1);
    Serial.print(" ");
    Serial.print(param2);
    Serial.println(" OK");
  }
}

//ProcessComms()
//Processes any host comms
void ProcessComms()
{
  while (Serial.available() > 0) 
  {
    char incoming = Serial.read();
    sprintf(command, "%s%c", command, incoming);

    if (incoming == '\r')
    {
      ProcessCommand(command);
      sprintf(command, "");
    }
  }
}

void setup()
{
  sprintf(command, "");

  Serial.begin(19200);
  Serial.println("Renbotics ServoShield Sequencer");
  
  sequencecount = EEPROM.read(0);
  length = EEPROM.read(1);
  Serial.print("IX: ");
  Serial.print(sequencecount);
  Serial.print(" ");
  Serial.println(length);
  
  playing = false;
  
  servoshield.start();
}

void loop()
{
  ProcessComms();
  PlaySequence();
}


