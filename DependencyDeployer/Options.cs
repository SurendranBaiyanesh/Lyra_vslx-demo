using System.ComponentModel;
using Community.VisualStudio.Toolkit;

namespace DependencyDeployer
{
    internal partial class OptionsProvider
    {
        [System.Runtime.InteropServices.ComVisible(true)]
        public class GeneralOptions : BaseOptionPage<General> { }
    }

    public class General : BaseOptionModel<General>
    {
        [Category("API")]
        [DisplayName("Base URL")]
        [Description("Dependency API root. A DLL is fetched from {BaseUrl}/dependencies/{name}/{version}.")]
        public string BaseUrl { get; set; } = "https://localhost:5001/api";

        [Category("API")]
        [DisplayName("Access token")]
        [Description("Optional bearer token sent with each request.")]
        [PasswordPropertyText(true)]
        public string AccessToken { get; set; } = "";

        [Category("Output")]
        [DisplayName("Target folder")]
        [Description("Folder, relative to the project directory, where DLLs are placed.")]
        public string TargetFolder { get; set; } = "lib";
    }
}
