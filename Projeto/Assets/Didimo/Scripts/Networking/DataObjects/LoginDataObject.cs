
namespace Didimo.Networking.DataObjects
{
    [System.Serializable]
    public class LoginDataObject : DataObject
    {
        public string email;
        public string password;

        public LoginDataObject()
        {
        }

        public LoginDataObject(string email, string password)
        {
            this.email = email;
            this.password = password;
        }
    }
}
