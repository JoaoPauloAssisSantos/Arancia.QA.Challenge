using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arancia.Test.API.Helpers
{
    public static class TextHelper
    {
        public static string GenerateLargeString(int length)
        {
            return new string('A', length);
        }

        public static string ScriptPayload => "<script>alert(1)</script>";
        public static string SqlInjectionPayload => "'; DROP TABLE bookings; --";
    }
}
