set plugin_folder=TimelinePlugins.Example
set lib_folder=%plugin_folder%\Lib
rd %plugin_folder% /s /q
md %plugin_folder%
md %lib_folder%

copy PluginIcon.png %plugin_folder%
copy PluginSpec.json  %plugin_folder%
copy Readme.txt %plugin_folder%
copy TimelinePlugins.Example.dll %lib_folder%
