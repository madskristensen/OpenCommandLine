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
        public static Regex _rString = new Regex("\"([^\"]+)\"", RegexOptions.Compiled);
        public static Regex _rComment = new Regex(@"(^([\s]+)?(rem|::).+)|((?<=[\s]+)&(rem|::).+)", RegexOptions.Compiled);
        public static Regex _rIdentifier = new Regex("(?<=(goto|^):)([\\w]+)|%([^%\\s]+)%|\\bnul\\b|%~([fdpnxsatz]+\\d)", RegexOptions.Compiled);
        public static Regex _rOperator = new Regex(@"(&|&&|\|\||([012]?>>?)|<|!|=|^)", RegexOptions.Compiled);
        public static Regex _rParameter = new Regex("(?<=(\\s))(/|-?-)([\\w]+)", RegexOptions.Compiled);
        public static Regex _rKeyword = CmdKeywords.KeywordRegex;
        public Dictionary<Regex, IClassificationType> _map;
        private IClassificationType _comment, _identifier;

        public CmdClassifier(IClassificationTypeRegistryService registry)
        {
            _comment = registry.GetClassificationType(PredefinedClassificationTypeNames.Comment);
            _identifier = registry.GetClassificationType(PredefinedClassificationTypeNames.SymbolDefinition);

            _map = new Dictionary<Regex, IClassificationType>
            {
                {_rString, registry.GetClassificationType(PredefinedClassificationTypeNames.String)},
                {_rKeyword, registry.GetClassificationType(PredefinedClassificationTypeNames.Keyword)},
                {_rOperator, registry.GetClassificationType(PredefinedClassificationTypeNames.Operator)},
                {_rParameter, registry.GetClassificationType(PredefinedClassificationTypeNames.ExcludedCode)},
            };
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            IList<ClassificationSpan> list = new List<ClassificationSpan>();
            string text = span.GetText();

            // Comments
            Match commentMatch = _rComment.Match(text);
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
            foreach (Match match in _rIdentifier.Matches(text))
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