using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Archskipelagill;

internal static class MiscUtils {
    public static string ToTitleCase(this string str) {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());
    }
}
