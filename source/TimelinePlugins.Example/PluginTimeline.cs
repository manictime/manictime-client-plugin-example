using System;
using Finkit.ManicTime.Plugins.Timelines;

namespace TimelinePlugins.Example
{
    
    [Serializable]
    public class PluginUserData
    {
        public string SourceUrl { get; set; }
        public string Response { get; set; }
    }

    public class PluginTimeline : Timeline
    {

        private TextDataSerializer<PluginUserData> _textDataSerializer;

        private TextDataSerializer<PluginUserData> TextDataSerializer
        {
            get { return _textDataSerializer ?? (_textDataSerializer = new TextDataSerializer<PluginUserData>()); }
        }


        public override string TextData
        {
            get { return TextDataSerializer.TextData; }
            set { TextDataSerializer.TextData = value; }
        }

        public string SourceUrl
        {
            get { return TextDataSerializer.Data.SourceUrl; }
            set
            {
                TextDataSerializer.SetValue(() => TextDataSerializer.Data.SourceUrl == value,
                    () => TextDataSerializer.Data.SourceUrl = value);
            }
        }

        public string Response
        {
            get { return TextDataSerializer.Data.Response; }
            set
            {
                TextDataSerializer.SetValue(() => TextDataSerializer.Data.Response == value,
                    () => TextDataSerializer.Data.Response = value);
            }
        }



        public PluginTimeline(TimelineType timelineType, string typeName, string genericTypeName,
            Func<Timeline, string> getDefaultDisplayName)
            : base(timelineType, typeName, genericTypeName, getDefaultDisplayName)
        {

        }
    }
}
