using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.OpenCommandLine
{
    [Export(typeof(IClassifierProvider))]
    [ContentType(CmdContentTypeDefinition.CmdContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class CmdClassifierProvider : IClassifierProvider
    {
        [Import]
        public IClassificationTypeRegistryService Registry { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty<CmdClassifier>(() => new CmdClassifier(Registry));
        }
    }
}