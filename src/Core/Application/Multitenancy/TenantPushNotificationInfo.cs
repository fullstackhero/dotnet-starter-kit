using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Application.Multitenancy;

public sealed record TenantPushNotificationInfo(string appId, string authKey, string baseUrl);