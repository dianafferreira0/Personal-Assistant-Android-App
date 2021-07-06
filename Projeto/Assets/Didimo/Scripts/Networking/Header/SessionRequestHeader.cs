using System;
using System.Collections.Generic;
using UnityEngine;

namespace Didimo.Networking.Header
{
    public partial class SessionRequestHeader : SessionRequestHeaderBase
    {
        public SessionRequestHeader() : base()
        {
        }
    }

    public class SessionRequestHeaderBase : IRequestHeader
    {
        protected const string setCookieField = "SET-COOKIE";
        protected const string cookieField = "Cookie";
        protected const string playerPrefsCookieKey = "DIDIMO.COOKIE";

        string _sessionID = playerPrefsCookieKey;
        public string SessionID
        {
            get { return _sessionID; }
            set { _sessionID = playerPrefsCookieKey + "." + value; }
        }

        public string cookieValue
        {
            get
            {
                return PlayerPrefs.GetString(SessionID);
            }
            set
            {
                PlayerPrefs.SetString(SessionID, value);
            }
        }

        public bool HasCookie
        {
            get
            {
                return !string.IsNullOrEmpty(cookieValue);
            }
        }

        public void DeleteCookie()
        {

            PlayerPrefs.DeleteKey(SessionID);
        }

        protected Dictionary<string, string> header;

        public SessionRequestHeaderBase()
        {
            header = new Dictionary<string, string>();
        }

        public virtual void UpdateForResponseHeader(WWWForm form, Dictionary<string, string> header)
        {
            if (form == null || form.headers == null)
            {
                this.header = new Dictionary<string, string>();
            }
            else
            {
                this.header = form.headers;
            }

            if (header != null && header.ContainsKey(setCookieField))
            {
                cookieValue = header[setCookieField];
            }

            if (cookieValue != null)
            {
                this.header[cookieField] = cookieValue;
            }
        }

        public void UpdateForRequestUri(WWWForm form, Uri uri)
        {
            if (form == null || form.headers == null)
            {
                header = new Dictionary<string, string>();
            }
            else
            {
                header = form.headers;
            }

            if (cookieValue != null)
            {
                header[cookieField] = cookieValue;
            }

        }

        public Dictionary<string, string> GetHeader()
        {
            return header;
        }
    }
}