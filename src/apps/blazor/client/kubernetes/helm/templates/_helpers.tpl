{{/*
Expand the name of the chart.
*/}}
{{- define "maxiar-dotnetstarterkit-blazor.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "maxiar-dotnetstarterkit-blazor.fullname" -}}
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

{{- define "maxiar-dotnetstarterkit-blazor-logging-files.fullname" -}}
{{- printf "%s-%s" ( include "maxiar-dotnetstarterkit-blazor.fullname" . ) "logging-files" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "maxiar-dotnetstarterkit-blazor.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "maxiar-dotnetstarterkit-blazor.labels" -}}
helm.sh/chart: {{ include "maxiar-dotnetstarterkit-blazor.chart" . }}
{{ include "maxiar-dotnetstarterkit-blazor.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
app.kubernetes.io/component: {{ .Values.appRoleKapsch }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "maxiar-dotnetstarterkit-blazor.selectorLabels" -}}
app.kubernetes.io/name: {{ include "maxiar-dotnetstarterkit-blazor.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Pod Annotations - for re-rollout in case configmaps or secretes changes
*/}}
{{- define "maxiar-dotnetstarterkit-blazor.podAnnotations" -}}
checksum/config: {{ include ("maxiar-dotnetstarterkit-blazor.config-map-contents") . | sha256sum }}
checksum/secret: {{ include ("maxiar-dotnetstarterkit-blazor.secret-contents") . | sha256sum }}
{{- end }}

{{/*
Create the name of the service account to use
*/}}
{{- define "maxiar-dotnetstarterkit-blazor.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "maxiar-dotnetstarterkit-blazor.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{- define "maxiar-dotnetstarterkit-blazor.imagePullSecrets.name"}}
{{- printf "%s-%s" .Release.Name .Values.imagePullSecrets.name | trunc 63 | trimSuffix "-" }}
{{- end }}

#Operian frontend
{{- define "kapsch-ingress-secret.name" -}}
{{- printf "%s-%s" .Release.Name .Values.ktcwebingress.name | trunc 63 | trimSuffix "-" }}
{{- end }}


#Logging helpers
{{- define "kapsch-elastic.name" -}}
{{- printf "%s-%s" .Release.Name "elastic-service-master" | trunc 63 | trimSuffix "-" }}
{{- end }}