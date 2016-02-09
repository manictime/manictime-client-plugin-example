manictime-client-plugin-example
===============================

An example of how to create custom ManicTime client plugins. ManicTime client supports two different types of plugins, Tag source plugin and Timeline plugin.

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

1. Compile the project.
2. After you compile it, there should be a folder in repository root - installable-plugin
3. Go to ManicTime, Settings -> Advanced -> Open db folder
4. Copy folder installable-plugin/Plugin to database folder, so that in the database folder it looks like
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

1. Compile the project.
2. After you compile it, there should be a folder in repository root - installable-plugin
3. Go to ManicTime, Settings -> Advanced -> Open db folder
4. Copy folder installable-plugin/Plugin to database folder, so that in the database folder it looks like
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
