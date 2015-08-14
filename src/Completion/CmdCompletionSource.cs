using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Operations;

namespace MadsKristensen.OpenCommandLine
{
    class CmdCompletionSource : ICompletionSource
    {
        private ITextBuffer _buffer;
        private static ImageSource _glyph;
        private ITextStructureNavigator _textStructureNavigator;
        private IClassifier _classifier;
        private bool _disposed = false;

        public CmdCompletionSource(ITextBuffer buffer, IClassifierAggregatorService classifier, IGlyphService glyphService, ITextStructureNavigator textStructureNavigator)
        {
            _buffer = buffer;
            _classifier = classifier.GetClassifier(buffer);
            _glyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic);
            _textStructureNavigator = textStructureNavigator;
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (_disposed)
                return;

            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            SnapshotPoint? triggerPoint = session.GetTriggerPoint(snapshot);

            if (triggerPoint == null || !triggerPoint.HasValue || triggerPoint.Value.Position == 0 || !IsAllowed(triggerPoint.Value))
                return;

            List<Completion> completions = new List<Completion>();

            foreach (string keyword in CmdKeywords.Keywords.Keys)
            {
                completions.Add(new Completion(keyword, keyword, CmdKeywords.Keywords[keyword], _glyph, keyword));
            }

            ITrackingSpan tracking = FindTokenSpanAtPosition(session);

            if (tracking != null)
                completionSets.Add(new CompletionSet("Cmd", "Cmd", tracking, completions, Enumerable.Empty<Completion>()));
        }

        private ITrackingSpan FindTokenSpanAtPosition(ICompletionSession session)
        {
            SnapshotPoint currentPoint = (session.TextView.Caret.Position.BufferPosition) - 1;
            TextExtent extent = _textStructureNavigator.GetExtentOfWord(currentPoint);

            var prev = _textStructureNavigator.GetSpanOfPreviousSibling(extent.Span);

            if (prev != null && !prev.Contains(extent.Span))
            {
                string text = prev.GetText();
                if (!string.IsNullOrEmpty(text) && !char.IsLetter(text[0]))
                    return null;
            }

            return currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
        }

        private bool IsAllowed(SnapshotPoint triggerPoint)
        {
            var line = triggerPoint.GetContainingLine().Extent;
            var spans = _classifier.GetClassificationSpans(line);

            bool isComment = spans.Any(c => c.Span.Contains(triggerPoint.Position) && c.ClassificationType.IsOfType(PredefinedClassificationTypeNames.Comment));
            if (isComment) return false;

            bool isString = spans.Any(c => c.Span.Contains(triggerPoint.Position) && c.ClassificationType.IsOfType(PredefinedClassificationTypeNames.String));
            if (isString) return false;

            return true;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}