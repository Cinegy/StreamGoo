﻿#Cinegy StreamGoo Tool

Use this tool to break your RTP streams, and then see what damage you can cause.

##Why would I do this?

If you are working with RTP-wrapped transport streams, it's good to test things under less-than-ideal cirumstances. Sure, your stuff all works on a sunny day, but how will your system hold up when your satellite dish is being hit by some biblical weather? I'm sure your network never had a broadcast storm, or lost any packets - or if it did, you'd never admit it to your boss, but why not prepare for that day the dog eats a cable and causes some spanning-tree events?

##Is there anything else cool you are not telling me?

Why yes, you can also disable any Goo from getting squirted into your packets, and just use the tool to relay between networks. It's quick and dirty - but that is useful sometimes!

You can also record whatever passes through this tool to file - so if you manage to get a nice bit of corruption killing something, you can open up a beer and set up a script to kill it over and over again!

##How easy is it?

Well, we've added everything you need into a single teeny-tiny EXE again, which just depends on .NET 4. And then we gave it all a nice Apache license, so you can tinker and throw the tool wherever you need to on the planet.

Just run the EXE from inside a command-prompt, and a handy help message will pop up like this:

##What type of Goo can I insert?

We cater to a wide-range of requirements, and can perform the following corrputions (if you don't specify one, we randomly pick):

Type 0 - Single bit error; we grab a single random byte from a packet, and then just flip a single bit from that byte... subtle, but effective.
Type 1 - Single byte increment; we grab a single random byte from a packet, then add 1 to it. 
Type 2 - Zero whole packet; as simple as it seems - set every byte in a packet to 0.
Type 3 - Random value filling whole packet; not content with just writing zero into a packet? Stuff it full of some random number instead!
Type 4 - Null whole packet; classic and simple, just take the packet, and eat it - like it never happened...
Type 5 - Out of order packet; one for the RTP fans out there, just sits on a packet and then transmits it after the next packet - for if you ever wanted to actually see the difference between a plain UDP and RTP stream...
Type 6 - Add jitter; Sends the stream into a little snooze, for between 0 and 80 ms - just to give that nice little rest period...

##Command line arguments:

Double click, or just run without (or with incorrect) arguments, and you'll see this:

```
StreamGoo.exe                                                                          
StreamGoo 1.0.0.0                                                                        
Copyright ©Cinegy GmbH  2016                                                             
                                                                                         
ERROR(S):                                                                                
  -m/--multicastaddress required option is missing.                                      
  -g/--mulicastgroup required option is missing.                                         
  -n/--outputaddress required option is missing.                                         
  -h/--outputport required option is missing.                                            
                                                                                         
                                                                                         
  -a, --adapter             IP address of the adapter to listen for specified            
                            multicast (has a random guess if left blank).                
                                                                                         
  -b, --outputadapter       IP address of the adapter to write the goo'd stream          
                            to (has a random guess if left blank).                       
                                                                                         
  -m, --multicastaddress    Required. Input multicast address to read from.              
                                                                                         
  -g, --mulicastgroup       Required. Input multicast group port to read from.           
                                                                                         
  -n, --outputaddress       Required. Output address to write goo'd stream to.           
                                                                                         
  -h, --outputport          Required. Output multicast group or UDP port to              
                            write goo'd stream to.                                       
                                                                                         
  -f, --goofactor           (Default: 0) Controllable level of Gooeyness to              
                            insert into stream (chances in 1000 of inserting a           
                            drop of scum).                                               
                                                                                         
  -p, --goopause            (Default: 0) How long to sleep between Goos                  
                            (milliseconds)                                               
                                                                                         
  -d, --gooduration         (Default: 1000) How long to sleep between Goos               
                            (millseconds)                                                
                                                                                         
  -t, --gootype             (Default: -1) Force a specific goo type rather than          
                            changing each run                                            
                                                                                         
  -q, --quiet               (Default: False) Run in quiet mode - print nothing           
                            to console.                                                  
                                                                                         
  -v, --verbose             (Default: False) Run in verbose mode.                        
                                                                                         
  -r, --record              Record output stream to a specified file.                    
                                                                                         
  -w, --warmup              (Default: 10000) Default normal, un-goo'd startup            
                            period (milliseconds) before starting goo                    
                                                                                         
  --help                    Display this help screen.                                    
                                                                                         
Press enter to exit      
```                                                                

Even better, just to make your life easier (we know you are too lazy to grab VS2015 Community and compile it), we auto-build this using AppVeyor - here is how we are doing right now: 

[![Build status](https://ci.appveyor.com/api/projects/status/00th33u0a4ie1cbv/branch/master?svg=true)](https://ci.appveyor.com/project/cinegy/streamgoo/branch/master)

We're just getting started, but if you want you can check out a compiled binary from the latest code here:

[AppVeyor StreamGoo Project Builder](https://ci.appveyor.com/project/cinegy/streamgoo)

