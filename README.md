manictime-client-plugin-example
===============================

This is example of how to create custom ManicTime client plugins. ManicTime client supports two different types of plugins, Timeline plugin and Tag source plugin.

<b>Timeline plugin</b> provides data and functionality that is presented in ManicTime as a new Timeline.

<picture of timeline>

<b>Tag source plugin</b> provides custom tags for tagging functionality in Manictime. This plugin also supports custom user commands.

<picture of tag source>

How to run
----------
To run you need Visual studio 2013. 

How to use
----------
Open solution. Modify. Build. Run tests. 
After build, open bin\debug folder of plugin project (Timeline or Tag source). There should be folder with the name of plugin.
Copy folder with content into %userprofile%\AppData\Local\Finkit\ManicTime\Plugins\Packages\ and restart ManicTime client application.

Now let's configure example plugins in Manictime client

####For ManicTime timeline plugins:
1. Open ManicTime client application.
2. Open Timeline editor with click on a Gear button above current timelines.
3. Now click on Add timeline button
4. List of displayed timeline plugins should contain "Timeline plugin example"
5. Select it and complete the wizard
6. Timeline plugin example should now be added to current timelines

####For Tag source plugins:
1. Open ManicTime client application
2. Open TagEditor on Tags timeline by clicking on dropdown button
3. Select Tag source tab and click on Add
4. Select Tag source example from list and clikc OK
5. Complete the wizard
6. Try add new tag. Tags provided by Tag source plugin should now be available.
7. To execute custom command, click Puzzle button and then click on custom menu command in Tag source example menu item














 
