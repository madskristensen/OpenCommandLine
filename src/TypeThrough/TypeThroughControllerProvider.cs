using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MadsKristensen.OpenCommandLine
{
    [Export(typeof(IIntellisenseControllerProvider))]
    [ContentType(CmdContentTypeDefinition.CmdContentType)]
    [Name("CMD Type Through Completion Controller")]
    [Order(Before = "Default Completion Controller")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class CmdTypeThroughControllerProvider : IIntellisenseControllerProvider
    {
        public IIntellisenseController TryCreateIntellisenseController(ITextView view, IList<ITextBuffer> subjectBuffers)
        {
            return new CmdTypeThroughController(view, subjectBuffers);
        }
    }

    internal class CmdTypeThroughController : TypeThroughController
    {
        public CmdTypeThroughController(ITextView textView, IList<ITextBuffer> subjectBuffers)
            : base(textView, subjectBuffers)
        {
        }

        protected override bool CanComplete(ITextBuffer textBuffer, int position)
        {
                var line = textBuffer.CurrentSnapshot.GetLineFromPosition(position);
                return line.Start.Position + line.GetText().TrimEnd('\r', '\n', ' ', ';', ',').Length == position + 1;
        }

        protected override char GetCompletionCharacter(char typedCharacter)
        {
            switch (typedCharacter)
            {
            case '[':
                return ']';

            case '(':
                return ')';

            case '{':
                return '}';

            case '%':
                return '%';

            case '"':
                return '"';

            case '\'':
                return '\'';
            }

            return '\0';
        }
    }
}
