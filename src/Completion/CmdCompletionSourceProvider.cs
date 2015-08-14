using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.OpenCommandLine
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType(CmdContentTypeDefinition.CmdContentType)]
    [Name("CmdCompletion")]
    class CmdCompletionSourceProvider : ICompletionSourceProvider
    {
        [Import]
        internal IClassifierAggregatorService AggregatorService = null;

        [Import]
        internal IGlyphService GlyphService = null;

        [Import]
        public ITextStructureNavigatorSelectorService TextStructureNavigatorSelector = null;

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            ITextStructureNavigator textStructureNavigator = TextStructureNavigatorSelector.GetTextStructureNavigator(textBuffer);

            return new CmdCompletionSource(textBuffer, AggregatorService, GlyphService, textStructureNavigator);
        }
    }
}