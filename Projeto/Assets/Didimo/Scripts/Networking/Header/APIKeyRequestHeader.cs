using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Didimo.Networking.Header
{
    public class APIKeyRequestHeader : IRequestHeader
    {
        const string authorizationField = "Authorization";

        string authorizationValue = null;
        string hash1;
        HashAlgorithm hashAlgorithm;
        string apiKey;
        string secretKey;
        Dictionary<string, string> header;

        public APIKeyRequestHeader(string apiKey, string secretKey)
        {
            this.apiKey = apiKey;
            this.secretKey = secretKey;
            hashAlgorithm = SHA256.Create();
            hash1 = HashToString(hashAlgorithm.ComputeHash(Encoding.ASCII.GetBytes(apiKey + secretKey)));
        }

        public void UpdateForResponseHeader(WWWForm form, Dictionary<string, string> header)
        {

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

            string nonce = Guid.NewGuid().ToString();
            byte[] hash2Bytes = hashAlgorithm.ComputeHash(Encoding.ASCII.GetBytes(apiKey + nonce + uri.LocalPath));
            string hash2 = HashToString(hash2Bytes);

            using (HMAC h = new HMACSHA256(Encoding.ASCII.GetBytes(secretKey)))
            {
                byte[] authDigest = h.ComputeHash(Encoding.ASCII.GetBytes(hash1 + hash2));

                authorizationValue = "DidimoDigest auth_method = sha256, realm = Avatar, auth_key = " + apiKey + ", auth_nonce = " + nonce + ", auth_digest = " + HashToString(authDigest);

                header[authorizationField] = authorizationValue;
            }
        }

        public Dictionary<string, string> GetHeader()
        {
            return header;
        }

        private string HashToString(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}