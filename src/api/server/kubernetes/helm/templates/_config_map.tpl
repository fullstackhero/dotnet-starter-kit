{{/* _config_map.tpl */}}
{{- define "maxiar-dotnetstarterkit-webapi.config-map-contents" -}}
DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE: {{ .Values.configmaps.hostBuilder.reloadConfigOnChange | quote }}
ASPNETCORE_ENVIRONMENT: "Development" #To enabled the /health endpoint for k8s
ASPNETCORE_URLS: http://*:80
ASPNETCORE_HTTPS_PORT: "80"
OTEL_EXPORTER_OTLP_ENDPOINT: "http://lgtm-alloy:4317"
#ASPNETCORE_Kestrel__Certificates__Default__Password: password!
#ASPNETCORE_Kestrel__Certificates__Default__Path: /https/cert.pfx
Host__Health__CheckTime: "30"
Host__Health__UnregisterTime: "90"
DatabaseOptions__ConnectionString: Server={{ include "maxiar-databaseinstance.postgres.name" . }};Port=5432;Database=fullstackhero;User Id={{ .Values.configmaps.database.postgres.credentials.userName }};Password={{ .Values.configmaps.database.postgres.credentials.password }};
DatabaseOptions__Provider: postgresql
JwtOptions__Key: QsJbczCNysv/5SGh+U7sxedX8C07TPQPBdsnSDKZ/aE=
HangfireOptions__Username: admin
HangfireOptions__Password: Secure1234!Me
MailOptions__From: mukesh@fullstackhero.net
MailOptions__Host: smtp.ethereal.email
MailOptions__Port: "587"
MailOptions__UserName: sherman.oconnell47@ethereal.email
MailOptions__Password: KbuTCFv4J6Fy7256vh
MailOptions__DisplayName: Mukesh Murugan
CorsOptions__AllowedOrigins__0: http://localhost:5010
CorsOptions__AllowedOrigins__1: http://localhost:7100
CorsOptions__AllowedOrigins__2: {{ .Values.configmaps.corsOptions.allowedOrigins_2 }}
CorsOptions__AllowedOrigins__3: {{ .Values.configmaps.corsOptions.allowedOrigins_3 }}
OpenTelemetryOptions__Endpoint: http://otel-collector:4317
RateLimitOptions__EnableRateLimiting: "false"
{{- end -}}
