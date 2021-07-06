namespace Didimo.Networking //this class cannot be in a different namespace as it extends the ServicesRequestsConfigurationBase class on the Didimo.Networking namespace
{
    public partial class ServicesRequestsConfiguration : ServicesRequestsConfigurationBase
    {
        //User Account
        public string paginatedListUrl { get { return apiUrl + "didimo/list"; } }

        // Speech
        public string speechUrl { get { return apiUrl + "speech/tts/oggjson"; } }
    }
}