Client plugins for ManicTime
===============================

An example of how to create custom ManicTime client plugins. ManicTime client supports three different types of plugins: 
- Tag source plugin
- Timeline plugin
- Tracker plugin

Tag Plugin
====================

Within ManicTime a tag plugin can be used to:
- Get tags to ManicTime from an outside service
- Export created tag activities (tags with start and end time) to an outside service

There are two important files:
ImportTags/TagsImporter.cs
Here you can get your tags from any service and pass them to ManicTime as a collection of TagSourceItem objects.

ExportTags/TagsExporter.cs
This will return a collection of TagActivity objects which you can then export to other services.


How to use
----------

1. Compile the project (source/tracker-plugin).
2. After you compile it, there should be a folder in repository root - installable-plugin/< BuildConfiguration >
3. Go to ManicTime, Settings -> Advanced -> Open db folder
4. Copy folder installable-plugin/< BuildConfiguration >/Plugin to database folder, so that in the database folder it looks like
....\db folder\Plugins\Packages\ManicTime.TagSource.SampleTagPlugin\...
5. Run ManicTime

If you go to Plugins, you should now see a new plugin

![Find Tag plugin](http://manictimecdn.blob.core.windows.net/images/github/tag-plugin-installed.png)

Click on Settings, to add new connection

![Add new connection](http://manictimecdn.blob.core.windows.net/images/github/tag-plugin-settings.png)

Then go to Add tag, you should see some new tags

![Create a tag](http://manictimecdn.blob.core.windows.net/images/github/tag-plugin-imported-tags.png)

Make some tags, then export them

![Export tags](http://manictimecdn.blob.core.windows.net/images/github/tag-plugin-export-tags.png)


Timeline plugin
===============

Timeline plugin can show data in ManicTime as a new timeline. Similar plugins shipped with ManicTime are Outlook calendar timeline, Google calendar timeline...

How to use
----------

1. Compile the project (source/timeline-plugin).
2. After you compile it, there should be a folder in repository root - installable-plugin/< BuildConfiguration >
3. Go to ManicTime, Settings -> Advanced -> Open db folder
4. Copy folder installable-plugin/< BuildConfiguration >/Plugin to database folder, so that in the database folder it looks like
....\db folder\Plugins\Packages\ManicTime.TagSource.SampleTagPlugin\...
5. Run ManicTime

###For ManicTime timeline plugins:
 1. Open ManicTime
 2. Open Timeline editor (click on a Gear button above timelines)
 3. Click on Add timeline button
 
   ![Add timeline](http://manictimecdn.blob.core.windows.net/images/github/timeline-plugin-add.png)

 4. Choose "Timeline plugin example"
 5. Select it and complete the wizard

   ![Timeline settings](http://manictimecdn.blob.core.windows.net/images/github/timeline-plugin-settings.png)

 6. Timeline plugin example should now be added

   ![Timeline on day view](http://manictimecdn.blob.core.windows.net/images/github/timeline-plugin-day-view.png)


Tracker plugin
===============

Tracker plugin fills Documents timeline. When ManicTime detects an application, it gets the general data like process name, window title... Some applications also contain other data like URLs, open files, email sender... To get this extra data, ManicTime relies on plugins. When an application is detected, ManicTime will go through a list of plugins and try to get more data. We provide plugins for popular applications, like MS Office products and browsers, but you can write the plugin for any app you use.

In the sample we included two plugins, Notepad plugin and Outlook plugin. We suggest you take a look at Notepad plugin, it is simpler to understand. The important code is in file NotepadFileRetreiver.cs. 
You don't need to look at PluginTester project, its there to help you debug your plugin. If you will create your own plugin (not modify an existing one), add it to the list of plugins in file TrackActiveApplication.cs.

How to use
----------

1. Compile the project (source/tracker-plugin).
2. After you compile it, there should be a folder in repository root - installable-plugin/< BuildConfiguration >
3. Go to ManicTime, Settings -> Advanced -> Open db folder
4. Copy folder installable-plugin/< BuildConfiguration >/Plugin to database folder, so that in the database folder it looks like
....\db folder\Plugins\Packages\ManicTime.DocumentTracker.Notepad\...
5. Run ManicTime

If it works ok, data should be visible on [Documents timeline](http://support.manictime.com/knowledgebase/articles/686226-document-timeline), when you use the application you wrote the plugin for.



Debugging in ManicTime
===============

Client plugins (Tag plugin and Timeline plugin) can be difficult to troubleshoot or simply irritating to test manually.

One thing you can do, is attach debugger to ManicTimeClient.exe process.

To do this:
1. Switch to Debug build.
2. Open plugin project properties.
3. Under Build property, "Output" section: edit "Output path:" relative to ManicTime's database folder. e.g. [ManicTime Database folder]\Plugins\Packages\ < PackageName >\Lib\
3. Under Debug property, "Start action" section: pick "Start external program:" and set it to ManicTimeClient.exe executable (ManicTime.exe in case of TrackerPlugin). e.g. C:\Program Files (x86)\ManicTime\ManicTimeClient.exe
4. Make sure ManicTime Client settings option "Settings -> General -> Keep user interface running when main window closes" is unchecked.
5. Make sure ManicTime Client is not running.
6. Press Start (F5) to start debugging

Starting project in Debug mode should open instance of ManicTime Client with debugger attached. 
Any breakpoints set in project should now be hit as the plugin code executes.


