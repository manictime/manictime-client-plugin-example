using Finkit.ManicTime.Client.ManicTimeGrammar;
using Finkit.ManicTime.Shared.ManicTimeGrammar;

namespace TagPlugin.ExportTags;

public class ExportActivityFilter : ActivityFilter
{
    public ExportActivityFilter(MTGrammarWrapper queryParser) : base(queryParser) { }
}