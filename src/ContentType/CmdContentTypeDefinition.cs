using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.OpenCommandLine
{
    class CmdContentTypeDefinition
    {
        public const string CmdContentType = "CMD_HAT";

        [Export(typeof(ContentTypeDefinition))]
        [Name(CmdContentType)]
        [BaseDefinition("text")]
        public ContentTypeDefinition ICmdContentType { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(CmdContentType)]
        [FileExtension(".cmd")]
        public FileExtensionToContentTypeDefinition CmdFileExtension { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(CmdContentType)]
        [FileExtension(".bat")]
        public FileExtensionToContentTypeDefinition BatFileExtension { get; set; }
    }
}
