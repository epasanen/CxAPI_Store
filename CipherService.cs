using Microsoft.AspNetCore.DataProtection;
using System;
using System.Text;

namespace CxAPI_Store
{
    public class CipherService
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private const string unique = "Checkmarx.Api_Core";
        

        public CipherService()
        {
            _dataProtectionProvider = DataProtectionProvider.Create("CxAPI_Store");
        }

        public string Encrypt(string input)
        {
            var protector = _dataProtectionProvider.CreateProtector(unique);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(protector.Protect(input)));
        }

        public string Decrypt(string cipherText)
        {
            var protector = _dataProtectionProvider.CreateProtector(unique);
            string fromBase64 = Encoding.UTF8.GetString(Convert.FromBase64String(cipherText));
            return protector.Unprotect(fromBase64);
        }
    }
}
