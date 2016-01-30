#Cinegy StreamGoo Tool

>Use this tool to break your RTP streams, and then see what damage you can cause.

##Why would I do this?

If you are working with RTP-wrapped transport streams, it's good to test things under less-than-ideal cirumstances. Sure, your stuff all works on a sunny day, but how will your system hold up when your satellite dish is being hit by some biblical weather? I'm sure your network never had a broadcast storm, or lost any packets - or if it did, you'd never admit it to your boss, but why not prepare for that day the dog eats a cable and causes some spanning-tree events?

##Is there anything else cool you are not telling me?

Why yes, you can also disable any Goo from getting squirted into your packets, and just use the tool to relay between networks. It's quick and dirty - but that is useful sometimes!

You can also record whatever passes through this tool to file - so if you manage to get a nice bit of corruption killing something, you can open up a beer and set up a script to kill it over and over again!

##How easy is it?

Well, we've added everything you need into a single teeny-tiny EXE again, which just depends on .NET 4. And then we gave it all a nice Apache license, so you can tinker and throw the tool wherever you need to on the planet.

Even better, just to make your life easier (we know you are too lazy to grab VS2015 Community and compile it), we auto-build this using AppVeyor - here is how we are doing right now: 

[![Build status](https://ci.appveyor.com/api/projects/status/00th33u0a4ie1cbv/branch/master?svg=true)](https://ci.appveyor.com/project/cinegy/streamgoo/branch/master)

We're just getting started, but if you want you can check out a compiled binary from the latest code here:

[AppVeyor StreamGoo Project Builder](https://ci.appveyor.com/project/cinegy/streamgoo)

