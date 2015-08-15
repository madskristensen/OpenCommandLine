using System;
using System.Collections;
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
        private static ImageSource _keywordGlyph, _environmentGlyph, _labelGlyph;
        private ITextStructureNavigator _textStructureNavigator;
        private IClassifier _classifier;
        private bool _disposed = false;

        public CmdCompletionSource(ITextBuffer buffer, IClassifierAggregatorService classifier, IGlyphService glyphService, ITextStructureNavigator textStructureNavigator)
        {
            _buffer = buffer;
            _classifier = classifier.GetClassifier(buffer);
            _keywordGlyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic);
            _environmentGlyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphAssembly, StandardGlyphItem.GlyphItemPublic);
            _labelGlyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphArrow, StandardGlyphItem.GlyphItemPublic);
            _textStructureNavigator = textStructureNavigator;
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (_disposed)
                return;

            ITextSnapshot snapshot = session.TextView.TextBuffer.CurrentSnapshot;
            SnapshotPoint? triggerPoint = session.GetTriggerPoint(snapshot);
            ClassificationSpan clsSpan;

            if (triggerPoint == null || !triggerPoint.HasValue || triggerPoint.Value.Position == 0 || !IsAllowed(triggerPoint.Value, out clsSpan))
                return;

            ITrackingSpan tracking = FindTokenSpanAtPosition(session);

            if (tracking == null)
                return;

            List<Completion> completions = new List<Completion>();

            if (clsSpan != null && clsSpan.ClassificationType.IsOfType(PredefinedClassificationTypeNames.SymbolDefinition))
            {
                AddVariableCompletions(snapshot, tracking, completions);
            }
            else if (!tracking.GetText(snapshot).Any(c => !char.IsLetter(c) && !char.IsWhiteSpace(c)))
            {
                AddKeywordCompletions(completions);
            }

            if (completions.Count > 0)
            {
                var ordered = completions.OrderBy(c => c.DisplayText);
                completionSets.Add(new CompletionSet("Cmd", "Cmd", tracking, ordered, Enumerable.Empty<Completion>()));
            }
        }

        private void AddVariableCompletions(ITextSnapshot snapshot, ITrackingSpan tracking, List<Completion> completions)
        {
            var envVars = Environment.GetEnvironmentVariables();

            foreach (DictionaryEntry variable in envVars)
            {
                string displayText = variable.Key.ToString();
                string description = Environment.GetEnvironmentVariable(displayText);
                completions.Add(new Completion(displayText, displayText, description, _environmentGlyph, "automationText"));
            }

            var doc = new SnapshotSpan(snapshot, 0, snapshot.Length);
            var idents = _classifier.GetClassificationSpans(doc).Where(g => g.ClassificationType.IsOfType(PredefinedClassificationTypeNames.SymbolDefinition));

            foreach (var ident in idents.Where(i => !i.Span.IntersectsWith(tracking.GetSpan(snapshot))))
            {
                string text = ident.Span.GetText().Trim();
                string displayText = text.Trim('%');

                if (text.StartsWith("%") && text.EndsWith("%") && !completions.Any(c => c.InsertionText == displayText))
                    completions.Add(new Completion(displayText, displayText, null, _keywordGlyph, "automationText"));
            }
        }

        private static void AddKeywordCompletions(List<Completion> completions)
        {
            foreach (string keyword in CmdLanguage.Keywords.Keys)
            {
                completions.Add(new Completion(keyword, keyword, CmdLanguage.Keywords[keyword], _keywordGlyph, keyword));
            }
        }

        private ITrackingSpan FindTokenSpanAtPosition(ICompletionSession session)
        {
            SnapshotPoint currentPoint = session.TextView.Caret.Position.BufferPosition - 1;
            TextExtent extent = _textStructureNavigator.GetExtentOfWord(currentPoint);

            var prev = _textStructureNavigator.GetSpanOfPreviousSibling(extent.Span);

            if (prev != null && !prev.Contains(extent.Span))
            {
                string text = prev.GetText();
                if (!string.IsNullOrEmpty(text) && text.Last() != '%' && !char.IsLetter(text[0]))
                    return null;

                if (text.First() != '%' && CmdLanguage.Keywords.ContainsKey(text.ToLowerInvariant()))
                {
                    text = text.ToLowerInvariant();
                    if (text != "if" && text != "not")
                        return null;
                }
            }

            return currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
        }

        private bool IsAllowed(SnapshotPoint triggerPoint, out ClassificationSpan classificationType)
        {
            var line = triggerPoint.GetContainingLine().Extent;
            var spans = _classifier.GetClassificationSpans(line).Where(c => c.Span.Contains(triggerPoint.Position - 1));
            classificationType = spans.LastOrDefault();

            if (spans.Any(c => c.ClassificationType.IsOfType(PredefinedClassificationTypeNames.SymbolDefinition)))
                return true;

            bool isComment = spans.Any(c => c.ClassificationType.IsOfType(PredefinedClassificationTypeNames.Comment));
            if (isComment) return false;

            bool isString = spans.Any(c => c.ClassificationType.IsOfType(PredefinedClassificationTypeNames.String));
            if (isString) return false;

            return true;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}