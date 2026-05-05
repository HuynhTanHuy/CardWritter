using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duali.Utils
{
    class TransparentResponse
    {
        public int ReaderResponseCode { get; set; }
        public int desfireResponseCode { get; set; }
        public String desfireResponseData;

        public TransparentResponse(int readerResponse, int desfireResponse, String desfireResponseData)
        {
            this.ReaderResponseCode = readerResponse;
            this.desfireResponseCode = desfireResponse;
            this.desfireResponseData = desfireResponseData;
        }

        public static bool VerifyResponse(TransparentResponse transparentResponse)
        {
            if (transparentResponse.ReaderResponseCode == ReaderResponse.DE_OK && transparentResponse.desfireResponseCode == DesfireResponse.OPERATION_OK)
            {
                return true;
            }

            return false;
        }
    }
}
