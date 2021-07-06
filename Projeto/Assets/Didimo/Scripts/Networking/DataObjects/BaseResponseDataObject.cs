
using System.Collections.Generic;

namespace Didimo.Networking.DataObjects
{
    public class BaseResponseDataObject : DataObject
    {
        public string stt;
        public string msg;
        public List<string> errors;
        public string support_id;

        public bool IsSuccess
        {
            get
            {
                return stt == null || stt.Equals("OK");
            }
        }
    }
}