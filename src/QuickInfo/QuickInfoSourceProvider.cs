using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.OpenCommandLine
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("Cmd QuickInfo Source")]
    [Order(Before = "Default Quick Info Presenter")]
    [ContentType(CmdContentTypeDefinition.CmdContentType)]
    internal class QuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        [Import]
        internal ITextBufferFactoryService TextBufferFactoryService { get; set; }

        [Import]
        internal IClassifierAggregatorService AggregatorService = null;

        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new FontQuickInfo(this, textBuffer, AggregatorService);
        }
    }
}
