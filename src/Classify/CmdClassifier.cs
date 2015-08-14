using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace MadsKristensen.OpenCommandLine
{
    public class CmdClassifier : IClassifier
    {
        public Dictionary<Regex, IClassificationType> _map;
        private IClassificationType _comment, _identifier;

        public CmdClassifier(IClassificationTypeRegistryService registry)
        {
            _comment = registry.GetClassificationType(PredefinedClassificationTypeNames.Comment);
            _identifier = registry.GetClassificationType(PredefinedClassificationTypeNames.SymbolDefinition);

            _map = new Dictionary<Regex, IClassificationType>
            {
                {CmdLanguage.StringRegex, registry.GetClassificationType(PredefinedClassificationTypeNames.String)},
                {CmdLanguage.KeywordRegex, registry.GetClassificationType(PredefinedClassificationTypeNames.Keyword)},
                {CmdLanguage.LabelRegex, registry.GetClassificationType(PredefinedClassificationTypeNames.SymbolReference)},
                {CmdLanguage.OperatorRegex, registry.GetClassificationType(PredefinedClassificationTypeNames.Operator)},
                {CmdLanguage.ParameterRegex, registry.GetClassificationType(PredefinedClassificationTypeNames.ExcludedCode)},
            };
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            IList<ClassificationSpan> list = new List<ClassificationSpan>();
            string text = span.GetText();

            // Comments
            Match commentMatch = CmdLanguage.CommentRegex.Match(text);
            if (commentMatch.Success)
            {
                var result = new SnapshotSpan(span.Snapshot, span.Start + commentMatch.Index, commentMatch.Length);
                list.Add(new ClassificationSpan(result, _comment));

                if (commentMatch.Index == 0)
                    return list;
            }

            // Strings, keywords, operators and parameters
            foreach (Regex regex in _map.Keys)
            {
                var classifier = _map[regex];
                var matches = regex.Matches(text);

                foreach (Match match in matches)
                {
                    var hitSpan = new Span(span.Start + match.Index, match.Length);

                    if (!list.Any(s => s.Span.IntersectsWith(hitSpan)))
                    {
                        var result = new SnapshotSpan(span.Snapshot, hitSpan);
                        list.Add(new ClassificationSpan(result, classifier));
                    }
                }
            }

            // Identifier
            foreach (Match match in CmdLanguage.IdentifierRegex.Matches(text))
            {
                var result = new SnapshotSpan(span.Snapshot, span.Start + match.Index, match.Length);
                list.Add(new ClassificationSpan(result, _identifier));
            }

            return list;
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged
        {
            add { }
            remove { }
        }
    }
}