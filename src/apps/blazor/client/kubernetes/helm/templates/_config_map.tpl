{{/* _config_map.tpl */}}
{{- define "maxiar-dotnetstarterkit-blazor.config-map-contents" -}}

Frontend_FSHStarterBlazorClient_Settings__AppSettingsTemplate: /usr/share/nginx/html/appsettings.json.TEMPLATE
Frontend_FSHStarterBlazorClient_Settings__AppSettingsJson: /usr/share/nginx/html/appsettings.json
#ASPNETCORE_ENVIRONMENT: "Development"
#ASPNETCORE_URLS: https://*:443;http://*:80
#ASPNETCORE_HTTPS_PORT: "443"
#ASPNETCORE_Kestrel__Certificates__Default__Password: password!
#ASPNETCORE_Kestrel__Certificates__Default__Path: /https/cert.pfx
FSHStarterBlazorClient_ApiBaseUrl:  https://dotnetstarterkitwebapi{{ tpl .Values.configmaps.frontend.domainNameCharSeparator . }}{{ tpl .Values.configmaps.frontend.ingressDomainName . }}

{{- end }}
