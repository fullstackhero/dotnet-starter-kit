{{/* _config_map.tpl */}}
{{- define "maxiar-dotnetstarterkit-webapi.secret-contents" -}}
DotnetStarterKit_Security__ClientSecret: {{ .Values.secrets.security.clientCredentials.secret | b64enc }}
DotnetStarterKit_Security__ClientCertificate__Password: {{ .Values.secrets.security.clientCertificate.password | b64enc }}
DotnetStarterKit_ServiceBus__Credentials__UserName: {{ .Values.secrets.serviceBus.credentials.userName | b64enc }}
DotnetStarterKit_ServiceBus__Credentials__Password: {{ .Values.secrets.serviceBus.credentials.password | b64enc }}
{{- end -}}