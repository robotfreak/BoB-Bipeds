//Web interface
byte mac[] = { 0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xED };
byte ip[] = { 192, 168, 1, 177 };
char commandresult[100];
Server server(80);

//HTML Formatting
char contenttype[] PROGMEM = "HTTP/1.1 200 OK\nContent-Type: text/html\n\n";
char pageheader[] PROGMEM = "<HTML>\n<HEAD>\n<title>Renbotics ServoShield Sequencer</title>\n<style type=\"text/css\"><!-- .text {font-family: Verdana, Geneva, sans-serif;}-->\n</style></HEAD>\n<BODY class=\"text\">\n<p><h1>Renbotics ServoShield</h1></p>\n";
char welcome[] PROGMEM = "<p>Welcome to the Renbotics ServoShield Sequencer web interface.<br />To play a sequence click on its name.<br />To add a new sequence use the Custom Command interface.</p>";
char pagefooter[] PROGMEM = "<p><center><a href=\"http://www.renbotics.com\">www.renbotics.com</a></center></p>\n</BODY></HTML>";
char commandform[] PROGMEM = "<p><form action=\"/command.html\" enctype=\"application/x-www-form-urlencoded\" method=\"get\">Custom Command<br /><input name=\"command\" type=\"text\" value=\"PS 1\" /><input name=\"Send\" type=\"submit\" value=\"Submit\" /></form></p>";
char sequencetableheader[] PROGMEM = "<p><table width=50%><tr><td width = 30%><b>Sequence</b></td><td><b>State</b></td></tr>";

void printlnP(Client client, prog_char *data, int length)
{
  char buffer[33];
  int bufferEnd = 0;

  while (length--)
  {
    if (bufferEnd == 32)
    {
      buffer[32] = '\0';
      client.print(buffer);
      bufferEnd = 0;
    }

    buffer[bufferEnd++] = pgm_read_byte(data++);
  }

  if (bufferEnd > 0)
  {
    buffer[bufferEnd - 1] = '\0';
    client.print(buffer);
  }
}

//DisplayStandardOptions(Client client)
//
void DisplaySequences(Client client)
{
  printlnP(client, sequencetableheader, 87);
  for (int i = 0; i < EEPROM.read(0); i++)
  {
    client.println("<tr>");
    client.println("<td>");
    client.print("<a href=\"/sequence");
    client.print((int)(i + 1));
    client.print("\">Play Sequence ");
    client.print((int)(i + 1));
    client.println("</a><br />");
    
    client.println("</td>");
    client.println("<td>");
    
    if (PlayingSequence() == i + 1)
      client.println("<a href=\"/stop\">Playing (Click to stop)</a>");
      
      else
      
        client.println("Stopped");
    
    client.println("</td>");
    client.println("</tr>");
  }
  client.println("</p>");
  client.println("</table>");
}

//ProcessWebRequest(buffer);
//
void ProcessWebRequest(char* process)
{
  // GET / HTTP/1.1
  
  // GET /sequence0 HTTP/1.1
  if (strstr(process, "GET /sequence") != 0)
  {
    char get[10], seq[16];
    int num;
    if (sscanf(process, "%3s %9s%d", get, seq, &num) == 3)
    {
      StartSequence(num);
      Serial.print("Sequence ");
      Serial.print(num);
      Serial.println(" web start");
    }
      else 
        Serial.println("Error processing web start request");
  }
  
  //GET /stop HTTP/1.1  
  if (strstr(process, "GET /stop") != 0)
  {
    StopSequence();
    Serial.println("Web sequence stop");
  } 
  
  //GET /command.html?command=SS+1+1+1000&Send=Submit HTTP/1.1
  if (strstr(process, "GET /command.html?command=") != 0)
  {
    char get[10], file[14], command[64], params[10];
    if (sscanf(process, "%3s %14s%s", get, file, command) == 3)
    {
      int posstart = (int)(strchr(command, '=') - command + 1);
      int posnext = posstart;
      if (strchr(command, '&') != 0)
        posnext = (int)(strchr(command, '&') - command + 1);
        
        else
      
          posnext = strlen(command);
      
      int length = posnext - posstart - 1;
      strncpy(params, command + posstart, length);
      params[length] = '\0';
      
      while (strchr(params, '+') != 0)
        params[(int)(strchr(params, '+') - params)] = ' ';
      
      if (ProcessSequencerCommand(params) == 0)
        sprintf(commandresult, "%s OK", params);
        
        else
        
        sprintf(commandresult, "%s ERROR", params);
    }
  }
}

//ProcessWeb()
//
void ProcessWeb()
{
  char buffer[255];
  sprintf(buffer, "");
  Client client = server.available();
  if (client) 
  {
    // an http request ends with a blank line
    boolean current_line_is_blank = true;
    
    while (client.connected()) 
    {
      if (client.available()) 
      {
        char c = client.read();
        sprintf(buffer, "%s%c", buffer, c);
        // if we've gotten to the end of the line (received a newline
        // character) and the line is blank, the http request has ended,
        // so we can send a reply
        if (c == '\n' && current_line_is_blank) 
        {
          printlnP(client, contenttype, 44);
          printlnP(client, pageheader, 223);
          printlnP(client, welcome, 173);
          
          DisplaySequences(client);
         
          printlnP(client, commandform, 219);    
     
          if (commandresult != "")
          {
            client.print("<p>");
            client.print(commandresult);
            client.println("</p>");
          }
          
          printlnP(client, pagefooter, 96);
          break;
        }
        
        if (c == '\n') 
        {
          // we're starting a new line
          current_line_is_blank = true;
        
          if (strstr(buffer, "GET /") != 0)
            ProcessWebRequest(buffer);
          sprintf(buffer, "");
        } 
        
        else if (c != '\r')
        {
          // we've gotten a character on the current line
          current_line_is_blank = false;
        }
      }
    }
    
    // give the web browser time to receive the data
    delay(1);
    client.stop();
  }
}

void SetupWeb()
{
  Ethernet.begin(mac, ip);
  server.begin();
  sprintf(commandresult, "");
}
