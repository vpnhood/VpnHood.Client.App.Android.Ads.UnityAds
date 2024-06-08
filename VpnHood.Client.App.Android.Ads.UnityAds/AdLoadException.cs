using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VpnHood.Client.App.Droid.Ads.VhUnityAds;

public class AdLoadException : Exception
{
    public AdLoadException(string message) : base(message)
    {
    }

    public AdLoadException(string message, Exception innerException) : base(message, innerException)
    {

    }
}