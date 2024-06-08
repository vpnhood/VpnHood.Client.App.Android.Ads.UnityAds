using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VpnHood.Client.App.Droid.Ads.VhUnityAds;

public class AdException : Exception
{
    public AdException(string message, Exception innerException) : base(message, innerException)
    {

    }

    public AdException(string message) : base(message)
    {
    }
}