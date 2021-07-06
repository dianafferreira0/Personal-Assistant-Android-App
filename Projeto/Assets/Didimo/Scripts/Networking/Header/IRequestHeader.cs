using System;
using System.Collections.Generic;
using UnityEngine;

namespace Didimo.Networking.Header
{
    public interface IRequestHeader
    {
        void UpdateForResponseHeader(WWWForm form, Dictionary<string, string> header);
        void UpdateForRequestUri(WWWForm form, Uri uri);

        Dictionary<string, string> GetHeader();
    }
}