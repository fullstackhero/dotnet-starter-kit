{{/*
Expand the name of the chart.
*/}}
{{- define "maxiar-dotnetstarterkit-webapi.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "maxiar-dotnetstarterkit-webapi.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{- define "maxiar-dotnetstarterkit-webapi-dataseed-files.fullname" -}}
{{- printf "%s-%s" (include "maxiar-dotnetstarterkit-webapi.fullname" .) "dataseed-files" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "maxiar-dotnetstarterkit-webapi.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "maxiar-dotnetstarterkit-webapi.labels" -}}
helm.sh/chart: {{ include "maxiar-dotnetstarterkit-webapi.chart" . }}
{{ include "maxiar-dotnetstarterkit-webapi.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
app.kubernetes.io/component: {{ .Values.appRoleKapsch }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "maxiar-dotnetstarterkit-webapi.selectorLabels" -}}
app.kubernetes.io/name: {{ include "maxiar-dotnetstarterkit-webapi.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Pod Annotations - for re-rollout in case configmaps or secretes changes
*/}}
{{- define "maxiar-dotnetstarterkit-webapi.podAnnotations" -}}
checksum/config: {{ include ("maxiar-dotnetstarterkit-webapi.config-map-contents") . | sha256sum }}
checksum/secret: {{ include ("maxiar-dotnetstarterkit-webapi.secret-contents") . | sha256sum }}
checksum/configdataseed: {{ include ("maxiar-dotnetstarterkit-webapi-dataseed-files.config-map-contents") . | sha256sum }}
kubectl.kubernetes.io/default-container: {{ .Chart.Name }}
{{- end }}

{{/*
Create the name of the service account to use
*/}}
{{- define "maxiar-dotnetstarterkit-webapi.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "maxiar-dotnetstarterkit-webapi.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{- define "maxiar-dotnetstarterkit-webapi.imagePullSecrets.name"}}
{{- printf "%s-%s" .Release.Name .Values.imagePullSecrets.name | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Generate certificates for identity server
*/}}
{{- define "maxiar-dotnetstarterkit-webapi.certificate-secret.name"}}
{{- printf "%s-%s" .Release.Name "secret-certs" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "maxiar-cache.redis.connectionString" -}}
{{- $nodeCount := .Values.cache.replicas | int }}
{{- $prefix := printf "%s-%s" .Release.Name .Values.services.cache.redis.name }}
  {{- range $index0 := until $nodeCount -}}
    {{- $index1 := $index0 | add1 -}}
{{ $prefix }}-{{ $index0 }}{{ if ne $index1 $nodeCount }},{{ end }}
  {{- end -}}
{{- end -}}

{{- define "maxiar-cache.redis.hostname" -}}
{{- printf "%s-%s" .Release.Name .Values.services.cache.redis.name | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "maxiar-cir.url" -}}
{{- printf "http://%s-%s" .Release.Name .Values.services.cir.name | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "maxiar-databaseinstance.name" -}}
{{- printf "%s-%s" .Release.Name .Values.services.sqlserver.name | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "maxiar-databaseinstance.postgres.name" -}}
{{- printf "%s-%s" .Release.Name .Values.services.postgres.name | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "maxiar-elastic.url" -}}
{{- printf "http://%s-%s:%s" .Release.Name .Values.services.elastic.name .Values.services.elastic.port | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "maxiar-listhandler.url" -}}
{{- printf "http://%s-%s" .Release.Name .Values.services.listhandler.name | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "maxiar-security.url" -}}
{{- printf "https://%s-%s" .Release.Name .Values.services.security.name | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "maxiar-servicebus.url" -}}
{{- printf "rabbitmq://%s-%s" .Release.Name .Values.services.servicebus.name | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "maxiar-vehiclemanager.url" -}}
{{- printf "http://%s-%s" .Release.Name .Values.services.vehiclemanager.name | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "maxiar-tracing.hostname" -}}
{{- printf "%s-%s" .Release.Name .Values.services.tracing.name | trunc 63 | trimSuffix "-" }}
{{- end }}

#Operian frontend
{{- define "maxiar-ingress-secret.name" -}}
{{- printf "%s-%s" .Release.Name .Values.ktcwebingress.name | trunc 63 | trimSuffix "-" }}
{{- end }}
