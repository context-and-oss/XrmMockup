using XrmPluginCore.Enums;
using XrmPluginCore.Interfaces.Plugin;

namespace DG.Tools.XrmMockup
{
    internal class ImageSpecification : IImageSpecification
    {
        public string Attributes { get; set; }

        public string EntityAlias { get; set; }

        public string ImageName { get; set; }

        public ImageType ImageType { get; set; }
    }
}
