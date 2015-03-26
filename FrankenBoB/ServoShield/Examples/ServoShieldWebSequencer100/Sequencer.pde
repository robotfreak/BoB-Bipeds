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

  playsequence = sequencenumber;
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

void StopSequence()
{
  playsequence = -1;
  playing = false;
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
int ProcessSequencerCommand(char process[80])
{
  char cmd[5];
  int param1, param2, param3;
  sscanf(process, "%s", cmd);
  
  if (strstr(cmd, "NS") != 0)
  {
    NewSequence();
    Serial.println("NS OK");
    return 0;
  }
  
  if (strstr(cmd, "SI") != 0)
  {
    if (sscanf(process, "%s %d", cmd, &param1) == 2)
    {
      SetSequenceInterval(param1);
      Serial.print("SI ");
      Serial.print(param1);
      Serial.println(" OK");
      return 0;
    }
    
      else
      
        return 1;
  }
  
  if (strstr(cmd, "SS") != 0)
  {
    if (sscanf(process, "%s %d %d %d", cmd, &param1, &param2, &param3) == 4)
    {
      AddSequenceStep(param1, param2, param3);    
      Serial.print("SS ");
      Serial.print(param1);
      Serial.print(" ");
      Serial.print(param2);
      Serial.print(" ");
      Serial.print(param3);
      Serial.println(" OK");
      return 0;
    }
    
      else
      
        return 1;
  }
 
  if (strstr(cmd, "CS") != 0)
  {
    ClearSequences();
    Serial.println("CS OK");
    return 0;
  }
  
  if (strstr(cmd, "DS") != 0)
  {
     DisplaySequences();
     Serial.println("DS OK");
     return 0;
  }
   
  if (strstr(cmd, "PS") != 0)
  {
    if (sscanf(process, "%s %d", cmd, &param1) == 2)
    {
      StartSequence(param1);
      Serial.print("PS ");
      Serial.print(param1);
      Serial.println(" OK");
      return 0;
    }
    
      else
      
        return 1;
  }
  
  if (strstr(cmd, "SP") != 0)
  {
    StopSequence();
    Serial.println("SP OK");
    return 0;
  }
  
  if (strstr(cmd, "SB") != 0)
  {
    if (sscanf(process, "%s %d %d %d", cmd, &param1, &param2, &param3) == 4)
    {
      servoshield.setbounds(param1, param2, param3);
       Serial.print("SB ");
      Serial.print(param1);
      Serial.print(" ");
      Serial.print(param2);
      Serial.print(" ");
      Serial.print(param3);
      Serial.println(" OK");
      return 0;
    }
    
      else
      
        return 1;
  }
  
  if (strstr(cmd, "MS") != 0)
  {
    if (sscanf(process, "%s %d %d", cmd, &param1, &param2) == 3)
    {
      servoshield.setposition(param1, param2);
      Serial.print("MS ");
      Serial.print(param1);
      Serial.print(" ");
      Serial.print(param2);
      Serial.println(" OK");
      return 0;
    }
    
      else
      
        return 1;
  }
  
  //Unsupported command
  return 1;
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
      ProcessSequencerCommand(command);
      sprintf(command, "");
    }
  }
}

int PlayingSequence()
{
  return playsequence;
}

void SetupSequencer()
{
  servoshield.start();
  
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
  playsequence = -1;
}
