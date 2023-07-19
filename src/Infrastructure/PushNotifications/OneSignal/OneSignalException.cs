using FSH.WebApi.Application.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Infrastructure.PushNotifications.OneSignal;

// Thrown when request is succesfully done, but onesignal returns an error like "invalid player id"
public class OneSignalException : CustomException
{
    public OneSignalException(string message, List<string>? errors)
        : base(message, errors, HttpStatusCode.BadRequest)
    {
    }
}