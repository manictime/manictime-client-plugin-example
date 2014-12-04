manictime-client-plugin-example
===============================

An example of how to create custom ManicTime client plugins. ManicTime client supports two different types of plugins, Timeline plugin and Tag source plugin.

###Timeline plugin
Timeline plugin can show data in ManicTime as a new timeline. Similar plugins shipped with ManicTime are Outlook calendar timeline, Google calendar timeline...

###Tag source plugin
Tag source plugin can import tags to ManicTime which you can then use for tagging. You can then also push created tags back to your system. This us useful if for example you have a system with project and tasks, into which you enter your work hours. You can present projects and tasks as tags in ManicTime, tag your work hours in ManicTime, then send these tags back to your system as work hours. Similar plugin shipped with ManicTime is Jira plugin.

How to run
----------
Open in Visual studio 2013 and run. Plugins need to be written for .Net 4.0 Client profile.

How to use
----------
 1. Open solution. 
 2. Build. 
 3. Run tests. 
 
 
Once it builds, open bin\debug folder of plugin project (Timeline or Tag source). There should be folder with the name of plugin.
Copy folder into %userprofile%\AppData\Local\Finkit\ManicTime\Plugins\Packages\ and restart ManicTime.

Using the plugins
-----------------

###For ManicTime timeline plugins:
 1. Open ManicTime
 2. Open Timeline editor (click on a Gear button above timelines)
 3. Click on Add timeline button
 4. Choose "Timeline plugin example"
 5. Select it and complete the wizard
 6. Timeline plugin example should now be added

###For Tag source plugins:
 1. Open ManicTime
 2. Open TagEditor on Tags timeline by clicking on dropdown button
 3. Select Tag source tab and click on Add
 4. Select Tag source example from list and click OK
 5. Complete the wizard
 6. Try adding a new tag. Tags provided by Tag source plugin should now be available.
 7. Custom commands for the plugin are available in Plugins menu (Puzzle icon next to the Settings). You can use this command to send data back to your system
 

 
